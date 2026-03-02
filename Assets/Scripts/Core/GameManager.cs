using System;
using System.Collections.Generic;
using UnityEngine;
using HajjFlow.Services;
using HajjFlow.UI;

namespace HajjFlow.Core
{
    /// <summary>
    /// Central singleton that implements the Service Locator pattern.
    /// Survives scene loads via DontDestroyOnLoad.
    /// Services are registered by the <see cref="Bootstrapper"/> and accessed via
    /// <see cref="GetService{T}"/> from any other script.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────────────────────────

        public static GameManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // ── Service Locator ──────────────────────────────────────────────────────

        private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

        /// <summary>Registers a service instance by its type.</summary>
        public void RegisterService<T>(T service) where T : class
        {
            if (service == null)
            {
                Debug.LogWarning($"[GameManager] Attempted to register null service for {typeof(T).Name}");
                return;
            }

            _services[typeof(T)] = service;
            Debug.Log($"[GameManager] Registered service: {typeof(T).Name}");
        }

        /// <summary>Returns the registered service of type T, or null if not found.</summary>
        public T GetService<T>() where T : class
        {
            if (_services.TryGetValue(typeof(T), out var service))
                return service as T;

            Debug.LogWarning($"[GameManager] Service not found: {typeof(T).Name}");
            return null;
        }

        /// <summary>Returns true if a service of type T is registered.</summary>
        public bool HasService<T>() where T : class
        {
            return _services.ContainsKey(typeof(T));
        }

        // ── Backward-compatible service accessors ────────────────────────────────

        public UserProfileService ProfileService => GetService<UserProfileService>();
        public ProgressService ProgressService => GetService<ProgressService>();
        public UIService uiService => GetService<UIService>();
        public StageCompletionService stageCompletionService => GetService<StageCompletionService>();
        public QuizService quizService => GetService<QuizService>();

        // ── Convenience helpers ──────────────────────────────────────────────────

        /// <summary>
        /// Adds gems to the player's profile and saves immediately.
        /// Call from RewardSystem or any other gem-granting code.
        /// </summary>
        public void AddGems(int amount)
        {
            
            
            var profileService = ProfileService;
            if (profileService == null) return;
            profileService.UpdateProfile(p => p.Gems += amount);
            
            uiService.UpdateGemsCounter(profileService.GetProfile().Gems);
            
            Debug.Log($"[GameManager] +{amount} gems. Total: {profileService.GetProfile().Gems}");
        }
    }
}