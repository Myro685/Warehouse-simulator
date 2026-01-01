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

        // Fronta čekajících objednávek
        private Queue<Order> _orderQueue = new Queue<Order>();

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

            if (Input.GetKeyDown(KeyCode.O))
            {
                CreateRandomTestOrder();
            }
        }

        private void CreateRandomTestOrder()
        {
            GridManager grid = GridManager.Instance;
            Debug.Log("Provedeno");
            // 1. Najdi náhodný start (Pickup)
            GridNode pickupNode = GetRandomWalkableNode();
            
            // 2. Najdi náhodný cíl (Delivery), který není stejný jako start
            GridNode deliveryNode = GetRandomWalkableNode();

            // Pokud se nepovedlo najít volné místo nebo jsou stejné, zkusíme to znova příště
            if (pickupNode == null || deliveryNode == null || pickupNode == deliveryNode)
            {
                Debug.LogWarning("Nepodařilo se najít validní body pro testovací objednávku.");
                return;
            }

            // 3. Vytvoř objednávku
            CreateOrder(pickupNode, deliveryNode);
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

                Debug.Log($"Objednávka #{order.OrderId} DOKONČENA!");
                
            }
        }
    }
}