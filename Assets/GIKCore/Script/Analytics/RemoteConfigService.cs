using System;
using System.Collections.Generic;
using UnityEngine;
#if FIREBASE_REMOTE_CONFIG
using Firebase.Extensions;
using Firebase.RemoteConfig;
#endif

namespace GIKCore
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-80)]
    public class RemoteConfigService : MonoBehaviour
    {
        private const string LogTag = "[RemoteConfigService]";

        private static readonly Dictionary<string, object> Defaults = new Dictionary<string, object>();

        public static RemoteConfigService Instance { get; private set; }

        public static bool Loaded { get; private set; }

        public event Action LoadEnded;

        [Header("Fetch")]
#pragma warning disable CS0414
        [Tooltip("Seconds a cached value stays valid. 0 forces a network fetch on every launch.")]
        [SerializeField] private float _cacheExpirationSeconds = 3600f;
#pragma warning restore CS0414

        [Tooltip("Fetch automatically as soon as FirebaseSDK reports ready.")]
        [SerializeField] private bool _fetchOnFirebaseReady = true;

        public static void SetDefault(string key, object value)
        {
            if (string.IsNullOrEmpty(key) || value == null)
            {
                return;
            }

            Defaults[key] = value;
        }

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

        private void Start()
        {
            if (!_fetchOnFirebaseReady)
            {
                return;
            }

            if (FirebaseSDK.Instance == null)
            {
                Debug.LogError($"{LogTag} FirebaseSDK is missing from the scene, remote config will never fetch.");
                return;
            }

            if (FirebaseSDK.IsReady)
            {
                Fetch();
                return;
            }

            FirebaseSDK.Instance.Ready += OnFirebaseReady;
            FirebaseSDK.Instance.Failed += OnFirebaseFailed;
        }

        private void OnDestroy()
        {
            if (FirebaseSDK.Instance != null)
            {
                FirebaseSDK.Instance.Ready -= OnFirebaseReady;
                FirebaseSDK.Instance.Failed -= OnFirebaseFailed;
            }

            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void OnFirebaseReady()
        {
            Fetch();
        }

        private void OnFirebaseFailed()
        {
            EndLoad();
        }

        public void Fetch()
        {
#if FIREBASE_REMOTE_CONFIG
            FirebaseRemoteConfig remoteConfig = FirebaseRemoteConfig.DefaultInstance;
            remoteConfig.SetDefaultsAsync(Defaults).ContinueWithOnMainThread(defaultsTask =>
            {
                if (defaultsTask.IsFaulted || defaultsTask.IsCanceled)
                {
                    Debug.LogError($"{LogTag} SetDefaults failed: {defaultsTask.Exception}");
                    EndLoad();
                    return;
                }

                remoteConfig.FetchAsync(TimeSpan.FromSeconds(_cacheExpirationSeconds)).ContinueWithOnMainThread(fetchTask =>
                {
                    if (fetchTask.IsFaulted || fetchTask.IsCanceled)
                    {
                        Debug.LogError($"{LogTag} Fetch failed: {fetchTask.Exception}");
                        EndLoad();
                        return;
                    }

                    remoteConfig.ActivateAsync().ContinueWithOnMainThread(activateTask =>
                    {
                        if (activateTask.IsFaulted || activateTask.IsCanceled)
                        {
                            Debug.LogError($"{LogTag} Activate failed: {activateTask.Exception}");
                        }

                        EndLoad();
                    });
                });
            });
#else
            Debug.LogWarning($"{LogTag} FIREBASE_REMOTE_CONFIG is not defined, serving defaults only.");
            EndLoad();
#endif
        }

        private void EndLoad()
        {
            Loaded = true;
            LoadEnded?.Invoke();
        }

        public static string GetString(string key, string defaultValue = "")
        {
#if FIREBASE_REMOTE_CONFIG
            if (!TryGetValue(key, out ConfigValue value))
            {
                return defaultValue;
            }

            return value.StringValue;
#else
            return defaultValue;
#endif
        }

        public static long GetLong(string key, long defaultValue = 0)
        {
#if FIREBASE_REMOTE_CONFIG
            if (!TryGetValue(key, out ConfigValue value))
            {
                return defaultValue;
            }

            return value.LongValue;
#else
            return defaultValue;
#endif
        }

        public static int GetInt(string key, int defaultValue = 0)
        {
            return (int)GetLong(key, defaultValue);
        }

        public static double GetDouble(string key, double defaultValue = 0d)
        {
#if FIREBASE_REMOTE_CONFIG
            if (!TryGetValue(key, out ConfigValue value))
            {
                return defaultValue;
            }

            return value.DoubleValue;
#else
            return defaultValue;
#endif
        }

        public static float GetFloat(string key, float defaultValue = 0f)
        {
            return (float)GetDouble(key, defaultValue);
        }

        public static bool GetBool(string key, bool defaultValue = false)
        {
#if FIREBASE_REMOTE_CONFIG
            if (!TryGetValue(key, out ConfigValue value))
            {
                return defaultValue;
            }

            return value.BooleanValue;
#else
            return defaultValue;
#endif
        }

#if FIREBASE_REMOTE_CONFIG
        private static bool TryGetValue(string key, out ConfigValue value)
        {
            value = default;
            if (string.IsNullOrEmpty(key) || !Loaded)
            {
                return false;
            }

            value = FirebaseRemoteConfig.DefaultInstance.GetValue(key);
            return value.Source != ValueSource.StaticValue;
        }
#endif
    }
}
