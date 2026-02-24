using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Core.Theory
{
    public enum SwipeDirection
    {
        Left,
        Right,
        Up,
        Down
    }

    public abstract class TheoryCardBase : MonoBehaviour
    {
        [SerializeField] private  TheoryCardData _data;

        [Header("Swipe Settings")]
        [SerializeField] private float _minSwipeDistance = 60f;
        [SerializeField] private float _maxSwipeTime = 0.4f;

        
        [Header(("Base UI"))]
        [SerializeField] private TextMeshProUGUI _title;
            [SerializeField] private TextMeshProUGUI _description;
            [SerializeField] private Image _image;
        
        
        public event System.Action<SwipeDirection> SwipeDetected;
        public event System.Action SwipeLeft;
        public event System.Action SwipeRight;
        public event System.Action SwipeUp;
        public event System.Action SwipeDown;

        // swype logic for card navigation
        private Vector2 _startTouchPosition;
        private Vector2 _endTouchPosition;
        private float _startTouchTime;
        private bool _trackingSwipe;

        public void Initialize(TheoryCardData data)
        {
            _data = data;   
            _title.text = _data.Title;
            _description.text = _data.Description;
            _image.sprite = _data?.Image;
        }

        private void Update()
        {
            HandleSwipeInput();
        }

        private void HandleSwipeInput()
        {
#if ENABLE_INPUT_SYSTEM
            if (Touchscreen.current != null)
            {
                var touch = Touchscreen.current.primaryTouch;
                if (touch.press.wasPressedThisFrame)
                {
                    BeginSwipe(touch.position.ReadValue());
                }
                else if (touch.press.wasReleasedThisFrame)
                {
                    EndSwipe(touch.position.ReadValue());
                }
                return;
            }

            if (Mouse.current != null)
            {
                if (Mouse.current.leftButton.wasPressedThisFrame)
                {
                    BeginSwipe(Mouse.current.position.ReadValue());
                }
                else if (Mouse.current.leftButton.wasReleasedThisFrame)
                {
                    EndSwipe(Mouse.current.position.ReadValue());
                }
            }
#else
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                switch (touch.phase)
                {
                    case TouchPhase.Began:
                        BeginSwipe(touch.position);
                        break;
                    case TouchPhase.Ended:
                    case TouchPhase.Canceled:
                        EndSwipe(touch.position);
                        break;
                }
                return;
            }

            if (Input.GetMouseButtonDown(0))
            {
                BeginSwipe(Input.mousePosition);
            }
            else if (Input.GetMouseButtonUp(0))
            {
                EndSwipe(Input.mousePosition);
            }
#endif
        }

        private void BeginSwipe(Vector2 position)
        {
            _trackingSwipe = true;
            _startTouchPosition = position;
            _startTouchTime = Time.time;
        }

        private void EndSwipe(Vector2 position)
        {
            if (!_trackingSwipe)
            {
                return;
            }

            _trackingSwipe = false;
            _endTouchPosition = position;

            float elapsed = Time.time - _startTouchTime;
            if (elapsed > _maxSwipeTime)
            {
                return;
            }

            Vector2 delta = _endTouchPosition - _startTouchPosition;
            if (delta.magnitude < _minSwipeDistance)
            {
                return;
            }

            SwipeDirection direction = Mathf.Abs(delta.x) > Mathf.Abs(delta.y)
                ? (delta.x > 0 ? SwipeDirection.Right : SwipeDirection.Left)
                : (delta.y > 0 ? SwipeDirection.Up : SwipeDirection.Down);

            SwipeDetected?.Invoke(direction);
            switch (direction)
            {
                case SwipeDirection.Left:
                    SwipeLeft?.Invoke();
                    break;
                case SwipeDirection.Right:
                    SwipeRight?.Invoke();
                    break;
                case SwipeDirection.Up:
                    SwipeUp?.Invoke();
                    break;
                case SwipeDirection.Down:
                    SwipeDown?.Invoke();
                    break;
            }
        }
    }
}
