using UnityEngine;
using Warehouse.Grid;
using Warehouse.Core;
using System.Collections.Generic;

namespace Warehouse.Managers
{
    /// <summary>
    /// Hlavní manažer starající se o generování a správu mřížky skladu.
    /// </summary>
    
    public class GridManager : MonoBehaviour
    {
        // Singleton instance
        public static GridManager Instance {get; private set;}

        [Header("Grid Settings")]
        [SerializeField] private int _width = 20; // Šířka skladu
        [SerializeField] private int _height = 20; // Délka skladu
        [SerializeField] private float _cellSize = 1.0f; // Velikost jedné buňky v Unity jednotkách
        [SerializeField] private Vector3 _originPosition = Vector3.zero; // Počátek mřížky

        // Hlavní datová struktura - 2D pole objektů GridNode
        private GridNode[,] _grid;

        private void Awake()
        {
            // Singleton pattern - inicializace
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            } 
            Instance = this;

            CreateGrid();
        }

        /// <summary>
        /// Vytvoří 2D pole nodů na základě nastavených rozměrů.
        /// </summary>
        private void CreateGrid()
        {
            _grid = new GridNode[_width, _height];

            // Vnořený cyklus pro naplnění mřížky
            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    // Výpočet světové pozice:
                    // Origin + (x * velikost) + polovina velikosti (aby pivot byl uprostřed buňky)
                    Vector3 worldPoint = _originPosition + new Vector3(x * _cellSize, 0, y * _cellSize) 
                                         + new Vector3(_cellSize / 2, 0, _cellSize / 2);

                    _grid[x, y] = new GridNode(x, y, worldPoint);
                }
            }

            Debug.Log($"Grid vytvořen: {_width}x{_height} buněk.");
        }

        /// <summary>
        /// Pomocná metoda pro získání Nodu ze světové pozice (např. kliknutí myší).
        /// </summary>
        public GridNode GetNodeFromWorldPosition(Vector3 worldPosition)
        {
            // Převod lokální pozice vůči počátku mřížky
            float percentX = (worldPosition.x - _originPosition.x) / (_width * _cellSize);
            float percentY = (worldPosition.z - _originPosition.z) / (_height * _cellSize);

            // Ořezání na 0-1, aby nešlo kliknout mimo
            percentX = Mathf.Clamp01(percentX);
            percentY = Mathf.Clamp01(percentY);

            // Převod na indexy pole
            int x = Mathf.RoundToInt((_width - 1) * percentX);
            int y = Mathf.RoundToInt((_height - 1) * percentY);

            return _grid[x, y];
        }
        
        private void OnDrawGizmos()
        {
            // Vykreslíme hranice mřížky i když aplikace neběží
            Gizmos.color = Color.yellow;
            Vector3 center = _originPosition + new Vector3(_width * _cellSize / 2, 0, _height * _cellSize / 2);
            Vector3 size = new Vector3(_width * _cellSize, 0.1f, _height * _cellSize);
            Gizmos.DrawWireCube(center, size);

            if (_grid != null)
            {
                foreach (GridNode node in _grid)
                {
                    // Barva podle typu (zatím jen bílá pro prázdné, červená kdyby byla zeď)
                    Gizmos.color = (node.IsWalkable()) ? new Color(1, 1, 1, 0.3f) : Color.red;
                    
                    // Vykreslení malé kostičky reprezentující uzel
                    Gizmos.DrawCube(node.WorldPosition, Vector3.one * (_cellSize * 0.9f));
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

        /// <summary>
        /// Vrátí seznam sousedních uzlů (nahoru, dolů, vlevo, vpravo).
        /// </summary>
        public List<GridNode> GetNeighbors(GridNode node)
        {
            List<GridNode> neighbors = new List<GridNode>();

            // Definice směrů (X, Y)
            int[] xDirs = {0, 0, 1, -1};
            int[] yDirs = {1, -1, 0, 0};

            for (int i = 0; i < 4; i++)
            {
                int checkX = node.GridX + xDirs[i];
                int checkY = node.GridY + yDirs[i];

                // Kontrola, zda jsme uvnitř mřížky
                if (checkX >= 0 && checkX < _width && checkY >= 0 && checkY < _height)
                {
                    neighbors.Add(_grid[checkX, checkY]);
                }
            }

            return neighbors;
        }
    }
}