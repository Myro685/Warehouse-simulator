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
        private Material _materialInstance; // Instance materiálu pro tento vozík

        // Barvy podle stavu
        private readonly Color _colorIdle = new Color(0.5f, 0.5f, 0.5f); // Šedá
        private readonly Color _colorMovingToPickup = new Color(1f, 0.84f, 0f); // Žlutá
        private readonly Color _colorLoading = new Color(0f, 0.5f, 1f); // Modrá
        private readonly Color _colorMovingToDelivery = new Color(0f, 1f, 0f); // Zelená
        private readonly Color _colorUnloading = new Color(1f, 0f, 0f); // Červená
        private readonly Color _colorMovingToWaiting = new Color(1f, 0.5f, 0f); // Oranžová

        private System.Action _onDestinationReached; 

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
            
            // Nastavení LineRendereru pro zobrazení cesty
            _lineRenderer.startWidth = 0.1f;
            _lineRenderer.endWidth = 0.1f;
            _lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            _lineRenderer.startColor = new Color(1f, 0.84f, 0f, 0.7f); // Žlutá, poloprůhledná
            _lineRenderer.endColor = new Color(1f, 0.84f, 0f, 0.7f); // Žlutá, poloprůhledná
            _lineRenderer.sortingOrder = 1;
            
            _meshRenderer = GetComponent<MeshRenderer>();
            
            // Vytvoříme instanci materiálu pro tento vozík, abychom mohli měnit barvu bez ovlivnění ostatních
            if (_meshRenderer != null && _meshRenderer.material != null)
            {
                _materialInstance = new Material(_meshRenderer.material);
                _meshRenderer.material = _materialInstance;
            }
            
            // Nastavíme počáteční barvu
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
            
            // Pokud SimulationManager ještě neexistuje (např. při testech), použijeme default A*
            PathAlgorithm algo = PathAlgorithm.AStar;
            if (Managers.SimulationManager.Instance != null)
            {
                algo = Managers.SimulationManager.Instance.CurrentAlgorithm;
            }

            // Předáme algoritmus do Pathfindera
            List<GridNode> path = Pathfinder.FindPath(CurrentNode, targetNode, algo);

            if (path != null && path.Count > 0)
            {
                StopAllCoroutines();
                StartCoroutine(FollowPath(path));
            } else
            {
                Debug.LogWarning("Cesta je nulová nebo prázdná.");
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

            // Zobrazíme LineRenderer pouze pokud je cesta
            _lineRenderer.enabled = true;
            _lineRenderer.positionCount = remainingPath.Count + 1;
            _lineRenderer.SetPosition(0, transform.position);

            for (int i = 0; i < remainingPath.Count; i++)
            {
                Vector3 pos = remainingPath[i].WorldPosition;
                pos.y = 0.5f; // Mírně nad zemí, aby byla cesta viditelná
                _lineRenderer.SetPosition(i + 1, pos);
            }
        }

        private IEnumerator FollowPath(List<GridNode> path)
        {
            IsMoving = true;
            List<GridNode> activePath = new List<GridNode>(path);
            UpdatePathVisual(activePath);

            // 1. Zarezervujeme si startovní pozici (kde právě stojíme)
            if (CurrentNode.OccupiedBy == null) CurrentNode.OccupiedBy = this;

            while (activePath.Count > 0)
            {
                GridNode targetNode = activePath[0];

                
                // --- KOLIZNÍ LOGIKA: Čekání na uvolnění ---
                // Pokud je cílový uzel obsazený NĚKÝM JINÝM, čekáme
                if (!targetNode.IsAvailable(this))
                {
                    if (Managers.StatsManager.Instance != null)
                    {
                        Managers.StatsManager.Instance.RegisterCollision();
                    }
                    
                    float waitTimer = 0f;
                    // Čekáme max 2 sekundy (pro plynulost)
                    while (!targetNode.IsAvailable(this))
                    {
                        waitTimer += Time.deltaTime;

                        // Pokud čekáme moc dlouho -> REROUTE (Přepočet)
                        if (waitTimer > 2.0f)
                        {
                            Debug.LogWarning($"AGV {name} je zablokován na [{targetNode.GridX},{targetNode.GridY}]. Přepočítávám trasu...");
                            IsMoving = false;

                            GridNode finalDestination = activePath[activePath.Count - 1];
                            SetDestination(finalDestination, _onDestinationReached);
                            yield break;
                        }
                        yield return null;
                    }
                }

                // Uzel je volný -> Zarezervujeme si ho
                targetNode.OccupiedBy = this;
                
                Vector3 startPos = transform.position;
                Vector3 endPos = targetNode.WorldPosition;
                endPos.y = 0.5f;

                transform.LookAt(endPos);

                float journeyLength = Vector3.Distance(startPos, endPos);
                float startTime = Time.time;

                // Vypočítáme vzdálenost jednou před pohybem (ne každý frame)
                float distanceTraveled = Vector3.Distance(CurrentNode.WorldPosition, targetNode.WorldPosition);

                // Pohyb k aktuálnímu cíli (jednomu nodu)
                while (Vector3.Distance(transform.position, endPos) > 0.01f)
                {
                    float distCovered = (Time.time - startTime) * _moveSpeed;
                    float fractionOfJourney = distCovered / journeyLength;

                    transform.position = Vector3.Lerp(startPos, endPos, fractionOfJourney);
                    
                    if (_lineRenderer.positionCount > 0)
                    {
                        _lineRenderer.SetPosition(0, transform.position);
                    }

                    yield return null; // Čekáme na další frame
                }

                // Jsme v uzlu - přidáme vzdálenost až když jsme skutečně dorazili
                transform.position = endPos;
                
                if (Managers.StatsManager.Instance != null)
                {
                    Managers.StatsManager.Instance.AddDistance(distanceTraveled);
                    
                    // Čas pohybu (přibližně): distance / speed
                    Managers.StatsManager.Instance.AddMovingTime(distanceTraveled / _moveSpeed);
                }

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
            _lineRenderer.enabled = false; // Skryjeme cestu když dorazíme
            
            // Zavoláme uloženou akci (např. "Nakládám zboží")
            _onDestinationReached?.Invoke();
        }

        /// <summary>
        /// Aktualizuje vizuální vzhled vozíku podle jeho stavu.
        /// </summary>
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
            _currentOrder.Status = OrderStatus.Assigned; // Opraven překlep

            State = AGVState.MovingToPickup;
            // Řekneme: Jeď na Pickup a AŽ TAM DOJEDEŠ, spusť OnPickupReached
            SetDestination(order.PickupNode, OnPickupReached);
        }

        private void OnPickupReached()
        {
            Debug.Log("Jsem na místě vyzvednutí. Nakládám...");
            State = AGVState.Loading; // Aktualizace stavu

            StartCoroutine(SimulateTaskDuration(2.0f, () => {
                
                // Po 2 sekundách nakládání:
                State = AGVState.MovingToDelivery;
                _currentOrder.Status = OrderStatus.PickedUp;
                
                // Řekneme: Jeď na Delivery a AŽ TAM DOJEDEŠ, spusť OnDeliveryReached
                SetDestination(_currentOrder.DeliveryNode, OnDeliveryReached);
            }));
        }

        private void OnDeliveryReached()
        {
            Debug.Log("Jsem v cíli. Vykládám...");
            State = AGVState.Unloading; // Aktualizace stavu

            StartCoroutine(SimulateTaskDuration(2.0f, () => {
                
                // Po 2 sekundách vykládání:
                Managers.OrderManager.Instance.CompleteOrder(_currentOrder);
                
                // Vizuální feedback při dokončení objednávky
                StartCoroutine(ShowCompletionEffect());
                
                _currentOrder = null;

                GoToWaitingArea();
            }));
        }

        /// <summary>
        /// Zobrazí vizuální efekt při dokončení objednávky.
        /// </summary>
        private IEnumerator ShowCompletionEffect()
        {
            if (_materialInstance == null) yield break;

            Color originalColor = _materialInstance.color;
            Color successColor = Color.green;
            float duration = 0.5f;
            float elapsed = 0f;

            // Rychlé bliknutí zelenou barvou
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.PingPong(elapsed * 4f, 1f); // Blikání
                _materialInstance.color = Color.Lerp(originalColor, successColor, t);
                yield return null;
            }

            // Vrátíme původní barvu (podle stavu)
            UpdateVisualState();
        }

        private void GoToWaitingArea()
        {
            // Získáme všechna parkovací místa z GridManageru
            var parkingSpots = Managers.GridManager.Instance.GetNodesByType(TileType.WaitingArea);

            GridNode bestSpot = null;
            float minDistance = float.MaxValue;

            // Najdeme nejbližší VOLNÉ parkoviště
            foreach (var spot in parkingSpots)
            {
                // Musí být volné a nesmí to být to, na kterém zrovna stojím (pokud bych už na parkovišti byl)
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
                Debug.Log($"AGV {name} jede na parkoviště [{bestSpot.GridX},{bestSpot.GridY}]");
                State = AGVState.MovingToWaiting;
                SetDestination(bestSpot, () => {
                    State = AGVState.Idle; // Až tam dojede, teprve pak je Idle
                });
            }
            else
            {
                // Žádné volné parkoviště -> Zůstane stát tam, kde je (bohužel)
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
            // 1. Uvolni místo na Gridu
            if (CurrentNode != null && CurrentNode.OccupiedBy == this)
            {
                CurrentNode.OccupiedBy = null;
            }

            // 2. Odhlas se z AgvManageru (NOVÉ)
            if (Managers.AgvManager.Instance != null)
            {
                Managers.AgvManager.Instance.UnregisterAgv(this);
            }

            // 3. Uvolníme instanci materiálu
            if (_materialInstance != null)
            {
                Destroy(_materialInstance);
            }
        }

    }
}