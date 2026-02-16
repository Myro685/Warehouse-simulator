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
    /// <summary>
    /// Spravuje životní cyklus objednávek a přiděluje je vozíkům.
    /// </summary>
    
    public class OrderManager : MonoBehaviour
    {
        public static OrderManager Instance {get; private set;}

        [Header("Order Generation Settings")]
        [SerializeField] private bool _autoGenerateOrders = true;
        
        [Header("Loading Dock (Naskladnění)")]
        [SerializeField] private bool _enableLoadingDockOrders = true;
        [SerializeField] private float _loadingDockInterval = 8f; // Jak často LoadingDock generuje objednávky
        private float _loadingDockTimer = 0f;
        
        [Header("Unloading Dock (Vyskladnění)")]
        [SerializeField] private bool _enableUnloadingDockOrders = true;
        [SerializeField] private float _unloadingDockInterval = 10f; // Jak často UnloadingDock generuje objednávky
        private float _unloadingDockTimer = 0f;

        // Fronta čekajících objednávek
        private Queue<Order> _orderQueue = new Queue<Order>();
        public int QueueCount => _orderQueue.Count;

        // Seznam aktivních objednávek (pro přehled)
        private List<Order> _activeOrders = new List<Order>();
        
        // Událost pro UI - když se změní fronta
        public event Action OnQueueChanged;

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
            // Každý frame zkusíme přiřadit čekající objednávky
            AssignOrders();

            // Manuální test (Klávesa O - vytvoří náhodnou objednávku)
            if (Input.GetKeyDown(KeyCode.O))
            {
                CreateRandomOrder();
            }

            // Automatické generování objednávek (pokud je zapnuto)
            if (_autoGenerateOrders)
            {
                // LoadingDock generuje objednávky naskladnění
                if (_enableLoadingDockOrders)
                {
                    _loadingDockTimer += Time.deltaTime;
                    if (_loadingDockTimer >= _loadingDockInterval)
                    {
                        _loadingDockTimer = 0f;
                        CreateInboundOrder(); // LoadingDock → Shelf
                    }
                }

                // UnloadingDock generuje objednávky vyskladnění
                if (_enableUnloadingDockOrders)
                {
                    _unloadingDockTimer += Time.deltaTime;
                    if (_unloadingDockTimer >= _unloadingDockInterval)
                    {
                        _unloadingDockTimer = 0f;
                        CreateOutboundOrder(); // Shelf → UnloadingDock
                    }
                }
            }
        }

        private GridNode GetRandomWalkableNode()
        {
            GridManager grid = GridManager.Instance;
            if (grid == null) return null;
            
            // Zkusíme 10x náhodně trefit volné místo. 
            // (V reálné aplikaci by to chtělo sofistikovanější výběr, ale pro test stačí).
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
        /// <summary>
        /// Vytvoří novou objednávku a zařadí ji do fronty.
        /// </summary>
        public void CreateOrder (GridNode pickup, GridNode delivery)
        {
            if (pickup == null || delivery == null) return;
            if (!pickup.IsWalkable() || !delivery.IsWalkable()) return;

            Order newOrder = new Order(_nextOrderId++, pickup, delivery);
            _orderQueue.Enqueue(newOrder);

            Debug.Log($"Objednávka #{newOrder.OrderId} vytvořena. (Fronta: {_orderQueue.Count})");
        }

        /// <summary>
        /// Pokusí se přiřadit nejstarší objednávku volnému vozíku.
        /// </summary>
        private void AssignOrders()
        {
            if (_orderQueue.Count == 0) return;

            // Získáme volný vozík z AgvManageru
            AGVController availableAgv = AgvManager.Instance.GetAvailableAgv();

            if (availableAgv != null)
            {
                Order order = _orderQueue.Dequeue();
                _activeOrders.Add(order);

                // Přiřazení úkolu vozíku
                availableAgv.AssignOrder(order);

                Debug.Log($"Objednávka #{order.OrderId} přiřazena vozíku {availableAgv.name}");
            }
        }

        /// <summary>
        /// Voláno vozíkem, když dokončí objednávku.
        /// </summary>
        public void CompleteOrder(Order order)
        {
            if (_activeOrders.Contains(order))
            {
                order.Status = OrderStatus.Completed;
                _activeOrders.Remove(order);

                float duration = Time.time - order.CreationTime;

                // Kontrola existence StatsManageru (pro bezpečnost)
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

        /// <summary>
        /// Vytvoří objednávku naskladnění (LoadingDock → Shelf).
        /// Voláno automaticky LoadingDockem.
        /// </summary>
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

            // Vybereme náhodný LoadingDock a náhodný regál
            GridNode pickup = loadingDocks[Random.Range(0, loadingDocks.Count)];
            GridNode delivery = shelves[Random.Range(0, shelves.Count)];

            CreateOrder(pickup, delivery);
            Debug.Log($"LoadingDock vytvořil objednávku naskladnění #{_nextOrderId - 1}: LoadingDock → Shelf");
            
            // Vizuální feedback - zobrazíme efekt na LoadingDocku
            StartCoroutine(ShowDockIndicator(pickup, Color.green));
        }

        /// <summary>
        /// Vytvoří objednávku vyskladnění (Shelf → UnloadingDock).
        /// Voláno automaticky UnloadingDockem.
        /// </summary>
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

            // Vybereme náhodný regál a náhodný UnloadingDock
            GridNode pickup = shelves[Random.Range(0, shelves.Count)];
            GridNode delivery = unloadingDocks[Random.Range(0, unloadingDocks.Count)];

            CreateOrder(pickup, delivery);
            Debug.Log($"UnloadingDock vytvořil objednávku vyskladnění #{_nextOrderId - 1}: Shelf → UnloadingDock");
            
            // Vizuální feedback - zobrazíme efekt na UnloadingDocku
            StartCoroutine(ShowDockIndicator(delivery, Color.cyan));
        }

        /// <summary>
        /// Vytvoří náhodnou objednávku (pro testování klávesou O).
        /// </summary>
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

        /// <summary>
        /// Zobrazí vizuální indikátor na docku při vytvoření objednávky.
        /// </summary>
        private IEnumerator ShowDockIndicator(GridNode dockNode, Color indicatorColor)
        {
            if (dockNode == null || dockNode.VisualObject == null) yield break;

            MeshRenderer renderer = dockNode.VisualObject.GetComponent<MeshRenderer>();
            if (renderer == null || renderer.material == null) yield break;

            // Vytvoříme instanci materiálu pro tento dock
            Material originalMaterial = renderer.material;
            Material tempMaterial = new Material(originalMaterial);
            renderer.material = tempMaterial;

            Color originalColor = tempMaterial.color;
            float duration = 1.0f; // Délka animace
            int blinkCount = 3;
            float blinkDuration = duration / blinkCount;

            // Blikání efekt - 3x blikne
            for (int i = 0; i < blinkCount; i++)
            {
                // Rozsvítíme
                float elapsed = 0f;
                while (elapsed < blinkDuration / 2)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / (blinkDuration / 2);
                    tempMaterial.color = Color.Lerp(originalColor, indicatorColor, t);
                    yield return null;
                }

                // Zhasneme
                elapsed = 0f;
                while (elapsed < blinkDuration / 2)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / (blinkDuration / 2);
                    tempMaterial.color = Color.Lerp(indicatorColor, originalColor, t);
                    yield return null;
                }
            }

            // Vrátíme původní materiál
            tempMaterial.color = originalColor;
            renderer.material = originalMaterial;
            Destroy(tempMaterial);
        }
        
        private void OnDestroy()
        {
            // Zastav všechny běžící coroutines aby se uvolnily Materialy
            StopAllCoroutines();
            
            // Odhlas všechny eventy
            OnQueueChanged = null;
        }
    }
}