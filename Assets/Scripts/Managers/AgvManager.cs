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

        // Přidej do Update v AgvManager.cs (jen pro testování!):
        private void Update()
        {
            // Když zmáčkneš klávesu T, první vozík pojede na náhodné volné místo
            if (Input.GetKeyDown(KeyCode.T) && _activeAgvs.Count > 0)
            {
                // Najdi náhodné souřadnice
                int x = Random.Range(0, 20);
                int y = Random.Range(0, 20);
                GridNode target = GridManager.Instance.GetNode(x, y);

                if (target != null && target.IsWalkable())
                {
                    Debug.Log($"Posílám vozík na [{x},{y}]");
                    _activeAgvs[0].SetDestination(target);
                }
            }
        }
    }
}