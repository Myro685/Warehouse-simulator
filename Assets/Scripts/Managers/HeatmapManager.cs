using UnityEngine;
using System.Collections.Generic;
using Warehouse.Grid;

namespace Warehouse.Managers
{
    public class HeatmapManager : MonoBehaviour
    {
        public static HeatmapManager Instance { get; private set; }

        [Header("Visualization")]
        [SerializeField] private GameObject _heatmapOverlayPrefab;
        [SerializeField] private Gradient _heatmapGradient;

        private List<GameObject> _activeOverlays = new List<GameObject>();
        private bool _isShowing = false;

        private void Awake()
        {
            Instance = this;
        }

        public void ToggleHeatmap()
        {
            _isShowing = !_isShowing;

            if (_isShowing)
            {
                ShowHeatmap();
            }
            else
            {
                HideHeatmap();
            }
        }

        private void ShowHeatmap()
        {
            HideHeatmap();

            int maxVisits = GetMaxVisits();
            if (maxVisits == 0) maxVisits = 1;

            GridManager grid = GridManager.Instance;
            if (grid == null) return;

            for (int x = 0; x < grid.Width; x++)
            {
                for (int y = 0; y < grid.Height; y++)
                {
                    GridNode node = grid.GetNode(x, y);
                    if (node.VisitCount > 0)
                    {
                        CreateOverlay(node, maxVisits);
                    }
                }
            }
            Debug.Log($"Heatmapa zobrazena. Max návštěv na jednom políčku: {maxVisits}");
        }

        private void HideHeatmap()
        {
            foreach (var overlay in _activeOverlays)
            {
                Destroy(overlay);
            }
            _activeOverlays.Clear();
        }

        private void CreateOverlay(GridNode node, int maxVisits)
        {
            if (_heatmapOverlayPrefab == null) return;

            // Instantiace overlaye na pozici nodu
            Vector3 pos = node.WorldPosition;
            pos.y = 0.05f;

            GameObject obj = Instantiate(_heatmapOverlayPrefab, pos, Quaternion.Euler(90, 0, 0), transform);
            _activeOverlays.Add(obj);

            float t = (float)node.VisitCount / maxVisits;
            Color c = _heatmapGradient.Evaluate(t);
            c.a = 0.6f;

            var renderer = obj.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.color = c;
            }
        }

        private int GetMaxVisits()
        {
            int max = 0;

            GridManager grid = GridManager.Instance;
            if (grid == null) return 0;
            
            for (int x = 0; x < grid.Width; x++)
            {
                for (int y = 0; y < grid.Height; y++)
                {
                    GridNode node = grid.GetNode(x, y);
                    if (node != null && node.VisitCount > max)
                    {
                        max = node.VisitCount;
                    }
                }
            }
            return max;
        }
    }
}