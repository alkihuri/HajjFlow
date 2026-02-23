using UnityEngine;
using HajjFlow.Core.LevelsLogic;

namespace HajjFlow.UI
{
    /// <summary>
    /// Пример компонента для управления блоком теории/мини-игры.
    /// Этот компонент должен быть прикреплён к GameObject блока теории.
    /// Когда блок завершён, вызывает контроллер уровня для регистрации завершения.
    /// </summary>
    public class StageGameplayController : MonoBehaviour
    {
        private WarmupLevelController levelController;

        private void Start()
        {
            // Находим контроллер уровня в сцене
            levelController = FindFirstObjectByType<WarmupLevelController>();
            
            if (levelController == null)
            {
                Debug.LogError("[StageGameplayController] WarmupLevelController not found!");
            }
        }

        /// <summary>
        /// Вызывается когда блок теории завершён (например, по нажатию кнопки "Next" или по условию)
        /// </summary>
        public void CompleteStage()
        {
            if (levelController == null)
            {
                Debug.LogError("[StageGameplayController] Level controller is not set!");
                return;
            }

            Debug.Log("[StageGameplayController] Stage gameplay completed, notifying level controller...");
            
            // Уведомляем контроллер уровня о завершении
            levelController.OnStageGameplayCompleted();

            // Можно отключить/скрыть этот GameObject
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Пример кнопки для завершения блока теории
        /// Подключить этот метод к кнопке "Next" в UI
        /// </summary>
        public void OnNextButtonClicked()
        {
            Debug.Log("[StageGameplayController] 'Next' button clicked");
            CompleteStage();
        }

        /// <summary>
        /// Пример автоматического завершения через N секунд (для тестирования)
        /// </summary>
        public void CompleteAfterDelay(float delay)
        {
            Invoke(nameof(CompleteStage), delay);
        }
    }
}

