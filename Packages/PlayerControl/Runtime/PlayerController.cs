using System.Runtime.CompilerServices;
using Unity.TinyCharacterController.Check;
using Unity.TinyCharacterController.Control;
using Unity.TinyCharacterController.Interfaces.Components;
using Unity.TinyCharacterController.Interfaces.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PlayerControl
{
    /// <summary>
    /// Player controller.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(PlayerInput))]
    [RequireComponent(typeof(ITransform))]
    [RequireComponent(typeof(IWarp))]
    [RequireComponent(typeof(MoveControl))]
    [RequireComponent(typeof(JumpControl))]
    [RequireComponent(typeof(GroundCheck))]
    [RequireComponent(typeof(TpsCameraControl))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField]
        private Animator _animator;
        [SerializeField]
        private PlayerInput _playerInput;
        [SerializeField]
        private MoveControl _moveControl;
        [SerializeField]
        private JumpControl _jumpControl;
        [SerializeField]
        private GroundCheck _groundCheck;
        [SerializeField]
        private TpsCameraControl _cameraControl;
        private ITransform _transform;
        private IWarp _warp;

        /// <summary>
        /// <see cref="Animator"/> component of the player.
        /// </summary>
        public Animator Animator => _animator;

        /// <summary>
        /// <see cref="PlayerInput"/> component of the player.
        /// </summary>
        public PlayerInput PlayerInput => _playerInput;

        /// <summary>
        /// The speed of the player when moving normally.
        /// </summary>
        public float DefaultSpeed { get; set; } = 1.2f;
        /// <summary>
        /// The speed of the player when sprinting.
        /// </summary>
        public float SprintSpeed { get; set; } = 4.0f;

        /// <summary>
        /// Whether the player can perform a double jump.
        /// </summary>
        public bool CanDoubleJump { get; set; } = true;

        /// <summary>
        /// Whether the player is performing a double jump.
        /// </summary>
        public bool IsDoubleJump
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => CanDoubleJump && _jumpControl.AerialJumpCount >= 1;
        }

        /// <summary>
        /// The current speed of the player.
        /// </summary>
        public float CurrentSpeed
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _moveControl.CurrentSpeed;
        }

        /// <summary>
        /// Whether the player is on the ground.
        /// </summary>
        public bool IsOnGround
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _groundCheck.IsOnGround;
        }

        /// <summary>
        /// The local direction of the player.
        /// </summary>
        public Vector3 LocalDirection
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _moveControl.LocalDirection;
        }

        /// <summary>
        /// The world position of the player.
        /// </summary>
        public Vector3 WorldPosition
        {
            get => (_transform ??= GetComponent<ITransform>()).Position;
            set => (_warp ??= GetComponent<IWarp>()).Warp(value);
        }

        /// <summary>
        /// The world rotation of the player.
        /// </summary>
        public Quaternion WorldRotation
        {
            get => (_transform ??= GetComponent<ITransform>()).Rotation;
            set => (_warp ??= GetComponent<IWarp>()).Warp(value);
        }

        private void Start()
        {
            _playerInput.onActionTriggered += OnActionTriggered;
            _jumpControl.OnJump.AddListener(OnJump);
        }

#if UNITY_ANDROID
        private void OnEnable() => UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.Enable();

        private void OnDisable() => UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.Disable();
#endif

        private void Update()
        {
            _animator.SetFloat(Constants.Hash.Speed, CurrentSpeed);
            _animator.SetBool(Constants.Hash.IsGround, IsOnGround);

            var currentDirection = LocalDirection;
            var deltaTime = Time.deltaTime;
            const float dampTime = 0.1f;

            _animator.SetFloat(Constants.Hash.Forward, currentDirection.z, dampTime, deltaTime);
            _animator.SetFloat(Constants.Hash.SideStep, currentDirection.x, dampTime, deltaTime);
        }

        private void OnActionTriggered(InputAction.CallbackContext context)
        {
            switch (context.action.name)
            {
                case Constants.Action.Move when context.phase is InputActionPhase.Performed or InputActionPhase.Canceled:
                    _moveControl.Move(context.ReadValue<Vector2>());
                    break;
                case Constants.Action.Look when context.phase is InputActionPhase.Performed:
                    _cameraControl.RotateCamera(context.ReadValue<Vector2>());
                    break;
                case Constants.Action.Jump when context.phase is InputActionPhase.Started:
                    _jumpControl.Jump();
                    break;
                case Constants.Action.Sprint when context.phase is InputActionPhase.Performed:
                    _moveControl.MoveSpeed = SprintSpeed;
                    break;
                case Constants.Action.Sprint when context.phase is InputActionPhase.Canceled:
                    _moveControl.MoveSpeed = DefaultSpeed;
                    break;
            }
        }

        private void OnJump() => _animator.Play(IsDoubleJump ? Constants.Hash.DoubleJump : Constants.Hash.JumpStart);
    }
}