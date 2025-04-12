using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace PlayerControl
{
    [RequireComponent(typeof(RectTransform))]
    public class MinimumVirtualJoyStick : MonoBehaviour, IPointerDownHandler
    {
        [SerializeField, Min(0)]
        private float _movementRange = 50;

        [SerializeField]
        private RectTransform _handle;

        [SerializeField]
        private RectTransform _background;

        private Vector3 _startPos;
        private Vector2 _pointerDownPos;
        private Camera _pressEventCamera;

        /// <summary>
        /// The ID of the touch that is currently using the control.
        /// </summary>
        public int TouchId { get; private set; }

        /// <summary>
        /// Whether the control is currently being used.
        /// </summary>
        public bool IsUsing { get; private set; } = false;

        /// <summary>
        /// Callback executed when the value of the control changes.
        /// </summary>
        public event Action<Vector2> OnValueChanged;

        /// <summary>
        /// The distance from the onscreen control's center of origin, around which the control can move.
        /// </summary>
        public float MovementRange => _movementRange;

        /// <inheritdoc />
        void IPointerDownHandler.OnPointerDown(PointerEventData eventData)
        {
            if (eventData == null)
            {
                throw new ArgumentNullException(nameof(eventData));
            }

            IsUsing = true;
            _pressEventCamera = eventData.pressEventCamera;

            Vector2 screenPoint = eventData.position;
            if (TouchUtility.TryGetApproximatelyActiveTouch(screenPoint, out Touch touch))
            {
                screenPoint = touch.screenPosition;
                TouchId = touch.touchId;
            }

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _background, screenPoint, _pressEventCamera, out _pointerDownPos);

            Touch.onFingerMove += OnFingerMove;

            Touch.onFingerUp += OnFingerUp;

            void OnFingerMove(Finger finger)
            {
                if (finger.currentTouch.touchId == TouchId)
                {
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        _background, finger.screenPosition, _pressEventCamera, out Vector2 position);

                    Vector2 delta = Vector2.ClampMagnitude(position - _pointerDownPos, MovementRange);
                    _handle.anchoredPosition = (Vector2)_startPos + delta;
                    OnValueChanged?.Invoke(delta / MovementRange);
                }
            }

            void OnFingerUp(Finger finger)
            {
                if (finger.currentTouch.touchId == TouchId)
                {
                    _handle.anchoredPosition = _pointerDownPos = _startPos;
                    OnValueChanged?.Invoke(Vector2.zero);
                    IsUsing = false;
                    Touch.onFingerMove -= OnFingerMove;
                    Touch.onFingerUp -= OnFingerUp;
                }
            }
        }

        private void Start() => _startPos = _handle.anchoredPosition;

        private void OnEnable() => EnhancedTouchSupport.Enable();

        private void OnDisable() => EnhancedTouchSupport.Disable();
    }
}