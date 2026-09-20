using _00_Main._01_Scripts.SO;
using UnityEngine;

namespace _00_Main._01_Scripts.Demo
{
    public class PlayerSightManager : MonoBehaviour
    {
        public static PlayerSightManager Instance { get; private set; }
        [field: SerializeField] public PlayerInputSO PlayerInput { get; private set; }

        public float moveSpeed = 5f;
        [Range(0, 10000)]
        public int zoomSpeed = 1000;
        public float minZoom = -30f;
        public float maxZoom = -5f;

        [SerializeField] private Transform pivot;
        
        private Rigidbody2D _rb;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(this);
            
            if(pivot != null)
                _rb = pivot.GetComponent<Rigidbody2D>();
        }

        private void OnEnable()
        {
            PlayerInput.OnMovementChange += Movement;
        }

        private void Update()
        {
            if (PlayerInput.IsShift || PlayerInput.ScrollY == 0) return;
            
            Vector3 camPos = pivot.position;

            camPos.z += PlayerInput.ScrollY * zoomSpeed * Time.deltaTime;

            camPos.z = Mathf.Clamp(camPos.z, minZoom, maxZoom);

            pivot.position = camPos;
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