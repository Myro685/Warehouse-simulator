using System.IO;
using UnityEngine;
using Warehouse.Core;
using Warehouse.Grid;
namespace Warehouse.Managers
{
    public class LevelStorageManager : MonoBehaviour
    {
        private string SavePath => Path.Combine(Application.persistentDataPath, "warehouse_layout.json");
        public void SaveLevel()
        {
            GridManager gridManager = GridManager.Instance;
            if(gridManager == null) return;
            LevelData data = new LevelData();
            data.width = gridManager.Width;
            data.height = gridManager.Height;
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
            string json = JsonUtility.ToJson(data, true);
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
        public void LoadLevel()
        {
            if (!File.Exists(SavePath))
            {
                Debug.LogWarning("Soubor s uložením neexistuje.");
                return;
            }
            try
            {
                string json = File.ReadAllText(SavePath);
                LevelData data = JsonUtility.FromJson<LevelData>(json);
                ClearLevel();
                foreach (TileData tile in data.tiles)
                {
                    GridNode node = GridManager.Instance.GetNode(tile.x, tile.y);
                    if (node != null)
                    {
                        GridManager.Instance.SetNodeType(node, (TileType)tile.type);
                    }
                }
                Debug.Log("Sklad načten.");
            }
            catch(System.Exception e)
            {
                Debug.LogError($"Chyba při načítání: {e.Message}");
            }
        }
        public void ClearLevel()
        {
            GridManager gridManager = GridManager.Instance;
            if (gridManager == null) return;
            for (int x = 0; x < gridManager.Width; x++)
            {
                for (int y = 0; y < gridManager.Height; y++)
                {
                    GridNode node = gridManager.GetNode(x, y);
                    if (node != null)
                    {
                        GridManager.Instance.SetNodeType(node, TileType.Empty);
                    }
                }
            }
        }
    }
}