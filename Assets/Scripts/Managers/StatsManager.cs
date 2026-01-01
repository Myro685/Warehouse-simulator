using UnityEngine;
using System;

namespace Warehouse.Managers
{
    /// <summary>
    /// Shromažďuje data o průběhu simulace.
    /// </summary>
    public class StatsManager : MonoBehaviour
    {
        public static StatsManager Instance { get; private set; }

        // --- Metriky ---
        public int CompletedOrders { get; private set; } = 0;
        public float TotalDeliveryTime { get; private set; } = 0f;

        // Vypočítaná vlastnost: Průměrný čas na jednu objednávku
        public float AverageDeliveryTime => CompletedOrders > 0 ? TotalDeliveryTime / CompletedOrders : 0f;

        // Událost pro UI, aby vědělo, že se změnila čísla (Observer pattern)
        public event Action OnStatsChanged;

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
        /// Zavolá se, když vozík dokončí objednávku.
        /// </summary>
        /// <param name="duration">Jak dlouho trvalo splnění objednávky (sekundy).</param>
        public void RegisterCompletedOrder(float duration)
        {
            CompletedOrders++;
            TotalDeliveryTime += duration;

            Debug.Log($"[STATS] Objednávka dokončena za {duration:F2}s. Průměr: {AverageDeliveryTime:F2}s");

            // Oznámíme všem (hlavně UI), že se změnila data
            OnStatsChanged?.Invoke();
        }

        public void ResetStats()
        {
            CompletedOrders = 0;
            TotalDeliveryTime = 0f;
            OnStatsChanged?.Invoke();
        }
    }
}