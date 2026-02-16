using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Warehouse.Grid;
using Warehouse.Pathfinding;
using Warehouse.Core;
using UnityEngine.Tilemaps;

namespace Warehouse.Units
{
    public enum AGVState { Idle, MovingToPickup, Loading, MovingToDelivery, Unloading, MovingToWaiting } 

    [RequireComponent(typeof(LineRenderer))]
    [RequireComponent(typeof(MeshRenderer))]
    public class AGVController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float _moveSpeed = 2.0f;

        [Header("Visual Settings")]
        [SerializeField] private bool _enableStateColors = true;

        private LineRenderer _lineRenderer;
        private MeshRenderer _meshRenderer;
        private Material _materialInstance;

        // Barvy podle stavu
        private readonly Color _colorIdle = new Color(0.5f, 0.5f, 0.5f);
        private readonly Color _colorMovingToPickup = new Color(1f, 0.84f, 0f);
        private readonly Color _colorLoading = new Color(0f, 0.5f, 1f);
        private readonly Color _colorMovingToDelivery = new Color(0f, 1f, 0f);
        private readonly Color _colorUnloading = new Color(1f, 0f, 0f);
        private readonly Color _colorMovingToWaiting = new Color(1f, 0.5f, 0f);

        private System.Action _onDestinationReached; 
        private GridNode _finalDestinationNode; // Pamatujeme si, kam chceme dojet

        public GridNode CurrentNode { get; private set; }
        public bool IsMoving { get; private set; }
        
        private AGVState _state = AGVState.Idle;
        public AGVState State 
        { 
            get => _state;
            private set 
            {
                if (_state != value)
                {
                    _state = value;
                    UpdateVisualState();
                }
            }
        }
        
        private Order _currentOrder; 
       
        private void Awake()
        {
            _lineRenderer = GetComponent<LineRenderer>();
            _lineRenderer.positionCount = 0;
            _lineRenderer.startWidth = 0.1f;
            _lineRenderer.endWidth = 0.1f;
            _lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            _lineRenderer.startColor = new Color(1f, 0.84f, 0f, 0.7f);
            _lineRenderer.endColor = new Color(1f, 0.84f, 0f, 0.7f);
            _lineRenderer.sortingOrder = 1;
            
            _meshRenderer = GetComponent<MeshRenderer>();
            if (_meshRenderer != null && _meshRenderer.material != null)
            {
                _materialInstance = new Material(_meshRenderer.material);
                _meshRenderer.material = _materialInstance;
            }
            UpdateVisualState();
        }

        public void Initialize(GridNode startNode)
        {
            CurrentNode = startNode;
            Vector3 startPos = startNode.WorldPosition;
            startPos.y = 0.5f;
            transform.position = startPos;
        }

        public void SetDestination(GridNode targetNode, System.Action onReached = null)
        {
            _onDestinationReached = onReached;
            _finalDestinationNode = targetNode; // Uložíme si cíl pro případné opakování
            
            PathAlgorithm algo = PathAlgorithm.AStar;
            if (Managers.SimulationManager.Instance != null)
            {
                algo = Managers.SimulationManager.Instance.CurrentAlgorithm;
            }

            List<GridNode> path = Pathfinder.FindPath(CurrentNode, targetNode, algo);

            if (path != null && path.Count > 0)
            {
                StopAllCoroutines();
                StartCoroutine(FollowPath(path));
            } 
            else
            {
                // DŮLEŽITÁ OPRAVA: Pokud cesta není, nevzdáváme to! Zkusíme to za chvíli znovu.
                Debug.LogWarning($"AGV {name}: Cesta k cíli nenalezena! Zkusím to znovu za 1s.");
                StopAllCoroutines(); // Zastavíme pohyb, pokud nějaký byl
                StartCoroutine(RetryDestination(1.0f)); 
            }
        }

        // Coroutina pro opakování pokusu o nalezení cesty
        private IEnumerator RetryDestination(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (_finalDestinationNode != null)
            {
                SetDestination(_finalDestinationNode, _onDestinationReached);
            }
        }

        private void UpdatePathVisual(List<GridNode> remainingPath)
        {
            if (_lineRenderer == null) return;

            if (remainingPath == null || remainingPath.Count == 0)
            {
                _lineRenderer.positionCount = 0;
                _lineRenderer.enabled = false;
                return;
            }

            _lineRenderer.enabled = true;
            _lineRenderer.positionCount = remainingPath.Count + 1;
            _lineRenderer.SetPosition(0, transform.position);

            for (int i = 0; i < remainingPath.Count; i++)
            {
                Vector3 pos = remainingPath[i].WorldPosition;
                pos.y = 0.5f;
                _lineRenderer.SetPosition(i + 1, pos);
            }
        }

        private IEnumerator FollowPath(List<GridNode> path)
        {
            IsMoving = true;
            List<GridNode> activePath = new List<GridNode>(path);
            UpdatePathVisual(activePath);

            // Rezervace startu
            if (CurrentNode.OccupiedBy == null) CurrentNode.OccupiedBy = this;

            while (activePath.Count > 0)
            {
                GridNode targetNode = activePath[0];

                // --- KOLIZNÍ LOGIKA ---
                if (!targetNode.IsAvailable(this))
                {
                    if (Managers.StatsManager.Instance != null)
                        Managers.StatsManager.Instance.RegisterCollision();
                    
                    float waitTimer = 0f;
                    // Náhodná složka čekání, aby se neodblokovali všichni naráz (0-0.5s navíc)
                    float maxWaitTime = 2.0f + Random.Range(0f, 0.5f); 

                    if (_currentOrder != null)
                    {
                        _currentOrder.CollisionCount++;
                    }
                    while (!targetNode.IsAvailable(this))
                    {
                        waitTimer += Time.deltaTime;

                        // REROUTE
                        if (waitTimer > maxWaitTime)
                        {
                            Debug.Log($"AGV {name}: Zablokován na [{CurrentNode.GridX},{CurrentNode.GridY}]. Přepočítávám...");
                            IsMoving = false;
                            
                            // Zkusíme najít cestu znova do PŮVODNÍHO cíle (_finalDestinationNode)
                            // Místo toho, abychom jeli jen na konec aktuální path, která může být zastaralá
                            if (_finalDestinationNode != null)
                            {
                                SetDestination(_finalDestinationNode, _onDestinationReached);
                            }
                            yield break; // Ukončíme TUTO coroutinu, SetDestination spustí novou
                        }
                        yield return null;
                    }
                }

                // Rezervace
                targetNode.OccupiedBy = this;
                
                Vector3 startPos = transform.position;
                Vector3 endPos = targetNode.WorldPosition;
                endPos.y = 0.5f;

                transform.LookAt(endPos);

                float journeyLength = Vector3.Distance(startPos, endPos);
                float startTime = Time.time;
                float distanceTraveled = Vector3.Distance(CurrentNode.WorldPosition, targetNode.WorldPosition);

                while (Vector3.Distance(transform.position, endPos) > 0.01f)
                {
                    float distCovered = (Time.time - startTime) * _moveSpeed;
                    float fractionOfJourney = distCovered / journeyLength;
                    transform.position = Vector3.Lerp(startPos, endPos, fractionOfJourney);
                    
                    if (_lineRenderer.positionCount > 0) 
                        _lineRenderer.SetPosition(0, transform.position);

                    yield return null;
                }

                transform.position = endPos;
                
                if (Managers.StatsManager.Instance != null)
                {
                    Managers.StatsManager.Instance.AddDistance(distanceTraveled);
                    Managers.StatsManager.Instance.AddMovingTime(distanceTraveled / _moveSpeed);
                }

                if (_currentOrder != null)
                {
                    _currentOrder.RealDistance += distanceTraveled;
                }

                // Uvolnění starého nodu
                if (CurrentNode != targetNode && CurrentNode.OccupiedBy == this)
                {
                    CurrentNode.OccupiedBy = null;
                }

                targetNode.AddVisit();
                CurrentNode = targetNode;
                activePath.RemoveAt(0);
                UpdatePathVisual(activePath);
            }

            IsMoving = false;
            _lineRenderer.positionCount = 0;
            _lineRenderer.enabled = false;
            
            _onDestinationReached?.Invoke();
        }

        private void UpdateVisualState()
        {
            if (!_enableStateColors || _materialInstance == null) return;

            Color targetColor = _state switch
            {
                AGVState.Idle => _colorIdle,
                AGVState.MovingToPickup => _colorMovingToPickup,
                AGVState.Loading => _colorLoading,
                AGVState.MovingToDelivery => _colorMovingToDelivery,
                AGVState.Unloading => _colorUnloading,
                AGVState.MovingToWaiting => _colorMovingToWaiting,
                _ => _colorIdle
            };

            _materialInstance.color = targetColor;
        }

        public void AssignOrder(Order order)
        {
            _currentOrder = order;
            _currentOrder.Status = OrderStatus.Assigned; 

            State = AGVState.MovingToPickup;
            SetDestination(order.PickupNode, OnPickupReached);
        }

        private void OnPickupReached()
        {
            Debug.Log("Jsem na místě vyzvednutí. Nakládám...");
            State = AGVState.Loading;

            StartCoroutine(SimulateTaskDuration(2.0f, () => {
                State = AGVState.MovingToDelivery;
                _currentOrder.Status = OrderStatus.PickedUp;
                SetDestination(_currentOrder.DeliveryNode, OnDeliveryReached);
            }));
        }

        private void OnDeliveryReached()
        {
            Debug.Log("Jsem v cíli. Vykládám...");
            State = AGVState.Unloading;

            StartCoroutine(SimulateTaskDuration(2.0f, () => {
                Managers.OrderManager.Instance.CompleteOrder(_currentOrder);
                StartCoroutine(ShowCompletionEffect());
                _currentOrder = null;
                GoToWaitingArea();
            }));
        }

        private IEnumerator ShowCompletionEffect()
        {
            if (_materialInstance == null) yield break;
            Color originalColor = _materialInstance.color;
            Color successColor = Color.green;
            float duration = 0.5f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.PingPong(elapsed * 4f, 1f); 
                _materialInstance.color = Color.Lerp(originalColor, successColor, t);
                yield return null;
            }
            UpdateVisualState();
        }

        private void GoToWaitingArea()
        {
            var parkingSpots = Managers.GridManager.Instance.GetNodesByType(TileType.WaitingArea);
            GridNode bestSpot = null;
            float minDistance = float.MaxValue;

            foreach (var spot in parkingSpots)
            {
                if (spot.IsAvailable(this))
                {
                    float dist = Vector3.Distance(transform.position, spot.WorldPosition);
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        bestSpot = spot;
                    }
                }
            }

            if (bestSpot != null)
            {
                State = AGVState.MovingToWaiting;
                SetDestination(bestSpot, () => {
                    State = AGVState.Idle; 
                });
            }
            else
            {
                Debug.LogWarning("Žádné volné parkoviště! Zůstávám stát.");
                State = AGVState.Idle;
            }
        }

        private IEnumerator SimulateTaskDuration(float duration, System.Action onComplete)
        {
            yield return new WaitForSeconds(duration);
            onComplete?.Invoke();
        }

        private void OnDestroy()
        {
            if (CurrentNode != null && CurrentNode.OccupiedBy == this)
            {
                CurrentNode.OccupiedBy = null;
            }
            if (Managers.AgvManager.Instance != null)
            {
                Managers.AgvManager.Instance.UnregisterAgv(this);
            }
            if (_materialInstance != null)
            {
                Destroy(_materialInstance);
            }
        }
    }
}