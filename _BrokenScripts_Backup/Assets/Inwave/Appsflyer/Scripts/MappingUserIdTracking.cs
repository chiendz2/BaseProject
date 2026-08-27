using System;
using System.Collections;
using System.Collections.Generic;
using AppsFlyerSDK;
#if ADJUST
using AdjustSdk;
#endif
using Firebase.Analytics;
using Firebase.Extensions;
using Unity.VisualScripting;
using UnityEngine;
#if UNITY_IOS
using Unity.Advertisement.IosSupport;
using UnityEngine.iOS;
#endif
using UnityEngine.Rendering;
using UnityEngine.Serialization;

public class MappingUserIdTracking : MonoBehaviour {
    private const string EventNameMappingID    = "mapping_id";
    private       string _idfa                 = null; //Global variant for async call
    private       string _idfv                 = null;
    private       string _adid                 = null;
    private       string _user_pseudo_id       = null;
    private       bool   _is_trigged_pseudo_id = false;
    
    private       AppsFlyerManager      _appsFlyerManager;
    public static MappingUserIdTracking Instance;

    private void Awake() {
        Instance = this;
    }
    public void StartTracking() {
        _appsFlyerManager = AppsFlyerManager.Instance;
        if (_appsFlyerManager.isDebug) {
            Debug.Log("[AppsFlyerManager] MappingUserIdTracking.StartTracking");
        }
        //Get firebase pseudo_id
        _user_pseudo_id = PlayerPrefs.GetString("_user_pseudo_id");
        if (string.IsNullOrEmpty(_user_pseudo_id)) {
            FirebaseAnalytics.GetAnalyticsInstanceIdAsync().ContinueWithOnMainThread(task => {
                if (task.IsCompleted) {
                    _user_pseudo_id = task.Result;
                    PlayerPrefs.SetString("_user_pseudo_id", _user_pseudo_id);
                } else {
                    Debug.LogError("[AppsFlyerManager] Firebase Get _pseudo_id Error");
                }
            });
        }
#if ADJUST
        if (_appsFlyerManager.enableAdjustMapping) {
            Adjust.GetAdid(adid => { _adid = adid; });
        }
#endif       
        StartCoroutine(CoDelayTracking());
    }

    private IEnumerator CoDelayTracking() {
        //Delay a bit for SDK init
        yield return new WaitForSeconds(1f);
        
        //Get idfv && map to MAX SDK
        _idfv = GetIdfv();
        if (!string.IsNullOrEmpty(_idfv)) {
            MaxSdk.SetUserId(_idfv);
            if (_appsFlyerManager.isDebug) {
                Debug.Log("[AppsFlyerManager] MaxSdk.SetUserId " + _idfv);
            }
            
        }
        
        //Trigger mapping for appsflyer
        StartCoroutine(MmpMapping());
        
#if UNITY_IOS
        // Check the user's consent status.
        // If the status is undetermined, display the request request:
        if(_appsFlyerManager.requestATT && ATTrackingStatusBinding.GetAuthorizationTrackingStatus() == ATTrackingStatusBinding.AuthorizationTrackingStatus.NOT_DETERMINED) {
            ATTrackingStatusBinding.RequestAuthorizationTracking();
        }
        // wait user response ATT
        yield return new WaitForSeconds(1f);

        string attEventName = ATTrackingStatusBinding.GetAuthorizationTrackingStatus() ==
                              ATTrackingStatusBinding.AuthorizationTrackingStatus.DENIED
            ? "FTUE_not_accept_tracking_ios"
            : "FTUE_accept_tracking_ios";
        LogEvent(attEventName);
        if (_appsFlyerManager.isDebug) {
            Debug.Log("[AppsFlyerManager] LogEvent " + attEventName);
        }
        Application.RequestAdvertisingIdentifierAsync((string advertisingId, bool trackingEnabled, string error) => {
            if (trackingEnabled) {
                _idfa = advertisingId;
            }
            if (_appsFlyerManager.isDebug) {
                Debug.Log("[AppsFlyerManager] advertisingId " + advertisingId + " " + trackingEnabled + " " + error);
            }
        });
#else
        _idfa = GetAndroidAdvertiserId();
#endif

        
        float currentTime = Time.time;
        yield return new WaitUntil(() => Time.time - currentTime > 3 || !string.IsNullOrEmpty(_idfa));
        
        //After get _idfa --> mapping firebase
        FirebaseMapping();
        
    }

    private IEnumerator MmpMapping() {
        Dictionary<string, string> afParams = new() { { "idfv", _idfv } };
        if (!string.IsNullOrEmpty(_user_pseudo_id)) {
            afParams.Add("user_pseudo_id", _user_pseudo_id);
        }
        AppsFlyer.sendEvent(EventNameMappingID, afParams);

#if ADJUST
        AdjustEvent adjustEvent = new AdjustEvent(EventNameMappingID);
        if (_appsFlyerManager.enableAdjustMapping) {
            adjustEvent.AddPartnerParameter("idfv", _idfv);
            if (!string.IsNullOrEmpty(_user_pseudo_id)) {
                adjustEvent.AddPartnerParameter("user_pseudo_id", _user_pseudo_id);
            }

            Adjust.TrackEvent(adjustEvent);
            if (_appsFlyerManager.isDebug) {
                Debug.Log("[AppsFlyerManager] Adjust.TrackEvent " + EventNameMappingID +
                          AFMiniJSON.Json.Serialize(adjustEvent.PartnerParameters));
            }
        }
#endif     
        
        if (_appsFlyerManager.isDebug) {
            Debug.Log("[AppsFlyerManager] AppsFlyer.sendEvent " + EventNameMappingID +
                      AFMiniJSON.Json.Serialize(afParams));
        }
        
        //if empty _pseudo_id, wait and trigger mapping again
        if (string.IsNullOrEmpty(_user_pseudo_id)) {
            float currentTime = Time.time;
            yield return new WaitUntil(() => Time.time - currentTime > 30 || !string.IsNullOrEmpty(_user_pseudo_id));

            if (!string.IsNullOrEmpty(_user_pseudo_id)) {
                afParams.Add("user_pseudo_id", _user_pseudo_id);
                AppsFlyer.sendEvent(EventNameMappingID, afParams);
#if ADJUST
                if (_appsFlyerManager.enableAdjustMapping) {
                    adjustEvent.AddPartnerParameter("user_pseudo_id", _user_pseudo_id);
                    Adjust.TrackEvent(adjustEvent);
                    if (_appsFlyerManager.isDebug) {
                        Debug.Log("[AppsFlyerManager] Adjust.TrackEvent Retry " + EventNameMappingID +
                                  AFMiniJSON.Json.Serialize(adjustEvent.PartnerParameters));
                    }
                }
#endif   
                
                if (_appsFlyerManager.isDebug) {
                    Debug.Log("[AppsFlyerManager] AppsFlyer.sendEvent Retry: " + EventNameMappingID +
                              AFMiniJSON.Json.Serialize(afParams));
                }
            }
        }
    }

    private void FirebaseMapping() {
        Dictionary<string, object> param = new();
        param.Add("appsflyer_id", AppsFlyer.getAppsFlyerId());
#if ADJUST
        if (_appsFlyerManager.enableAdjustMapping) {
            param.Add("adid", _adid);
        }
#endif
        param.Add("idfa", _idfa);
        param.Add("idfv", _idfv);
        LogEvent(EventNameMappingID, param);
        if (_appsFlyerManager.isDebug) {
            Debug.Log("[AppsFlyerManager] Firebase LogEvent " + EventNameMappingID +
                      AFMiniJSON.Json.Serialize(param));
        }
    }
    
    private static string GetIdfv() {
#if UNITY_IOS
        return Device.vendorIdentifier;
#else
        return SystemInfo.deviceUniqueIdentifier;
#endif
    }
    
    private string GetAndroidAdvertiserId() {
        string advertisingID = null;

#if UNITY_ANDROID && !UNITY_EDITOR
        try {
            // Lấy UnityPlayer và Activity
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

            // Gọi AdvertisingIdClient
            AndroidJavaClass advertisingIdClient =
                new AndroidJavaClass("com.google.android.gms.ads.identifier.AdvertisingIdClient");
            AndroidJavaObject adInfo =
                advertisingIdClient.CallStatic<AndroidJavaObject>("getAdvertisingIdInfo", currentActivity);

            // Lấy ID từ adInfo
            advertisingID = adInfo.Call<string>("getId");
            if (_appsFlyerManager.isDebug) {
                Debug.Log("[AppsFlyerManager] advertisingId " + advertisingID);
            }
        } catch (Exception e) {
            if (_appsFlyerManager.isDebug) {
                Debug.Log("[AppsFlyerManager] Failed to get Advertising ID: {e.Message}");
            }
        }
#endif
        return advertisingID;
    }

    private void LogEvent(string eventName, Dictionary<string, object> parameters = null)
    {
#if FIREBASE_SDK
        AnalyticHelper.LogEvent(eventName, parameters);
#else
        if (parameters != null && parameters.Count > 0) {
            // Chuyển đổi Dictionary<string, object> sang params Parameter[]
            List<Parameter> firebaseParameters = new List<Parameter>();

            foreach (var param in parameters) {
                // FirebaseAnalytics chỉ hỗ trợ một số kiểu dữ liệu, nên kiểm tra và chuyển đổi
                if (param.Value is string) {
                    firebaseParameters.Add(new Parameter(param.Key, param.Value.ToString()));
                } else if (param.Value is int) {
                    firebaseParameters.Add(new Parameter(param.Key,
                        Convert.ToInt64(param.Value))); // Firebase yêu cầu int64
                } else if (param.Value is long) {
                    firebaseParameters.Add(new Parameter(param.Key, (long) param.Value));
                } else if (param.Value is double) {
                    firebaseParameters.Add(new Parameter(param.Key, (double) param.Value));
                } else {
                    Debug.Log(
                        $"[AppsFlyerManager] Unsupported parameter type for key: {param.Key}, value: {param.Value}");
                }
            }

            // Gọi FirebaseAnalytics.LogEvent với các tham số đã chuyển đổi
            FirebaseAnalytics.LogEvent(eventName, firebaseParameters.ToArray());
        } else {
            // Nếu không có parameters, chỉ log event name
            FirebaseAnalytics.LogEvent(eventName);
        }
#endif
    }
}