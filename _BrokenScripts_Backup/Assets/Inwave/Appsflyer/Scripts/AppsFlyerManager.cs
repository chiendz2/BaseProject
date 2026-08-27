using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using AppsFlyerSDK;
#if MAX_ADS
using Inwave.Production.Ads;
#endif

public class AppsFlyerManager : AppsFlyer, IAppsFlyerConversionData {
    #region properties

    public static AppsFlyerManager Instance;

    [Header("General:")]
    public string appsFlyerKey;
#if ADJUST
    public bool enableAdjustMapping = true;
#endif    
    public bool isDebug = false;

    [Header("IOS Settings:")]
    public string iosAppId;

    public bool requestATT = true;

    #endregion

    #region Unity functions

    private void Awake() {
        DontDestroyOnLoad(this.gameObject);
        Instance = this;
        AppsFlyer.OnRequestResponse += OnAppsFlyerStartResponse;
        AppsFlyer.OnInAppResponse += OnAppsFlyerEventResponse;
    }

    private void Start() {
#if FIREBASE_SDK
        // Guard null: nếu FirebaseSDK chưa Awake (đổi thứ tự init/prefab) thì đừng để NRE
        // abort cả Start() -> sẽ mất startSDK + SetAppsFlyerReady bên dưới -> AF không bao giờ ready.
        if (FirebaseSDK.Instance != null) {
            FirebaseSDK.Instance.OnFirebaseReady += OnFirebaseReady;
        } else {
            Debug.LogError("[Analytics][AppsFlyer] FirebaseSDK.Instance null lúc AppsFlyer Start - bỏ qua callback Firebase, vẫn start AF.");
        }
#else
        if (isDebug) {
            Debug.Log("[AppsFlyerManager] Lưu ý set callback từ firebase sau khi init thành công. VD: AppsFlyerManager.Instance.OnFirebaseReady()");
        }
#endif
#if MAX_ADS
        AdsEvents.OnAdImpressionSuccess += OnAdImpressionSuccess;
#else
        if (isDebug) {
            Debug.Log("[AppsFlyerManager] Lưu ý set callback từ Max mediation khi có impression ad thành công.VD: AppsFlyerManager.Instance.OnAdImpressionSuccess(adInfo)");
        }
#endif

        // For detailed logging
        if (isDebug) {
            AppsFlyer.setIsDebug(true);
        }

        //Start Appsflyer SDK
        bool isIos = Application.platform == RuntimePlatform.IPhonePlayer;
        var appId = isIos ? iosAppId : null;
        AppsFlyer.initSDK(appsFlyerKey, appId, this);

        if (string.IsNullOrWhiteSpace(appsFlyerKey) || (isIos && string.IsNullOrWhiteSpace(appId))) {
            Debug.LogError("[Analytics][AppsFlyer] Missing AppsFlyer dev key or iOS Apple app ID.");
        }

#if UNITY_IOS && !UNITY_EDITOR
        // 2024/04/29 Hotfix for DnI tracking
        AppsFlyer.waitForATTUserAuthorizationWithTimeoutInterval(60);
#endif
        // Purchase + AdRevenue connector: các call JNI/native này CÓ THỂ throw (thiếu native lib,
        // sandbox, platform...). Nếu throw mà không bọc thì startSDK + SetAppsFlyerReady bên dưới
        // bị skip -> _appsFlyerReady mãi false -> mọi event AF kẹt queue (Firebase vẫn chạy).
        // Bọc try/catch để lỗi connector không kéo sập việc start SDK + gửi event.
        try {
            // Purchase connector implementation
            InitPurchaseConnector(this);
        } catch (System.Exception e) {
            Debug.LogError($"[Analytics][AppsFlyer] InitPurchaseConnector failed: {e}");
        }

        try {
            // AdRevenue connector implementation
            AppsFlyerAdRevenue.start();
            if (isDebug) {
                AppsFlyerAdRevenue.setIsDebug(true);
            }
        } catch (System.Exception e) {
            Debug.LogError($"[Analytics][AppsFlyer] AppsFlyerAdRevenue.start failed: {e}");
        }

        // Critical: luôn chạy dù connector ở trên có lỗi.
        AppsFlyer.startSDK();
        AnalyticHelper.SetAppsFlyerReady();
        Debug.Log($"[Analytics][AppsFlyer] SDK start requested. appId={Application.identifier}, sdk={AppsFlyer.kAppsFlyerPluginVersion}");
    }

    private void OnDestroy() {
        AppsFlyer.OnRequestResponse -= OnAppsFlyerStartResponse;
        AppsFlyer.OnInAppResponse -= OnAppsFlyerEventResponse;

        if (Instance == this) {
            Instance = null;
        }
    }

    private static void OnAppsFlyerStartResponse(object sender, EventArgs eventArgs) {
        // logSuccess=true: SDK start chỉ chạy 1 lần, giữ để biết SDK đã kết nối server.
        LogAppsFlyerResponse("SDK start", eventArgs, logSuccess: true);
    }

    private static void OnAppsFlyerEventResponse(object sender, EventArgs eventArgs) {
        // logSuccess=false: mỗi in-app event thành công KHÔNG log nữa (tránh spam). Vẫn log khi lỗi.
        LogAppsFlyerResponse("in-app event", eventArgs, logSuccess: false);
    }

    private static void LogAppsFlyerResponse(string requestType, EventArgs eventArgs, bool logSuccess) {
        var response = eventArgs as AppsFlyerRequestEventArgs;
        if (response == null) {
            if (logSuccess)
                Debug.LogWarning($"[Analytics][AppsFlyer] {requestType} returned an unknown response.");
            return;
        }

        if (response.statusCode == 200) {
            if (logSuccess)
                Debug.Log($"[Analytics][AppsFlyer] {requestType} accepted by server (HTTP 200).");
            return;
        }

        // Mất mạng không phải lỗi tích hợp: máy bay / rớt sóng là chuyện bình thường và
        // SDK tự cache event rồi gửi lại khi có mạng. Nuốt im, đừng spam LogError.
        // code 40 = network error của AppsFlyer; kèm check reachability cho chắc.
        if (IsNetworkFailure(response))
            return;

        Debug.LogError(
            $"[Analytics][AppsFlyer] {requestType} failed. code={response.statusCode}, error={response.errorDescription}");
    }

    private const int AppsFlyerNetworkErrorCode = 40;

    private static bool IsNetworkFailure(AppsFlyerRequestEventArgs response) {
        return response.statusCode == AppsFlyerNetworkErrorCode
               || Application.internetReachability == NetworkReachability.NotReachable;
    }

    #endregion

    #region Conversion events

    private void InitPurchaseConnector(MonoBehaviour appsflyerMonoBehaviour = null) {
        AppsFlyerPurchaseConnector.init(appsflyerMonoBehaviour, Store.GOOGLE); // appsflyerMonoBehaviour is us
        AppsFlyerPurchaseConnector.setIsSandbox(false);
        // Consumable IAP is logged exactly once from IAPManager.ProcessPurchase.
        // Keep connector auto-log disabled for in-app purchases to avoid duplicate revenue.
        AppsFlyerPurchaseConnector.setAutoLogPurchaseRevenue(
            AppsFlyerAutoLogPurchaseRevenueOptions.AppsFlyerAutoLogPurchaseRevenueOptionsAutoRenewableSubscriptions);
        AppsFlyerPurchaseConnector.setPurchaseRevenueValidationListeners(true);
        AppsFlyerPurchaseConnector.build();
        AppsFlyerPurchaseConnector.startObservingTransactions();
    }

    public void OnFirebaseReady() {
        // Init FirebaseMessaging
        Firebase.Messaging.FirebaseMessaging.TokenReceived += OnTokenReceived;
        MappingUserIdTracking.Instance.StartTracking();
    }

#if MAX_ADS
    private void OnAdImpressionSuccess(AdNetwork.AdImpressionData revenueData) {

        Dictionary<string, string> dic = new Dictionary<string, string>();
        dic.Add(AFAdRevenueEvent.AD_UNIT, revenueData.UnitName);
        dic.Add(AFAdRevenueEvent.AD_TYPE, revenueData.Format);
        if (!string.IsNullOrEmpty(revenueData.Placement)) {
            dic.Add(AFAdRevenueEvent.PLACEMENT, revenueData.Placement);
        }

        AFAdRevenueData logRevenue = new(revenueData.Source, MediationNetwork.ApplovinMax, revenueData.Currency,
            revenueData.Revenue);
        AppsFlyer.logAdRevenue(logRevenue, dic);
        if (isDebug) {
            Debug.Log("[AppsFlyerManager] TrackRevenue " + revenueData.Source + " " + revenueData.Revenue + " " +
                      revenueData.Currency + " " + AFMiniJSON.Json.Serialize(dic));
        }
    }
#else
    private void OnAdImpressionSuccess(MaxSdkBase.AdInfo revenueData) {

        Dictionary<string, string> dic = new Dictionary<string, string>();
        dic.Add(AFAdRevenueEvent.AD_UNIT, revenueData.AdUnitIdentifier);
        dic.Add(AFAdRevenueEvent.AD_TYPE, revenueData.AdFormat);
        if (!string.IsNullOrEmpty(revenueData.Placement)) {
            dic.Add(AFAdRevenueEvent.PLACEMENT, revenueData.Placement);
        }

        AFAdRevenueData logRevenue = new(revenueData.NetworkPlacement, MediationNetwork.ApplovinMax, "USD",
            revenueData.Revenue);
        AppsFlyer.logAdRevenue(logRevenue, dic);
        if (isDebug) {
            Debug.Log("[AppsFlyerManager] TrackRevenue " + revenueData.NetworkName + " " + revenueData.Revenue + " " +
                      "USD" + " " + AFMiniJSON.Json.Serialize(dic));
        }
    }
#endif

    public void onConversionDataSuccess(string conversionData) {
        if (isDebug) {
            Debug.Log("[AppsFlyerManager] onConversionDataSuccess " + conversionData);
        }
    }

    public void onConversionDataFail(string error) {
        if (isDebug) {
            Debug.Log("[AppsFlyerManager] onConversionDataFail" + error);
        }
    }

    public void onAppOpenAttribution(string attributionData) {
        if (isDebug) {
            Debug.Log("[AppsFlyerManager] onAppOpenAttribution" + attributionData);
        }
    }

    public void onAppOpenAttributionFailure(string error) {
        if (isDebug) {
            Debug.Log("[AppsFlyerManager] onAppOpenAttributionFailure" + error);
        }
    }

    #endregion

    #region Uninstall measurement Tracking

    private bool _tokenSent;
#if UNITY_IOS
    private void Update() {
        if (!_tokenSent && Application.platform == RuntimePlatform.IPhonePlayer && Time.frameCount % 30 == 0) {
            byte[] token = null;

            token = UnityEngine.iOS.NotificationServices.deviceToken;
            if (token != null) {
                registerUninstall(token); //IOS
                _tokenSent = true;
            }
            if (isDebug) {
                Debug.Log("[AppsFlyerManager] Uninstall measurement Tracking Token " + token);
            }

        }
    }
#endif
    public void OnTokenReceived(object sender, Firebase.Messaging.TokenReceivedEventArgs token) {
#if UNITY_ANDROID
        AppsFlyer.updateServerUninstallToken(token.Token);
        if (isDebug) {
            Debug.Log("[AppsFlyerManager] Uninstall measurement Tracking Token " + token.Token);
        }
#endif
    }

    #endregion
}
