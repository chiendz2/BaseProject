#define DEBUG_ANALYTIC_INTERNAL
using System.Collections;
using Inwave.Production.Utils;
using UnityEngine;

public class FirebaseInitializer : MonoBehaviour
    {
#if DEBUG_ANALYTIC
        private const string LogTag = "FirebaseInitializer";
#endif
        #region properties

        [SerializeField]
        private bool _enableRemoteSync = true;

        #endregion

        private IEnumerator Start()
        {
            PreInitialize();
            yield return null;
            Initialize();
        }

        private void PreInitialize()
        {
#if FIREBASE_SDK
            FirebaseSDK.Instance.OnFirebaseInitialized += OnFirebaseInitialized;
            FirebaseSDK.Instance.OnFirebaseReady += OnFirebaseReady;
            FirebaseSDK.Instance.OnInitializeFailed += OnInitializeFailed;
            FirebaseSDK.Instance.OnFirebaseUserIdReady += OnFirebaseUserIdReady;
#endif
    }
        private void OnFirebaseUserIdReady()
        {
            InitializeRemoteConfig();
            RemoteConfig.Instance.LoadRemoteConfig();
        }
    private void InitializeRemoteConfig()
        {
#if DEBUG_ANALYTIC &&DEBUG_ANALYTIC_INTERNAL
            LogUtils.LogWarning(LogTag,$"Enable Remote Sync:{_enableRemoteSync}");
#endif
            RemoteConfig.Instance.SetEnableRemoteSync(_enableRemoteSync);

            RemoteConfig.Instance.OnEndLoad += OnRemoreConfigEndLoad;
            if (!RemoteConfig.Instance.AutoInitialization && !RemoteConfig.Instance.Initialized)
            {
                RemoteConfig.Instance.Initialize();
            }
        }


        protected virtual void Initialize()
        {
            if (!FirebaseSDK.Instance.Initialized)
            {
                FirebaseSDK.Instance.Initialize();
            }
        }
        protected virtual void OnInitializeFailed()
        {
           RemoteConfig.Instance.EndLoad();
        }

        protected virtual void OnFirebaseReady()
        { 
        }

        protected virtual void OnFirebaseInitialized(FirebaseSDK obj)
        {
            
        }

        protected virtual void OnRemoreConfigEndLoad()
        {
        }
    }

