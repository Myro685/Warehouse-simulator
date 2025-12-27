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

        // --- Data pro Pathfinding (A* / Dijkstra) ---

        // Cena cesty od startu k tomuto uzlu
        public int GCost {get; set;}

        // Heuristická cena (odhad) od tohoto uzlu k cíli
        public int HCost {get; set;}

        // Rodičovský uzel (pro zpětné sestavení cesty)
        public GridNode Parent {get; set;}

        // F Cost je součet G + H (vypočítaná vlastnost)
        public int FCost => GCost + HCost;

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
            // Zdi a regály jsou neprůchozí (pro pohyb skrz)
            return Type != TileType.Wall && Type != TileType.Shelf;
        }
    }
}