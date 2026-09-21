using _00_Main._01_Scripts.SO;
using UnityEngine;

namespace _00_Main._01_Scripts.Camera
{
    public class PlayerSightManager : MonoBehaviour
    {
        public static PlayerSightManager Instance { get; private set; }
        [field: SerializeField] public PlayerInputSO PlayerInput { get; private set; }

        [SerializeField] private float moveSpeed = 5f;
        [Range(0, 10000)]
        [SerializeField] private int zoomSpeed = 1000;
        [SerializeField] private float minZoom = -30f;
        [SerializeField] private float maxZoom = -5f;

        [SerializeField] private Transform pivot;
        
        private PlayerSightMovementController _movementController;
        private PlayerSightZoomController _zoomController;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(this);
            
            Rigidbody2D pivotRigidbody = pivot != null ? pivot.GetComponent<Rigidbody2D>() : null;
            _movementController = new PlayerSightMovementController(pivotRigidbody, moveSpeed);
            _zoomController = new PlayerSightZoomController(pivot, zoomSpeed, minZoom, maxZoom);
        }

        private void OnEnable()
        {
            PlayerInput.OnMovementChange += Movement;
        }

        private void Update()
        {
            if (!PlayerInput.IsShift)
            {
                _zoomController.Zoom(PlayerInput.ScrollY, Time.deltaTime);
            }
        }

        private void Movement(Vector2 moveDir)
        {
            _movementController.Move(moveDir);
        }

        private void OnDisable()
        {
            PlayerInput.OnMovementChange -= Movement;
        }
    }
}
