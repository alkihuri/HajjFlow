using UnityEngine;
using HajjFlow.Data;

namespace HajjFlow.Core.States
{
    /// <summary>
    /// Базовый класс для всех состояний уровней.
    /// Каждое состояние представляет отдельный уровень с собственной логикой.
    /// </summary>
    public abstract class BaseLevelState
    {
        protected LevelStateMachine _stateMachine;
        protected LevelData _levelData;

        /// <summary>Уникальный идентификатор состояния.</summary>
        public abstract string StateId { get; }

        /// <summary>
        /// Инициализирует состояние с данными уровня.
        /// </summary>
        public virtual void Initialize(LevelStateMachine stateMachine, LevelData levelData)
        {
            _stateMachine = stateMachine;
            _levelData = levelData;
        }

        /// <summary>
        /// Вызывается при входе в состояние.
        /// Используйте для инициализации UI, загрузки ресурсов и т.д.
        /// </summary>
        public virtual void Enter()
        {
            Debug.Log($"[{StateId}] Entering state: {_levelData?.LevelName ?? "Unknown"}");
        }

        /// <summary>
        /// Вызывается каждый кадр пока состояние активно.
        /// Используйте для обновления игровой логики.
        /// </summary>
        public virtual void Update()
        {
            // Переопределите в дочерних классах
        }

        /// <summary>
        /// Вызывается при выходе из состояния.
        /// Используйте для очистки ресурсов, сохранения прогресса и т.д.
        /// </summary>
        public virtual void Exit()
        {
            Debug.Log($"[{StateId}] Exiting state");
        }

        /// <summary>
        /// Вызывается при паузе игры.
        /// </summary>
        public virtual void OnPause()
        {
            // Переопределите при необходимости
        }

        /// <summary>
        /// Вызывается при возобновлении игры.
        /// </summary>
        public virtual void OnResume()
        {
            // Переопределите при необходимости
        }
    }
}

