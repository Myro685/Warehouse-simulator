using System;

using System.Collections;

using System.Collections.Generic;

using UnityEngine;

using Random = UnityEngine.Random;

using Warehouse.Core;

using Warehouse.Grid;

using Warehouse.Units;

namespace Warehouse.Managers

{

    public class OrderManager : MonoBehaviour

    {

        public static OrderManager Instance {get; private set;}

        [Header("Order Generation Settings")]

        [SerializeField] private bool _autoGenerateOrders = true;

        [Header("Loading Dock (Naskladnění)")]

        [SerializeField] private bool _enableLoadingDockOrders = true;

        [SerializeField] private float _loadingDockInterval = 8f; 

        private float _loadingDockTimer = 0f;

        [Header("Unloading Dock (Vyskladnění)")]

        [SerializeField] private bool _enableUnloadingDockOrders = true;

        [SerializeField] private float _unloadingDockInterval = 10f; 

        private float _unloadingDockTimer = 0f;

        private Queue<Order> _orderQueue = new Queue<Order>();

        public int QueueCount => _orderQueue.Count;

        private List<Order> _activeOrders = new List<Order>();



        private int _nextOrderId = 1;

        private void Awake()

        {

            if (Instance != null && Instance != this)

            {

                Destroy(gameObject);

                return;

            }

            Instance = this;

        }

        private void Update()

        {

            TryAssignOrders();

            if (Input.GetKeyDown(KeyCode.O))

            {

                CreateRandomOrder();

            }

            if (_autoGenerateOrders)

            {

                if (_enableLoadingDockOrders)

                {

                    _loadingDockTimer += Time.deltaTime;

                    if (_loadingDockTimer >= _loadingDockInterval)

                    {

                        _loadingDockTimer = 0f;

                        CreateInboundOrder(); 

                    }

                }

                if (_enableUnloadingDockOrders)

                {

                    _unloadingDockTimer += Time.deltaTime;

                    if (_unloadingDockTimer >= _unloadingDockInterval)

                    {

                        _unloadingDockTimer = 0f;

                        CreateOutboundOrder(); 

                    }

                }

            }

        }

        private GridNode GetRandomWalkableNode()

        {

            GridManager grid = GridManager.Instance;

            if (grid == null) return null;

            for (int i = 0; i < 10; i++)

            {

                int x = Random.Range(0, grid.Width);

                int y = Random.Range(0, grid.Height);

                GridNode node = grid.GetNode(x, y);

                if (node != null && node.IsWalkable())

                {

                    return node;

                }

            }

            return null;

        }   

        public void CreateOrder (GridNode pickup, GridNode delivery)

        {

            if (pickup == null || delivery == null) return;

            if (!pickup.IsWalkable() || !delivery.IsWalkable()) return;

            Order newOrder = new Order(_nextOrderId++, pickup, delivery);

            _orderQueue.Enqueue(newOrder);

            Debug.Log($"Objednávka #{newOrder.OrderId} vytvořena. (Fronta: {_orderQueue.Count})");

        }

        public void TryAssignOrders()
        {
            while (_orderQueue.Count > 0)
            {
                AGVController availableAgv = AgvManager.Instance.GetAvailableAgv();
                if (availableAgv != null)
                {
                    Order order = _orderQueue.Dequeue();
                    _activeOrders.Add(order);
                    availableAgv.AssignOrder(order);
                    Debug.Log($"Objednávka #{order.OrderId} přiřazena vozíku {availableAgv.name}");
                }
                else
                {
                    break;
                }
            }
        }

        public void CompleteOrder(Order order)

        {

            if (_activeOrders.Contains(order))

            {

                order.Status = OrderStatus.Completed;

                _activeOrders.Remove(order);

                float duration = Time.time - order.CreationTime;

                if (StatsManager.Instance != null)

                {

                    StatsManager.Instance.RegisterCompletedOrder(duration);

                }

                string algoName = "Unknown";

                if (SimulationManager.Instance != null)

                {

                    algoName = SimulationManager.Instance.CurrentAlgorithm.ToString();

                }

                CsvExporter.WriteRow(

                    order.OrderId,

                    algoName,

                    order.RealDistance,   

                    duration,

                    order.CollisionCount, 

                    order.CreationTime,

                    Time.time

                );

                Debug.Log($"Objednávka #{order.OrderId} DOKONČENA!");

            }

        }

        private void CreateInboundOrder()

        {

            GridManager grid = GridManager.Instance;

            if (grid == null) return;

            List<GridNode> shelves = grid.GetNodesByType(TileType.Shelf);

            List<GridNode> loadingDocks = grid.GetNodesByType(TileType.LoadingDock);

            if (loadingDocks.Count == 0)

            {

                Debug.LogWarning("Nelze vytvořit objednávku naskladnění: Žádné LoadingDocky.");

                return;

            }

            if (shelves.Count == 0)

            {

                Debug.LogWarning("Nelze vytvořit objednávku naskladnění: Žádné regály (Shelf).");

                return;

            }

            GridNode pickup = loadingDocks[Random.Range(0, loadingDocks.Count)];

            GridNode delivery = shelves[Random.Range(0, shelves.Count)];

            CreateOrder(pickup, delivery);

            Debug.Log($"LoadingDock vytvořil objednávku naskladnění #{_nextOrderId - 1}: LoadingDock → Shelf");

            StartCoroutine(ShowDockIndicator(pickup, Color.green));

        }

        private void CreateOutboundOrder()

        {

            GridManager grid = GridManager.Instance;

            if (grid == null) return;

            List<GridNode> shelves = grid.GetNodesByType(TileType.Shelf);

            List<GridNode> unloadingDocks = grid.GetNodesByType(TileType.UnloadingDock);

            if (unloadingDocks.Count == 0)

            {

                Debug.LogWarning("Nelze vytvořit objednávku vyskladnění: Žádné UnloadingDocky.");

                return;

            }

            if (shelves.Count == 0)

            {

                Debug.LogWarning("Nelze vytvořit objednávku vyskladnění: Žádné regály (Shelf).");

                return;

            }

            GridNode pickup = shelves[Random.Range(0, shelves.Count)];

            GridNode delivery = unloadingDocks[Random.Range(0, unloadingDocks.Count)];

            CreateOrder(pickup, delivery);

            Debug.Log($"UnloadingDock vytvořil objednávku vyskladnění #{_nextOrderId - 1}: Shelf → UnloadingDock");

            StartCoroutine(ShowDockIndicator(delivery, Color.cyan));

        }

        private void CreateRandomOrder()

        {

            bool isInBound = Random.value > 0.5f;

            if (isInBound)

            {

                CreateInboundOrder();

            }

            else

            {

                CreateOutboundOrder();

            }

        }

        private IEnumerator ShowDockIndicator(GridNode dockNode, Color indicatorColor)

        {

            if (dockNode == null || dockNode.VisualObject == null) yield break;

            MeshRenderer renderer = dockNode.VisualObject.GetComponent<MeshRenderer>();

            if (renderer == null || renderer.material == null) yield break;

            MaterialPropertyBlock propBlock = new MaterialPropertyBlock();

            renderer.GetPropertyBlock(propBlock);

            Color originalColor = renderer.material.color; 

            float duration = 1.0f; 

            int blinkCount = 3;

            float blinkDuration = duration / blinkCount;

            for (int i = 0; i < blinkCount; i++)

            {

                float elapsed = 0f;

                while (elapsed < blinkDuration / 2)

                {

                    elapsed += Time.deltaTime;

                    float t = elapsed / (blinkDuration / 2);

                    propBlock.SetColor("_Color", Color.Lerp(originalColor, indicatorColor, t));

                    renderer.SetPropertyBlock(propBlock);

                    yield return null;

                }

                elapsed = 0f;

                while (elapsed < blinkDuration / 2)

                {

                    elapsed += Time.deltaTime;

                    float t = elapsed / (blinkDuration / 2);

                    propBlock.SetColor("_Color", Color.Lerp(indicatorColor, originalColor, t));

                    renderer.SetPropertyBlock(propBlock);

                    yield return null;

                }

            }

            propBlock.SetColor("_Color", originalColor);

            renderer.SetPropertyBlock(propBlock);

        }

        private void OnDestroy()

        {

            StopAllCoroutines();

        }

    }

}