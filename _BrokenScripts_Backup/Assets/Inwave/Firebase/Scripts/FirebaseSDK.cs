#define DEBUG_ANALYTIC_INTERNAL
using System;
using UnityEngine;
using Firebase.Extensions;
#if FIREBASE_SDK
using Firebase;
using Firebase.Extensions;

#endif
using Inwave.Production.Utils;

public partial class FirebaseSDK : MonoBehaviour
    {
        public enum FirebaseStatus
        {
            None,
            Init,
            Available,
            Ready,
            Failed
        }

        public enum AuthType
        {
            None = 0,
            Facebook = 1,
            Google = 2,
            Apple = 3,
        }

        public static FirebaseSDK Instance;

#if DEBUG_ANALYTIC
        private const string LogTag = "FirebaseSDK";
#endif
        public static FirebaseStatus Status { get; private set; } = FirebaseStatus.None;


        #region events

        public event Action<FirebaseSDK> OnFirebaseInitialized;

        public event Action OnInitializeFailed;
        public event Action OnFirebaseReady;
        public event Action OnFirebaseUserIdReady;
    #endregion

    public bool Initialized { get; private set; }

        public bool AutoInitialization => _autoInitialization;

        [SerializeField]
        [Space(height: 10, order = -1), Header("Init")]
        private bool _autoInitialization = false;


        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != null)
            {
                Destroy(gameObject);
            }
        }

        void Start()
        {
            if (_autoInitialization)
            {
                if (Status == FirebaseStatus.None || !Initialized)
                {
                    Initialize();
                }
            }
        }
        void GetUserId()
        {
            if (PlayerPrefs.HasKey(ParameterName.user_pseudo_id))
            {
                OnFirebaseUserIdReady?.Invoke();
            }
            else
            {
                Firebase.Analytics.FirebaseAnalytics.GetAnalyticsInstanceIdAsync().ContinueWithOnMainThread(task =>
                {
                    if (task.IsCompleted)
                    {
                        PlayerPrefs.SetString(ParameterName.user_pseudo_id, task.Result);
                    }
                    else
                    {
                        PlayerPrefs.SetString(ParameterName.user_pseudo_id, SystemInfo.deviceUniqueIdentifier);
                        Debug.LogError("Can not get Firebase pseudo ID.");
                    }
                    OnFirebaseUserIdReady?.Invoke();
                });
            }
        }
    public void Initialize()
        {
#if FIREBASE_SDK
            Status = FirebaseStatus.Init;
            FirebaseApp.CheckAndFixDependenciesAsync()
                .ContinueWithOnMainThread
                (task =>
                {
                    if (task.Result == DependencyStatus.Available)
                    {
                        try
                        {
                            Ready();
                            Status = FirebaseStatus.Ready;
                            Initialized = true;
                            AnalyticHelper.FlushFirebaseQueue();
                            OnFirebaseReady?.Invoke();
                            GetUserId();
                        }
                        catch (Exception ex)
                        {
#if DEBUG_ANALYTIC && DEBUG_ANALYTIC_INTERNAL
                            LogError(ex.ToString());
#endif
                            Status = FirebaseStatus.Failed;
                            OnInitializeFailed?.Invoke();
                        }
                    }
                    else
                    {
#if DEBUG_ANALYTIC && DEBUG_ANALYTIC_INTERNAL
                        LogError($"Could not resolve all Firebase dependencies: {task.Result}");
#endif
                        Status = FirebaseStatus.Failed;
                        OnInitializeFailed?.Invoke();
                    }
                });
#endif
        }

        void Ready()
        {
            OnFirebaseInitialized?.Invoke(this);
#if DEBUG_ANALYTIC &&DEBUG_ANALYTIC_INTERNAL
            LogWarning("Ready");
#endif
        }

        #region Util Functions
#if DEBUG_ANALYTIC
        static void Log(string content)
        {
            LogUtils.Log(LogTag, content);
        }

        static void LogWarning(string content)
        {
            LogUtils.LogWarning(LogTag, content);
        }

        static void LogError(string content)
        {
            LogUtils.LogError(LogTag, content);
        }
#endif
        #endregion
    }
