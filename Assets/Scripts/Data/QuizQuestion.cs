using System;
using System.Collections.Generic;
using UnityEngine;

namespace HajjFlow.Data
{
    /// <summary>
    /// Метаданные уровня, хранящиеся в первом элементе JSON файла.
    /// </summary>
    [Serializable]
    public class LevelMetadata
    {
        public string LevelId = "";
        public string LevelName = "";
        public string Description = "";
        public string imagePath = null;
    }

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
        /// Десериализует массив вопросов из JSON строки (пропуская первый элемент с метаданными уровня).
        /// </summary>
        public static QuizQuestion[] FromJsonArray(string jsonArray)
        {
            // JsonUtility не поддерживает массивы напрямую, оборачиваем в объект
            string wrapped = "{\"items\":" + jsonArray + "}";
            QuizQuestionWrapper wrapper = JsonUtility.FromJson<QuizQuestionWrapper>(wrapped);
            
            if (wrapper?.items == null || wrapper.items.Length == 0)
                return new QuizQuestion[0];
            
            // Фильтруем только вопросы (у которых есть QuestionText)
            var questions = new List<QuizQuestion>();
            foreach (var item in wrapper.items)
            {
                if (!string.IsNullOrEmpty(item.QuestionText))
                {
                    questions.Add(item);
                }
            }
            
            return questions.ToArray();
        }

        /// <summary>
        /// Десериализует метаданные уровня из первого элемента JSON массива.
        /// </summary>
        public static LevelMetadata ExtractLevelMetadata(string jsonArray)
        {
            string wrapped = "{\"items\":" + jsonArray + "}";
            LevelMetadataWrapper wrapper = JsonUtility.FromJson<LevelMetadataWrapper>(wrapped);
            
            if (wrapper?.items == null || wrapper.items.Length == 0)
                return null;
            
            // Первый элемент должен содержать метаданные уровня
            var firstItem = wrapper.items[0];
            if (!string.IsNullOrEmpty(firstItem.LevelId))
            {
                return firstItem;
            }
            
            return null;
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
    [Serializable]
    public class QuizQuestionWrapper
    {
        public QuizQuestion[] items = new QuizQuestion[0];
    }

    /// <summary>
    /// Вспомогательный класс для десериализации массива метаданных уровня.
    /// </summary>
    [Serializable]
    public class LevelMetadataWrapper
    {
        public LevelMetadata[] items = new LevelMetadata[0];
    }
}
