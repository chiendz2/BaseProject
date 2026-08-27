using UnityEngine;
#if APPSFLYER_SDK
using AppsFlyerSDK;
#endif

namespace GIKCore
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-85)]
#if APPSFLYER_SDK
    public class AppsFlyerManager : MonoBehaviour, IAppsFlyerConversionData
#else
    public class AppsFlyerManager : MonoBehaviour
#endif
    {
        private const string LogTag = "[AppsFlyerManager]";

        public static AppsFlyerManager Instance { get; private set; }

#pragma warning disable CS0169, CS0414
        [Header("Keys")]
        [Tooltip("AppsFlyer dev key from the dashboard.")]
        [SerializeField] private string _devKey;

        [Tooltip("Apple app id, digits only. iOS attribution does not work without it.")]
        [SerializeField] private string _iosAppId;

        [Header("Debug")]
        [Tooltip("Verbose AppsFlyer SDK logging. Leave off for release builds.")]
        [SerializeField] private bool _verboseLogging;
#pragma warning restore CS0169, CS0414

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
            StartSdk();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void StartSdk()
        {
#if APPSFLYER_SDK
            if (string.IsNullOrEmpty(_devKey))
            {
                Debug.LogError($"{LogTag} Dev key is empty, AppsFlyer will not start.");
                return;
            }

            bool isIos = Application.platform == RuntimePlatform.IPhonePlayer;
            if (isIos && string.IsNullOrEmpty(_iosAppId))
            {
                Debug.LogError($"{LogTag} iOS app id is empty, AppsFlyer will not start.");
                return;
            }

            AppsFlyer.setIsDebug(_verboseLogging);
            AppsFlyer.initSDK(_devKey, isIos ? _iosAppId : null, this);
            AppsFlyer.startSDK();
            AppsFlyerProvider.SetReady();
#else
            Debug.LogWarning($"{LogTag} APPSFLYER_SDK is not defined, AppsFlyer events are dropped.");
#endif
        }

#if APPSFLYER_SDK
        public void onConversionDataSuccess(string conversionData)
        {
            if (_verboseLogging)
            {
                Debug.Log($"{LogTag} Conversion data: {conversionData}");
            }
        }

        public void onConversionDataFail(string error)
        {
            Debug.LogError($"{LogTag} Conversion data failed: {error}");
        }

        public void onAppOpenAttribution(string attributionData)
        {
            if (_verboseLogging)
            {
                Debug.Log($"{LogTag} App open attribution: {attributionData}");
            }
        }

        public void onAppOpenAttributionFailure(string error)
        {
            Debug.LogError($"{LogTag} App open attribution failed: {error}");
        }
#endif
    }
}
