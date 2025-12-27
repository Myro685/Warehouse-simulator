using System.Collections.Generic;
using UnityEngine;
using Warehouse.Grid;
using Warehouse.Units;

namespace Warehouse.Managers
{
    /// <summary>
    /// Stará se o vytváření (spawning) a evidenci všech vozíků ve skladu.
    /// </summary>
    
    public class AgvManager : MonoBehaviour
    {
        public static AgvManager Instance {get; private set;}

        [Header("Settings")]
        [SerializeField] private GameObject _agvPrefab;
        [SerializeField] private Transform _agvContainer;

        // Seznam všech aktivních vozíků
        private List<AGVController> _activeAgvs = new List<AGVController>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>
        /// Vytvoří vozík na daných souřadnicích mřížky.
        /// </summary>
        public void SpawnAgv(int x, int y)
        {
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

            // Instantiace
            GameObject agvObj = Instantiate(_agvPrefab, _agvContainer);
            AGVController controller = agvObj.GetComponent<AGVController>();

            if (controller != null)
            {
                controller.Initialize(node);
                _activeAgvs.Add(controller);
                Debug.Log($"AGV vytvořeno na [{x},{y}]");
            }
        }

        // Debug metoda pro testování pohybu (později smažeme)
        public void TestMoveFirstAgv()
        {
            // Zkusíme posunout první vozík o 1 doprava
            AGVController agv = _activeAgvs[0];
            GridNode current = agv.CurrentNode;
            GridNode target = GridManager.Instance.GetNode(current.GridX + 1, current.GridY);

            if (target != null && target.IsWalkable())
            {
                agv.MoveToNode(target);
            }
        }
    }
}