using UnityEngine;
using Warehouse.Core;
namespace Warehouse.Grid
{
    public class GridNode
    {
        public int GridX {get; private set;}
        public int GridY {get; private set;}
        public Vector3 WorldPosition {get; private set;}
        public TileType Type {get; set;}
        public GameObject VisualObject {get; set;}
        public Warehouse.Units.AGVController OccupiedBy { get; set; }
        public int VisitCount { get; private set; } = 0;
        public void AddVisit()
        {
            VisitCount++;
        }
        public void ResetVisits()
        {
            VisitCount = 0;
        }
        public GridNode(int gridX, int gridY, Vector3 worldPos)
        {
            GridX = gridX;
            GridY = gridY;
            WorldPosition = worldPos;
            Type = TileType.Empty;
        }
        public bool IsWalkable()
        {
            return Type != TileType.Wall;
        }
        public bool IsAvailable(Warehouse.Units.AGVController asker)
        {
           return OccupiedBy == null || OccupiedBy == asker;
        }
    }
}