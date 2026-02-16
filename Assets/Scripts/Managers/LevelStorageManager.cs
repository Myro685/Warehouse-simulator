using System.IO;
using UnityEngine;
using Warehouse.Core;
using Warehouse.Grid;

namespace Warehouse.Managers
{
    /// <summary>
    /// Zajišťuje serializaci (ukládání) a deserializaci (načítání) dat skladu do JSON souboru.
    /// </summary>
    
    public class LevelStorageManager : MonoBehaviour
    {
        // Název souboru. Application.persistentDataPath je bezpečné místo pro zápis na všech OS.
        private string SavePath => Path.Combine(Application.persistentDataPath, "warehouse_layout.json");

        [SerializeField] private LevelEditorManager _levelEditor; // Potřebujeme pro obnovu vizuálu

        /// <summary>
        /// Uloží aktuální stav gridu do JSON.
        /// </summary>
        public void SaveLevel()
        {
            GridManager gridManager = GridManager.Instance;
            if(gridManager == null) return;

            // 1. Příprava dat (Mapping)
            LevelData data = new LevelData();
            // Čteme velikost gridu z GridManageru místo hardcoded hodnoty
            data.width = gridManager.Width;
            data.height = gridManager.Height;

            // Projdeme celý grid a uložíme jen ty buňky, které nejsou prázdné
            for (int x = 0; x < data.width; x++)
            {
                for (int y = 0; y < data.height; y++)
                {
                    GridNode node = gridManager.GetNode(x, y);
                    if (node != null && node.Type != TileType.Empty)
                    {
                        TileData tile = new TileData
                        {
                            x = x,
                            y = y,
                            type = (int)node.Type
                        };
                        data.tiles.Add(tile);
                    }
                }
            }

            // 2. Serializace do JSON
            string json = JsonUtility.ToJson(data, true);

            // 3. Zápis do souboru
            try
            {
                File.WriteAllText(SavePath, json);
                Debug.Log($"Sklad úspěšně uložen do: {SavePath}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Chyba při ukládání: {e.Message}");
            }
        }

        /// <summary>
        /// Načte data z JSON a zrekonstruuje sklad.
        /// </summary>
        public void LoadLevel()
        {
            if (!File.Exists(SavePath))
            {
                Debug.LogWarning("Soubor s uložením neexistuje.");
                return;
            }

            try
            {
                // 1. Čtení ze souboru
                string json = File.ReadAllText(SavePath);
                LevelData data = JsonUtility.FromJson<LevelData>(json);

                // 2. Vymazání současného stavu (Clear All)
                ClearLevel();

                // 3. Rekonstrukce
                foreach (TileData tile in data.tiles)
                {
                    GridNode node = GridManager.Instance.GetNode(tile.x, tile.y);
                    if (node != null)
                    {
                        // Využijeme existující metodu v LevelEditorManageru, 
                        // která řeší data I vizuál (DRY princip)
                        _levelEditor.ForcePlaceObject(node, (TileType)tile.type);
                    }
                }
                Debug.Log("Sklad načten.");
            }
            catch(System.Exception e)
            {
                Debug.LogError($"Chyba při načítání: {e.Message}");
            }
        }

        /// <summary>
        /// Vymaže všechny objekty ze skladu.
        /// </summary>
        public void ClearLevel()
        {
            GridManager gridManager = GridManager.Instance;
            if (gridManager == null) return;
            
            // Projdeme grid a vše nastavíme na Empty
            for (int x = 0; x < gridManager.Width; x++)
            {
                for (int y = 0; y < gridManager.Height; y++)
                {
                    GridNode node = gridManager.GetNode(x, y);
                    if (node != null)
                    {
                        // Opět využijeme LevelEditor pro smazání vizuálu
                        _levelEditor.ForcePlaceObject(node, TileType.Empty);
                    }
                }
            }
        }
    }
}