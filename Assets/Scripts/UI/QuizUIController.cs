using UnityEngine;
using UnityEngine.UI;
using HajjFlow.Data;
using HajjFlow.Services;
using HajjFlow.Core;
using HajjFlow.Core.States;
using TMPro;
using System.Collections;
using DG.Tweening;

namespace HajjFlow.UI
{
    /// <summary>
    /// UI контроллер квиза.
    /// Взаимодействует с QuizService для отображения вопросов и обработки ответов.
    /// </summary>
    public class QuizUIController : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI questionText;
        [SerializeField] private Button[] answerButtons = new Button[4];
        [SerializeField] private TextMeshProUGUI progressText;
        [SerializeField] private Image correctIndicator;
        [SerializeField] private Image incorrectIndicator;

        [SerializeField] private QuizService quizService;

        [SerializeField] private GameObject _resultsPanel;
        [SerializeField] private TextMeshProUGUI _resultsText;
        [SerializeField] private Button _retryButton;
        
        [Header("Feedback Settings")]
        [SerializeField] private float _feedbackDelay = 1.5f;
        [SerializeField] private Color _correctColor = Color.green;
        [SerializeField] private Color _incorrectColor = Color.red;
        [SerializeField] private Color _defaultColor = Color.white;
        
        private bool _isInitialized;
        private bool _buttonsInitialized;
        private int _lastClickedButtonIndex = -1;
        private Color[] _originalColors;

        private void OnDisable()
        {
            UnsubscribeFromQuizService();
        }

        private void Start()
        {
            InitializeButtons();
        }
        
        private void InitializeButtons()
        {
            if (_buttonsInitialized) return;
            _buttonsInitialized = true;
            
            // Сохраняем оригинальные цвета кнопок
            _originalColors = new Color[answerButtons.Length];
            
            // Инициализируем кнопки ответов
            for (int i = 0; i < answerButtons.Length; i++)
            {
                if (answerButtons[i] == null) continue;
                
                _originalColors[i] = answerButtons[i].colors.normalColor;
                
                int answerIndex = i;
                answerButtons[i].onClick.AddListener(() => OnAnswerButtonClicked(answerIndex));
            }
            
            // Подписываемся на кнопку retry один раз
            if (_retryButton != null)
            {
                _retryButton.onClick.AddListener(OnRetryClicked);
            }
        }
        
        private void OnRetryClicked()
        {
            var stateMachine = GameManager.Instance?.GetService<GameStateMachine>();
            stateMachine?.ChangeState(GameStateIds.LevelSelect);
        }

        /// <summary>
        /// Обработчик события отображения вопроса
        /// </summary>
        private void HandleQuestionDisplayed(QuizQuestion question)
        {
            if (question == null)
                return;
            
            Debug.Log("[QuizUIController] Displaying question: " + question.QuestionText);

            // Отображаем текст вопроса
            questionText.text = question.QuestionText;

            // Отображаем варианты ответов
            for (int i = 0; i < answerButtons.Length && i < question.Options.Length; i++)
            {
                var  buttonText = answerButtons[i].GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                {
                    buttonText.text = question.Options[i];
                }

                // Включаем кнопку
                answerButtons[i].interactable = true;
            }

            // Обновляем прогресс
            int current = quizService.GetCurrentQuestionIndex() + 1;
            int total = quizService.GetTotalQuestions();
            progressText.text = $"{current} / {total}";

            // Скрываем индикаторы результата
            if (correctIndicator != null) correctIndicator.gameObject.SetActive(false);
            if (incorrectIndicator != null) incorrectIndicator.gameObject.SetActive(false);
        }

        /// <summary>
        /// Обработчик при клике на кнопку ответа
        /// </summary>
        private void OnAnswerButtonClicked(int answerIndex)
        {
            _lastClickedButtonIndex = answerIndex;
            
            // Отключаем все кнопки во время проверки
            foreach (Button btn in answerButtons)
            {
                btn.interactable = false;
            }

            // Проверяем ответ
            quizService.SubmitAnswer(answerIndex);
        }

        /// <summary>
        /// Обработчик при правильном ответе
        /// </summary>
        private void HandleAnswerCorrect(int gemsReward)
        {
            Debug.Log($"✓ Correct! +{gemsReward} gems");

            // Показываем индикатор успеха
            if (correctIndicator != null)
            {
                correctIndicator.gameObject.SetActive(true);
            }

            // Подсвечиваем правильный ответ зелёным
            if (_lastClickedButtonIndex >= 0 && _lastClickedButtonIndex < answerButtons.Length)
            {
                var correctButton = answerButtons[_lastClickedButtonIndex];
                
                ColorBlock colors = correctButton.colors;
                colors.normalColor = _correctColor;
                colors.disabledColor = _correctColor;
                correctButton.colors = colors;
                
                // Анимация успеха
                correctButton.transform.DOScale(1.15f, 0.2f).SetLoops(2, LoopType.Yoyo);
            }

            // Добавляем геммы в профиль
            GameManager.Instance.AddGems(gemsReward);
            
            // Задержка перед следующим вопросом
            StartCoroutine(WaitAndProceedCorrect());
        }
        
        /// <summary>
        /// Ждёт задержку после правильного ответа
        /// </summary>
        private IEnumerator WaitAndProceedCorrect()
        {
            yield return new WaitForSeconds(_feedbackDelay * 0.7f); // Чуть меньше задержки для правильного ответа
            
            // Сбрасываем цвета кнопок
            ResetButtonColors();
            
            // Скрываем индикаторы
            if (correctIndicator != null) correctIndicator.gameObject.SetActive(false);
            
            _lastClickedButtonIndex = -1;
            
            // Переходим к следующему вопросу
            quizService?.MoveToNextQuestion();
        }

        /// <summary>
        /// Обработчик при неправильном ответе
        /// </summary>
        private void HandleAnswerIncorrect(int correctIndex)
        {
            Debug.Log($"✗ Incorrect! Correct answer index: {correctIndex}");

            // Показываем индикатор ошибки
            if (incorrectIndicator != null)
            {
                incorrectIndicator.gameObject.SetActive(true);
            }

            // Подсвечиваем неправильный ответ красным и трясём кнопку
            if (_lastClickedButtonIndex >= 0 && _lastClickedButtonIndex < answerButtons.Length)
            {
                var wrongButton = answerButtons[_lastClickedButtonIndex];
                
                // Красный цвет для неправильного ответа
                ColorBlock wrongColors = wrongButton.colors;
                wrongColors.normalColor = _incorrectColor;
                wrongColors.disabledColor = _incorrectColor;
                wrongButton.colors = wrongColors;
                
                // Анимация тряски неправильной кнопки
                wrongButton.transform.DOShakePosition(0.5f, new Vector3(10f, 0f, 0f), 10, 90f, false, true);
            }

            // Подсвечиваем правильный ответ зелёным
            if (correctIndex >= 0 && correctIndex < answerButtons.Length)
            {
                var correctButton = answerButtons[correctIndex];
                
                ColorBlock correctColors = correctButton.colors;
                correctColors.normalColor = _correctColor;
                correctColors.disabledColor = _correctColor;
                correctButton.colors = correctColors;
                
                // Пульсация правильной кнопки
                correctButton.transform.DOScale(1.1f, 0.3f).SetLoops(2, LoopType.Yoyo);
            }

            // Задержка перед следующим вопросом
            StartCoroutine(WaitAndProceed());
        }

        /// <summary>
        /// Ждёт задержку и разрешает переход к следующему вопросу
        /// </summary>
        private IEnumerator WaitAndProceed()
        {
            yield return new WaitForSeconds(_feedbackDelay);
            
            // Сбрасываем цвета кнопок
            ResetButtonColors();
            
            // Скрываем индикаторы
            if (correctIndicator != null) correctIndicator.gameObject.SetActive(false);
            if (incorrectIndicator != null) incorrectIndicator.gameObject.SetActive(false);
            
            _lastClickedButtonIndex = -1;
            
            // Переходим к следующему вопросу
            quizService?.MoveToNextQuestion();
        }

        /// <summary>
        /// Сбрасывает цвета всех кнопок к оригинальным
        /// </summary>
        private void ResetButtonColors()
        {
            for (int i = 0; i < answerButtons.Length; i++)
            {
                if (answerButtons[i] == null) continue;
                
                ColorBlock colors = answerButtons[i].colors;
                colors.normalColor = _originalColors != null && i < _originalColors.Length 
                    ? _originalColors[i] 
                    : _defaultColor;
                colors.disabledColor = colors.normalColor;
                answerButtons[i].colors = colors;
                
                // Сбрасываем scale
                answerButtons[i].transform.localScale = Vector3.one;
            }
        }

        /// <summary>
        /// Обработчик при завершении квиза
        /// </summary>
        private void HandleQuizCompleted(int totalQuestions, int correctAnswers, float scorePercent)
        {
            Debug.Log($"Quiz Completed! Score: {correctAnswers}/{totalQuestions} ({scorePercent:F1}%)");

            // TODO: Показать экран результатов
            // Можно открыть отдельный UI с результатами, кнопкой "Retry" или "Next Level"
             
            _resultsPanel.SetActive(true);  
            _resultsText.text = $"Ты ответил правильно на  {correctAnswers} из {totalQuestions}  это ({scorePercent:F1}%)";
                
            //gameObject.SetActive(false);
        }

        [ContextMenu("Init")]
        public void Init()
        {
            var service = GameManager.Instance?.quizService;
            if (service != null)
            {
                Init(service);
            }
            else
            {
                Debug.LogError("[QuizUIController] Cannot init - QuizService is null!");
            }
        }
        
        /// <summary>
        /// Инициализирует UI контроллер с переданным QuizService.
        /// Вызывается перед InitializeQuiz, чтобы события были подписаны.
        /// </summary>
        public void Init(QuizService service)
        {
            if (service == null)
            {
                Debug.LogError("[QuizUIController] Cannot init with null QuizService!");
                return;
            }
            
            InitializeButtons();
            
            // Отписываемся от старого сервиса если был
            UnsubscribeFromQuizService();
            
            quizService = service;

            quizService.OnQuestionDisplayed += HandleQuestionDisplayed;
            quizService.OnAnswerCorrect += HandleAnswerCorrect;
            quizService.OnAnswerIncorrect += HandleAnswerIncorrect;
            quizService.OnQuizCompleted += HandleQuizCompleted;
            
            _isInitialized = true;
            Debug.Log("[QuizUIController] Subscribed to QuizService events");
        }
        
        private void UnsubscribeFromQuizService()
        {
            if (quizService != null)
            {
                quizService.OnQuestionDisplayed -= HandleQuestionDisplayed;
                quizService.OnAnswerCorrect -= HandleAnswerCorrect;
                quizService.OnAnswerIncorrect -= HandleAnswerIncorrect;
                quizService.OnQuizCompleted -= HandleQuizCompleted;
            }
            _isInitialized = false;
        }

        /// <summary>
        /// Сбрасывает UI квиза к начальному состоянию.
        /// </summary>
        public void ResetUI()
        {
            Debug.Log("[QuizUIController] Resetting UI");
            
            // Скрываем панель результатов
            if (_resultsPanel != null)
            {
                _resultsPanel.SetActive(false);
            }
            
            // Сбрасываем текст вопроса
            if (questionText != null)
            {
                questionText.text = "";
            }
            
            // Сбрасываем прогресс
            if (progressText != null)
            {
                progressText.text = "0 / 0";
            }
            
            // Включаем все кнопки и сбрасываем их цвета
            foreach (var btn in answerButtons)
            {
                if (btn == null) continue;
                
                btn.interactable = true;
                
                // Сбрасываем цвет кнопки
                ColorBlock colors = btn.colors;
                colors.normalColor = Color.white;
                btn.colors = colors;
                
                // Сбрасываем текст кнопки
                var buttonText = btn.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                {
                    buttonText.text = "";
                }
            }
            
            // Скрываем индикаторы
            if (correctIndicator != null) correctIndicator.gameObject.SetActive(false);
            if (incorrectIndicator != null) incorrectIndicator.gameObject.SetActive(false);
            
            // Отписываемся от сервиса
            UnsubscribeFromQuizService();
            
            // Деактивируем себя
            gameObject.SetActive(false);
        }
    }
}