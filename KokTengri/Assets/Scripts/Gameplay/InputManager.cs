using System;
using KokTengri.Core;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace KokTengri.Gameplay
{
    public sealed class InputManager : MonoBehaviour, IInputProvider
    {
#if ENABLE_INPUT_SYSTEM
        private const string GameplayActionMapName = "Gameplay";
        private const string MoveActionName = "Move";

        [SerializeField] private InputActionAsset _inputActionsAsset;

        private InputAction _moveAction;
#endif

        public Vector2 MoveDirection { get; private set; }

        public event Action<Vector2> OnMove;

        private void Awake()
        {
#if ENABLE_INPUT_SYSTEM
            InitializeInputActions();
#endif
        }

        private void OnEnable()
        {
#if ENABLE_INPUT_SYSTEM
            EnableMoveAction();
#endif
        }

        private void OnDisable()
        {
#if ENABLE_INPUT_SYSTEM
            DisableMoveAction();
#endif
        }

        private void OnDestroy()
        {
#if ENABLE_INPUT_SYSTEM
            UnsubscribeMoveAction();
#endif
        }

        public void UpdateMoveDirection(Vector2 moveDirection)
        {
            MoveDirection = moveDirection;
            OnMove?.Invoke(moveDirection);
        }

#if ENABLE_INPUT_SYSTEM
        private void InitializeInputActions()
        {
            if (_moveAction != null || _inputActionsAsset == null)
            {
                return;
            }

            _moveAction = _inputActionsAsset.FindAction($"{GameplayActionMapName}/{MoveActionName}", false);

            if (_moveAction == null)
            {
                return;
            }

            _moveAction.performed += HandleMovePerformed;
            _moveAction.canceled += HandleMoveCanceled;
        }

        private void EnableMoveAction()
        {
            InitializeInputActions();

            if (_moveAction != null && !_moveAction.enabled)
            {
                _moveAction.Enable();
            }
        }

        private void DisableMoveAction()
        {
            if (_moveAction != null && _moveAction.enabled)
            {
                _moveAction.Disable();
            }
        }

        private void UnsubscribeMoveAction()
        {
            if (_moveAction == null)
            {
                return;
            }

            _moveAction.performed -= HandleMovePerformed;
            _moveAction.canceled -= HandleMoveCanceled;
        }

        private void HandleMovePerformed(InputAction.CallbackContext context)
        {
            UpdateMoveDirection(context.ReadValue<Vector2>());
        }

        private void HandleMoveCanceled(InputAction.CallbackContext context)
        {
            UpdateMoveDirection(Vector2.zero);
        }
#endif
    }
}
