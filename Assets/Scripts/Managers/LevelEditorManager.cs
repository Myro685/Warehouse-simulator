using UnityEngine;
using Warehouse.Grid;
using Warehouse.Core;
using UnityEngine.Tilemaps;
namespace Warehouse.Managers
{
    public enum EditorMode { PaintTile, SpawnUnit }
    public class LevelEditorManager : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private LayerMask _groundLayer; 
        [SerializeField] private Transform _objectsContainer; 
        [Header("Prefabs")]
        [SerializeField] private GameObject _wallPrefab;
        [SerializeField] private GameObject _shelfPrefab;
        [SerializeField] private GameObject _loadingPrefab;
        [SerializeField] private GameObject _unloadingPrefab;
        [SerializeField] private GameObject _waitingAreaPrefab;
        private TileType _currentTool = TileType.Wall;
        private EditorMode _currentMode = EditorMode.PaintTile;
        private void Start()
        {
            if (GridManager.Instance != null)
            {
                GridManager.Instance.OnNodeTypeChanged += OnNodeTypeChanged;
            }
        }
        private void OnDestroy()
        {
            if (GridManager.Instance != null)
            {
                GridManager.Instance.OnNodeTypeChanged -= OnNodeTypeChanged;
            }
        }
        private void Update()
        {
            if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;
            if (SimulationManager.Instance != null && !SimulationManager.Instance.IsPaused)
            {
                return;
            }
            if (_currentMode == EditorMode.SpawnUnit)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    HandleInput();
                }
            }
            else 
            {
                if (Input.GetMouseButton(0))
                {
                    HandleInput();
                }
            }
            if (Input.GetMouseButton(1))
            {
                HandleInput(isErasing: true);
            }
        }
        private void HandleInput(bool isErasing = false)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 1000f, _groundLayer))
            {
                GridNode node = GridManager.Instance.GetNodeFromWorldPosition(hit.point);
                if (node != null)
                {
                    if (isErasing)
                    {
                        if (node.OccupiedBy != null)
                        {
                            Destroy(node.OccupiedBy.gameObject);
                            node.OccupiedBy = null;
                        }
                        else
                        {
                            GridManager.Instance.SetNodeType(node, TileType.Empty);
                        }
                    }
                    else
                    {
                        if (_currentMode == EditorMode.PaintTile)
                        {
                            GridManager.Instance.SetNodeType(node, _currentTool);
                        }
                        else if (_currentMode == EditorMode.SpawnUnit)
                        {
                            Managers.AgvManager.Instance.SpawnAgv(node.GridX, node.GridY);
                        }
                    }
                }
            }
        }
        private void OnNodeTypeChanged(GridNode node, TileType newType)
        {
            if (node.VisualObject != null)
            {
                Destroy(node.VisualObject);
                node.VisualObject = null;
            }
            if (newType != TileType.Empty)
            {
                GameObject prefabToSpawn = GetPrefabByType(newType);
                if (prefabToSpawn != null)
                {
                    Vector3 spawnPos = node.WorldPosition;
                    spawnPos.y += 0.5f;
                    GameObject newObj = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity, _objectsContainer);
                    node.VisualObject = newObj;
                }
            }
        }
        private GameObject GetPrefabByType(TileType type)
        {
            switch (type)
            {
                case TileType.Wall: return _wallPrefab;
                case TileType.Shelf: return _shelfPrefab;
                case TileType.LoadingDock: return _loadingPrefab;
                case TileType.UnloadingDock: return _unloadingPrefab;
                case TileType.WaitingArea: return _waitingAreaPrefab;
                default: return null;
            }
        }
        public void SetTool(int typeIndex)
        {
            _currentMode = EditorMode.PaintTile;
            _currentTool = (TileType)typeIndex;
        }
        public void SetSpawnMode()
        {
            _currentMode = EditorMode.SpawnUnit;
        }
    }
}