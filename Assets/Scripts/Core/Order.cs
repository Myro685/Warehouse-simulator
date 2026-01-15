using Warehouse.Grid;

namespace Warehouse.Grid
{
    public enum OrderStatus
    {
        Pending, // Čeká ve frontě
        Assigned, // Přiřazena vozíku (jede pro ni)
        PickedUp, // Vozík náklad naložil (jede do cíle)
        Completed, // Hotovo
    }

    /// <summary>
    /// Reprezentuje jednu přepravní úlohu v systému.
    /// </summary>
    public class Order
    {
        public int OrderId {get; private set;}

        // Odkud má vozík zboží vzít
        public GridNode PickupNode {get; private set;}

        // Kam má zboží dovézt
        public GridNode DeliveryNode {get; private set;}

        public OrderStatus Status {get; set;}

        // Čas vytvoření (pro statistiky)
        public float CreationTime {get; private set;}

        public Order(int id, GridNode pickup, GridNode delivery)
        {
            OrderId = id;
            PickupNode = pickup;
            DeliveryNode = delivery;
            Status = OrderStatus.Pending;
            CreationTime = UnityEngine.Time.time;
        }
    }
}