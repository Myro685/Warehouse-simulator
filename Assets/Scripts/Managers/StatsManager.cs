using UnityEngine;
using System;
namespace Warehouse.Managers
{
    public class StatsManager : MonoBehaviour
    {
        public static StatsManager Instance { get; private set; }
        public int CompletedOrders { get; private set; } = 0;
        public float TotalDeliveryTime { get; private set; } = 0f;
        public int TotalCollisions { get; private set; } = 0;
        public float AverageDeliveryTime => CompletedOrders > 0 ? TotalDeliveryTime / CompletedOrders : 0f;
        public event Action OnStatsChanged;
        public float TotalDistanceTravelled { get; private set; } = 0f;
        private float _totalMovingTime = 0f;
        private float _simulationStartTime;
        private void Start()
        {
            _simulationStartTime = Time.time;
        }
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }
        public void AddDistance(float dist)
        {
            TotalDistanceTravelled += dist;
            OnStatsChanged?.Invoke();
        }
        public void AddMovingTime(float time)
        {
            _totalMovingTime += time;
        }
        public void RegisterCompletedOrder(float duration)
        {
            CompletedOrders++;
            TotalDeliveryTime += duration;
            Debug.Log($"[STATS] Objednávka dokončena za {duration:F2}s. Průměr: {AverageDeliveryTime:F2}s");
            OnStatsChanged?.Invoke();
        }
        public void RegisterCollision()
        {
            TotalCollisions++;
            OnStatsChanged?.Invoke();
        }
        public void ResetStats()
        {
            CompletedOrders = 0;
            TotalDeliveryTime = 0f;
            TotalDistanceTravelled = 0f;
            TotalCollisions = 0;
            _totalMovingTime = 0f;
            _simulationStartTime = Time.time;
            OnStatsChanged?.Invoke();
        }
        public float GetFleetUtilization()
        {
            if (AgvManager.Instance == null) return 0f;
            int agvCount = AgvManager.Instance.ActiveAgvCount; 
            if (agvCount == 0) return 0f;
            float totalAvailableTime = agvCount * (Time.time - _simulationStartTime);
            if (totalAvailableTime <= 0) return 0f;
            return (_totalMovingTime / totalAvailableTime) * 100f;
        }
    }
}