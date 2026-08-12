using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace HajjFlow.Services
{
    /// <summary>
    /// Сервис загрузки и кэширования Asset Bundles.
    /// Загружает бандлы из Remote URL или StreamingAssets.
    /// Возвращает Sprite по ключу (imageBundleKey из Google Таблицы).
    /// </summary>
    public class AssetBundleService : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private string _remoteBundleBaseUrl = "";
        [SerializeField] private bool _useStreamingAssetsAsFallback = true;

        // Кэш загруженных бандлов
        private readonly Dictionary<string, AssetBundle> _loadedBundles = new Dictionary<string, AssetBundle>();

        // Кэш загруженных спрайтов
        private readonly Dictionary<string, Sprite> _spriteCache = new Dictionary<string, Sprite>();

        // Очередь загрузки
        private readonly Queue<string> _loadQueue = new Queue<string>();
        private bool _isLoading;

        /// <summary>
        /// Событие при завершении загрузки бандла (bundleKey, success).
        /// </summary>
        public event Action<string, bool> OnBundleLoaded;

        /// <summary>
        /// Базовый URL для удалённых бандлов.
        /// </summary>
        public string RemoteBundleBaseUrl
        {
            get => _remoteBundleBaseUrl;
            set => _remoteBundleBaseUrl = value;
        }

        /// <summary>
        /// Получить Sprite по ключу из уже загруженных бандлов.
        /// Возвращает null если бандл не загружен или спрайт не найден.
        /// </summary>
        public Sprite GetSprite(string spriteKey)
        {
            if (string.IsNullOrEmpty(spriteKey))
                return null;

            // Проверяем кэш
            if (_spriteCache.TryGetValue(spriteKey, out var cachedSprite))
                return cachedSprite;

            // Ищем во всех загруженных бандлах
            foreach (var bundle in _loadedBundles.Values)
            {
                var sprite = bundle.LoadAsset<Sprite>(spriteKey);
                if (sprite != null)
                {
                    _spriteCache[spriteKey] = sprite;
                    return sprite;
                }
            }

            return null;
        }

        /// <summary>
        /// Загружает Asset Bundle по имени. Пытается сначала из Remote URL, затем из StreamingAssets.
        /// </summary>
        public IEnumerator LoadBundle(string bundleName, Action<bool> onComplete = null)
        {
            if (string.IsNullOrEmpty(bundleName))
            {
                onComplete?.Invoke(false);
                yield break;
            }

            // Если уже загружен — пропускаем
            if (_loadedBundles.ContainsKey(bundleName))
            {
                onComplete?.Invoke(true);
                yield break;
            }

            bool loaded = false;

            // Попытка 1: Remote URL
            if (!string.IsNullOrEmpty(_remoteBundleBaseUrl))
            {
                string remoteUrl = $"{_remoteBundleBaseUrl.TrimEnd('/')}/{bundleName}";
                yield return StartCoroutine(LoadBundleFromUrl(remoteUrl, bundleName, success =>
                {
                    loaded = success;
                }));
            }

            // Попытка 2: StreamingAssets
            if (!loaded && _useStreamingAssetsAsFallback)
            {
                string localPath = System.IO.Path.Combine(Application.streamingAssetsPath, "AssetBundles", bundleName);
                yield return StartCoroutine(LoadBundleFromUrl(localPath, bundleName, success =>
                {
                    loaded = success;
                }));
            }

            Debug.Log($"[AssetBundleService] Bundle '{bundleName}': {(loaded ? "loaded" : "failed")}");
            OnBundleLoaded?.Invoke(bundleName, loaded);
            onComplete?.Invoke(loaded);
        }

        /// <summary>
        /// Загружает несколько бандлов одновременно.
        /// </summary>
        public IEnumerator LoadBundles(IEnumerable<string> bundleNames, Action<int, int> onProgress = null, Action onComplete = null)
        {
            var names = new List<string>(bundleNames);
            int total = names.Count;
            int loaded = 0;

            foreach (var bundleName in names)
            {
                yield return StartCoroutine(LoadBundle(bundleName));
                loaded++;
                onProgress?.Invoke(loaded, total);
            }

            onComplete?.Invoke();
        }

        /// <summary>
        /// Выгружает бандл из памяти.
        /// </summary>
        public void UnloadBundle(string bundleName, bool unloadAllLoadedObjects = false)
        {
            if (_loadedBundles.TryGetValue(bundleName, out var bundle))
            {
                bundle.Unload(unloadAllLoadedObjects);
                _loadedBundles.Remove(bundleName);

                // Очищаем кэш спрайтов из этого бандла
                var keysToRemove = new List<string>();
                foreach (var kvp in _spriteCache)
                {
                    if (kvp.Value == null)
                        keysToRemove.Add(kvp.Key);
                }
                foreach (var key in keysToRemove)
                {
                    _spriteCache.Remove(key);
                }

                Debug.Log($"[AssetBundleService] Unloaded bundle: {bundleName}");
            }
        }

        /// <summary>
        /// Выгружает все загруженные бандлы.
        /// </summary>
        public void UnloadAllBundles(bool unloadAllLoadedObjects = false)
        {
            foreach (var bundle in _loadedBundles.Values)
            {
                bundle.Unload(unloadAllLoadedObjects);
            }
            _loadedBundles.Clear();
            _spriteCache.Clear();
            Debug.Log("[AssetBundleService] All bundles unloaded");
        }

        /// <summary>
        /// Проверяет, загружен ли бандл.
        /// </summary>
        public bool IsBundleLoaded(string bundleName)
        {
            return _loadedBundles.ContainsKey(bundleName);
        }

        // ── Private ──────────────────────────────────────────────────────────────

        private IEnumerator LoadBundleFromUrl(string url, string bundleName, Action<bool> onComplete)
        {
            using (var request = UnityWebRequestAssetBundle.GetAssetBundle(url))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    var bundle = DownloadHandlerAssetBundle.GetContent(request);
                    if (bundle != null)
                    {
                        _loadedBundles[bundleName] = bundle;
                        onComplete?.Invoke(true);
                        yield break;
                    }
                }

                onComplete?.Invoke(false);
            }
        }

        private void OnDestroy()
        {
            UnloadAllBundles(true);
        }
    }
}
