using UnityEngine;

namespace _00_Main._01_Scripts.Camera
{
    /// <summary>시점 피벗의 2D 이동을 담당한다.</summary>
    public sealed class PlayerSightMovementController
    {
        private readonly Rigidbody2D _rigidbody;
        private readonly float _moveSpeed;

        public PlayerSightMovementController(Rigidbody2D rigidbody, float moveSpeed)
        {
            _rigidbody = rigidbody;
            _moveSpeed = moveSpeed;
        }

        public void Move(Vector2 direction)
        {
            if (_rigidbody != null)
            {
                _rigidbody.linearVelocity = direction * _moveSpeed;
            }
        }
    }

    /// <summary>시점 피벗의 Z축 줌을 담당한다.</summary>
    public sealed class PlayerSightZoomController
    {
        private readonly Transform _pivot;
        private readonly float _zoomSpeed;
        private readonly float _minZoom;
        private readonly float _maxZoom;

        public PlayerSightZoomController(Transform pivot, float zoomSpeed, float minZoom, float maxZoom)
        {
            _pivot = pivot;
            _zoomSpeed = zoomSpeed;
            _minZoom = minZoom;
            _maxZoom = maxZoom;
        }

        public void Zoom(float scrollDelta, float deltaTime)
        {
            if (_pivot == null || Mathf.Approximately(scrollDelta, 0f))
            {
                return;
            }

            Vector3 position = _pivot.position;
            position.z = Mathf.Clamp(position.z + scrollDelta * _zoomSpeed * deltaTime, _minZoom, _maxZoom);
            _pivot.position = position;
        }
    }
}
