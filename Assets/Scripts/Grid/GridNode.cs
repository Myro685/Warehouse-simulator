using UnityEngine;
using Warehouse.Core;

namespace Warehouse.Grid
{
    /// <summary>
    /// Reprezentuje jednu buňku v navigační mřížce.
    /// Drží informace o své poloze, průchodnosti a data pro pathfinding.
    /// </summary>
    
    public class GridNode
    {
        // Souřadnice v mřížce (X, Y)
        public int GridX {get; private set;}
        public int GridY {get; private set;}

        // Světová pozice (pro pohyb vozíků)
        public Vector3 WorldPosition {get; private set;}

        // Typ políčka (určuje průchodnost)
        public TileType Type {get; set;}

        // Odkaz na vizuální objekt ve scéně (např. instance zdi)
        public GameObject VisualObject {get; set;}

        // Kdo na mě aktuálně stojí/jede
        public Warehouse.Units.AGVController OccupiedBy { get; set; }

        // --- Data pro Pathfinding (A* / Dijkstra) ---

        // Cena cesty od startu k tomuto uzlu
        public int GCost {get; set;}

        // Heuristická cena (odhad) od tohoto uzlu k cíli
        public int HCost {get; set;}

        // Rodičovský uzel (pro zpětné sestavení cesty)
        public GridNode Parent {get; set;}

        // F Cost je součet G + H (vypočítaná vlastnost)
        public int FCost => GCost + HCost;

        // Počet návštěv vozíkem
        public int VisitCount { get; private set; } = 0;

        public void AddVisit()
        {
            VisitCount++;
        }

        public void ResetVisits()
        {
            VisitCount = 0;
        }

        /// <summary>
        /// Konstruktor uzlu.
        /// </summary>
        /// <param name="gridX">X souřadnice v poli.</param>
        /// <param name="gridY">Y souřadnice v poli.</param>
        /// <param name="worldPos">Reálná pozice ve 3D světě.</param>
        public GridNode(int gridX, int gridY, Vector3 worldPos)
        {
            GridX = gridX;
            GridY = gridY;
            WorldPosition = worldPos;
            Type = TileType.Empty;
        }

        /// <summary>
        /// Zjistí, zda je uzel průchozí pro AGV.
        /// </summary>
        public bool IsWalkable()
        {
            // Zeď je jediná překážka. 
            // Regály a Docky musí být průchozí, aby na ně vozík mohl vjet a pracovat.
            return Type != TileType.Wall;
        }

        // Je volno, pokud tu nikdo není NEBO pokud tu jsem já sám (pro případ přeplánování)
        public bool IsAvailable(Warehouse.Units.AGVController asker)
        {
           return OccupiedBy == null || OccupiedBy == asker;
        }
    }
}