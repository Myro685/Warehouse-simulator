using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Warehouse.Managers;
using Warehouse.Pathfinding;
using UnityEngine.SceneManagement;

namespace Warehouse.UI
{
    /// <summary>
    /// Zobrazuje statistiky simulace v reálném čase.
    /// </summary>
    public class SimulationUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI _textCompleted;
        [SerializeField] private TextMeshProUGUI _textAvgTime;
        [SerializeField] private TMP_Dropdown _algoDropdown;

        [Header("Time Controls")]
        [SerializeField] private Slider _speedSlider;
        [SerializeField] private TextMeshProUGUI _speedValueText;
        [SerializeField] private Button _pauseButton;
        [SerializeField] private TextMeshProUGUI _pauseButtonText;

        [Header("System")]
        [SerializeField] private Button _btnMenu;

        private void Start()
        {
            // Přihlásíme se k odběru události ze StatsManageru
            if (StatsManager.Instance != null)
            {
                StatsManager.Instance.OnStatsChanged += UpdateUI;

                // Prvotní update, aby tam nebyla nula
                UpdateUI();
            }
            
            // --- Nastavení Dropdownu ---
            if (_algoDropdown != null)
            {
                _algoDropdown.onValueChanged.AddListener(OnAlgoChanged);
            }

            if (_speedSlider != null)
            {
                _speedSlider.onValueChanged.AddListener(OnSpeedChanged);
            }

            if (_pauseButton != null)
            {
                _pauseButton.onClick.AddListener(OnPauseClicked);
            }

            if (_btnMenu != null)
            {
                _btnMenu.onClick.AddListener(BackToMenu);
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
            if (SimulationManager.Instance != null)
            {
                SimulationManager.Instance.SetSimulationSpeed(value);
            }

            // Aktualizace textu (např. "5x")
            if (_speedValueText != null)
            {
                _speedValueText.text = $"{value}x";
            }
        }

        private void OnPauseClicked()
        {
            if (SimulationManager.Instance != null)
            {
                SimulationManager.Instance.TogglePause();

                // Změna textu tlačítka
                if (_pauseButtonText != null)
                {
                    bool isPaused = SimulationManager.Instance.IsPaused;
                    _pauseButtonText.text = isPaused ? "PLAY" : "PAUSE";
                }
            }
        }

        private void OnDestroy()
        {
            // Vždy se musíme odhlásit, abychom nezpůsobili memory leaks
            if (StatsManager.Instance != null)
            {
                StatsManager.Instance.OnStatsChanged -= UpdateUI;
            }
        }

        private void UpdateUI()
        {
            if (StatsManager.Instance == null) return;

            // Zobrazení dat
            if (_textCompleted != null)
                _textCompleted.text = $"Hotovo: {StatsManager.Instance.CompletedOrders}";
            
            if (_textAvgTime != null)
                _textAvgTime.text = $"Čas/Avg: {StatsManager.Instance.AverageDeliveryTime:F1} s";
        }
    }
}