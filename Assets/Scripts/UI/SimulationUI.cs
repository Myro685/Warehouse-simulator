using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Warehouse.Managers;
using System;

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
        [SerializeField] private TextMeshProUGUI _speedValueText; // Pokud ho máš zvlášť, jinak je v Timeru
        [SerializeField] private Button _btnPlayPause;
        [SerializeField] private TextMeshProUGUI _btnPlayPauseLabel;
        [SerializeField] private TextMeshProUGUI _textTimer;
        [SerializeField] private Button _btnReset;

        [Header("Top Right - Stats")]
        [SerializeField] private TextMeshProUGUI _textCompleted;
        [SerializeField] private TextMeshProUGUI _textAvgTime;
        [SerializeField] private TextMeshProUGUI _textCollisions; // NOVÉ
        [SerializeField] private TextMeshProUGUI _textDistance;   // NOVÉ
        [SerializeField] private TextMeshProUGUI _textQueue;      // NOVÉ (Fronta)

        // Rychlostní kroky
        private readonly float[] _speedSteps = { 0.5f, 1f, 2f, 4f, 8f };

        [SerializeField] private LevelStorageManager _storageManager;

        private void Start()
        {
            InitializeManagers();
            InitializeListeners();
            
            // Inicializace slideru
            if (_speedSlider != null)
            {
                _speedSlider.minValue = 0;
                _speedSlider.maxValue = _speedSteps.Length - 1;
                _speedSlider.wholeNumbers = true;
                _speedSlider.value = 1; // Start na 1x
            }
        }

        private void InitializeManagers()
        {
            if (StatsManager.Instance != null)
            {
                StatsManager.Instance.OnStatsChanged += UpdateStatsUI;
                UpdateStatsUI(); // Prvotní vykreslení
            }

            if (SimulationManager.Instance != null)
            {
                SimulationManager.Instance.OnPauseStateChanged += UpdatePauseButton;
                SimulationManager.Instance.OnTimeUpdated += UpdateTimer;
                // SimulationManager.Instance.OnSpeedChanged += ... (řešíme lokálně přes slider)
            }
        }

        private void InitializeListeners()
        {
            // Left Panel
            if (_algoDropdown != null) _algoDropdown.onValueChanged.AddListener(OnAlgoChanged);
            if (_btnHeatmap != null) _btnHeatmap.onClick.AddListener(OnHeatmapClicked);
            if (_btnSave != null) _btnSave.onClick.AddListener(OnSaveClicked);
            if (_btnLoad != null) _btnLoad.onClick.AddListener(OnLoadClicked);

            // Center Panel
            if (_speedSlider != null) _speedSlider.onValueChanged.AddListener(OnSpeedChanged);
            if (_btnPlayPause != null) _btnPlayPause.onClick.AddListener(OnPlayPauseClicked);
            if (_btnReset != null) _btnReset.onClick.AddListener(OnResetClicked);
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

        // --- UI Updates ---

        private void UpdateStatsUI()
        {
            if (StatsManager.Instance == null) return;

            // Základní
            if (_textCompleted) _textCompleted.text = $"Hotovo: {StatsManager.Instance.CompletedOrders}";
            if (_textAvgTime) _textAvgTime.text = $"Prům. čas: {StatsManager.Instance.AverageDeliveryTime:F1}s";
            
            // Rozšířené (pokud jsi je přidal do StatsManageru v minulé fázi)
            if (_textCollisions) _textCollisions.text = $"Kolize: {StatsManager.Instance.TotalCollisions}";
            if (_textDistance) _textDistance.text = $"Ujeto: {StatsManager.Instance.TotalDistanceTravelled:F0}m";

            // Fronta (Musíme získat z OrderManageru)
            if (_textQueue != null && OrderManager.Instance != null)
            {
                 // Potřebuješ přidat property 'QueueCount' do OrderManageru!
                 // Zatím to hardcoduji na 0, dokud to nepřidáš:
                 // _textQueue.text = $"📦 Ve frontě: {OrderManager.Instance.QueueCount}";
                 _textQueue.text = "Ve frontě: ?"; 
            }
        }

        private void UpdateTimer(float totalSeconds)
        {
            if (_textTimer != null)
            {
                TimeSpan t = TimeSpan.FromSeconds(totalSeconds);
                float currentSpeed = SimulationManager.Instance.SimulationSpeed;
                _textTimer.text = string.Format("{0:D2}:{1:D2} <size=60%><color=#AAAAAA></color></size>", 
                                                t.Minutes, t.Seconds, currentSpeed);
            }
        }

        private void UpdatePauseButton(bool isPaused)
        {
            if (_btnPlayPauseLabel != null)
                _btnPlayPauseLabel.text = isPaused ? "START" : "STOP";
        }

        // --- Actions ---

        private void OnAlgoChanged(int index) 
        { 
            // 0 = A*, 1 = Dijkstra
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
            
            // Aktualizace textu proběhne v UpdateTimer
        }

        private void OnPlayPauseClicked() => SimulationManager.Instance.TogglePause();
        private void OnResetClicked() => SimulationManager.Instance.ResetSimulation();

        // --- Nové Tlačítka ---

        private void OnHeatmapClicked()
        {
            if (HeatmapManager.Instance != null)
                HeatmapManager.Instance.ToggleHeatmap();
        }

        private void OnSaveClicked()
        {
            // ZMĚNA: Voláme LevelStorageManager místo GridManageru
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
            // ZMĚNA: Voláme LevelStorageManager místo GridManageru
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
    }
}