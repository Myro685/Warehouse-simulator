using UnityEngine;
using TMPro;
namespace Warehouse.Units
{
    public class AGVInfoDisplay : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private AGVController _controller;
        [SerializeField] private TextMeshPro _textLabel;
        private Camera _mainCamera;
        private void Start()
        {
            _mainCamera = Camera.main;
            if (_controller == null) _controller = GetComponentInParent<AGVController>();
        }
        private void Update()
        {
            if (_textLabel == null || _controller == null) return;
            string statusIcon = GetStatusIcon(_controller.State);
            _textLabel.text = $"{statusIcon}\nAGV";
            if (_mainCamera != null)
            {
                transform.rotation = Quaternion.LookRotation(transform.position - _mainCamera.transform.position);
            }
        }
        private string GetStatusIcon(AGVState state)
        {
            switch (state)
            {
                case AGVState.Idle: return "<color=grey>💤</color>";
                case AGVState.MovingToPickup: return "<color=yellow>🚚</color>";
                case AGVState.Loading: return "<color=blue>📥</color>";
                case AGVState.MovingToDelivery: return "<color=green>📦</color>";
                case AGVState.Unloading: return "<color=red>📤</color>";
                case AGVState.MovingToWaiting: return "<color=orange>🅿️</color>";
                default: return "";
            }
        }
    }
}