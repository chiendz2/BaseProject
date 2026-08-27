using System;
using UnityEngine;
#if FIREBASE_SDK
using Firebase;
using Firebase.Extensions;
#endif
#if FIREBASE_ANALYTICS
using Firebase.Analytics;
#endif

namespace GIKCore
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-90)]
    public class FirebaseSDK : MonoBehaviour
    {
        public enum FirebaseStatus
        {
            None,
            Initializing,
            Ready,
            Failed
        }

        private const string LogTag = "[FirebaseSDK]";

        public static FirebaseSDK Instance { get; private set; }

        public static FirebaseStatus Status { get; private set; } = FirebaseStatus.None;

        public static bool IsReady => Status == FirebaseStatus.Ready;

#pragma warning disable CS0067
        public event Action Ready;
        public event Action Failed;
        public event Action UserIdReady;
#pragma warning restore CS0067

        [Header("Init")]
        [Tooltip("Initialize on Start. Untick when another manager decides the moment.")]
        [SerializeField] private bool _autoInitialize = true;

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
            if (_autoInitialize && Status == FirebaseStatus.None)
            {
                Initialize();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void Initialize()
        {
            if (Status == FirebaseStatus.Initializing || Status == FirebaseStatus.Ready)
            {
                return;
            }

#if FIREBASE_SDK
            Status = FirebaseStatus.Initializing;
            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    Fail($"dependency check did not complete: {task.Exception}");
                    return;
                }

                if (task.Result != DependencyStatus.Available)
                {
                    Fail($"dependencies unavailable: {task.Result}");
                    return;
                }

                Status = FirebaseStatus.Ready;
#if FIREBASE_ANALYTICS
                FirebaseAnalyticsProvider.SetReady();
#endif
                Ready?.Invoke();
                ResolveUserId();
            });
#else
            Fail("FIREBASE_SDK is not defined");
#endif
        }

        private void Fail(string reason)
        {
            Status = FirebaseStatus.Failed;
            Debug.LogError($"{LogTag} Initialization failed - {reason}");
            Failed?.Invoke();
        }

        private void ResolveUserId()
        {
#if FIREBASE_SDK && FIREBASE_ANALYTICS
            string storedId = UserDataManager.GetUserPseudoId();
            if (!string.IsNullOrEmpty(storedId))
            {
                FirebaseAnalyticsProvider.SetUserId(storedId);
                UserIdReady?.Invoke();
                return;
            }

            FirebaseAnalytics.GetAnalyticsInstanceIdAsync().ContinueWithOnMainThread(task =>
            {
                string resolvedId = task.IsFaulted || task.IsCanceled || string.IsNullOrEmpty(task.Result)
                    ? SystemInfo.deviceUniqueIdentifier
                    : task.Result;

                UserDataManager.SetUserPseudoId(resolvedId);
                FirebaseAnalyticsProvider.SetUserId(resolvedId);
                UserIdReady?.Invoke();
            });
#else
            UserIdReady?.Invoke();
#endif
        }
    }
}
