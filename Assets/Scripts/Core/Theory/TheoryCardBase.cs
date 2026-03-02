using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
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
        [field: SerializeField] public TheoryCardData Data { get; private set; }

        [Header("Swipe Settings")]
        [SerializeField] private float _minSwipeDistance = 60f;
        [SerializeField] private float _maxSwipeTime = 0.4f;

        [Header("Animation Settings")]
        [SerializeField] private float _swipeAnimationDuration = 0.3f;
        [SerializeField] private float _swipeDistanceMultiplier = 1.5f;
        [SerializeField] private float _rotationAngle = 15f;
        [SerializeField] private Ease _swipeEase = Ease.OutQuad;
        
        [Header(("Base UI"))]
        [SerializeField] private TextMeshProUGUI _title;
        [SerializeField] private TextMeshProUGUI _description;
        [SerializeField] private Image _image;
        [SerializeField] private CanvasGroup _canvasGroup;
        
        
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
        private RectTransform _rectTransform;
        private Vector3 _originalPosition;
        private Quaternion _originalRotation;
        private bool _isAnimating;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            if (_rectTransform != null)
            {
                _originalPosition = _rectTransform.localPosition;
                _originalRotation = _rectTransform.localRotation;
            }

            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
                if (_canvasGroup == null)
                {
                    _canvasGroup = gameObject.AddComponent<CanvasGroup>();
                }
            }
        }

        public void Initialize(TheoryCardData data)
        {
            Data = data;   
            _title.text = Data.Title;
            _description.text = Data.Description;
            _image.sprite = Data?.Image;

            if (_image.sprite == null)
            {
                _image.gameObject.SetActive(false); 
            }
            
            // Reset position and alpha
            if (_rectTransform != null)
            {
                _rectTransform.localPosition = _originalPosition;
                _rectTransform.localRotation = _originalRotation;
            }
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
            }
            _isAnimating = false;
        }

        private void Update()
        {
            HandleSwipeInput();
            
            // Visual feedback during drag
            if (_trackingSwipe && _rectTransform != null && !_isAnimating)
            {
                UpdateDragVisuals();
            }
        }

        private void UpdateDragVisuals()
        {
#if ENABLE_INPUT_SYSTEM
            Vector2 currentPos = Vector2.zero;
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            {
                currentPos = Touchscreen.current.primaryTouch.position.ReadValue();
            }
            else if (Mouse.current != null && Mouse.current.leftButton.isPressed)
            {
                currentPos = Mouse.current.position.ReadValue();
            }
            else
            {
                return;
            }
#else
            Vector2 currentPos = Vector2.zero;
            if (Input.touchCount > 0)
            {
                currentPos = Input.GetTouch(0).position;
            }
            else if (Input.GetMouseButton(0))
            {
                currentPos = Input.mousePosition;
            }
            else
            {
                return;
            }
#endif

            Vector2 delta = currentPos - _startTouchPosition;
            
            // Move card
            _rectTransform.localPosition = _originalPosition + new Vector3(delta.x * 0.5f, delta.y * 0.5f, 0);
            
            // Rotate based on horizontal movement
            float rotation = (delta.x / Screen.width) * _rotationAngle;
            _rectTransform.localRotation = Quaternion.Euler(0, 0, -rotation);
            
            // Fade based on distance
            float distance = delta.magnitude;
            float alpha = 1f - Mathf.Clamp01(distance / (_minSwipeDistance * 3f));
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = alpha;
            }
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
            if (_isAnimating)
                return;
                
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
            Vector2 delta = _endTouchPosition - _startTouchPosition;
            
            // Check if swipe is valid
            bool isValidSwipe = elapsed <= _maxSwipeTime && delta.magnitude >= _minSwipeDistance;
            
            if (isValidSwipe)
            {
                SwipeDirection direction = Mathf.Abs(delta.x) > Mathf.Abs(delta.y)
                    ? (delta.x > 0 ? SwipeDirection.Right : SwipeDirection.Left)
                    : (delta.y > 0 ? SwipeDirection.Up : SwipeDirection.Down);

                AnimateSwipe(direction);
                
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
            else
            {
                // Return to original position
                ReturnToOriginalPosition();
            }
        }
        
        private void AnimateSwipe(SwipeDirection direction)
        {
            if (_rectTransform == null)
                return;

            _isAnimating = true;
            
            Vector3 targetPosition = _originalPosition;
            float targetRotation = 0f;
            
            switch (direction)
            {
                case SwipeDirection.Left:
                    targetPosition += Vector3.left * Screen.width * _swipeDistanceMultiplier;
                    targetRotation = -_rotationAngle;
                    break;
                case SwipeDirection.Right:
                    targetPosition += Vector3.right * Screen.width * _swipeDistanceMultiplier;
                    targetRotation = _rotationAngle;
                    break;
                case SwipeDirection.Up:
                    targetPosition += Vector3.up * Screen.height * _swipeDistanceMultiplier;
                    break;
                case SwipeDirection.Down:
                    targetPosition += Vector3.down * Screen.height * _swipeDistanceMultiplier;
                    break;
            }
            
            // Animate position, rotation and fade
            Sequence swipeSequence = DOTween.Sequence();
            swipeSequence.Append(_rectTransform.DOLocalMove(targetPosition, _swipeAnimationDuration).SetEase(_swipeEase));
            swipeSequence.Join(_rectTransform.DOLocalRotate(new Vector3(0, 0, targetRotation), _swipeAnimationDuration).SetEase(_swipeEase));
            
            if (_canvasGroup != null)
            {
                swipeSequence.Join(_canvasGroup.DOFade(0f, _swipeAnimationDuration).SetEase(_swipeEase));
            }
            
            swipeSequence.OnComplete(() =>
            {
                _isAnimating = false;
                gameObject.SetActive(false);
            });
        }
        
        private void ReturnToOriginalPosition()
        {
            if (_rectTransform == null)
                return;

            _isAnimating = true;
            
            Sequence returnSequence = DOTween.Sequence();
            returnSequence.Append(_rectTransform.DOLocalMove(_originalPosition, _swipeAnimationDuration * 0.5f).SetEase(Ease.OutBack));
            returnSequence.Join(_rectTransform.DOLocalRotate(Vector3.zero, _swipeAnimationDuration * 0.5f).SetEase(Ease.OutBack));
            
            if (_canvasGroup != null)
            {
                returnSequence.Join(_canvasGroup.DOFade(1f, _swipeAnimationDuration * 0.5f).SetEase(Ease.OutQuad));
            }
            
            returnSequence.OnComplete(() => _isAnimating = false);
        }

        public void Show()
        { 
            _canvasGroup.DOFade(1f, _swipeAnimationDuration);
            transform.DOLocalMove(_originalPosition, _swipeAnimationDuration);
            gameObject.SetActive(true); 
        }
    }
}
