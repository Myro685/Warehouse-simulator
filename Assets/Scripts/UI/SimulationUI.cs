using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Warehouse.Managers;
using System;
using UnityEngine.SceneManagement;
namespace Warehouse.UI
{
    public class SimulationUI : MonoBehaviour
    {
        [Header("Top Left - Tools")]
        [SerializeField] private TMP_Dropdown _algoDropdown;
        [SerializeField] private Button _btnHeatmap;
        [SerializeField] private Button _btnSave;
        [SerializeField] private Button _btnLoad;
        [Header("Top Center - Controls")]
        [SerializeField] private Slider _speedSlider;
        [SerializeField] private TextMeshProUGUI _speedValueText; 
        [SerializeField] private Button _btnPlayPause;
        [SerializeField] private TextMeshProUGUI _btnPlayPauseLabel;
        [SerializeField] private TextMeshProUGUI _textTimer;
        [SerializeField] private Button _btnReset;
        [Header("Top Right - Stats")]
        [SerializeField] private TextMeshProUGUI _textCompleted;
        [SerializeField] private TextMeshProUGUI _textAvgTime;
        [SerializeField] private TextMeshProUGUI _textCollisions; 
        [SerializeField] private TextMeshProUGUI _textDistance;   
        [SerializeField] private TextMeshProUGUI _textQueue;      
        [Header("System")]
        [SerializeField] private Button _btnExitToMenu;
        private readonly float[] _speedSteps = { 0.5f, 1f, 2f, 4f, 8f };
        [SerializeField] private LevelStorageManager _storageManager;
        private void Start()
        {
            InitializeManagers();
            InitializeListeners();
            if (_speedSlider != null)
            {
                _speedSlider.minValue = 0;
                _speedSlider.maxValue = _speedSteps.Length - 1;
                _speedSlider.wholeNumbers = true;
                _speedSlider.value = 1; 
            }
        }
        private void InitializeManagers()
        {
            if (StatsManager.Instance != null)
            {
                StatsManager.Instance.OnStatsChanged += UpdateStatsUI;
                UpdateStatsUI(); 
            }
            if (SimulationManager.Instance != null)
            {
                SimulationManager.Instance.OnPauseStateChanged += UpdatePauseButton;
                SimulationManager.Instance.OnTimeUpdated += UpdateTimer;
            }
        }
        private void InitializeListeners()
        {
            if (_algoDropdown != null) _algoDropdown.onValueChanged.AddListener(OnAlgoChanged);
            if (_btnHeatmap != null) _btnHeatmap.onClick.AddListener(OnHeatmapClicked);
            if (_btnSave != null) _btnSave.onClick.AddListener(OnSaveClicked);
            if (_btnLoad != null) _btnLoad.onClick.AddListener(OnLoadClicked);
            if (_speedSlider != null) _speedSlider.onValueChanged.AddListener(OnSpeedChanged);
            if (_btnPlayPause != null) _btnPlayPause.onClick.AddListener(OnPlayPauseClicked);
            if (_btnReset != null) _btnReset.onClick.AddListener(OnResetClicked);
            if (_btnExitToMenu != null) _btnExitToMenu.onClick.AddListener(OnExitToMenuClicked);
        }
        private void OnDestroy()
        {
            if (StatsManager.Instance != null) StatsManager.Instance.OnStatsChanged -= UpdateStatsUI;
            if (SimulationManager.Instance != null)
            {
                SimulationManager.Instance.OnPauseStateChanged -= UpdatePauseButton;
                SimulationManager.Instance.OnTimeUpdated -= UpdateTimer;
            }
        }
        private void UpdateStatsUI()
        {
            if (StatsManager.Instance == null) return;
            if (_textCompleted) _textCompleted.text = $"Hotovo: {StatsManager.Instance.CompletedOrders}";
            if (_textAvgTime) _textAvgTime.text = $"Prům. čas: {StatsManager.Instance.AverageDeliveryTime:F1}s";
            if (_textCollisions) _textCollisions.text = $"Kolize: {StatsManager.Instance.TotalCollisions}";
            if (_textDistance) _textDistance.text = $"Ujeto: {StatsManager.Instance.TotalDistanceTravelled:F0}m";
            if (_textQueue != null && OrderManager.Instance != null)
            {
                _textQueue.text = $"Ve frontě: {OrderManager.Instance.QueueCount}";
            }
        }
        private void UpdateTimer(float totalSeconds)
        {
            if (_textTimer != null)
            {
                TimeSpan t = TimeSpan.FromSeconds(totalSeconds);
                float currentSpeed = SimulationManager.Instance.SimulationSpeed;
                _textTimer.text = string.Format("{0:D2}:{1:D2} <size=60%><color=#AAAAAA>({2}x)</color></size>", 
                                t.Minutes, t.Seconds, currentSpeed);
            }
        }
        private void UpdatePauseButton(bool isPaused)
        {
            if (_btnPlayPauseLabel != null)
                _btnPlayPauseLabel.text = isPaused ? "START" : "STOP";
        }
        private void OnAlgoChanged(int index) 
        { 
            var algo = (index == 0) ? Warehouse.Pathfinding.PathAlgorithm.AStar : Warehouse.Pathfinding.PathAlgorithm.Dijkstra;
            SimulationManager.Instance.SetAlgorithm(algo);
        }
        private void OnSpeedChanged(float value)
        {
            int index = Mathf.RoundToInt(value);
            index = Mathf.Clamp(index, 0, _speedSteps.Length - 1);
            float realSpeed = _speedSteps[index];
            if (SimulationManager.Instance != null)
                SimulationManager.Instance.SetSimulationSpeed(realSpeed);
        }
        private void OnPlayPauseClicked() => SimulationManager.Instance.TogglePause();
        private void OnResetClicked() => SimulationManager.Instance.ResetSimulation();
        private void OnHeatmapClicked()
        {
            if (HeatmapManager.Instance != null)
                HeatmapManager.Instance.ToggleHeatmap();
        }
        private void OnSaveClicked()
        {
            if (_storageManager != null)
            {
                Debug.Log("UI: Ukládám...");
                _storageManager.SaveLevel(); 
            }
            else
            {
                Debug.LogError("Chybí reference na LevelStorageManager v SimulationUI!");
            }
        }
        private void OnLoadClicked()
        {
            if (_storageManager != null)
            {
                Debug.Log("UI: Načítám...");
                _storageManager.LoadLevel();
            }
             else
            {
                Debug.LogError("Chybí reference na LevelStorageManager v SimulationUI!");
            }
        }
        private void OnExitToMenuClicked()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(0);
        }
    }
}