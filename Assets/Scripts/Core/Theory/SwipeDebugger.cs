using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Core.Theory
{
    /// <summary>
    /// Visual debugger for swipe detection
    /// Shows swipe direction, distance, and timing in real-time
    /// </summary>
    public class SwipeDebugger : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TheoryCardBase _card;
        
        [Header("Debug UI")]
        [SerializeField] private TextMeshProUGUI _debugText;
        [SerializeField] private Image _directionIndicator;
        [SerializeField] private LineRenderer _swipePathRenderer;
        [SerializeField] private bool _showDebugInfo = true;
        
        [Header("Colors")]
        [SerializeField] private Color _leftColor = Color.red;
        [SerializeField] private Color _rightColor = Color.green;
        [SerializeField] private Color _upColor = Color.blue;
        [SerializeField] private Color _downColor = Color.yellow;
        
        private Vector2 _startPosition;
        private float _startTime;
        private bool _isTracking;
        
        private void OnEnable()
        {
            if (_card != null)
            {
                _card.SwipeDetected += OnSwipeDetected;
            }
        }
        
        private void OnDisable()
        {
            if (_card != null)
            {
                _card.SwipeDetected -= OnSwipeDetected;
            }
        }
        
        private void Update()
        {
            if (!_showDebugInfo) return;
            
            UpdateDebugInfo();
        }
        
        private void UpdateDebugInfo()
        {
            if (_debugText == null) return;
            
            // Get current input position
            Vector2 currentPos = GetCurrentInputPosition();
            
            if (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
            {
                _startPosition = currentPos;
                _startTime = Time.time;
                _isTracking = true;
            }
            
            if (_isTracking && (Input.GetMouseButton(0) || Input.touchCount > 0))
            {
                Vector2 delta = currentPos - _startPosition;
                float distance = delta.magnitude;
                float time = Time.time - _startTime;
                float speed = distance / (time + 0.001f);
                
                string direction = GetDirectionString(delta);
                
                _debugText.text = $"<b>SWIPE DEBUG</b>\n" +
                                 $"Direction: <color=yellow>{direction}</color>\n" +
                                 $"Distance: <color=cyan>{distance:F1}</color> px\n" +
                                 $"Time: <color=cyan>{time:F2}</color> sec\n" +
                                 $"Speed: <color=cyan>{speed:F1}</color> px/s\n" +
                                 $"Start: ({_startPosition.x:F0}, {_startPosition.y:F0})\n" +
                                 $"Current: ({currentPos.x:F0}, {currentPos.y:F0})\n" +
                                 $"Delta: ({delta.x:F0}, {delta.y:F0})";
                
                // Draw swipe path
                DrawSwipePath(_startPosition, currentPos);
            }
            
            if (Input.GetMouseButtonUp(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Ended))
            {
                _isTracking = false;
                ClearSwipePath();
            }
        }
        
        private Vector2 GetCurrentInputPosition()
        {
            if (Input.touchCount > 0)
            {
                return Input.GetTouch(0).position;
            }
            return Input.mousePosition;
        }
        
        private string GetDirectionString(Vector2 delta)
        {
            if (delta.magnitude < 10f) return "None";
            
            if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
            {
                return delta.x > 0 ? "→ RIGHT" : "← LEFT";
            }
            else
            {
                return delta.y > 0 ? "↑ UP" : "↓ DOWN";
            }
        }
        
        private void OnSwipeDetected(SwipeDirection direction)
        {
            if (!_showDebugInfo) return;
            
            UnityEngine.Debug.Log($"[SwipeDebugger] Swipe detected: {direction}");
            
            // Flash direction indicator
            if (_directionIndicator != null)
            {
                Color color = GetDirectionColor(direction);
                _directionIndicator.color = color;
                
                // You can add DOTween here for smooth fade
                // _directionIndicator.DOColor(new Color(color.r, color.g, color.b, 0), 0.5f);
            }
            
            // Update debug text
            if (_debugText != null)
            {
                _debugText.text = $"<b><color=green>✓ SWIPE DETECTED!</color></b>\n" +
                                 $"Direction: <color=yellow>{direction}</color>\n" +
                                 $"<size=24>Swipe again to test...</size>";
            }
        }
        
        private Color GetDirectionColor(SwipeDirection direction)
        {
            switch (direction)
            {
                case SwipeDirection.Left: return _leftColor;
                case SwipeDirection.Right: return _rightColor;
                case SwipeDirection.Up: return _upColor;
                case SwipeDirection.Down: return _downColor;
                default: return Color.white;
            }
        }
        
        private void DrawSwipePath(Vector2 start, Vector2 end)
        {
            if (_swipePathRenderer == null) return;
            
            _swipePathRenderer.enabled = true;
            _swipePathRenderer.positionCount = 2;
            
            // Convert screen space to world space
            Camera cam = Camera.main;
            if (cam == null) return;
            
            Vector3 startWorld = cam.ScreenToWorldPoint(new Vector3(start.x, start.y, 10));
            Vector3 endWorld = cam.ScreenToWorldPoint(new Vector3(end.x, end.y, 10));
            
            _swipePathRenderer.SetPosition(0, startWorld);
            _swipePathRenderer.SetPosition(1, endWorld);
            
            // Set color based on direction
            Vector2 delta = end - start;
            Color color = GetDirectionColor(GetSwipeDirection(delta));
            _swipePathRenderer.startColor = color;
            _swipePathRenderer.endColor = new Color(color.r, color.g, color.b, 0);
        }
        
        private void ClearSwipePath()
        {
            if (_swipePathRenderer != null)
            {
                _swipePathRenderer.enabled = false;
            }
        }
        
        private SwipeDirection GetSwipeDirection(Vector2 delta)
        {
            if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
            {
                return delta.x > 0 ? SwipeDirection.Right : SwipeDirection.Left;
            }
            else
            {
                return delta.y > 0 ? SwipeDirection.Up : SwipeDirection.Down;
            }
        }
        
        private void OnGUI()
        {
            if (!_showDebugInfo) return;
            
            // Draw on-screen debug info
            GUI.color = Color.white;
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.fontSize = 20;
            style.normal.textColor = Color.white;
            style.alignment = TextAnchor.UpperLeft;
            
            string info = $"Swipe Debug Mode: ON\n" +
                         $"FPS: {(1f / Time.deltaTime):F0}\n" +
                         $"Input: {(Input.touchCount > 0 ? "Touch" : "Mouse")}";
            
            GUI.Label(new Rect(10, 10, 400, 100), info, style);
        }
        
        /// <summary>
        /// Toggle debug visualization on/off
        /// </summary>
        public void ToggleDebug()
        {
            _showDebugInfo = !_showDebugInfo;
            
            if (_debugText != null)
                _debugText.gameObject.SetActive(_showDebugInfo);
                
            if (_directionIndicator != null)
                _directionIndicator.gameObject.SetActive(_showDebugInfo);
                
            if (_swipePathRenderer != null)
                _swipePathRenderer.enabled = false;
        }
        
        /// <summary>
        /// Call this to log detailed swipe statistics
        /// </summary>
        public void LogSwipeStats()
        {
            if (_card == null) return;
            
            UnityEngine.Debug.Log($"<b>=== SWIPE STATISTICS ===</b>\n" +
                                 $"Card: {_card.Data?.Title ?? "N/A"}\n" +
                                 $"Min Swipe Distance: {_card.GetType().GetField("_minSwipeDistance", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(_card)} px\n" +
                                 $"Max Swipe Time: {_card.GetType().GetField("_maxSwipeTime", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(_card)} sec");
        }
    }
}

