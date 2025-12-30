using UnityEngine;
using UnityEngine.UI;
using Warehouse.Managers;
using Warehouse.Core;

namespace Warehouse.UI
{
    /// <summary>
    /// Ovládá tlačítka v editoru a přepíná nástroje.
    /// </summary>
    
    public class EditorUIManager : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private LevelEditorManager _levelEditor;

        [Header("Buttons")]
        [SerializeField] private Button _btnWall;
        [SerializeField] private Button _btnShelf;

        [Header("System Buttons")]
        [SerializeField] private Button _btnSave;
        [SerializeField] private Button _btnLoad;
        [SerializeField] private Button _btnClear;

        [SerializeField] private Button _btnSpawn;

        [SerializeField] private LevelStorageManager _storageManager;

        private void Start()
        {
            // --- Nastavení posluchačů (Listeners) ---

            // TileType.Wall má index 1
            _btnWall.onClick.AddListener(() => SelectTool(TileType.Wall)); 
            
            // TileType.Shelf má index 2
            _btnShelf.onClick.AddListener(() => SelectTool(TileType.Shelf));

            if (_btnSave) _btnSave.onClick.AddListener(() => _storageManager.SaveLevel());
            if (_btnLoad) _btnLoad.onClick.AddListener(() => _storageManager.LoadLevel());
            if (_btnClear) _btnClear.onClick.AddListener(() => _storageManager.ClearLevel());


            if (_btnSpawn) 
            {
                _btnSpawn.onClick.AddListener(() => {
                    // Natvrdo spawneme vozík na pozici 0,0
                    Managers.AgvManager.Instance.SpawnAgv(0, 0); 
                });
            }
        }

        private void SelectTool(TileType type)
        {
            // Předáme informaci do LevelEditorManageru
            // Musíme přetypovat enum na int, protože metoda SetTool bere int
            _levelEditor.SetTool((int)type);

            Debug.Log($"Vybrán nástroj: {type}");
            
            // TODO: Zde by se mohla měnit barva aktivního tlačítka pro vizuální odezvu
        }
    }
}