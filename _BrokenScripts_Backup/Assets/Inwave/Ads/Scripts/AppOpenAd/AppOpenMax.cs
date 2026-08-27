//#define DEBUG_ADS_INTERNAL
using Inwave.Production.Utils;
using UnityEngine;

namespace Inwave.Production.Ads
{
    public class AppOpenMax : AppOpenAdNetwork
    {
        [SerializeField]
        private string AppOpenAdUnitId = "YOUR_AD_UNIT_ID";

#if MAX_ADS
        public override bool IsAdAvailable => MaxSdk.IsAppOpenAdReady(AppOpenAdUnitId);

        private bool _isPaused = false;
        private bool _isFocused = false;
        public override void Initialize()
        {
#if DEBUG_ADS
            LogTag = "AppOpenMax";
#endif
            MaxSdkCallbacks.OnSdkInitializedEvent += MaxSdkCallbacks_OnSdkInitializedEvent;
        }

        private void MaxSdkCallbacks_OnSdkInitializedEvent(MaxSdkBase.SdkConfiguration obj)
        {
            MaxSdkCallbacks.AppOpen.OnAdClickedEvent += AppOpen_OnAdClickedEvent;
            MaxSdkCallbacks.AppOpen.OnAdDisplayedEvent += AppOpen_OnAdDisplayedEvent;
            MaxSdkCallbacks.AppOpen.OnAdDisplayFailedEvent += AppOpen_OnAdDisplayFailedEvent;
            MaxSdkCallbacks.AppOpen.OnAdLoadedEvent += AppOpen_OnAdLoadedEvent;
            MaxSdkCallbacks.AppOpen.OnAdLoadFailedEvent += AppOpen_OnAdLoadFailedEvent;
            MaxSdkCallbacks.AppOpen.OnAdHiddenEvent += AppOpen_OnAdHiddenEvent;
            MaxSdkCallbacks.AppOpen.OnAdRevenuePaidEvent += AppOpen_OnAdRevenuePaidEvent;
        }

        private void OnDestroy()
        {
            MaxSdkCallbacks.AppOpen.OnAdClickedEvent -= AppOpen_OnAdClickedEvent;
            MaxSdkCallbacks.AppOpen.OnAdDisplayedEvent -= AppOpen_OnAdDisplayedEvent;
            MaxSdkCallbacks.AppOpen.OnAdDisplayFailedEvent -= AppOpen_OnAdDisplayFailedEvent;
            MaxSdkCallbacks.AppOpen.OnAdLoadedEvent -= AppOpen_OnAdLoadedEvent;
            MaxSdkCallbacks.AppOpen.OnAdLoadFailedEvent -= AppOpen_OnAdLoadFailedEvent;
            MaxSdkCallbacks.AppOpen.OnAdHiddenEvent -= AppOpen_OnAdHiddenEvent;
            MaxSdkCallbacks.AppOpen.OnAdRevenuePaidEvent -= AppOpen_OnAdRevenuePaidEvent;
        }
        private void OnApplicationFocus(bool focus)
        {
#if DEBUG_ADS && DEBUG_ADS_INTERNAL
            LogUtils.Log(LogTag, $"OnApplicationFocus:{focus}");
#endif
            _isFocused = focus;
        }

        private void OnApplicationPause(bool pause)
        {
#if DEBUG_ADS&&DEBUG_ADS_INTERNAL
            LogUtils.Log(LogTag, $"OnApplicationPause:{pause}");
#endif
            _isPaused = pause;
            if (!_isPaused &&_isFocused)
            {
                OnAppStateChangedEvent(AppState.Foreground);
            }
            else
            {
                OnAppStateChangedEvent(AppState.Background);
            }
        }

        public override void Load()
        {
#if DEBUG_ADS && DEBUG_ADS_INTERNAL
            LogUtils.Log(LogTag, "Load");
#endif
            MaxSdk.LoadAppOpenAd(AppOpenAdUnitId);
        }

        public override void Show()
        {
#if DEBUG_ADS && DEBUG_ADS_INTERNAL
            LogUtils.Log(LogTag, "Show");
#endif
            if (isShowingAd)
            {
                return;
            }
            MaxSdk.ShowAppOpenAd(AppOpenAdUnitId);
        }

        private void AppOpen_OnAdRevenuePaidEvent(string adUnit, MaxSdkBase.AdInfo maxAdInfo)
        {
            var impressionData = AdMax.MaxAdInfoToAdImpressionData(maxAdInfo);
            OnAdPaidEvent(impressionData);
        }

        private void AppOpen_OnAdHiddenEvent(string adUnit, MaxSdkBase.AdInfo maxAdInfo)
        {
            AdNetwork.AdInfo adInfo = AdMax.MaxAdInfoToAdInfo(maxAdInfo);
            OnAdClosedEvent(adInfo);
            Load();
        }

        private void AppOpen_OnAdLoadFailedEvent(string adUnit, MaxSdkBase.ErrorInfo maxErrorInfo)
        {
            AdNetwork.AdError adError = AdMax.MaxAdErrorToAdError(adUnit, maxErrorInfo);
            OnAdLoadFailedEvent(adError);
        }

        private void AppOpen_OnAdLoadedEvent(string adUnit, MaxSdkBase.AdInfo maxAdInfo)
        {
            AdNetwork.AdInfo adInfo = AdMax.MaxAdInfoToAdInfo(maxAdInfo);
            OnAdLoadedEvent(adInfo);
        }

        private void AppOpen_OnAdDisplayFailedEvent(string adUnit, MaxSdkBase.ErrorInfo maxErrorInfo, MaxSdkBase.AdInfo maxAdInfo)
        {
            AdNetwork.AdInfo adInfo = AdMax.MaxAdInfoToAdInfo(maxAdInfo);
            AdNetwork.AdError adError = AdMax.MaxAdErrorToAdError(adUnit, maxErrorInfo);
            OnAdShowFailedEvent(adInfo,adError);
        }

        private void AppOpen_OnAdDisplayedEvent(string adUnit, MaxSdkBase.AdInfo maxAdInfo)
        {
            AdNetwork.AdInfo adInfo = AdMax.MaxAdInfoToAdInfo(maxAdInfo);
            OnAdShowedEvent(adInfo);
        }

        private void AppOpen_OnAdClickedEvent(string adUnit, MaxSdkBase.AdInfo maxAdInfo)
        {
            AdNetwork.AdInfo adInfo = AdMax.MaxAdInfoToAdInfo(maxAdInfo);
            OnAdClickedEvent(adInfo);
        }
#endif
        }
}

