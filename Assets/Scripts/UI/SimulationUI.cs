using UnityEngine;
using TMPro;
using Warehouse.Managers;
using Warehouse.Pathfinding;

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