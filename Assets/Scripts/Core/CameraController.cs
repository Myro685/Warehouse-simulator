using UnityEngine;
namespace Warehouse.Core
{
    public class CameraController : MonoBehaviour
    {
        [Header("Movement Setting")]
        [SerializeField] private float _moveSpeed = 20f;
        [SerializeField] private float _zoomSpeed = 10f;
        [SerializeField] private float _damping = 5f;
        [SerializeField] private Vector2 _heightLimits = new Vector2(5f, 30f);
        [SerializeField] private Vector2 _limitX = new Vector2(-10f, 50f);
        [SerializeField] private Vector2 _limitZ = new Vector2(-20f, 40f);
        private Vector3 _targetPosition;
        private void Start()
        {
            _targetPosition = transform.position;
        }
        private void Update()
        {
            HandleMovement();
            HandleZoom();
            transform.position = Vector3.Lerp(transform.position, _targetPosition, Time.unscaledDeltaTime * _damping);
        }
        private void HandleMovement()
        {
            float h = Input.GetAxisRaw("Horizontal"); 
            float v = Input.GetAxisRaw("Vertical"); 
            Vector3 moveDir = new Vector3(h, 0, v);
            _targetPosition += moveDir * _moveSpeed * Time.unscaledDeltaTime;
            _targetPosition.x = Mathf.Clamp(_targetPosition.x, _limitX.x, _limitX.y);
            _targetPosition.z = Mathf.Clamp(_targetPosition.z, _limitZ.x, _limitZ.y);
        }
        private void HandleZoom()
        {
            float scroll = Input.GetAxisRaw("Mouse ScrollWheel");
            if (scroll != 0)
            {
                _targetPosition.y -= scroll * _zoomSpeed * 100f * Time.unscaledDeltaTime;
                _targetPosition.y = Mathf.Clamp(_targetPosition.y, _heightLimits.x, _heightLimits.y);
            }
        }
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            float width = _limitX.y - _limitX.x;
            float depth = _limitZ.y - _limitZ.x;
            float centerX = _limitX.x + width / 2;
            float centerZ = _limitZ.x + depth / 2;
            Vector3 center = new Vector3(centerX, 0, centerZ); 
            Vector3 size = new Vector3(width, 10, depth);
            Gizmos.DrawWireCube(center, size);
        }
    }
}