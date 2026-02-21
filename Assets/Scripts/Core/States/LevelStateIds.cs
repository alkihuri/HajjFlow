using System.Collections.Generic;

namespace HajjFlow.Core.States
{
    /// <summary>
    /// Константы для идентификаторов состояний уровней.
    /// Используйте вместо строковых литералов для избежания опечаток.
    /// </summary>
    public static class LevelStateIds
    {
        public const string Warmup = "warmup";
        public const string Miqat = "miqat";
        public const string Tawaf = "tawaf";

        /// <summary>Все доступные StateId в правильном порядке прохождения.</summary>
        public static readonly List<string> AllStates = new List<string>
        {
            Warmup,
            Miqat,
            Tawaf
        };

        /// <summary>
        /// Получить следующее состояние в последовательности.
        /// Возвращает null если текущее состояние последнее.
        /// </summary>
        public static string GetNextState(string currentStateId)
        {
            int index = AllStates.IndexOf(currentStateId);
            if (index < 0 || index >= AllStates.Count - 1)
                return null;
            
            return AllStates[index + 1];
        }

        /// <summary>
        /// Получить предыдущее состояние в последовательности.
        /// Возвращает null если текущее состояние первое.
        /// </summary>
        public static string GetPreviousState(string currentStateId)
        {
            int index = AllStates.IndexOf(currentStateId);
            if (index <= 0)
                return null;
            
            return AllStates[index - 1];
        }

        /// <summary>
        /// Проверяет является ли StateId валидным.
        /// </summary>
        public static bool IsValid(string stateId)
        {
            return AllStates.Contains(stateId);
        }
    }
}

