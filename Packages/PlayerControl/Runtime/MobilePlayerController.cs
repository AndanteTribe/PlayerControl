using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.InputSystem.Processors;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace PlayerControl
{
    public class MobilePlayerController : PlayerController
    {
        [SerializeField]
        private MobileControlUIView _uiView;

        [Header("Processors")]
        [SerializeReference]
        private InvertVector2Processor _invertProcessor = new () { invertX = false, invertY = true };

        [SerializeReference]
        private ScaleVector2Processor _scaleProcessor = new () { x = 15.0f, y = 15.0f };

        [Header("Debug Options")]
        [SerializeField]
        [Tooltip("If true, the look action will be triggered when the mouse changes direction.")]
        private bool _isDebug = false;

        /// <summary>
        /// Operation UI for Mobile.
        /// </summary>
        public virtual MobileControlUIView UIView
        {
            get => _uiView;
            set
            {
                _uiView = value;
                _uiView.Joystick.OnValueChanged += value =>
                {
                    base.OnActionTriggered(new CallbackContext(Move, InputActionPhase.Performed, value));
                };
                _uiView.SprintButton.OnStart += () =>
                {
                    base.OnActionTriggered(new CallbackContext(Sprint, InputActionPhase.Performed));
                };
                _uiView.SprintButton.OnRelease += () =>
                {
                    base.OnActionTriggered(new CallbackContext(Sprint, InputActionPhase.Canceled));
                };
                _uiView.JumpButton.onClick.AddListener(() =>
                {
                    base.OnActionTriggered(new CallbackContext(Jump, InputActionPhase.Started));
                });
            }
        }

        protected virtual void OnEnable() => EnhancedTouchSupport.Enable();

        protected virtual void OnDisable() => EnhancedTouchSupport.Disable();

        protected override void Start()
        {
            base.Start();
            if (_uiView != null)
            {
                UIView = _uiView;
            }
        }

        protected override void Update()
        {
            base.Update();
            var activeTouches = Touch.activeTouches;
            ReadOnlySpan<int> avoidTouchIds = (_uiView.Joystick.IsUsing, _uiView.SprintButton.IsUsing) switch
            {
                (true, true) => stackalloc int[] { _uiView.Joystick.TouchId, _uiView.SprintButton.TouchId },
                (true, false) => stackalloc int[] { _uiView.Joystick.TouchId },
                (false, true) => stackalloc int[] { _uiView.SprintButton.TouchId },
                _ => ReadOnlySpan<int>.Empty
            };
            if (activeTouches.Count <= avoidTouchIds.Length) return;
            foreach (Touch touch in activeTouches)
            {
                if (avoidTouchIds.IndexOf(touch.touchId) == -1)
                {
                    Vector2 delta = touch.delta;
                    delta = _invertProcessor.Process(delta, null);
                    delta = _scaleProcessor.Process(delta, null);
                    base.OnActionTriggered(new CallbackContext(Look, InputActionPhase.Performed, delta));
                    break;
                }
            }
        }

        protected override void OnActionTriggered(in CallbackContext context)
        {
            if (context.CompareActionName(Look))
            {
#if UNITY_EDITOR
                if (!_isDebug)
                {
                    return;
                }
#else
                return;
#endif
            }
            base.OnActionTriggered(context);
        }
    }
}