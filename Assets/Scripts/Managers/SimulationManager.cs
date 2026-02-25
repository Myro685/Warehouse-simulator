using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using Warehouse.Pathfinding;
namespace Warehouse.Managers
{
    public class SimulationManager : MonoBehaviour
    {
        public static SimulationManager Instance { get; private set; }
        public PathAlgorithm CurrentAlgorithm { get; private set; } = PathAlgorithm.AStar;
        public bool IsPaused { get; private set; } = false;
        public float SimulationSpeed { get; private set; } = 1.0f;
        public float TotalTime { get; private set; } = 0f;
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
        public void SetAlgorithm(PathAlgorithm algorithm)
        {
            CurrentAlgorithm = algorithm;
            Debug.Log($"[SIM] Algoritmus přepnut na: {algorithm}");
            if (StatsManager.Instance != null)
            {
                StatsManager.Instance.ResetStats();
            }
        }
        public void SetSimulationSpeed(float speed)
        {
            SimulationSpeed = speed;
            if (!IsPaused)
            {
                Time.timeScale = speed;
            }
            OnSpeedChanged?.Invoke(speed);
        }
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
        public void ResetSimulation()
        {
            int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
            SceneManager.LoadScene(currentSceneIndex);
        }
    }
}