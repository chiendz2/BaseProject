using GameIAP;


namespace Inwave.Production.Ads
{
    public class AdsManagerEventHandler : AdsManagerInitializer
    {
        //private void AdsManager_OnSetCustomSegmentData(string key, string value)
        //{
        //    AnalyticHelper.LogEvent(key);
        //}

        //private void RemoteConfig_OnEndLoad()
        //{
        //    AdsManager.instance.SetCustomSegmentData(new List<string>{RemoteConfig.Instance.MediationDataSegment});
        //}

        #region Banner

        protected override void OnBannerAdClicked(AdNetwork.AdInfo adInfo)
        {
            base.OnBannerAdClicked(adInfo);
            AnalyticsAds.BannerAdsClick();
        }
        protected override void OnBannerAdScreenPresented(AdNetwork.AdInfo adInfo)
        {
            base.OnBannerAdScreenPresented(adInfo);
            AnalyticsAds.BannerAdsShow();
        }

        #endregion

        #region Rewarded
        protected override void OnRewardRequest(string location, AdNetwork.AdInfo adInfo)
        {
            base.OnRewardRequest(location, adInfo);
            AnalyticsAds.VideoAdsRequest(location, adInfo.Mediation);
        }

        protected override void OnRewardRequestSucess(string location, AdNetwork.AdInfo adInfo)
        {
            base.OnRewardRequestSucess(location, adInfo);
            AnalyticsAds.VideoAdsRequestSuccess(location, adInfo.Mediation);
        }

        protected override void OnRewardRequestFailed(string location, AdNetwork.AdInfo adInfo)
        {
            base.OnRewardRequestFailed(location, adInfo);
            AnalyticsAds.VideoAdsRequestFailed(location, adInfo.Mediation);
        }
        protected override void OnRewardedVideoAdShowFailed(string location, AdNetwork.AdInfo adInfo, AdNetwork.AdError adError)
        {
            base.OnRewardedVideoAdShowFailed(location, adInfo, adError);
            AnalyticsAds.VideoAdsShowFailed(location , adInfo.Mediation);
        }

        protected override void OnRewardedVideoAdClosed(string location, AdNetwork.AdInfo adInfo)
        {
            base.OnRewardedVideoAdClosed(location, adInfo);
            AnalyticsAds.VideoAdsFinish(location, adInfo.Mediation);
        }

        protected override void OnShowRewardAd(string location, AdNetwork.AdInfo adInfo)
        {
            base.OnShowRewardAd(location, adInfo);
            AnalyticsAds.VideoAdsShow(location , adInfo.Mediation);
        }

        protected override void OnRequestShowRewardAd(string location, AdNetwork.AdInfo adInfo)
        {
            base.OnRequestShowRewardAd(location, adInfo);
        }

        protected override void OnRewardedVideoAdClicked(string location, AdNetwork.AdInfo adInfo)
        {
            base.OnRewardedVideoAdClicked(location, adInfo);
            AnalyticsAds.VideoAdsClick(location, adInfo.Mediation);
        }

        #endregion

        #region Interstitial

        protected override void OnInterstitialRequestLoad()
        {
            base.OnInterstitialRequestLoad();
            AnalyticsAds.FullAdsRequest();
        }
        protected override void OnInterstitialLoadSucceed(string location, AdNetwork.AdInfo adInfo)
        {
            base.OnInterstitialLoadSucceed(location, adInfo);
            AnalyticsAds.FullAdsRequestSuccess(location, adInfo.Mediation);
        }

        protected override void OnInterstitialAdClosed(string location, AdNetwork.AdInfo adInfo)
        {
            base.OnInterstitialAdClosed(location,adInfo);
            AnalyticsAds.FullAdsShowFinish(location, adInfo.Mediation);
            IAPSuggestionManager.NotifyInterstitialClosed(location);
        }

        protected override void OnInterstitialAdShowFailed(string location,AdNetwork.AdInfo adInfo,AdNetwork.AdError adError)
        {
            base.OnInterstitialAdShowFailed(location,adInfo, adError);
            AnalyticsAds.FullAdsShowFailed(location, adInfo.Mediation);
        }

        protected override void OnInterstitialAdShowSucceeded(string location, AdNetwork.AdInfo adInfo)
        {
            base.OnInterstitialAdShowSucceeded(location, adInfo);
            AnalyticsAds.FullAdsShow(location, adInfo.Mediation);
        }

        protected override void OnShowInterstitial(string location, AdNetwork.AdInfo adInfo)
        {
            base.OnShowInterstitial(location, adInfo);
        }

        protected override void OnInterstitialRequestShow(string location, AdNetwork.AdInfo adInfo)
        {
            base.OnInterstitialRequestShow(location, adInfo);
            //AnalyticsAds.FullAdsShowReady(location, adInfo.Mediation);
        }


        protected override void OnInterstitialAdLoadFailed(AdNetwork.AdError adError)
        {
            base.OnInterstitialAdLoadFailed(adError);
            AnalyticsAds.FullAdsRequestFailed();
        }

        protected override void OnInterstitialAdClicked(string location, AdNetwork.AdInfo adInfo)
        {
            base.OnInterstitialAdClicked(location, adInfo);
            AnalyticsAds.FullAdsClickCTA(location, adInfo.Mediation);
        }

        #endregion

        #region Banner Collapsible

        protected override void OnBannerCollapsibleRequestLoadAd()
        {
            base.OnBannerCollapsibleRequestLoadAd();
            AnalyticsAds.BannerCollapRequest();
        }

        protected override void OnBannerCollapsibleAdLoaded(AdNetwork.AdInfo adInfo)
        {
            base.OnBannerCollapsibleAdLoaded(adInfo);
            AnalyticsAds.BannerCollapRequestSuccess();
        }

        protected override void OnBannerCollapsibleAdLoadFailed(AdNetwork.AdError adInfo)
        {
            base.OnBannerCollapsibleAdLoadFailed(adInfo);
            AnalyticsAds.BannerCollapRequestFailed();
        }

        protected override void OnBannerCollapsibleAdScreenPresented(AdNetwork.AdInfo adInfo, bool isCollapsible)
        {
            base.OnBannerCollapsibleAdScreenPresented(adInfo, isCollapsible);
            AnalyticsAds.BannerCollapShow(isCollapsible);
        }

        protected override void OnBannerCollapsibleAdClicked(AdNetwork.AdInfo adInfo, bool isCollapsible)
        {
            base.OnBannerCollapsibleAdClicked(adInfo, isCollapsible);
            AnalyticsAds.BannerCollapClick(isCollapsible);
        }

        protected override void OnBannerCollapsibleAdImpressionRecorded(AdNetwork.AdInfo adInfo, bool isCollapsible)
        {
            base.OnBannerCollapsibleAdImpressionRecorded(adInfo, isCollapsible);
            AnalyticsAds.BannerCollapImpressionRecorded(adInfo,isCollapsible);
        }

        protected override void OnBannerCollapsibleAdPaid(AdNetwork.AdImpressionData impressionData, bool isCollapsible)
        {
            AnalyticsAds.BannerCollapPaid(impressionData,isCollapsible);
            AnalyticsAds.AdImpressionAdmobCollapsibleBanner(impressionData);
        }

        protected override void OnBannerCollapsibleStartShow()
        {
            base.OnBannerCollapsibleStartShow();
            AnalyticsAds.BannerCollapStartShow();
        }

        #endregion

        protected override void OnImpressionSuccessEvent(AdNetwork.AdImpressionData impressionData)
        {
            base.OnImpressionSuccessEvent(impressionData);
            AnalyticsAds.AdImpression(impressionData);
        }
    }
}
