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
        private List<GameObject> _pool = new List<GameObject>();
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
                overlay.SetActive(false);
                _pool.Add(overlay);
            }
            _activeOverlays.Clear();
        }
        private void CreateOverlay(GridNode node, int maxVisits)
        {
            if (_heatmapOverlayPrefab == null) return;
            GameObject obj;
            if (_pool.Count > 0)
            {
                obj = _pool[_pool.Count - 1];
                _pool.RemoveAt(_pool.Count - 1);
                obj.SetActive(true);
            }
            else
            {
                obj = Instantiate(_heatmapOverlayPrefab, transform);
                obj.transform.rotation = Quaternion.Euler(90, 0, 0);
            }
            Vector3 pos = node.WorldPosition;
            pos.y = 0.05f;
            obj.transform.position = pos;
            _activeOverlays.Add(obj);
            float t = (float)node.VisitCount / maxVisits;
            t = Mathf.Pow(t, 0.4f);
            Color c = _heatmapGradient.Evaluate(t);
            c.a = 0.6f;
            var renderer = obj.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                if (renderer.material == null || renderer.material.name == "Default-Material")
                {
                    renderer.material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                }
                renderer.material.SetColor("_BaseColor", c);
                renderer.material.SetColor("_Color", c); 
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