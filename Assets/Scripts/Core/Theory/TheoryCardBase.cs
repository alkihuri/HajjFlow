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
        [SerializeField] private float _maxSwipeTime = 0.5f;

        [Header("Animation Settings")]
        [SerializeField] private float _swipeAnimationDuration = 0.2f;
        [SerializeField] private float _swipeDistanceMultiplier = 1.5f;
        [SerializeField] private float _rotationAngle = 15f;
        [SerializeField] private float _minScale = 0.3f;
        [SerializeField] private Ease _swipeEase = Ease.OutQuad;
        
        [Header("Base UI")]
        [SerializeField] private TextMeshProUGUI _title;
        [SerializeField] private TextMeshProUGUI _description;
        [SerializeField] private Image _image;
        
        /// <summary>
        /// Индекс карточки в колоде (0 = первая/верхняя)
        /// </summary>
        public int CardIndex { get; set; }
        
        /// <summary>
        /// Активна ли карточка (верхняя в колоде). Используется вместо gameObject.activeSelf для пула.
        /// </summary>
        public bool IsActive { get; private set; } = false;
        
        public event System.Action SwipeLeft;

        // Swipe logic
        private Vector2 _startTouchPosition;
        private float _startTouchTime;
        private bool _trackingSwipe;
        private RectTransform _rectTransform;
        private Vector2 _originalAnchoredPosition;
        private Vector3 _originalScale;
        private Quaternion _originalRotation;
        private bool _isAnimating;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            if (_rectTransform != null)
            {
                _originalAnchoredPosition = _rectTransform.anchoredPosition;
                _originalRotation = _rectTransform.localRotation;
                _originalScale = _rectTransform.localScale;
            }
        }

        public void Initialize(TheoryCardData data)
        {
            Data = data;   
            if (_title != null) _title.text = Data.Title;
            if (_description != null) _description.text = Data.Description;
            if (_image != null)
            {
                _image.sprite = Data?.Image;
                if (_image.sprite == null)
                {
                    _image.gameObject.SetActive(false); 
                }
            }
            
            ResetCardState();
        }

        /// <summary>
        /// Сбрасывает состояние карточки к исходному
        /// </summary>
        public void ResetCardState()
        {
            if (_rectTransform != null)
            {
                _rectTransform.anchoredPosition = _originalAnchoredPosition;
                _rectTransform.localRotation = _originalRotation;
                _rectTransform.localScale = _originalScale;
            }
            _isAnimating = false;
            _trackingSwipe = false;
        }

        private void Update()
        {
            // Обрабатываем ввод только если карточка активна (верхняя в колоде)
            if (!IsActive) return;
            
            HandleSwipeInput();
            
            // Visual feedback during drag - только влево
            if (_trackingSwipe && _rectTransform != null && !_isAnimating)
            {
                UpdateDragVisuals();
            }
        }

        private void UpdateDragVisuals()
        {
            Vector2 currentPos = GetCurrentInputPosition();
            if (currentPos == Vector2.zero) return;

            Vector2 delta = currentPos - _startTouchPosition;
            
            // Только влево - ограничиваем движение
            float xOffset = Mathf.Min(0, delta.x * 0.5f); // Только отрицательные значения (влево)
            
            // Move card only horizontally to the left
            _rectTransform.anchoredPosition = _originalAnchoredPosition + new Vector2(xOffset, 0);
            
            // Rotate based on horizontal movement (only when moving left)
            float rotation = (xOffset / Screen.width) * _rotationAngle * 2f;
            _rectTransform.localRotation = Quaternion.Euler(0, 0, -rotation);
            
            // Scale down based on distance (instead of fade)
            float distance = Mathf.Abs(xOffset);
            float scaleProgress = Mathf.Clamp01(distance / (_minSwipeDistance * 2f));
            float currentScale = Mathf.Lerp(1f, _minScale, scaleProgress);
            _rectTransform.localScale = _originalScale * currentScale;
        }

        private Vector2 GetCurrentInputPosition()
        {
#if ENABLE_INPUT_SYSTEM
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            {
                return Touchscreen.current.primaryTouch.position.ReadValue();
            }
            if (Mouse.current != null && Mouse.current.leftButton.isPressed)
            {
                return Mouse.current.position.ReadValue();
            }
#else
            if (Input.touchCount > 0)
            {
                return Input.GetTouch(0).position;
            }
            if (Input.GetMouseButton(0))
            {
                return Input.mousePosition;
            }
#endif
            return Vector2.zero;
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
            if (_isAnimating) return;
                
            _trackingSwipe = true;
            _startTouchPosition = position;
            _startTouchTime = Time.time;
        }

        private void EndSwipe(Vector2 position)
        {
            if (!_trackingSwipe) return;

            _trackingSwipe = false;

            float elapsed = Time.time - _startTouchTime;
            Vector2 delta = position - _startTouchPosition;
            
            // Проверяем свайп влево (delta.x < 0)
            bool isValidSwipeLeft = elapsed <= _maxSwipeTime 
                                    && delta.x < -_minSwipeDistance 
                                    && Mathf.Abs(delta.x) > Mathf.Abs(delta.y); // Горизонтальный свайп

            if (isValidSwipeLeft)
            {
                AnimateSwipeOut();
                SwipeLeft?.Invoke();
            }
            else
            {
                // Return to original position
                ReturnToOriginalPosition();
            }
        }
        
        private void AnimateSwipeOut()
        {
            if (_rectTransform == null) return;

            _isAnimating = true;
            
            Vector2 targetPosition = _originalAnchoredPosition + Vector2.left * Screen.width * _swipeDistanceMultiplier;
            float targetRotation = -_rotationAngle;
            
            // Animate: move left, rotate, scale down
            Sequence swipeSequence = DOTween.Sequence();
            swipeSequence.Append(_rectTransform.DOAnchorPos(targetPosition, _swipeAnimationDuration).SetEase(_swipeEase));
            swipeSequence.Join(_rectTransform.DOLocalRotate(new Vector3(0, 0, targetRotation), _swipeAnimationDuration).SetEase(_swipeEase));
            swipeSequence.Join(_rectTransform.DOScale(_originalScale * _minScale, _swipeAnimationDuration).SetEase(_swipeEase));
            
            swipeSequence.OnComplete(() =>
            {
                _isAnimating = false;
                IsActive = false;
            });
        }
        
        private void ReturnToOriginalPosition()
        {
            if (_rectTransform == null) return;

            _isAnimating = true;
            
            Sequence returnSequence = DOTween.Sequence();
            returnSequence.Append(_rectTransform.DOAnchorPos(_originalAnchoredPosition, _swipeAnimationDuration * 0.5f).SetEase(Ease.OutBack));
            returnSequence.Join(_rectTransform.DOLocalRotate(Vector3.zero, _swipeAnimationDuration * 0.5f).SetEase(Ease.OutBack));
            returnSequence.Join(_rectTransform.DOScale(_originalScale, _swipeAnimationDuration * 0.5f).SetEase(Ease.OutBack));
            
            returnSequence.OnComplete(() => _isAnimating = false);
        }

        /// <summary>
        /// Устанавливает карточку как активную (верхнюю в колоде)
        /// </summary>
        public void SetAsActiveCard()
        {
            IsActive = true;
            // НЕ сбрасываем позицию - она уже установлена в SetStackPosition
            _isAnimating = false;
            _trackingSwipe = false;
            transform.SetAsLastSibling();
        }

        /// <summary>
        /// Устанавливает карточку как неактивную (в колоде, но не верхняя)
        /// </summary>
        public void SetAsInactiveCard()
        {
            IsActive = false;
            _isAnimating = false;
            _trackingSwipe = false;
        }

        /// <summary>
        /// Устанавливает позицию карточки в колоде (смещение для визуализации стопки)
        /// </summary>
        public void SetStackPosition(int indexFromTop, float offsetX = 8f, float offsetY = -8f)
        {
            if (_rectTransform == null) return;
            
            Vector2 stackOffset = new Vector2(indexFromTop * offsetX, indexFromTop * offsetY);
            _rectTransform.anchoredPosition = _originalAnchoredPosition + stackOffset;
            _rectTransform.localRotation = _originalRotation;
            
            float scaleFactor = 1f - (indexFromTop * 0.03f);
            scaleFactor = Mathf.Max(scaleFactor, 0.85f);
            _rectTransform.localScale = _originalScale * scaleFactor;
        }

        /// <summary>
        /// Плавно анимирует карточку на новую позицию в стеке
        /// </summary>
        public void AnimateToStackPosition(int indexFromTop, float offsetX, float offsetY, float duration)
        {
            if (_rectTransform == null) return;
            
            Vector2 targetPosition = _originalAnchoredPosition + new Vector2(indexFromTop * offsetX, indexFromTop * offsetY);
            
            float scaleFactor = 1f - (indexFromTop * 0.03f);
            scaleFactor = Mathf.Max(scaleFactor, 0.85f);
            Vector3 targetScale = _originalScale * scaleFactor;
            
            // Анимируем позицию и масштаб
            _rectTransform.DOAnchorPos(targetPosition, duration).SetEase(Ease.OutCubic);
            _rectTransform.DOScale(targetScale, duration).SetEase(Ease.OutCubic);
            _rectTransform.DOLocalRotate(Vector3.zero, duration).SetEase(Ease.OutCubic);
        }
    }
}
