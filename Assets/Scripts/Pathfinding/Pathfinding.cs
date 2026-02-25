using System.Collections.Generic;
using UnityEngine;
using Warehouse.Grid;
using Warehouse.Managers;

namespace Warehouse.Pathfinding
{
    public enum PathAlgorithm
    {
        AStar,
        Dijkstra
    }

    public interface IHeapItem<T> : System.IComparable<T>
    {
        int HeapIndex { get; set; }
    }

    public class MinHeap<T> where T : IHeapItem<T>
    {
        private T[] _items;
        private int _currentItemCount;

        public MinHeap(int maxHeapSize)
        {
            _items = new T[maxHeapSize];
        }

        public void Add(T item)
        {
            item.HeapIndex = _currentItemCount;
            _items[_currentItemCount] = item;
            SortUp(item);
            _currentItemCount++;
        }

        public T RemoveFirst()
        {
            T firstItem = _items[0];
            _currentItemCount--;
            _items[0] = _items[_currentItemCount];
            _items[0].HeapIndex = 0;
            SortDown(_items[0]);
            return firstItem;
        }

        public void UpdateItem(T item)
        {
            SortUp(item);
        }

        public int Count => _currentItemCount;

        public bool Contains(T item)
        {
            if (item.HeapIndex < 0 || item.HeapIndex >= _currentItemCount) return false;
            return object.Equals(_items[item.HeapIndex], item);
        }

        private void SortDown(T item)
        {
            while (true)
            {
                int childIndexLeft = item.HeapIndex * 2 + 1;
                int childIndexRight = item.HeapIndex * 2 + 2;
                int swapIndex = 0;

                if (childIndexLeft < _currentItemCount)
                {
                    swapIndex = childIndexLeft;

                    if (childIndexRight < _currentItemCount)
                    {
                        if (_items[childIndexLeft].CompareTo(_items[childIndexRight]) < 0)
                        {
                            swapIndex = childIndexRight;
                        }
                    }

                    if (item.CompareTo(_items[swapIndex]) < 0)
                    {
                        Swap(item, _items[swapIndex]);
                    }
                    else
                    {
                        return;
                    }
                }
                else
                {
                    return;
                }
            }
        }

        private void SortUp(T item)
        {
            int parentIndex = (item.HeapIndex - 1) / 2;

            while (true)
            {
                T parentItem = _items[parentIndex];
                if (item.CompareTo(parentItem) > 0)
                {
                    Swap(item, parentItem);
                }
                else
                {
                    break;
                }
                parentIndex = (item.HeapIndex - 1) / 2;
            }
        }

        private void Swap(T itemA, T itemB)
        {
            _items[itemA.HeapIndex] = itemB;
            _items[itemB.HeapIndex] = itemA;
            int itemAIndex = itemA.HeapIndex;
            itemA.HeapIndex = itemB.HeapIndex;
            itemB.HeapIndex = itemAIndex;
        }
    }

    public static class Pathfinder
    {
        private class PathNode : IHeapItem<PathNode>
        {
            public GridNode Node { get; set; }
            public int GCost { get; set; }
            public int HCost { get; set; }
            public PathNode Parent { get; set; }
            public Vector2Int Direction { get; set; }
            
            public int FCost => GCost + HCost;
            public int HeapIndex { get; set; }

            public PathNode(GridNode node)
            {
                Node = node;
                GCost = int.MaxValue;
                HCost = 0;
                Parent = null;
                Direction = Vector2Int.zero;
            }

            public int CompareTo(PathNode other)
            {
                int compare = FCost.CompareTo(other.FCost);
                if (compare == 0)
                {
                    compare = HCost.CompareTo(other.HCost);
                }
                // Vyšší priorita znamená vrácení kladného čísla, chceme řadit od nejmenšího FCost
                return -compare;
            }
        }

        public static List<GridNode> FindPath(GridNode startNode, GridNode targetNode, PathAlgorithm algorithm = PathAlgorithm.AStar, List<GridNode> ignoredNodes = null)
        {
            if (!startNode.IsWalkable() || !targetNode.IsWalkable())
            {
                return null;
            }

            GridManager grid = GridManager.Instance;
            if (grid == null) return null;

            int maxHeapSize = grid.Width * grid.Height;
            MinHeap<PathNode> openSet = new MinHeap<PathNode>(maxHeapSize);
            HashSet<GridNode> closedSet = new HashSet<GridNode>();
            Dictionary<GridNode, PathNode> allNodes = new Dictionary<GridNode, PathNode>();

            PathNode GetPathNode(GridNode gridNode)
            {
                if (!allNodes.TryGetValue(gridNode, out PathNode pathNode))
                {
                    pathNode = new PathNode(gridNode);
                    allNodes.Add(gridNode, pathNode);
                }
                return pathNode;
            }

            PathNode startPathNode = GetPathNode(startNode);
            startPathNode.GCost = 0;
            startPathNode.HCost = algorithm == PathAlgorithm.AStar ? GetDistance(startNode, targetNode) : 0;
            openSet.Add(startPathNode);

            while (openSet.Count > 0)
            {
                PathNode currentNode = openSet.RemoveFirst();
                closedSet.Add(currentNode.Node);

                if (currentNode.Node == targetNode)
                {
                    return RetracePath(startPathNode, currentNode);
                }

                foreach (GridNode neighborNode in grid.GetNeighbors(currentNode.Node))
                {
                    bool isIgnored = ignoredNodes != null && ignoredNodes.Contains(neighborNode);

                    if (!neighborNode.IsWalkable() || closedSet.Contains(neighborNode) || isIgnored)
                    {
                        continue;
                    }

                    PathNode neighborPathNode = GetPathNode(neighborNode);
                    
                    Vector2Int newDirection = new Vector2Int(neighborNode.GridX - currentNode.Node.GridX, neighborNode.GridY - currentNode.Node.GridY);
                    
                    int movementCostToNeighbor = currentNode.GCost + GetDistance(currentNode.Node, neighborNode);
                    
                    // Implementace penalizace za zatáčku (vyhlazení tras, preferuje rovné čáry)
                    if (currentNode.Parent != null && currentNode.Direction != newDirection)
                    {
                        movementCostToNeighbor += 2; 
                    }

                    if (movementCostToNeighbor < neighborPathNode.GCost || !openSet.Contains(neighborPathNode))
                    {
                        neighborPathNode.GCost = movementCostToNeighbor;
                        neighborPathNode.Direction = newDirection;
                        
                        if (algorithm == PathAlgorithm.AStar)
                        {
                            neighborPathNode.HCost = GetDistance(neighborNode, targetNode);
                        }
                        else
                        {
                            neighborPathNode.HCost = 0;
                        }
                        
                        neighborPathNode.Parent = currentNode;

                        if (!openSet.Contains(neighborPathNode))
                        {
                            openSet.Add(neighborPathNode);
                        }
                        else
                        {
                            openSet.UpdateItem(neighborPathNode);
                        }
                    }
                }
            }

            return null;
        }

        private static List<GridNode> RetracePath(PathNode startPathNode, PathNode endPathNode)
        {
            List<GridNode> path = new List<GridNode>();
            PathNode currentNode = endPathNode;

            while (currentNode != startPathNode)
            {
                path.Add(currentNode.Node);
                currentNode = currentNode.Parent;
            }
            path.Reverse();

            return path;
        }

        private static int GetDistance(GridNode nodeA, GridNode nodeB)
        {
            int dstX = Mathf.Abs(nodeA.GridX - nodeB.GridX);
            int dstY = Mathf.Abs(nodeA.GridY - nodeB.GridY);

            // Základní vzdálenost násobena 10, aby penalizace +2 za zatočení fungovala jako menší váha
            return (dstX + dstY) * 10;
        }
    }
}