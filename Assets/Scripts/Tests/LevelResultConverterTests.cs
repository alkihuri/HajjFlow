using System;
using Newtonsoft.Json;
using UnityEngine;
using HajjFlow.Services;
using HajjFlow.Data;

namespace HajjFlow.Tests
{
    /// <summary>
    /// Юнит-тесты для LevelResultConverter.
    /// Проверяет корректность десериализации разных форматов LevelResult.
    /// </summary>
    public class LevelResultConverterTests : MonoBehaviour
    {
        /// <summary>
        /// Тестирует десериализацию C# Dictionary формата из Google Sheets.
        /// </summary>
        [ContextMenu("Test 1: C# Dictionary Format")]
        public void TestCSharpDictionaryFormat()
        {
            string json = @"{
                ""row"": 3,
                ""user"": {
                    ""UserId"": ""test-id-123"",
                    ""PilgrimNumber"": ""Akhmed"",
                    ""FullName"": ""Akhmed"",
                    ""GroupId"": ""3"",
                    ""CreatedAt"": ""2026-09-08T19:57:43.798Z"",
                    ""UpdatedAt"": ""2026-09-15T10:41:57.558Z"",
                    ""Status"": ""active"",
                    ""LevelResult"": ""{level_3={CompletedAt=2026-09-08T20:04:44.912572Z, LevelId=level_3, ScorePercent=85.71429}, level_0={ScorePercent=42.8571434, LevelId=level_0, CompletedAt=2026-09-08T19:59:56.560544Z}, level_2={ScorePercent=71.42857, LevelId=level_2, CompletedAt=2026-09-08T20:01:26.141696Z}}""
                }
            }";

            try
            {
                var response = JsonConvert.DeserializeObject<CreateUserResponse>(json);
                
                Assert(response != null, "CreateUserResponse is null");
                Assert(response.User != null, "User is null");
                Assert(response.User.LevelResults != null, "LevelResult is null");
                Assert(response.User.LevelResults.Length == 3, $"Expected 3 LevelResults, got {response.User.LevelResults.Length}");
                
                // Проверяем каждый результат
                foreach (var result in response.User.LevelResults)
                {
                    Assert(!string.IsNullOrEmpty(result.LevelId), "LevelId is empty");
                    Assert(result.ScorePercent > 0, $"ScorePercent is {result.ScorePercent}, should be > 0");
                    Assert(result.CompletedAt != DateTime.MinValue, "CompletedAt is not set");
                    
                    Debug.Log($"✅ {result.LevelId}: {result.ScorePercent:F2}% at {result.CompletedAt}");
                }
                
                Debug.Log("✅ Test 1 PASSED: C# Dictionary Format");
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Test 1 FAILED: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Тестирует десериализацию JSON массива.
        /// </summary>
        [ContextMenu("Test 2: JSON Array Format")]
        public void TestJsonArrayFormat()
        {
            string json = @"{
                ""row"": 3,
                ""user"": {
                    ""UserId"": ""test-id-456"",
                    ""PilgrimNumber"": ""Test"",
                    ""FullName"": ""Test User"",
                    ""GroupId"": ""1"",
                    ""CreatedAt"": ""2026-09-08T19:57:43.798Z"",
                    ""UpdatedAt"": ""2026-09-15T10:41:57.558Z"",
                    ""Status"": ""active"",
                    ""LevelResult"": [
                        {""LevelId"": ""level_0"", ""ScorePercent"": 50.0, ""CompletedAt"": ""2026-09-08T19:59:56.560544Z""},
                        {""LevelId"": ""level_1"", ""ScorePercent"": 75.5, ""CompletedAt"": ""2026-09-08T20:01:26.141696Z""}
                    ]
                }
            }";

            try
            {
                var response = JsonConvert.DeserializeObject<CreateUserResponse>(json);
                
                Assert(response != null, "CreateUserResponse is null");
                Assert(response.User.LevelResults != null, "LevelResult is null");
                Assert(response.User.LevelResults.Length == 2, $"Expected 2 LevelResults, got {response.User.LevelResults.Length}");
                
                foreach (var result in response.User.LevelResults)
                {
                    Debug.Log($"✅ {result.LevelId}: {result.ScorePercent:F2}%");
                }
                
                Debug.Log("✅ Test 2 PASSED: JSON Array Format");
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Test 2 FAILED: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Тестирует десериализацию null LevelResult.
        /// </summary>
        [ContextMenu("Test 3: Null LevelResult")]
        public void TestNullLevelResult()
        {
            string json = @"{
                ""row"": 3,
                ""user"": {
                    ""UserId"": ""test-id-789"",
                    ""PilgrimNumber"": ""Test"",
                    ""FullName"": ""Test User"",
                    ""GroupId"": ""1"",
                    ""CreatedAt"": ""2026-09-08T19:57:43.798Z"",
                    ""UpdatedAt"": ""2026-09-15T10:41:57.558Z"",
                    ""Status"": ""active"",
                    ""LevelResult"": null
                }
            }";

            try
            {
                var response = JsonConvert.DeserializeObject<CreateUserResponse>(json);
                
                Assert(response != null, "CreateUserResponse is null");
                Assert(response.User.LevelResults != null, "LevelResult should not be null (should be empty array)");
                Assert(response.User.LevelResults.Length == 0, $"Expected empty LevelResult, got {response.User.LevelResults.Length}");
                
                Debug.Log("✅ Test 3 PASSED: Null LevelResult");
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Test 3 FAILED: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Тестирует десериализацию пустого LevelResult.
        /// </summary>
        [ContextMenu("Test 4: Empty LevelResult")]
        public void TestEmptyLevelResult()
        {
            string json = @"{
                ""row"": 3,
                ""user"": {
                    ""UserId"": ""test-id-000"",
                    ""PilgrimNumber"": ""Test"",
                    ""FullName"": ""Test User"",
                    ""GroupId"": ""1"",
                    ""CreatedAt"": ""2026-09-08T19:57:43.798Z"",
                    ""UpdatedAt"": ""2026-09-15T10:41:57.558Z"",
                    ""Status"": ""active"",
                    ""LevelResult"": """"
                }
            }";

            try
            {
                var response = JsonConvert.DeserializeObject<CreateUserResponse>(json);
                
                Assert(response != null, "CreateUserResponse is null");
                Assert(response.User.LevelResults != null, "LevelResult should not be null");
                Assert(response.User.LevelResults.Length == 0, $"Expected empty LevelResult, got {response.User.LevelResults.Length}");
                
                Debug.Log("✅ Test 4 PASSED: Empty LevelResult");
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Test 4 FAILED: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Тестирует десериализацию смешанного формата.
        /// </summary>
        [ContextMenu("Test 5: Mixed Format with Missing Fields")]
        public void TestMixedFormatWithMissingFields()
        {
            string json = @"{
                ""row"": 3,
                ""user"": {
                    ""UserId"": ""test-id-mixed"",
                    ""PilgrimNumber"": ""Test"",
                    ""FullName"": ""Test User"",
                    ""GroupId"": ""1"",
                    ""CreatedAt"": ""2026-09-08T19:57:43.798Z"",
                    ""UpdatedAt"": ""2026-09-15T10:41:57.558Z"",
                    ""Status"": ""active"",
                    ""LevelResult"": ""{level_0={ScorePercent=30}, level_1={LevelId=level_1, ScorePercent=60, CompletedAt=2026-09-08T20:01:26Z}}""
                }
            }";

            try
            {
                var response = JsonConvert.DeserializeObject<CreateUserResponse>(json);
                
                Assert(response != null, "CreateUserResponse is null");
                Assert(response.User.LevelResults != null, "LevelResult is null");
                Assert(response.User.LevelResults.Length >= 1, $"Expected at least 1 LevelResult, got {response.User.LevelResults.Length}");
                
                foreach (var result in response.User.LevelResults)
                {
                    Debug.Log($"✅ {(string.IsNullOrEmpty(result.LevelId) ? "Unknown" : result.LevelId)}: {result.ScorePercent:F2}%");
                }
                
                Debug.Log("✅ Test 5 PASSED: Mixed Format");
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Test 5 FAILED: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Запускает все тесты по очереди.
        /// </summary>
        [ContextMenu("Run All Tests")]
        public void RunAllTests()
        {
            Debug.Log("═══════════════════════════════════════════════════════════════");
            Debug.Log("Starting LevelResultConverter Tests...");
            Debug.Log("═══════════════════════════════════════════════════════════════");
            
            TestCSharpDictionaryFormat();
            Debug.Log("");
            
            TestJsonArrayFormat();
            Debug.Log("");
            
            TestNullLevelResult();
            Debug.Log("");
            
            TestEmptyLevelResult();
            Debug.Log("");
            
            TestMixedFormatWithMissingFields();
            Debug.Log("");
            
            Debug.Log("═══════════════════════════════════════════════════════════════");
            Debug.Log("All tests completed!");
            Debug.Log("═══════════════════════════════════════════════════════════════");
        }

        /// <summary>
        /// Простой assert для логирования.
        /// </summary>
        private void Assert(bool condition, string message)
        {
            if (!condition)
            {
                throw new Exception($"Assertion failed: {message}");
            }
        }
    }
}

