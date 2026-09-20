using _00_Main._01_Scripts.SO;
using UnityEngine;

namespace _00_Main._01_Scripts.Demo
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerSightMovementDemo : MonoBehaviour
    {
        [field: SerializeField] public PlayerInputSO PlayerInput { get; private set; }

        public float moveSpeed = 5f;
        [Range(0, 10000)]
        public int zoomSpeed = 1000;
        public float minZoom = -30f;
        public float maxZoom = -5f;
        
        private Rigidbody2D _rb;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
        }

        private void OnEnable()
        {
            PlayerInput.OnMovementChange += Movement;
        }

        private void Update()
        {
            if (PlayerInput.ScrollAxis != 0)
            {
                Vector3 camPos = transform.position;

                camPos.z += PlayerInput.ScrollAxis * zoomSpeed * Time.deltaTime;

                camPos.z = Mathf.Clamp(camPos.z, minZoom, maxZoom);

                transform.position = camPos;
            }
        }

        private void Movement(Vector2 moveDir)
        {
            _rb.linearVelocity = moveDir * moveSpeed;
        }

        private void OnDisable()
        {
            PlayerInput.OnMovementChange -= Movement;
        }
    }
}