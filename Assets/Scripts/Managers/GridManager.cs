using UnityEngine;

using Warehouse.Grid;

using Warehouse.Core;

using System.Collections.Generic;

namespace Warehouse.Managers

{

    public class GridManager : MonoBehaviour

    {

        public static GridManager Instance {get; private set;}

        [Header("Grid Settings")]

        [SerializeField] private int _width = 20; 

        [SerializeField] private int _height = 20; 

        [SerializeField] private float _cellSize = 1.0f; 

        [SerializeField] private Vector3 _originPosition = Vector3.zero; 

        public int Width => _width;

        public int Height => _height;

        private GridNode[,] _grid;

        public event System.Action<GridNode, TileType> OnNodeTypeChanged;

        private Dictionary<TileType, List<GridNode>> _cachedNodesByType = new Dictionary<TileType, List<GridNode>>();

        private void Awake()

        {

            if (Instance != null && Instance != this)

            {

                Destroy(gameObject);

                return;

            } 

            Instance = this;

            CreateGrid();

        }

        private void CreateGrid()

        {

            _grid = new GridNode[_width, _height];

            foreach (TileType type in System.Enum.GetValues(typeof(TileType)))

            {

                _cachedNodesByType[type] = new List<GridNode>();

            }

            for (int x = 0; x < _width; x++)

            {

                for (int y = 0; y < _height; y++)

                {

                    Vector3 worldPoint = _originPosition + new Vector3(x * _cellSize, 0, y * _cellSize) 

                                         + new Vector3(_cellSize / 2, 0, _cellSize / 2);

                    GridNode node = new GridNode(x, y, worldPoint);

                    node.Type = TileType.Empty; 

                    _grid[x, y] = node;

                    _cachedNodesByType[TileType.Empty].Add(node);

                }

            }

            Debug.Log($"Grid vytvořen: {_width}x{_height} buněk.");

        }

        public void SetNodeType(GridNode node, TileType newType)

        {

            if (node == null || node.Type == newType) return;

            TileType oldType = node.Type;

            if (_cachedNodesByType.ContainsKey(oldType))

            {

                _cachedNodesByType[oldType].Remove(node);

            }

            if (!_cachedNodesByType.ContainsKey(newType))

            {

                _cachedNodesByType[newType] = new List<GridNode>();

            }

            _cachedNodesByType[newType].Add(node);

            node.Type = newType;

            OnNodeTypeChanged?.Invoke(node, newType);

        }

        public GridNode GetNodeFromWorldPosition(Vector3 worldPosition)

        {

            float percentX = (worldPosition.x - _originPosition.x) / (_width * _cellSize);

            float percentY = (worldPosition.z - _originPosition.z) / (_height * _cellSize);

            percentX = Mathf.Clamp01(percentX);

            percentY = Mathf.Clamp01(percentY);

            int x = Mathf.RoundToInt((_width - 1) * percentX);

            int y = Mathf.RoundToInt((_height - 1) * percentY);

            return _grid[x, y];

        }

        private void OnDrawGizmos()

        {

            Gizmos.color = Color.yellow;

            Vector3 center = _originPosition + new Vector3(_width * _cellSize / 2, 0, _height * _cellSize / 2);

            Vector3 size = new Vector3(_width * _cellSize, 0.1f, _height * _cellSize);

            Gizmos.DrawWireCube(center, size);

            if (_grid != null)

            {

                foreach (GridNode node in _grid)

                {

                    if (node == null) continue;

                    if (!node.IsWalkable())

                    {

                        Gizmos.color = new Color(1, 0, 0, 0.5f);

                        Gizmos.DrawCube(node.WorldPosition, Vector3.one * (_cellSize * 0.9f));

                    }

                    else if (node.OccupiedBy != null)

                    {

                        Gizmos.color = Color.cyan;

                        Gizmos.DrawWireCube(node.WorldPosition, Vector3.one * (_cellSize * 0.8f));

                        Gizmos.DrawLine(node.WorldPosition, node.OccupiedBy.transform.position);

                    }

                    else

                    {

                        Gizmos.color = new Color(1, 1, 1, 0.1f);

                        Gizmos.DrawWireCube(node.WorldPosition, Vector3.one * (_cellSize * 0.9f));

                    }

                }

            }

        }

        public GridNode GetNode(int x, int y)

        {

           if (x >= 0 && x < _width && y >= 0 && y < _height)

            {

                return _grid[x, y];

            }

            return null;

        }

        public List<GridNode> GetNeighbors(GridNode node)

        {

            List<GridNode> neighbors = new List<GridNode>();

            int[] xDirs = {0, 0, 1, -1};

            int[] yDirs = {1, -1, 0, 0};

            for (int i = 0; i < 4; i++)

            {

                int checkX = node.GridX + xDirs[i];

                int checkY = node.GridY + yDirs[i];

                if (checkX >= 0 && checkX < _width && checkY >= 0 && checkY < _height)

                {

                    neighbors.Add(_grid[checkX, checkY]);

                }

            }

            return neighbors;

        }

        public List<GridNode> GetNodesByType(TileType type)

        {

            if (_cachedNodesByType.ContainsKey(type))

            {

                return _cachedNodesByType[type];

            }

            return new List<GridNode>(); 

        }

    }

}