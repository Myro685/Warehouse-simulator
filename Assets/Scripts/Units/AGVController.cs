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

        private MaterialPropertyBlock _propBlock;

        private readonly Color _colorIdle = new Color(0.5f, 0.5f, 0.5f);

        private readonly Color _colorMovingToPickup = new Color(1f, 0.84f, 0f);

        private readonly Color _colorLoading = new Color(0f, 0.5f, 1f);

        private readonly Color _colorMovingToDelivery = new Color(0f, 1f, 0f);

        private readonly Color _colorUnloading = new Color(1f, 0f, 0f);

        private readonly Color _colorMovingToWaiting = new Color(1f, 0.5f, 0f);

        private System.Action _onDestinationReached; 

        private GridNode _finalDestinationNode; 

        public GridNode CurrentNode { get; private set; }

        public bool IsMoving { get; private set; }

        public bool IsWaiting { get; private set; }

        public event System.Action<float, float> OnMoved; 

        public event System.Action OnCollision;

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

            if (_meshRenderer != null)

            {

                _propBlock = new MaterialPropertyBlock();

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

        public void SetDestination(GridNode targetNode, System.Action onReached = null, List<GridNode> ignoredNodes = null)

        {

            _onDestinationReached = onReached;

            _finalDestinationNode = targetNode; 

            PathAlgorithm algo = PathAlgorithm.AStar;

            if (Managers.SimulationManager.Instance != null)

            {

                algo = Managers.SimulationManager.Instance.CurrentAlgorithm;

            }

            List<GridNode> path = Pathfinder.FindPath(CurrentNode, targetNode, algo, ignoredNodes);

            if (path != null && path.Count > 0)

            {

                StopAllCoroutines();

                StartCoroutine(FollowPath(path));

            } 

            else

            {

                if (ignoredNodes != null && ignoredNodes.Count > 0)

                {

                    Debug.LogWarning($"AGV {name}: Slepá ulice kvůli deadlocku! Hledám místo k vyhnutí.");

                    GridNode evadeNode = FindEvadeNode(ignoredNodes[0]);

                    if (evadeNode != null)

                    {

                        System.Action originalOnReached = _onDestinationReached;

                        System.Action resumeAfterEvade = () => {

                            _onDestinationReached = originalOnReached;

                            StartCoroutine(RetryDestination(UnityEngine.Random.Range(4.0f, 6.0f)));

                        };

                        StopAllCoroutines();

                        List<GridNode> evadePath = Pathfinder.FindPath(CurrentNode, evadeNode, algo, ignoredNodes);

                        if (evadePath != null && evadePath.Count > 0)

                        {

                            _onDestinationReached = resumeAfterEvade;

                            StartCoroutine(FollowPath(evadePath));

                            return;

                        }

                    }

                }

                Debug.LogWarning($"AGV {name}: Cesta k cíli nenalezena! Zkusím to znovu za 1s.");

                StopAllCoroutines(); 

                StartCoroutine(RetryDestination(1.0f)); 

            }

        }

        private GridNode FindEvadeNode(GridNode obstacle)

        {

            System.Collections.Generic.Queue<(GridNode node, bool passedIntersection)> queue = new System.Collections.Generic.Queue<(GridNode, bool)>();

            System.Collections.Generic.HashSet<GridNode> visited = new System.Collections.Generic.HashSet<GridNode>();

            queue.Enqueue((CurrentNode, false));

            visited.Add(CurrentNode);

            if (obstacle != null) visited.Add(obstacle);

            GridNode fallbackNode = null;

            while (queue.Count > 0)

            {

                var pair = queue.Dequeue();

                GridNode current = pair.node;

                bool passedInt = pair.passedIntersection;

                int walkableNeighbors = 0;

                foreach (var neighbor in Managers.GridManager.Instance.GetNeighbors(current))

                {

                    if (neighbor.IsWalkable()) walkableNeighbors++;

                }

                if (walkableNeighbors > 2) 

                {

                    passedInt = true;

                    if (fallbackNode == null && current != CurrentNode && current.IsAvailable(this)) 

                    {

                        fallbackNode = current; 

                    }

                }

                if (current != CurrentNode && current.IsAvailable(this))

                {

                    if (walkableNeighbors == 1) return current; 

                    if (walkableNeighbors == 2 && passedInt)

                    {

                        return current;

                    }

                }

                foreach (var neighbor in Managers.GridManager.Instance.GetNeighbors(current))

                {

                    if (neighbor.IsWalkable() && !visited.Contains(neighbor))

                    {

                        visited.Add(neighbor);

                        queue.Enqueue((neighbor, passedInt));

                    }

                }

            }

            return fallbackNode;

        }

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

            if (CurrentNode.OccupiedBy == null) CurrentNode.OccupiedBy = this;

            while (activePath.Count > 0)

            {

                GridNode targetNode = activePath[0];

                if (!targetNode.IsAvailable(this))

                {

                    OnCollision?.Invoke();

                    float waitTimer = 0f;

                    float maxWaitTime = 4.0f + Random.Range(1.0f, 2.0f); 

                    if (_currentOrder != null)

                    {

                        _currentOrder.CollisionCount++;

                    }

                    IsWaiting = true;

                    while (!targetNode.IsAvailable(this))

                    {

                        waitTimer += Time.deltaTime;

                        bool isDeadlock = (targetNode.OccupiedBy != null && targetNode.OccupiedBy.IsWaiting);

                        if (waitTimer > maxWaitTime || isDeadlock)

                        {

                            if (isDeadlock && this.GetInstanceID() > targetNode.OccupiedBy.GetInstanceID())

                            {

                                waitTimer = 0; 

                            }

                            else

                            {

                                if (isDeadlock) 

                                    Debug.Log($"AGV {name}: Detekován čelní deadlock s {targetNode.OccupiedBy.name}! Já (Evader) vycouvávám...");

                                else 

                                    Debug.Log($"AGV {name}: Zablokován na [{CurrentNode.GridX},{CurrentNode.GridY}]. Přepočítávám...");

                                IsMoving = false;

                                IsWaiting = false;

                                if (_finalDestinationNode != null)

                                {

                                    List<GridNode> ignored = new List<GridNode> { targetNode };

                                    SetDestination(_finalDestinationNode, _onDestinationReached, ignored);

                                }

                                yield break; 

                            }

                        }

                        yield return null;

                    }

                    IsWaiting = false;

                }

                targetNode.OccupiedBy = this;

                Vector3 startPos = transform.position;

                Vector3 endPos = targetNode.WorldPosition;

                endPos.y = 0.5f;

                transform.LookAt(endPos);

                float distanceTraveled = Vector3.Distance(CurrentNode.WorldPosition, targetNode.WorldPosition);

                while (Vector3.Distance(transform.position, endPos) > 0.01f)

                {

                    transform.position = Vector3.MoveTowards(transform.position, endPos, _moveSpeed * Time.deltaTime);

                    if (_lineRenderer.positionCount > 0) 

                        _lineRenderer.SetPosition(0, transform.position);

                    yield return null;

                }

                transform.position = endPos;

                OnMoved?.Invoke(distanceTraveled, distanceTraveled / _moveSpeed);

                if (_currentOrder != null)

                {

                    _currentOrder.RealDistance += distanceTraveled;

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

            _lineRenderer.enabled = false;

            _onDestinationReached?.Invoke();

        }

        private void UpdateVisualState()

        {

            if (!_enableStateColors || _propBlock == null || _meshRenderer == null) return;

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

            _meshRenderer.GetPropertyBlock(_propBlock);

            _propBlock.SetColor("_Color", targetColor);

            _meshRenderer.SetPropertyBlock(_propBlock);

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
                State = AGVState.Idle; // Můžeme přijmout další objednávku ihned
                Managers.OrderManager.Instance.TryAssignOrders();
                
                if (_currentOrder == null)
                {
                    GoToWaitingArea();
                }
            }));
        }
        private System.Collections.IEnumerator ShowCompletionEffect()
        {
            if (_propBlock == null || _meshRenderer == null) yield break;
            Color originalColor = _colorUnloading; 
            Color successColor = Color.green;
            float duration = 0.5f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;

                float t = Mathf.PingPong(elapsed * 4f, 1f); 

                _meshRenderer.GetPropertyBlock(_propBlock);

                _propBlock.SetColor("_Color", Color.Lerp(originalColor, successColor, t));

                _meshRenderer.SetPropertyBlock(_propBlock);

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

        }

    }

}