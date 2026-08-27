//#define DEBUG_ADS_INTERNAL
using UnityEngine;
using System.Collections.Generic;
using System;
using Inwave.Production.Utils;
using static Inwave.Production.Ads.AdNetwork;

namespace Inwave.Production.Ads
{


    //Main AdsManager
    public partial class AdsManager : MonoBehaviour
    {
#if DEBUG_ADS
        private const string LogTagAdsManagerMain = "AdsManager Main";
#endif
        public static AdsManager Instance;


        [Header("+Main AdsManager")]
        public AdNetwork adn;

        [Header("Initialization")]
        [SerializeField]
        private bool _autoInitialize = true;
        private float _lastTimeShowAds = float.MinValue;

        public bool AutoInitialize => _autoInitialize;
        public float LastTimeShowAds => _lastTimeShowAds;
        /// <summary>
        /// Ads placement for rewarded Ads. You can implement IPlacement for customizing
        /// </summary>

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }

        void Start()
        {
            SetPrivacyMetaData();
            //_adErrors = new Dictionary<AdErrorType, string>
            //{
            //    {AdErrorType.FsShow, null},
            //    {AdErrorType.FsCaching, null},
            //    {AdErrorType.RwShow, null},
            //    {AdErrorType.RwCaching, null}
            //};
            if (_autoInitialize)
            {
                Initialize();
            }
            
        }

        //public string GetAdError(AdErrorType type)
        //{
        //    return _adErrors[type];
        //}

        public void Initialize()
        {
#if DEBUG_ADS&&DEBUG_ADS_INTERNAL
            LogUtils.LogWarning(LogTagAdsManagerMain,"Initialized ...");
#endif
            //Placement = new DefaultRewardAdsPlacement();
            if (adn != null)
            {
                adn.Init();
                adn.OnSetCustomSegmentData += OnSetCustomSegmentDataHandler;
                InitInterstitial();
                InitRewarded();
                InitBanner();
                InitializeMRec();
            }
#if DEBUG_ADS
            else
            {
                LogUtils.LogError(LogTagAdsManagerMain, "adn is null!");
            }
#endif
            InitAppOpen();
            InitBannerCollapsible();
            InitFakeInterstitial();
        }

        #region Segment data

        public void SetCustomSegmentData(List<AdNetwork.SegmentCustomData> segmentData)
        {
            adn?.SetSegmentCustomData(segmentData);
        }

        public void SetCustomSegmentData(List<string> segmentKeys)
        {
            List<AdNetwork.SegmentCustomData> segmentData = new List<AdNetwork.SegmentCustomData>(segmentKeys.Count);
            foreach (var segmentKey in segmentKeys)
            {
                segmentData.Add(new AdNetwork.SegmentCustomData
                {
                    Key = segmentKey,
                    Value = "1"
                });
            }
            SetCustomSegmentData(segmentData);
        }

        private void OnSetCustomSegmentDataHandler(string key, string value)
        {
            AdsEvents.OnSetCustomSegmentDataEvent(key,value);
        }

        #endregion


        public void OnApplicationPause(bool pauseStatus)
        {
            if (adn != null)
            {
                adn.AppPause(pauseStatus);
            }
        }

        public void OnImpressionSuccessEvent(AdNetwork.AdImpressionData impressionData)
        {
            AdsEvents.OnAdImpressionSuccessEvent(impressionData);
        }

        #region UnityAds

        private void SetPrivacyMetaData()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
        AndroidJavaClass unityPlayerClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        AndroidJavaObject unityActivity = unityPlayerClass.GetStatic<AndroidJavaObject>("currentActivity");

        AndroidJavaObject metaDataObj = new AndroidJavaObject("com.unity3d.ads.metadata.MetaData", unityActivity);
        metaDataObj.Call<bool>("set", "privacy.mode", "mixed");
        metaDataObj.Call<bool>("set", "user.nonbehavioral", false);
        metaDataObj.Call("commit");
#endif
        }

        #endregion

        #region Util Functions

        public bool IsConnectedToInternet()
        {
            bool isNetWorkAvailable = Application.internetReachability != NetworkReachability.NotReachable;
            return isNetWorkAvailable;
        }

        public static Dictionary<string, object> AdImpressionDataToFirebaseParameter(AdImpressionData impressionData)
        {
            Dictionary<string, object> paramFirebase = new Dictionary<string, object>
            {
                { "ad_platform", impressionData.Platform },
                { "ad_format", impressionData.Format },
                { "ad_unit_name", impressionData.UnitName },
                { "ad_source", impressionData.Source },
                { "value", impressionData.Revenue.ToString() },
                { "currency", impressionData.Currency },
            };
            return paramFirebase;
        }
#endregion
    }
}