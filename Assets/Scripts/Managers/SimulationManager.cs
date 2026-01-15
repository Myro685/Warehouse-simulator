using UnityEngine;
using UnityEngine.SceneManagement;
using System;
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

        // --- Stav simulace ---
        public bool IsPaused { get; private set; } = false;
        public float SimulationSpeed { get; private set; } = 1.0f;
        
        // --- Time Tracking ---
        // Celkový čas simulace v sekundách (ovlivněný zrychlením)
        public float TotalTime { get; private set; } = 0f;

        // UI se přihlásí k odběru těchto událostí
        public event Action<bool> OnPauseStateChanged;
        public event Action<float> OnSpeedChanged;
        public event Action<float> OnTimeUpdated;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            Time.timeScale = 1.0f;
        }

        public void Update()
        {
            if (!IsPaused)
            {
                TotalTime += Time.deltaTime;
                OnTimeUpdated?.Invoke(TotalTime);
            }
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

        /// <summary>
        /// Nastaví rychlost simulace (1x, 2x, 5x...).
        /// </summary>
        public void SetSimulationSpeed(float speed)
        {
            SimulationSpeed = speed;
            if (!IsPaused)
            {
                Time.timeScale = speed;
            }

            OnSpeedChanged?.Invoke(speed);
        }

        /// <summary>
        /// Pozastaví nebo obnoví simulaci.
        /// </summary>
        public void TogglePause()
        {
            IsPaused = !IsPaused;

            if (IsPaused)
            {
                Time.timeScale = 0f;
            } else
            {
                Time.timeScale = SimulationSpeed;
            }

            OnPauseStateChanged?.Invoke(IsPaused);
        }

        /// <summary>
        /// Restartuje celou simulaci znovu načtením scény. 
        /// </summary>
        public void ResetSimulation()
        {
            // Získáme index aktuální scény a načteme ji znovu
            // Tím se vyčistí všechny objekty, vozíky, statistiky a grid.
            int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
            SceneManager.LoadScene(currentSceneIndex);
        }
    }
}