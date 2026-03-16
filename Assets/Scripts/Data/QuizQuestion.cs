using System;
using UnityEngine;

namespace HajjFlow.Data
{
    /// <summary>
    /// A single multiple-choice quiz question used inside a level.
    /// Stored as plain data so it can live inside a LevelData ScriptableObject.
    /// </summary>
    [Serializable]
    public class QuizQuestion
    {
        /// <summary>The question text shown to the player.</summary>
        public string QuestionText = "";

        /// <summary>Four possible answers. Index matches CorrectAnswerIndex.</summary>
        public string[] Options = new string[4];

        /// <summary>Zero-based index of the correct answer in Options.</summary>
        public int CorrectAnswerIndex = 0;

        /// <summary>Short explanation shown after the player answers.</summary>
        public string Explanation = "";

        /// <summary>Gems awarded for answering this question correctly.</summary>
        public int GemsReward = 5;

        /// <summary>
        /// Создаёт QuizQuestion из JSON строки.
        /// </summary>
        public static QuizQuestion FromJson(string json)
        {
            return JsonUtility.FromJson<QuizQuestion>(json);
        }

        /// <summary>
        /// Десериализует массив вопросов из JSON строки.
        /// </summary>
        public static QuizQuestion[] FromJsonArray(string jsonArray)
        {
            // JsonUtility не поддерживает массивы напрямую, оборачиваем в объект
            string wrapped = "{\"items\":" + jsonArray + "}";
            QuizQuestionWrapper wrapper = JsonUtility.FromJson<QuizQuestionWrapper>(wrapped);
            return wrapper?.items ?? new QuizQuestion[0];
        }

        public void ShuffleOptions()
        { 
            // Простая реализация тасования Фишера-Йетса
            for (int i = Options.Length - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                // Поменять местами Options[i] и Options[j]
                string tempOption = Options[i];
                Options[i] = Options[j];
                Options[j] = tempOption;

                // Если перемешиваем правильный ответ, нужно обновить индекс
                if (i == CorrectAnswerIndex)
                    CorrectAnswerIndex = j;
                else if (j == CorrectAnswerIndex)
                    CorrectAnswerIndex = i;
            }
        }
    }

    /// <summary>
    /// Вспомогательный класс для десериализации массива вопросов.
    /// </summary>
    [System.Serializable]
    public class QuizQuestionWrapper
    {
        public QuizQuestion[] items = new QuizQuestion[0];
    }
}
