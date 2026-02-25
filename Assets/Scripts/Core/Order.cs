using Warehouse.Grid;
namespace Warehouse.Grid
{
    public enum OrderStatus
    {
        Pending, 
        Assigned, 
        PickedUp, 
        Completed, 
    }
    public class Order
    {
        public int OrderId {get; private set;}
        public GridNode PickupNode {get; private set;}
        public GridNode DeliveryNode {get; private set;}
        public OrderStatus Status {get; set;}
        public float CreationTime {get; private set;}
        public int CollisionCount {get; set;} = 0;
        public float RealDistance {get; set;} = 0f;
        public Order(int id, GridNode pickup, GridNode delivery)
        {
            OrderId = id;
            PickupNode = pickup;
            DeliveryNode = delivery;
            Status = OrderStatus.Pending;
            CreationTime = UnityEngine.Time.time;
            RealDistance = 0f;
            CollisionCount = 0;
        }
    }
}