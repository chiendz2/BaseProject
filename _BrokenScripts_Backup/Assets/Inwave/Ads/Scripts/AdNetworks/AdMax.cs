using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Inwave.Production.Ads.AdNetwork;
#if MAX_ADS
using static MaxSdkBase;
#endif
namespace Inwave.Production.Ads
{

    public class AdMax : AdNetwork
    {
        [SerializeField] private string MaxSdkKey = "ENTER_MAX_SDK_KEY_HERE";
        [SerializeField] private string InterstitialAdUnitId = "ENTER_INTERSTITIAL_AD_UNIT_ID_HERE";
        [SerializeField] private string RewardedAdUnitId = "ENTER_REWARD_AD_UNIT_ID_HERE";
        [SerializeField] private string BannerAdUnitId = "ENTER_BANNER_AD_UNIT_ID_HERE";
        [SerializeField] private string MRecAdUnitId = "ENTER_MREC_AD_UNIT_ID_HERE";
        private bool isBannerShowing;

        private int interstitialRetryAttempt;
        private int rewardedRetryAttempt;

        private AdsManager _adsManager => AdsManager.Instance;

#if MAX_ADS
        public override void Init()
        {
            MaxSdkCallbacks.OnSdkInitializedEvent += sdkConfiguration =>
            {
                InitInterstitial();
                InitializeRewardedAds();
                InitializeBannerAds();
                InitializeMRec();
            };

            //MaxSdk.SetSdkKey(MaxSdkKey);
            MaxSdk.InitializeSdk();

        }

        private void TrackRevenue(MaxSdkBase.AdInfo adInfo)
        {
            var impressionData = MaxAdInfoToAdImpressionData(adInfo);
            _adsManager.OnImpressionSuccessEvent(impressionData);
        }

        /// <summary>
        /// for MAX Mediation, only support one item - the first one
        /// </summary>
        /// <param name="segmentData"></param>
        public override void SetSegmentCustomData(List<SegmentCustomData> segmentData)
        {
            if (segmentData.Count > 0)
            {
                var segment = segmentData[0];
                //MaxSdk.UserSegment.Name = segment.Value;
                OnSetCustomSegmentDataEvent(segment.Key,segment.Value);
            }
        }


        #region Interstitial

        private void InitInterstitial()
        {
            MaxSdkCallbacks.Interstitial.OnAdClickedEvent += Interstitial_OnAdClickedEvent;
            MaxSdkCallbacks.Interstitial.OnAdLoadedEvent += Interstitial_OnAdLoadedEvent;
            MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent += Interstitial_OnAdLoadFailedEvent;
            MaxSdkCallbacks.Interstitial.OnAdDisplayedEvent += Interstitial_OnAdDisplayedEvent;
            MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent += Interstitial_OnAdDisplayFailedEvent;
            MaxSdkCallbacks.Interstitial.OnAdHiddenEvent += Interstitial_OnAdHiddenEvent;
            MaxSdkCallbacks.Interstitial.OnAdRevenuePaidEvent += Interstitial_OnAdRevenuePaidEvent;
        }

        private void Interstitial_OnAdRevenuePaidEvent(string adUnitId, MaxSdkBase.AdInfo maxAdInfo)
        {
            TrackRevenue(maxAdInfo);
        }

        private void Interstitial_OnAdHiddenEvent(string adUnitId, MaxSdkBase.AdInfo maxAdInfo)
        {
            _adsManager.SetCanShowAppOpenAdd(true);
            AdInfo adInfo = MaxAdInfoToAdInfo(maxAdInfo);
            _adsManager.InterstitialAdClosedEvent(adInfo);
        }

        private void Interstitial_OnAdDisplayFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo maxAdError, MaxSdkBase.AdInfo maxAdInfo)
        {
            _adsManager.SetCanShowAppOpenAdd(true);
            AdInfo adInfo = MaxAdInfoToAdInfo(maxAdInfo);
            AdError adError = MaxAdErrorToAdError(adUnitId,maxAdError);
            _adsManager.InterstitialAdShowFailedEvent(adInfo,adError);
        }

        private void Interstitial_OnAdDisplayedEvent(string adUnitId, MaxSdkBase.AdInfo maxAdInfo)
        {
            AdInfo adInfo = MaxAdInfoToAdInfo(maxAdInfo);
            _adsManager.InterstitialAdShowSucceededEvent(adInfo);
        }

        private void Interstitial_OnAdLoadFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo adMaxError)
        {
            AdError adError = MaxAdErrorToAdError(adUnitId, adMaxError);
            _adsManager.InterstitialAdLoadFailedEvent(adError);
        }

        private void Interstitial_OnAdLoadedEvent(string adUnitId, MaxSdkBase.AdInfo maxAdInfo)
        {
            AdInfo adInfo = MaxAdInfoToAdInfo(maxAdInfo);
            _adsManager.InterstitialAdLoadSucceedEvent(adInfo);
        }

        private void Interstitial_OnAdClickedEvent(string adUnitId, MaxSdkBase.AdInfo maxAdInfo)
        {
            AdInfo adInfo = MaxAdInfoToAdInfo(maxAdInfo);
            _adsManager.InterstitialAdClickedEvent(adInfo);
        }
        

        public override void LoadInterstitial()
        {
            MaxSdk.LoadInterstitial(InterstitialAdUnitId);
        }

        public override bool IsInterstitialReady()
        {
            return MaxSdk.IsInterstitialReady(InterstitialAdUnitId);
        }

        public override void ShowInterstitial(AdInfo adInfo)
        {
            _adsManager.SetCanShowAppOpenAdd(false);
            MaxSdk.ShowInterstitial(InterstitialAdUnitId);
            _adsManager.InterstitialAdShowEvent(adInfo);
        }
        #endregion

        #region Rewarded

        private void InitializeRewardedAds()
        {
            MaxSdkCallbacks.Rewarded.OnAdClickedEvent += Rewarded_OnAdClickedEvent;
            MaxSdkCallbacks.Rewarded.OnAdDisplayedEvent += Rewarded_OnAdDisplayedEvent;
            MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent += Rewarded_OnAdDisplayFailedEvent;
            MaxSdkCallbacks.Rewarded.OnAdHiddenEvent += Rewarded_OnAdHiddenEvent;
            MaxSdkCallbacks.Rewarded.OnAdLoadedEvent += Rewarded_OnAdLoadedEvent;
            MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent += Rewarded_OnAdLoadFailedEvent;
            MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent += Rewarded_OnAdReceivedRewardEvent;
            MaxSdkCallbacks.Rewarded.OnAdRevenuePaidEvent += Rewarded_OnAdRevenuePaidEvent;

            LoadRewardedVideo();
        }

        private void Rewarded_OnAdRevenuePaidEvent(string unitId, MaxSdkBase.AdInfo maxAdInfo)
        {
            TrackRevenue(maxAdInfo);
        }

        public override void LoadRewardedVideo(string placement = null)
        {
            MaxSdk.LoadRewardedAd(RewardedAdUnitId);
        }

        public override void ShowRewardedVideo(string placement = null)
        {
            MaxSdk.ShowRewardedAd(RewardedAdUnitId);
            _adsManager.SetCanShowAppOpenAdd(false);
        }

        public override bool IsRewardedVideoAvailable()
        {
            return MaxSdk.IsRewardedAdReady(RewardedAdUnitId);
        }

        private void Rewarded_OnAdReceivedRewardEvent(string adUnit, MaxSdkBase.Reward reward, MaxSdkBase.AdInfo maxAdInfo)
        {
            AdInfo adInfo = MaxAdInfoToAdInfo(maxAdInfo);
            _adsManager.RewardedVideoAdRewardedEvent(adInfo);
        }

        private void Rewarded_OnAdLoadFailedEvent(string unitId, MaxSdkBase.ErrorInfo maxAdError)
        {
            AdError adError = MaxAdErrorToAdError(unitId,maxAdError);
            _adsManager.RewardedVideoAdLoadFailedEvent(adError);
            AdInfo adInfo = new AdInfo
            {
                UnitId = unitId,
                Format = AdsValues.AD_FORMAT_REWARDED,
                Platform = AdsValues.AD_PLATFORM_APPLOVIN,
                Mediation = AdsValues.AD_MEDIATION_UNKNOWN
            };
            _adsManager.RewardedVideoAvailabilityChangedEvent(false, adInfo);
        }

        private void Rewarded_OnAdLoadedEvent(string unitId, MaxSdkBase.AdInfo maxAdInfo)
        {
            AdInfo adInfo = MaxAdInfoToAdInfo(maxAdInfo);
            _adsManager.RewardedVideoAdLoadSucceedEvent(adInfo);
            _adsManager.RewardedVideoAvailabilityChangedEvent(true,adInfo);
        }

        private void Rewarded_OnAdHiddenEvent(string unitId, MaxSdkBase.AdInfo maxAdInfo)
        {
            AdInfo adInfo = MaxAdInfoToAdInfo(maxAdInfo);
            _adsManager.RewardedVideoAdClosedEvent(adInfo);
            _adsManager.SetCanShowAppOpenAdd(true);
        }

        private void Rewarded_OnAdDisplayFailedEvent(string unitId, MaxSdkBase.ErrorInfo maxAdError, MaxSdkBase.AdInfo maxAdInfo)
        {
            AdInfo adInfo = MaxAdInfoToAdInfo(maxAdInfo);
            AdError adError = MaxAdErrorToAdError(unitId, maxAdError);
            _adsManager.RewardedVideoAdShowFailedEvent(adInfo,adError);
            _adsManager.SetCanShowAppOpenAdd(true);
        }

        private void Rewarded_OnAdDisplayedEvent(string unitId, MaxSdkBase.AdInfo maxAdInfo)
        {
            AdInfo adInfo = MaxAdInfoToAdInfo(maxAdInfo);
            _adsManager.RewardedVideoAdOpenedEvent(adInfo);
        }

        private void Rewarded_OnAdClickedEvent(string unitId, MaxSdkBase.AdInfo maxAdInfo)
        {
            AdInfo adInfo = MaxAdInfoToAdInfo(maxAdInfo);
            _adsManager.RewardedVideoClickedEvent(adInfo);   
        }

        #endregion

        #region Banner

        private void InitializeBannerAds()
        {
            if (!_adsManager.EnableBanner)
            {
                return;
            }
            MaxSdkCallbacks.Banner.OnAdClickedEvent += Banner_OnAdClickedEvent;
            MaxSdkCallbacks.Banner.OnAdLoadedEvent += Banner_OnAdLoadedEvent;
            MaxSdkCallbacks.Banner.OnAdLoadFailedEvent += Banner_OnAdLoadFailedEvent;
            MaxSdkCallbacks.Banner.OnAdCollapsedEvent += Banner_OnAdCollapsedEvent;
            MaxSdkCallbacks.Banner.OnAdExpandedEvent += Banner_OnAdExpandedEvent;
            MaxSdkCallbacks.Banner.OnAdRevenuePaidEvent += Banner_OnAdRevenuePaidEvent;
            CreateBanner();
        }

        private MaxSdkBase.BannerPosition GetMaxBannerPosition()
        {
            MaxSdkBase.BannerPosition bannerPosition = MaxSdkBase.BannerPosition.BottomCenter;
            switch (_adsManager.BannerPosition)
            {
                case BannerPosition.TOP:
                    bannerPosition = MaxSdkBase.BannerPosition.TopCenter;
                    break;
                case BannerPosition.BOTTOM:
                    bannerPosition = MaxSdkBase.BannerPosition.BottomCenter;
                    break;
                default:
                    break;
            }

            return bannerPosition;
        }
        private void CreateBanner()
        {
            MaxSdkBase.BannerPosition bannerPosition = GetMaxBannerPosition();
            MaxSdk.CreateBanner(BannerAdUnitId, bannerPosition);
            MaxSdk.SetBannerBackgroundColor(BannerAdUnitId, Color.clear);
        }

        private void Banner_OnAdRevenuePaidEvent(string adUnit, MaxSdkBase.AdInfo maxAdInfo)
        {
            TrackRevenue(maxAdInfo);
            if (maxAdInfo.AdFormat == AdsValues.AD_FORMAT_BANNER)
            {
                AdInfo adInfo = AdMax.MaxAdInfoToAdInfo(maxAdInfo);
                AdsEvents.Banner.OnAdScreenPresentedEvent(adInfo);
            }
        }

        private void Banner_OnAdExpandedEvent(string adUnit, MaxSdkBase.AdInfo maxAdInfo)
        {
            AdInfo adInfo = AdMax.MaxAdInfoToAdInfo(maxAdInfo);
            _adsManager.BannerAdScreenPresentedEvent(adInfo);
        }

        private void Banner_OnAdCollapsedEvent(string adUnit, MaxSdkBase.AdInfo maxAdInfo)
        {
            AdInfo adInfo = AdMax.MaxAdInfoToAdInfo(maxAdInfo);
            _adsManager.BannerAdScreenDismissedEvent(adInfo);
        }

        private void Banner_OnAdLoadFailedEvent(string adUnit, MaxSdkBase.ErrorInfo maxAdError)
        {
            AdError adError = AdMax.MaxAdErrorToAdError(adUnit,maxAdError);
            _adsManager.BannerAdLoadFailedEvent(adError);
        }

        private void Banner_OnAdLoadedEvent(string adUnit, MaxSdkBase.AdInfo maxAdInfo)
        {
            AdInfo adInfo = AdMax.MaxAdInfoToAdInfo(maxAdInfo);
            _adsManager.BannerAdLoadedEvent(adInfo);
        }

        private void Banner_OnAdClickedEvent(string adUnit, MaxSdkBase.AdInfo maxAdInfo)
        {
            AdInfo adInfo = AdMax.MaxAdInfoToAdInfo(maxAdInfo);
            _adsManager.BannerAdClickedEvent(adInfo);
            _adsManager.SetCanShowAppOpenAdd(false);
        }

        public override void LoadBanner(BannerSize size = BannerSize.SMART, BannerPosition position =
 BannerPosition.BOTTOM)
        {
            if (!_adsManager.EnableBanner)
            {
                return;
            }
            MaxSdk.LoadBanner(BannerAdUnitId);
        }

        public override void ShowBanner()
        {
            if (!_adsManager.EnableBanner)
            {
                return;
            }
            MaxSdk.ShowBanner(BannerAdUnitId);
        }

        public override void HideBanner()
        {
            if (!_adsManager.EnableBanner)
            {
                return;
            }
            MaxSdk.HideBanner(BannerAdUnitId);
        }

        public override void DestroyBanner()
        {
            if (!_adsManager.EnableBanner)
            {
                return;
            }
            MaxSdk.DestroyBanner(BannerAdUnitId);
        }

        public override void SetBannerPosition(BannerPosition position)
        {
            base.SetBannerPosition(position);
            var bannerPosition = GetMaxBannerPosition();
            MaxSdk.UpdateBannerPosition(BannerAdUnitId,bannerPosition);
        }
        #endregion

        #region MRec

        private void InitializeMRec()
        {
            if (!_adsManager.EnableMRecAd)
            {
                return;
            }

            MaxSdkCallbacks.MRec.OnAdLoadedEvent += OnMRecAdLoadedEvent;
            MaxSdkCallbacks.MRec.OnAdLoadFailedEvent += OnMRecAdLoadFailedEvent;
            MaxSdkCallbacks.MRec.OnAdClickedEvent += OnMRecAdClickedEvent;
            MaxSdkCallbacks.MRec.OnAdRevenuePaidEvent += OnMRecAdRevenuePaidEvent;
            MaxSdkCallbacks.MRec.OnAdExpandedEvent += OnMRecAdExpandedEvent;
            MaxSdkCallbacks.MRec.OnAdCollapsedEvent += OnMRecAdCollapsedEvent;

            CreateMRecAd();
        }

        private MaxSdkBase.AdViewPosition MRecPositionToMaxAdViewPosition(MRecPosition mRecPosition)
        {
            switch (mRecPosition)
            {
                case MRecPosition.TOP_CENTER:
                    return AdViewPosition.TopCenter;
                //case MRecPosition.CENTERED:
                //    return AdViewPosition.Centered;
                case MRecPosition.BOTTOM_CENTERED:
                    return AdViewPosition.BottomCenter;
                default:
                    break;
            }

            return AdViewPosition.BottomCenter;
        }
        public override void CreateMRecAd()
        {
            if (!_adsManager.EnableMRecAd)
            {
                return;
            }

            var mRecPosition = MRecPositionToMaxAdViewPosition(_adsManager.MRecPosition);
            MaxSdk.CreateMRec(MRecAdUnitId, mRecPosition);
        }

        public override void ShowMRecAd()
        {
            if (!_adsManager.EnableMRecAd)
            {
                return;
            }
            MaxSdk.ShowMRec(MRecAdUnitId);
        }

        public override void HideMRecAd()
        {
            if (!_adsManager.EnableMRecAd)
            {
                return;
            }

            MaxSdk.HideMRec(MRecAdUnitId);
        }

        public override void DestroyMRecAd()
        {
            if (!_adsManager.EnableMRecAd)
            {
                return;
            }
            MaxSdk.DestroyMRec(MRecAdUnitId);
        }

        public void OnMRecAdLoadedEvent(string adUnitId, MaxSdkBase.AdInfo maxAdInfo)
        {
            var adInfo = AdMax.MaxAdInfoToAdInfo(maxAdInfo);
            _adsManager.OnMRecAdLoadedEvent(adInfo);
        }

        public void OnMRecAdLoadFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo error)
        {
            var adError = AdMax.MaxAdErrorToAdError(adUnitId, error);
            _adsManager.OnMRecAdLoadFailedEvent(adError);
        }

        public void OnMRecAdClickedEvent(string adUnitId, MaxSdkBase.AdInfo maxAdInfo)
        {
            var adInfo = AdMax.MaxAdInfoToAdInfo(maxAdInfo);
            _adsManager.OnMRecAdClickedEvent(adInfo);
        }

        public void OnMRecAdRevenuePaidEvent(string adUnitId, MaxSdkBase.AdInfo maxAdInfo)
        {
            TrackRevenue(maxAdInfo);
        }

        public void OnMRecAdExpandedEvent(string adUnitId, MaxSdkBase.AdInfo maxAdInfo)
        {
            var adInfo = AdMax.MaxAdInfoToAdInfo(maxAdInfo);
            _adsManager.OnMRecAdExpandedEvent(adInfo);
        }

        public void OnMRecAdCollapsedEvent(string adUnitId, MaxSdkBase.AdInfo maxAdInfo)
        {
            var adInfo = AdMax.MaxAdInfoToAdInfo(maxAdInfo);
            _adsManager.OnMRecAdCollapsedEvent(adInfo);
        }
        #endregion

        public static AdNetwork.AdInfo MaxAdInfoToAdInfo(MaxSdkBase.AdInfo maxAdInfo)
        {
            AdNetwork.AdInfo adInfo = new AdNetwork.AdInfo
            {
                Platform = AdsValues.AD_PLATFORM_APPLOVIN,
                Revenue = maxAdInfo.Revenue,
                Format = maxAdInfo.AdFormat,
                Placement = maxAdInfo.Placement,
                Mediation = maxAdInfo.NetworkName,
                UnitId = maxAdInfo.AdUnitIdentifier
            };
            return adInfo;
        }

        public static AdNetwork.AdError MaxAdErrorToAdError(string unitId, MaxSdkBase.ErrorInfo maxError)
        {
            AdNetwork.AdError adError = new AdError
            {
                Code = maxError.MediatedNetworkErrorCode,
                Message = maxError.Message,
                UnitId = unitId
            };
            return adError;
        }

        public static AdImpressionData MaxAdInfoToAdImpressionData(MaxSdk.AdInfo maxAdInfo)
        {
            AdImpressionData impressionData = new AdImpressionData()
            {
                Platform = AdsValues.AD_PLATFORM_APPLOVIN,
                Source = maxAdInfo.NetworkName,
                UnitName = maxAdInfo.AdUnitIdentifier,
                Format = maxAdInfo.AdFormat,
                Revenue = maxAdInfo.Revenue,
                Currency = AdsValues.AD_REVENUE_CURRENCY
            };
            return impressionData;
        }

        void OnDestroy()
        {
            MaxSdkCallbacks.Interstitial.OnAdClickedEvent -= Interstitial_OnAdClickedEvent;
            MaxSdkCallbacks.Interstitial.OnAdLoadedEvent -= Interstitial_OnAdLoadedEvent;
            MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent -= Interstitial_OnAdLoadFailedEvent;
            MaxSdkCallbacks.Interstitial.OnAdDisplayedEvent -= Interstitial_OnAdDisplayedEvent;
            MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent -= Interstitial_OnAdDisplayFailedEvent;
            MaxSdkCallbacks.Interstitial.OnAdHiddenEvent -= Interstitial_OnAdHiddenEvent;
            MaxSdkCallbacks.Interstitial.OnAdRevenuePaidEvent -= Interstitial_OnAdRevenuePaidEvent;

            MaxSdkCallbacks.Rewarded.OnAdClickedEvent -= Rewarded_OnAdClickedEvent;
            MaxSdkCallbacks.Rewarded.OnAdDisplayedEvent -= Rewarded_OnAdDisplayedEvent;
            MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent -= Rewarded_OnAdDisplayFailedEvent;
            MaxSdkCallbacks.Rewarded.OnAdHiddenEvent -= Rewarded_OnAdHiddenEvent;
            MaxSdkCallbacks.Rewarded.OnAdLoadedEvent -= Rewarded_OnAdLoadedEvent;
            MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent -= Rewarded_OnAdLoadFailedEvent;
            MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent -= Rewarded_OnAdReceivedRewardEvent;
            MaxSdkCallbacks.Rewarded.OnAdRevenuePaidEvent -= Rewarded_OnAdRevenuePaidEvent;

            MaxSdkCallbacks.Banner.OnAdClickedEvent -= Banner_OnAdClickedEvent;
            MaxSdkCallbacks.Banner.OnAdLoadedEvent -= Banner_OnAdLoadedEvent;
            MaxSdkCallbacks.Banner.OnAdLoadFailedEvent -= Banner_OnAdLoadFailedEvent;
            MaxSdkCallbacks.Banner.OnAdCollapsedEvent -= Banner_OnAdCollapsedEvent;
            MaxSdkCallbacks.Banner.OnAdExpandedEvent -= Banner_OnAdExpandedEvent;
            MaxSdkCallbacks.Banner.OnAdRevenuePaidEvent -= Banner_OnAdRevenuePaidEvent;

            MaxSdkCallbacks.MRec.OnAdLoadedEvent -= OnMRecAdLoadedEvent;
            MaxSdkCallbacks.MRec.OnAdLoadFailedEvent -= OnMRecAdLoadFailedEvent;
            MaxSdkCallbacks.MRec.OnAdClickedEvent -= OnMRecAdClickedEvent;
            MaxSdkCallbacks.MRec.OnAdRevenuePaidEvent -= OnMRecAdRevenuePaidEvent;
            MaxSdkCallbacks.MRec.OnAdExpandedEvent -= OnMRecAdExpandedEvent;
            MaxSdkCallbacks.MRec.OnAdCollapsedEvent -= OnMRecAdCollapsedEvent;
        }
#endif
    }

}
