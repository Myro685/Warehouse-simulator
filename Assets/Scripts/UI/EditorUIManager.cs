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

        [Header("Build Buttons")]
        [SerializeField] private Button _btnWall;
        [SerializeField] private Button _btnShelf;
        [SerializeField] private Button _btnLoading;
        [SerializeField] private Button _btnUnloading;
        [SerializeField] private Button _btnSpawnAgv;
        [SerializeField] private LevelStorageManager _storageManager;

        private void Start()
        {
            // --- Nastavení posluchačů (Listeners) ---

            // Přiřazení funkcí
            _btnWall.onClick.AddListener(()=> SelectTool(TileType.Wall, _btnWall));
            _btnShelf.onClick.AddListener(()=> SelectTool(TileType.Shelf, _btnShelf));
            _btnLoading.onClick.AddListener(()=> SelectTool(TileType.LoadingDock, _btnLoading));
            _btnUnloading.onClick.AddListener(()=> SelectTool(TileType.UnloadingDock, _btnUnloading));
            _btnSpawnAgv.onClick.AddListener(()=> {_levelEditor.SetSpawnMode();});
        }

        private void SelectTool(TileType type, Button clickedButton)
        {
            // Předáme informaci do LevelEditorManageru
            // Musíme přetypovat enum na int, protože metoda SetTool bere int
            _levelEditor.SetTool((int)type);

            Debug.Log($"Vybrán nástroj: {type}");
            
            // TODO: Zde by se mohla měnit barva aktivního tlačítka pro vizuální odezvu
        }
    }
}