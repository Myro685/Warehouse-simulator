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

            // 1. Kontrola: Je to zeď?
            if (!node.IsWalkable())
            {
                Debug.LogWarning("Nelze spawnout AGV do zdi nebo překážky.");
                return;
            }

            // 2. Kontrola: Stojí tam už někdo? (Prevence spawnu 20 vozíků v sobě)
            if (node.OccupiedBy != null)
            {
                Debug.LogWarning($"Na pozici [{x},{y}] už stojí vozík {node.OccupiedBy.name}!");
                return;
            }

            // Instantiace
            GameObject agvObj = Instantiate(_agvPrefab, _agvContainer);
            AGVController controller = agvObj.GetComponent<AGVController>();

            if (controller != null)
            {
                controller.Initialize(node);
                
                // DŮLEŽITÉ: Hned při startu obsadíme uzel!
                node.OccupiedBy = controller; 
                
                _activeAgvs.Add(controller);
                Debug.Log($"AGV vytvořeno na [{x},{y}]");
            }
        }

        // Debug kód pro testování (zakomentováno pro produkci)
        // Odkomentuj, pokud potřebuješ testovat pohyb vozíků klávesou T
        /*
        private void Update()
        {
            // Když zmáčkneš klávesu T, první vozík pojede na náhodné volné místo
            if (Input.GetKeyDown(KeyCode.T) && _activeAgvs.Count > 0)
            {
                // Najdi náhodné souřadnice
                int x = Random.Range(0, GridManager.Instance.Width);
                int y = Random.Range(0, GridManager.Instance.Height);
                GridNode target = GridManager.Instance.GetNode(x, y);

                if (target != null && target.IsWalkable())
                {
                    Debug.Log($"Posílám vozík na [{x},{y}]");
                    _activeAgvs[0].SetDestination(target);
                }
            }
        }
        */

        /// <summary>
        /// Najde první vozík, který nic nedělá.
        /// </summary>
        public AGVController GetAvailableAgv()
        {
            // Procházíme seznam pozpátku, abychom mohli bezpečně odebírat prvky
            for (int i = _activeAgvs.Count - 1; i >= 0; i--)
            {
                // Pokud je objekt null (byl zničen), vyhodíme ho ze seznamu
                if (_activeAgvs[i] == null)
                {
                    _activeAgvs.RemoveAt(i);
                    continue;
                }

                // Pokud existuje a je Idle, vrátíme ho
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
                _activeAgvs.Remove(agv);
            }
        }
    }
}