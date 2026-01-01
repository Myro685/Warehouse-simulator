using UnityEngine;
using TMPro;
using Warehouse.Managers;
using Unity.VisualScripting.Antlr3.Runtime.Misc;

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

        private void Start()
        {
            // Přihlásíme se k odběru události ze StatsManageru
            if (StatsManager.Instance != null)
            {
                StatsManager.Instance.OnStatsChanged += UpdateUI;

                // Prvotní update, aby tam nebyla nula
                UpdateUI();
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