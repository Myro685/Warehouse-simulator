using UnityEngine;
using Warehouse.Grid;
using Warehouse.Core;
using UnityEngine.Tilemaps;

namespace Warehouse.Managers
{
    /// <summary>
    /// Řídí interakci uživatele při editaci skladu (pokládání zdí, regálů).
    /// Převádí vstup myši na změny v Gridu.
    /// </summary>
    
    public enum EditorMode { PaintTile, SpawnUnit }

    public class LevelEditorManager : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private LayerMask _groundLayer; // Vrstva podlahy pro Raycast
        [SerializeField] private Transform _objectsContainer; // Rodičovský objekt pro pořádek v hierarchii

        [Header("Prefabs")]
        [SerializeField] private GameObject _wallPrefab;
        [SerializeField] private GameObject _shelfPrefab;
        [SerializeField] private GameObject _loadingPrefab;
        [SerializeField] private GameObject _unloadingPrefab;
        [SerializeField] private GameObject _waitingAreaPrefab;

        // Aktuálně vybraný typ pro stavbu
        private TileType _currentTool = TileType.Wall;
        private EditorMode _currentMode = EditorMode.PaintTile;

        private void Update()
        {
            // Ochrana proti klikání do UI (aby se nekreslilo skrz tlačítka)
            if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;

            if (SimulationManager.Instance != null && !SimulationManager.Instance.IsPaused)
            {
                return;
            }
            
            // Logika podle režimu
            if (_currentMode == EditorMode.SpawnUnit)
            {
                // PRO SPAWN: Reagujeme jen na stisknutí (Down)
                if (Input.GetMouseButtonDown(0))
                {
                    HandleInput();
                }
            }
            else // EditorMode.PaintTile
            {
                // PRO KRESLENÍ: Reagujeme po celou dobu držení
                if (Input.GetMouseButton(0))
                {
                    HandleInput();
                }
            }

            // Mazání (Pravé tlačítko)
            if (Input.GetMouseButton(1))
            {
                HandleInput(isErasing: true);
            }
        }

        /// <summary>
        /// Zpracuje pozici myši a provede akci na mřížce.
        /// </summary>
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
                        // Pravé tlačítko maže buď vozík, nebo zeď
                        // Priorita: Nejdřív smaž vozík, pokud tam je, jinak zeď
                        if (node.OccupiedBy != null)
                        {
                            // TODO: Zde bychom ideálně měli volat metodu AgvManageru pro smazání
                            // Prozatím Destroy(node.OccupiedBy.gameObject) je "dirty" řešení
                            Destroy(node.OccupiedBy.gameObject);
                            node.OccupiedBy = null;
                        }
                        else
                        {
                            ClearNode(node);
                        }
                    }
                    else
                    {
                        // Levé tlačítko - chování podle módu
                        if (_currentMode == EditorMode.PaintTile)
                        {
                            PlaceObject(node, _currentTool);
                        }
                        else if (_currentMode == EditorMode.SpawnUnit)
                        {
                            // Spawnujeme vozík (GetMouseButtonDown je už kontrolováno v Update)
                            Managers.AgvManager.Instance.SpawnAgv(node.GridX, node.GridY);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Umístí objekt na daný uzel, pokud tam už není stejný typ.
        /// </summary>
        private void PlaceObject(GridNode node, TileType type)
        {
            if (node.Type == type) return;
            
            // Nejdřív vyčistíme starý objekt, pokud tam nějaký byl
            ClearNode(node);

            // Aktualizace dat v Nodu
            node.Type = type;

            // Vizuální vytvoření objektu
            GameObject prefabToSpawn = GetPrefabByType(type);
            if (prefabToSpawn != null)
            {
                // Instantiace na pozici Nodu + malá korekce Y (aby nebyl utopený v zemi)
                Vector3 spawnPos = node.WorldPosition;
                spawnPos.y += 0.5f; // Protože Cube má pivot uprostřed a výšku 1

                GameObject newObj = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity, _objectsContainer);
                
                // Uložíme si odkaz na vizuál
                node.VisualObject = newObj;
            }
        }

        /// <summary>
        /// Veřejná metoda pro programové umístění objektů (např. při Loadingu).
        /// </summary>
        public void ForcePlaceObject(GridNode node, TileType type)
        {
            if (type == TileType.Empty)
            {
                ClearNode(node);
            }
            else
            {
                PlaceObject(node, type);
            }
        }

        /// <summary>
        /// Odstraní objekt z uzlu a nastaví ho na Empty.
        /// </summary>
        private void ClearNode(GridNode node)
        {
            if(node.Type == TileType.Empty) return;

            if (node.VisualObject != null)
            {
                Destroy(node.VisualObject);
                node.VisualObject = null;
            }

            node.Type = TileType.Empty;
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

        // Metoda pro UI tlačítka
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