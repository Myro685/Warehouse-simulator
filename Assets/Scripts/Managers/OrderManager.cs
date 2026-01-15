using System.Collections.Generic;
using UnityEngine;
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

        [Header("Setting")]
        [SerializeField] private bool _autoGenerateOrders = false;
        [SerializeField] private float _orderGenerationInterval = 5f;
        private float _timer = 0f;

        // Fronta čekajících objednávek
        private Queue<Order> _orderQueue = new Queue<Order>();
        public int QueueCount => _orderQueue.Count;

        // Seznam aktivních objednávek (pro přehled)
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
            // Každý frame zkusíme přiřadit čekající objednávky
            AssignOrders();

            // Manuální test (Klávesa O)
            if (Input.GetKeyDown(KeyCode.O))
            {
                 CreateLogisticOrder();
            }

            // Automatické generování (pokud je zapnuto)
            if (_autoGenerateOrders)
            {
                _timer += Time.deltaTime;
                if (_timer >= _orderGenerationInterval)
                {
                    _timer = 0f;
                    CreateLogisticOrder();
                }
            }
        }

        private GridNode GetRandomWalkableNode()
        {
            // Zkusíme 10x náhodně trefit volné místo. 
            // (V reálné aplikaci by to chtělo sofistikovanější výběr, ale pro test stačí).
            for (int i = 0; i < 10; i++)
            {
                int x = Random.Range(0, 20); // Změň podle velikosti gridu, pokud ji máš dynamickou
                int y = Random.Range(0, 20);
                
                GridNode node = GridManager.Instance.GetNode(x, y);
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
                    0, // TODO: Vylepšení - ukládat vzdálenost do Orderu
                    duration,
                    0, // TODO: Vylepšení - ukládat kolize do Orderu
                    order.CreationTime,
                    Time.time
                );

                Debug.Log($"Objednávka #{order.OrderId} DOKONČENA!");
                
            }
        }

        private void CreateLogisticOrder()
        {
            GridManager grid = GridManager.Instance;

            // Získáme všechny důležité body
            List<GridNode> shelves = grid.GetNodesByType(TileType.Shelf);
            List<GridNode> loadingDocks = grid.GetNodesByType(TileType.LoadingDock);
            List<GridNode> unloadingDocks = grid.GetNodesByType(TileType.UnloadingDock);

            // Pokud nemáme dostatek bodů, nelze vytvořit logistickou trasu
            if (shelves.Count == 0)
            {
                Debug.LogWarning("Nelze generovat objednávku: Žádné regály (Shelf).");
                return;
            }

            GridNode pickup = null;
            GridNode delivery = null;

            // Náhodně rozhodneme typ úlohy (50/50)
            // Typ A: Naskladnění (Příjem -> Regál)
            // Typ B: Vyskladnění (Regál -> Výdej)

            bool isInBound = Random.value > 0.5f;

            if (isInBound && loadingDocks.Count > 0)
            {
                // NASKLADNĚNÍ: Vyber náhodný příjem a náhodný regál
                pickup = loadingDocks[Random.Range(0, loadingDocks.Count)];
                delivery = shelves[Random.Range(0, shelves.Count)];
                Debug.Log("Generuji NASKLADNĚNÍ (Loading -> Shelf)");
            }
            else if (!isInBound && unloadingDocks.Count > 0)
            {
                // VYSKLADNĚNÍ: Vyber náhodný regál a náhodný výdej
                pickup = shelves[Random.Range(0, shelves.Count)];
                delivery = unloadingDocks[Random.Range(0, unloadingDocks.Count)];
                Debug.Log("Generuji VYSKLADNĚNÍ (Shelf -> Unloading)");
            }
            else
            {
                // Fallback, pokud chybí docky
                Debug.LogWarning("Chybí Loading nebo Unloading docky pro generování trasy.");
                return;
            }

            // Vytvoření objednávky
            CreateOrder(pickup, delivery);
        }
    }
}