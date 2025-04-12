using System;
using System.Runtime.CompilerServices;
using Unity.TinyCharacterController.Check;
using Unity.TinyCharacterController.Control;
using Unity.TinyCharacterController.Core;
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
        private readonly Lazy<AnimHashConstants> _constants = new (static () => new AnimHashConstants());

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
        private new ITransform _transform;
        private IWarp _warp;

        public ref readonly Animator Animator => ref _animator;
        public ref readonly PlayerInput PlayerInput => ref _playerInput;
        public ref readonly MoveControl MoveControl => ref _moveControl;
        public ref readonly JumpControl JumpControl => ref _jumpControl;
        public ref readonly GroundCheck GroundCheck => ref _groundCheck;
        public ref readonly TpsCameraControl CameraControl => ref _cameraControl;
        public ref readonly ITransform Transform => ref _transform;
        public ref readonly IWarp Warp => ref _warp;

        /// <summary>
        /// The event that is triggered when the player jumps.
        /// </summary>
        public ref readonly UnityEngine.Events.UnityEvent OnJumped => ref JumpControl.OnJump;

        /// <summary>
        /// The constants for the animation hash.
        /// </summary>
        public AnimHashConstants Constants => _constants.Value;

        /// <summary>
        /// Whether the player is performing a double jump.
        /// </summary>
        public bool IsDoubleJump
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => JumpControl.AerialJumpCount >= 1;
        }

        /// <summary>
        /// The current speed of the player.
        /// </summary>
        public float CurrentSpeed
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => MoveControl.CurrentSpeed;
        }

        /// <summary>
        /// Whether the player is on the ground.
        /// </summary>
        public bool IsOnGround
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => GroundCheck.IsOnGround;
        }

        /// <summary>
        /// The local direction of the player.
        /// </summary>
        public Vector3 LocalDirection
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => MoveControl.LocalDirection;
        }

        /// <summary>
        /// The world position of the player.
        /// </summary>
        public Vector3 WorldPosition
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Transform.Position;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => Warp.Warp(value);
        }

        /// <summary>
        /// The world rotation of the player.
        /// </summary>
        public Quaternion WorldRotation
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Transform.Rotation;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => Warp.Warp(value);
        }

        /// <summary>
        /// Whether the player can move.
        /// <remarks>
        /// Jumping and looking around are still allowed.
        /// </remarks>
        /// </summary>
        public bool UnlockMove { get; set; } = true;

        /// <summary>
        /// "Move" action name.
        /// </summary>
        public const string Move = nameof(Move);

        /// <summary>
        /// "Sprint" action name.
        /// </summary>
        public const string Sprint = nameof(Sprint);

        /// <summary>
        /// "Jump" action name.
        /// </summary>
        public const string Jump = nameof(Jump);

        /// <summary>
        /// "Look" action name.
        /// </summary>
        public const string Look = nameof(Look);

        /// <summary>
        /// The damper time for the move animation.
        /// </summary>
        public const float MoveDampTime = 0.1f;

        protected virtual void Start()
        {
            if (TryGetComponent(out BrainBase brain))
            {
                (_transform, _warp) = (brain, brain);
            }
            else
            {
                TryGetComponent(out _transform);
                TryGetComponent(out _warp);
            }
            PlayerInput.onActionTriggered += context => OnActionTriggered(context);
            JumpControl.OnJump.AddListener(OnJump);
        }

        protected virtual void Update()
        {
            Animator.SetFloat(Constants.Speed, CurrentSpeed);
            Animator.SetBool(Constants.IsGround, IsOnGround);

            Vector3 currentDirection = LocalDirection;
            float deltaTime = Time.deltaTime;
            Animator.SetFloat(Constants.Forward, currentDirection.z, MoveDampTime, deltaTime);
            Animator.SetFloat(Constants.SideStep, currentDirection.x, MoveDampTime, deltaTime);
        }

        protected virtual void OnActionTriggered(in CallbackContext context)
        {
            switch (context.ActionName)
            {
                case Move when context.Phase is InputActionPhase.Performed or InputActionPhase.Canceled && UnlockMove:
                    MoveControl.Move(context.Value);
                    break;
                case Sprint when context.Phase is InputActionPhase.Performed:
                    const float sprintHoldSpeed = 4.0f;
                    MoveControl.MoveSpeed = sprintHoldSpeed;
                    break;
                case Sprint when context.Phase is InputActionPhase.Canceled:
                    const float sprintReleasedSpeed = 1.2f;
                    MoveControl.MoveSpeed = sprintReleasedSpeed;
                    break;
                case Jump when context.Phase is InputActionPhase.Started:
                    JumpControl.Jump();
                    break;
                case Look when context.Phase is InputActionPhase.Performed:
                    CameraControl.RotateCamera(context.Value);
                    break;
            }
        }

        protected virtual void OnJump() => Animator.Play(IsDoubleJump ? Constants.DoubleJump : Constants.JumpStart);
    }
}
