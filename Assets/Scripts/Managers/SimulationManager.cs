using UnityEngine;
using Warehouse.Pathfinding;

namespace Warehouse.Managers
{
    /// <summary>
    /// Drží globální nastavení simulace (vybraný algoritmus, rychlost atd.).
    /// </summary>
    public class SimulationManager : MonoBehaviour
    {
        public static SimulationManager Instance { get; private set; }

        // Aktuálně vybraný algoritmus (výchozí je A*)
        public PathAlgorithm CurrentAlgorithm { get; private set; } = PathAlgorithm.AStar;

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
        /// Změní algoritmus a restartuje statistiky (aby bylo měření fér).
        /// </summary>
        public void SetAlgorithm(PathAlgorithm algorithm)
        {
            CurrentAlgorithm = algorithm;
            Debug.Log($"[SIM] Algoritmus přepnut na: {algorithm}");

            // Když změníme algoritmus, měli bychom vynulovat statistiky
            if (StatsManager.Instance != null)
            {
                StatsManager.Instance.ResetStats();
            }
        }
    }
}