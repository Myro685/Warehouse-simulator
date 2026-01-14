using UnityEngine;

namespace Warehouse.Core
{
    public class CameraController : MonoBehaviour
    {
        [Header("Movement Setting")]
        [SerializeField] private float _moveSpeed = 20f;
        [SerializeField] private float _zoomSpeed = 10f;
        [SerializeField] private Vector2 _heightLimits = new Vector2(5f, 30f);
        [SerializeField] private Vector2 _limitX = new Vector2(-10f, 50f);
        [SerializeField] private Vector2 _limitZ = new Vector2(-20f, 40f);

        private void Update()
        {
            HandleMovement();
            HandleZoom();
        }

        private void HandleMovement()
        {
            float h = Input.GetAxis("Horizontal"); // A/D nebo Šipky
            float v = Input.GetAxis("Vertical"); // W/S nebo Šipky

            // Pohyb v rovině X/Z (ignorujeme rotaci kamery pro směr pohybu, chceme absolutní pohyb)
            Vector3 moveDir = new Vector3(h, 0, v);

            // Aplikace pohybu
            Vector3 newPos = transform.position + moveDir * _moveSpeed * Time.unscaledDeltaTime;

            // Omezení hranic (Clamping)
            newPos.x = Mathf.Clamp(newPos.x, _limitX.x, _limitX.y);
            newPos.z = Mathf.Clamp(newPos.z, _limitZ.x, _limitZ.y);

            transform.position = newPos;
        }

        private void HandleZoom()
        {
            // Kolečko myši
            float scroll = Input.GetAxis("Mouse ScrollWheel");
        
            if (scroll != 0)
            {
                Vector3 pos = transform.position;
                // Přiblížení probíhá pohybem po ose Y 
                // Zde uděláme jednoduchý vertikální zoom
                pos.y -= scroll * _zoomSpeed * 100f * Time.unscaledDeltaTime;
                pos.y = Mathf.Clamp(pos.y, _heightLimits.x, _heightLimits.y);
                transform.position = pos;
            }
        }
    }
}