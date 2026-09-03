using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace LastSeed.Infrastructure.Input
{
    [DisallowMultipleComponent]
    public sealed class PlayerInputSnapshotProvider : MonoBehaviour, IPlayerInputSnapshotProvider
    {
        private const float MinimumScreenWidthPixels = 1f;
        private const string TouchPressActionName = "Touch Press";
        private const string TouchPressBinding = "<Touchscreen>/primaryTouch/press";
        private const string TouchPositionActionName = "Touch Position";
        private const string TouchPositionBinding = "<Touchscreen>/primaryTouch/position";
        private const string TouchPositionControlType = "Vector2";

        [FormerlySerializedAs("_enableTouchTarget")]
        [SerializeField] private bool _enableTouchDrag = true;

        private InputActions _inputActions;
        private InputAction _touchPressAction;
        private InputAction _touchPositionAction;
        private float _previousTouchPositionX;
        private bool _wasTouchActive;

        public PlayerInputSnapshot CurrentSnapshot { get; private set; }

        private void Awake()
        {
            EnsureInputActionsCreated();
        }

        private void OnEnable()
        {
            EnsureInputActionsCreated();
            _inputActions.Player.Enable();

            if (_enableTouchDrag)
            {
                _touchPressAction.Enable();
                _touchPositionAction.Enable();
            }
        }

        private void OnDisable()
        {
            if (_inputActions != null)
                _inputActions.Player.Disable();

            _touchPressAction?.Disable();
            _touchPositionAction?.Disable();
            ResetState();
        }

        private void OnDestroy()
        {
            _touchPressAction?.Dispose();
            _touchPositionAction?.Dispose();
            _inputActions?.Dispose();
        }

        public void CaptureFrame()
        {
            EnsureInputActionsCreated();

            float horizontalMovement = _inputActions.Player.Move.ReadValue<float>();
            bool isTouchActive = _enableTouchDrag && _touchPressAction.IsPressed();
            float normalizedTouchDeltaX = CaptureNormalizedTouchDeltaX(isTouchActive);

            CurrentSnapshot = new PlayerInputSnapshot(
                horizontalMovement,
                isTouchActive,
                normalizedTouchDeltaX);
        }

        public void ResetState()
        {
            CurrentSnapshot = default;
            _previousTouchPositionX = 0f;
            _wasTouchActive = false;
        }

        private float CaptureNormalizedTouchDeltaX(bool isTouchActive)
        {
            if (!isTouchActive)
            {
                _wasTouchActive = false;
                return 0f;
            }

            float currentTouchPositionX = _touchPositionAction.ReadValue<Vector2>().x;
            float touchDeltaX = _wasTouchActive
                ? currentTouchPositionX - _previousTouchPositionX
                : 0f;

            _previousTouchPositionX = currentTouchPositionX;
            _wasTouchActive = true;

            return touchDeltaX / Mathf.Max(MinimumScreenWidthPixels, Screen.width);
        }

        private void EnsureInputActionsCreated()
        {
            if (_inputActions != null)
                return;

            _inputActions = new InputActions();
            _touchPressAction = new InputAction(
                TouchPressActionName,
                InputActionType.Button,
                TouchPressBinding);
            _touchPositionAction = new InputAction(
                TouchPositionActionName,
                InputActionType.PassThrough,
                TouchPositionBinding,
                expectedControlType: TouchPositionControlType);
        }
    }
}
