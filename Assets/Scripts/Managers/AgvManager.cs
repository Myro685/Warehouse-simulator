using System.Collections.Generic;
using UnityEngine;
using Warehouse.Grid;
using Warehouse.Units;
namespace Warehouse.Managers
{
    public class AgvManager : MonoBehaviour
    {
        public static AgvManager Instance {get; private set;}
        [Header("Settings")]
        [SerializeField] private GameObject _agvPrefab;
        [SerializeField] private Transform _agvContainer;
        public List<AGVController> _activeAgvs = new List<AGVController>();
        public int ActiveAgvCount => _activeAgvs.Count;
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }
        public void SpawnAgv(int x, int y)
        {
            if (GridManager.Instance == null)
            {
                Debug.LogError("AgvManager: GridManager není inicializován!");
                return;
            }
            GridNode node = GridManager.Instance.GetNode(x, y);
            if (node == null)
            {
                Debug.LogError("Pokus o spawn AGV mimo mřížku!");
                return;
            }
            if (!node.IsWalkable())
            {
                Debug.LogWarning("Nelze spawnout AGV do zdi nebo překážky.");
                return;
            }
            if (node.OccupiedBy != null)
            {
                Debug.LogWarning($"Na pozici [{x},{y}] už stojí vozík {node.OccupiedBy.name}!");
                return;
            }
            GameObject agvObj = Instantiate(_agvPrefab, _agvContainer);
            AGVController controller = agvObj.GetComponent<AGVController>();
            if (controller != null)
            {
                controller.Initialize(node);
                node.OccupiedBy = controller; 
                controller.OnMoved += HandleAgvMoved;
                controller.OnCollision += HandleAgvCollision;
                _activeAgvs.Add(controller);
                Debug.Log($"AGV vytvořeno na [{x},{y}]");
            }
        }
        public AGVController GetAvailableAgv()
        {
            for (int i = _activeAgvs.Count - 1; i >= 0; i--)
            {
                if (_activeAgvs[i] == null)
                {
                    _activeAgvs.RemoveAt(i);
                    continue;
                }
                if (_activeAgvs[i].State == AGVState.Idle)
                {
                    return _activeAgvs[i];
                }
            }
            return null;
        }
        public void UnregisterAgv(AGVController agv)
        {
            if (_activeAgvs.Contains(agv))
            {
                agv.OnMoved -= HandleAgvMoved;
                agv.OnCollision -= HandleAgvCollision;
                _activeAgvs.Remove(agv);
            }
        }
        private void HandleAgvMoved(float distance, float time)
        {
            if (StatsManager.Instance != null)
            {
                StatsManager.Instance.AddDistance(distance);
                StatsManager.Instance.AddMovingTime(time);
            }
        }
        private void HandleAgvCollision()
        {
            if (StatsManager.Instance != null)
            {
                StatsManager.Instance.RegisterCollision();
            }
        }
    }
}