using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Warehouse.Managers;
using Warehouse.Pathfinding;
using UnityEngine.SceneManagement;
using System;

namespace Warehouse.UI
{
    /// <summary>
    /// Zobrazuje statistiky simulace v reálném čase.
    /// </summary>
    public class SimulationUI : MonoBehaviour
    {
        [Header("Stats")]
        [SerializeField] private TextMeshProUGUI _textCompleted;
        [SerializeField] private TextMeshProUGUI _textAvgTime;
        [SerializeField] private TMP_Dropdown _algoDropdown;

        [Header("Controls (Modern UI)")]
        [SerializeField] private Slider _speedSlider;
        [SerializeField] private TextMeshProUGUI _speedValueText;
        [SerializeField] private Button _btnPlayPause;
        [SerializeField] private TextMeshProUGUI _btnPlayPauseLabel;
        [SerializeField] private TextMeshProUGUI _textTimer;
        [SerializeField] private Button _btnReset;

        [Header("System")]
        [SerializeField] private Button _btnMenu;

        private readonly float[] _speedSteps = { 0.5f, 1f, 2f, 4f, 8f };

        private void Start()
        {
            // Přihlásíme se k odběru události ze StatsManageru
            if (StatsManager.Instance != null)
            {
                StatsManager.Instance.OnStatsChanged += UpdateStatsUI;

                // Prvotní update, aby tam nebyla nula
                UpdateStatsUI();
            }
            
            // --- Napojení Simulation Manageru ---
            if (SimulationManager.Instance != null)
            {
                // Přihlášení k událostem (Observer)
                SimulationManager.Instance.OnPauseStateChanged += UpdatePauseButton;
                SimulationManager.Instance.OnSpeedChanged += UpdateSpeedText;
                SimulationManager.Instance.OnTimeUpdated += UpdateTimer;
            }

            // --- Listenery pro UI prvky ---
            if (_algoDropdown != null) _algoDropdown.onValueChanged.AddListener(OnAlgoChanged);
            if (_speedSlider != null) _speedSlider.onValueChanged.AddListener(OnSpeedChanged);
            if (_btnPlayPause != null) _btnPlayPause.onClick.AddListener(OnPlayPauseClicked);
            if (_btnReset != null) _btnReset.onClick.AddListener(OnResetClicked);

            if (_speedSlider != null)
        {
            // Nastavíme slideru správný rozsah podle našeho pole
            _speedSlider.minValue = 0;
            _speedSlider.maxValue = _speedSteps.Length - 1;
            _speedSlider.wholeNumbers = true;
            
            // Nastavíme výchozí pozici na "1x" (což je index 1 v poli: 0.5, [1], 2...)
            _speedSlider.value = 1; 

            _speedSlider.onValueChanged.AddListener(OnSpeedChanged);
        }
        }
        
        private void OnDestroy()
        {
            // Vždy se musíme odhlásit, abychom nezpůsobili memory leaks
            if (StatsManager.Instance != null) StatsManager.Instance.OnStatsChanged -= UpdateStatsUI;
            
            if (SimulationManager.Instance != null)
            {
                SimulationManager.Instance.OnPauseStateChanged -= UpdatePauseButton;
                SimulationManager.Instance.OnSpeedChanged -= UpdateSpeedText;
                SimulationManager.Instance.OnTimeUpdated -= UpdateTimer;
            }
        }

        
        private void UpdateStatsUI()
        {
            if (_textCompleted) _textCompleted.text = $"HOTOVO: {StatsManager.Instance.CompletedOrders}";
            if (_textAvgTime) _textAvgTime.text = $"PRŮMĚR: {StatsManager.Instance.AverageDeliveryTime:F1}s";
        }

        private void UpdatePauseButton(bool isPaused)
        {
            if (_btnPlayPauseLabel != null)
            {
                // Použijeme EMOJI pro moderní vzhled
                _btnPlayPauseLabel.text = isPaused ? "PLAY" : "PAUSE"; 
            }
        }

        private void UpdateSpeedText(float speed)
        {
            if (_speedValueText != null) _speedValueText.text = $"{speed}x";
        }

        private void UpdateTimer(float totalSeconds)
        {
            if (_textTimer != null)
            {
                TimeSpan t = TimeSpan.FromSeconds(totalSeconds);
                
                // Rich Text formátování pro hezčí vzhled
                _textTimer.text = string.Format("{0:D2}:{1:D2} <size=60%><color=#AAAAAA></color></size>", 
                                                t.Minutes, t.Seconds);
            }
        }

        private void BackToMenu()
        {
            SceneManager.LoadScene(0);

            Time.timeScale = 1.0f;
        }

        private void OnAlgoChanged(int index)
        {
            // Index 0 = A*, Index 1 = Dijkstra (podle pořadí v Options)
            PathAlgorithm selectedAlgo = (index == 0) ? PathAlgorithm.AStar : PathAlgorithm.Dijkstra;

            if (SimulationManager.Instance != null)
            {
                SimulationManager.Instance.SetAlgorithm(selectedAlgo);
            }
        }

        private void OnSpeedChanged(float value)
        {
            // Převedeme float ze slideru na int index (0, 1, 2, 3, 4)
            int index = Mathf.RoundToInt(value);
            
            // Pojistka, abychom nesáhli mimo pole
            index = Mathf.Clamp(index, 0, _speedSteps.Length - 1);

            // Vybereme skutečnou rychlost z pole
            float realSpeed = _speedSteps[index];

            if (SimulationManager.Instance != null)
            {
                SimulationManager.Instance.SetSimulationSpeed(realSpeed);
            }
            
            // Aktualizujeme text (teď to bude ukazovat správně 0.5x, 8x atd.)
            if (_speedValueText != null)
            {
                _speedValueText.text = $"{realSpeed}x";
            }
        }

        private void OnPlayPauseClicked()
        {
            SimulationManager.Instance.TogglePause();
        }
        private void OnResetClicked()
        {
            SimulationManager.Instance.ResetSimulation();
        }
    }
}