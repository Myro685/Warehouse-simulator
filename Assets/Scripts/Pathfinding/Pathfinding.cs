using System.Collections.Generic;
using UnityEngine;
using Warehouse.Grid;
using Warehouse.Managers;

namespace Warehouse.Pathfinding
{
    /// <summary>
    /// Statická třída obsahující algoritmy pro hledání cesty.
    /// </summary>
    
    public enum PathAlgorithm
    {
        AStar,
        Dijkstra
    }

    public static class Pathfinder
    {
        /// <summary>
        /// Najde cestu z bodu A do bodu B pomocí A* algoritmu.
        /// </summary>
        /// <returns>Seznam uzlů tvořících cestu (nebo null, pokud cesta neexistuje).</returns>
        public static List<GridNode> FindPath(GridNode startNode, GridNode targetNode, PathAlgorithm algorithm = PathAlgorithm.AStar)
        {
            // Ošetření: Start nebo Cíl jsou neprůchozí
            if (!startNode.IsWalkable() || !targetNode.IsWalkable())
            {
                Debug.LogWarning("Start nebo Cíl je neprůchozí!");
                return null;
            }

            // OPEN SET: Uzly k prozkoumání
            List<GridNode> openSet = new List<GridNode>();

            // CLOSED SET: Již prozkoumané uzly 
            HashSet<GridNode> closedSet = new HashSet<GridNode>();

            openSet.Add(startNode);

            while (openSet.Count > 0)
            {
                // 1. Najdi uzel v openSet s nejnižším FCost
                GridNode currentNode = openSet[0];
                for (int i = 1; i < openSet.Count; i++)
                {
                    // Rozdíl A* vs Dijkstra je v tom, zda porovnáváme FCost (G+H) nebo jen GCost
                    // Ale protože FCost = G + H, tak pro Dijkstru stačí nastavit H = 0.
                    if (openSet[i].FCost < currentNode.FCost || 
                       (openSet[i].FCost == currentNode.FCost && openSet[i].HCost < currentNode.HCost))
                    {
                        currentNode = openSet[i];
                    }
                }

                openSet.Remove(currentNode);
                closedSet.Add(currentNode);

                // 2. Pokud jsme v cíli, sestav cestu
                if (currentNode == targetNode)
                {
                    return RetracePath(startNode, targetNode);
                }

                // 3. Projdi sousedy
                foreach (GridNode neighbor in GridManager.Instance.GetNeighbors(currentNode))
                {
                    // Pokud je soused neprůchozí nebo už hotový, přeskoč ho
                    bool isOccupiedByOther = (neighbor.OccupiedBy != null);
                    if (!neighbor.IsWalkable() || closedSet.Contains(neighbor) || isOccupiedByOther)
                    {
                        continue;
                    }

                    // Cena cesty k sousedovi = GCost aktuálního + vzdálenost (zde vždy 1)
                    int newMovementCostToNeighbor = currentNode.GCost + GetDistance(currentNode, neighbor);

                    // Pokud jsme našli kratší cestu k sousedovi NEBO soused ještě není v OpenSet
                    if (newMovementCostToNeighbor < neighbor.GCost || !openSet.Contains(neighbor))
                    {
                        neighbor.GCost = newMovementCostToNeighbor;
                        
                        if (algorithm == PathAlgorithm.AStar)
                        {
                            neighbor.HCost = GetDistance(neighbor, targetNode);
                        } else // Dijkstra
                        {
                            neighbor.HCost = 0; 
                        }

                        neighbor.Parent = currentNode; // Důležité: uložíme odkud jsme přišli

                        if (!openSet.Contains(neighbor))
                        {
                            openSet.Add(neighbor);
                        }
                    }
                }
            }

            // Cesta nenalezena
            return null;
        }

        /// <summary>
        /// Zpětně projde rodiče od cíle ke startu a vytvoří seznam.
        /// </summary>
        private static List<GridNode> RetracePath(GridNode startNode, GridNode endNode)
        {
            List<GridNode> path = new List<GridNode>();
            GridNode currentNode = endNode;

            while (currentNode != startNode)
            {
                path.Add(currentNode);
                currentNode = currentNode.Parent;
            }
            
            // Cesta je pozpátku (Cíl -> Start), musíme ji otočit
            path.Reverse();
            return path;
        }

        /// <summary>
        /// Vypočítá Manhattan vzdálenost (pravoúhlá mřížka).
        /// </summary>
        private static int GetDistance(GridNode nodeA, GridNode nodeB)
        {
            int dstX = Mathf.Abs(nodeA.GridX - nodeB.GridX);
            int dstY = Mathf.Abs(nodeA.GridY - nodeB.GridY);
            
            // V Manhattan metric je vzdálenost prostý součet rozdílů
            return dstX + dstY;
        }
    }
}