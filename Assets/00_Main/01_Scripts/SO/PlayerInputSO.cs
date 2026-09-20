using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _00_Main._01_Scripts.SO
{
    [CreateAssetMenu(fileName = "PlayerInput", menuName = "SO/PlayerInput")]
    public class PlayerInputSO : ScriptableObject, Controls.IPlayerActions
    {
        public event Action<Vector2> OnMovementChange;
        public event Action<bool> OnCursorClicked;
        public event Action<bool> OnCursorToggle;
        
        [field:SerializeField] public float ScrollY { get; private set; }
        [field:SerializeField] public bool IsClicking { get; private set; }
        [field:SerializeField] public bool IsShift { get; private set; }  
        
        private Controls _controls;

        
        public void OnMove(InputAction.CallbackContext context)
        {
            Vector2 moveDir = context.ReadValue<Vector2>();
            OnMovementChange?.Invoke(moveDir);
        }

        public void OnMouseLeftClick(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                OnCursorClicked?.Invoke(true);
                IsClicking = true;
            }
            else if (context.canceled)
            {
                OnCursorClicked?.Invoke(false);
                IsClicking = false;
            }
        }
        
        public void OnCurTgl(InputAction.CallbackContext context)
        {
            if (context.performed)
                OnCursorToggle?.Invoke(true);
            else if (context.canceled)
                OnCursorToggle?.Invoke(false);
        }

        public void OnMouseScroll(InputAction.CallbackContext context)
        {
            ScrollY = context.ReadValue<float>();
        }

        public void OnShift(InputAction.CallbackContext context)
        {
            if(context.performed)
                IsShift = true;
            else if(context.canceled)
                IsShift = false;
        }

        private void OnEnable()
        {
            if (_controls == null)
            {
                _controls = new Controls();
                _controls.Player.SetCallbacks(this);
            }
            _controls.Player.Enable();
        }

        private void OnDisable()
        {
            _controls.Player.Disable();
        }
    }
}
