using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Warehouse.Grid;
using Warehouse.Pathfinding;
using Warehouse.Core; 

namespace Warehouse.Units
{
    public enum AGVState { Idle, MovingToPickup, Loading, MovingToDelivery, Unloading } 

    [RequireComponent(typeof(LineRenderer))]
    public class AGVController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float _moveSpeed = 2.0f;

        private LineRenderer _lineRenderer;

        private System.Action _onDestinationReached; 

        public GridNode CurrentNode { get; private set; }
        public bool IsMoving { get; private set; }
        
        public AGVState State { get; private set; } = AGVState.Idle;
        
        private Order _currentOrder; 
       
        private void Awake()
        {
            _lineRenderer = GetComponent<LineRenderer>();
            _lineRenderer.positionCount = 0;
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
            
            List<GridNode> path = Pathfinder.FindPath(CurrentNode, targetNode);
            if (path != null && path.Count > 0) 
            {
                StopAllCoroutines();
                StartCoroutine(FollowPath(path));
            }
            else
            {
                // Pokud cesta neexistuje (např. stojíme na místě), rovnou zavoláme hotovo
                Debug.LogWarning("Cesta je nulová nebo prázdná.");
                onReached?.Invoke();
            }
        }

        private void UpdatePathVisual(List<GridNode> remainingPath)
        {
            if (_lineRenderer == null) return;

            if (remainingPath == null || remainingPath.Count == 0)
            {
                _lineRenderer.positionCount = 0;
                return;
            }

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

            while (activePath.Count > 0)
            {
                GridNode targetNode = activePath[0];
                
                Vector3 startPos = transform.position;
                Vector3 endPos = targetNode.WorldPosition;
                endPos.y = 0.5f;

                transform.LookAt(endPos);

                float journeyLength = Vector3.Distance(startPos, endPos);
                float startTime = Time.time;

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

                // Jsme v uzlu
                transform.position = endPos;
                CurrentNode = targetNode;

                activePath.RemoveAt(0);
                UpdatePathVisual(activePath);
            }

            IsMoving = false;
            _lineRenderer.positionCount = 0;
            
            // Zavoláme uloženou akci (např. "Nakládám zboží")
            _onDestinationReached?.Invoke();
        }

        public void AssignOrder(Order order)
        {
            _currentOrder = order;
            _currentOrder.Status = OrderStatus.Assingned; // Opraven překlep

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
                
                _currentOrder = null;
                State = AGVState.Idle;
            }));
        }

        private IEnumerator SimulateTaskDuration(float duration, System.Action onComplete)
        {
            yield return new WaitForSeconds(duration);
            onComplete?.Invoke();
        }
    }
}