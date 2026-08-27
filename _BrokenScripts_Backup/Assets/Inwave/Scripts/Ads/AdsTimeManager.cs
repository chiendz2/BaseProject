using Inwave.Production.Ads;
using System;
using UnityEngine;

namespace HyperGame
{
    public class AdsTimeManager : MonoBehaviour
    {
        private float _realTimeShow;
        private float _maxDuration;

        private void Awake()
        {
            AdsEvents.AppOpen.OnAdShowed += OnAppOpenShowed;
            AdsEvents.AppOpen.OnAdClosed += OnAppOpenClosed;
            AdsEvents.Interstitial.OnShow += OnInterstitialShowed;
            AdsEvents.Interstitial.OnAdClosed += OnInterstitialClosed;
            AdsEvents.Rewarded.OnShowRewardAd += OnRewardAdsShowed;
            AdsEvents.Rewarded.OnRewardedVideoAdClosed += OnRewardAdsClosed;
        }

        private void OnDestroy()
        {
            AdsEvents.AppOpen.OnAdShowed -= OnAppOpenShowed;
            AdsEvents.AppOpen.OnAdClosed -= OnAppOpenClosed;
            AdsEvents.Interstitial.OnShow -= OnInterstitialShowed;
            AdsEvents.Interstitial.OnAdClosed -= OnInterstitialClosed;
            AdsEvents.Rewarded.OnShowRewardAd -= OnRewardAdsShowed;
            AdsEvents.Rewarded.OnRewardedVideoAdClosed -= OnRewardAdsClosed;
        }

        private void OnApplicationPause(bool pause)
        {
            if (pause)
            {
                _realTimeShow = Time.realtimeSinceStartup;
            }
        }

        private void OnAppOpenShowed(AdNetwork.AdInfo obj)
        {
            _maxDuration = 20f;
            _realTimeShow = Time.realtimeSinceStartup;
            OnAdsShowed();
        }

        private void OnAppOpenClosed(AdNetwork.AdInfo obj)
        {
            OnAdsClosed();
        }

        private void OnInterstitialShowed(string arg1, AdNetwork.AdInfo arg2)
        {
            _maxDuration = 30f;
            OnAdsShowed();
        }

        private void OnInterstitialClosed(string arg1, AdNetwork.AdInfo arg2)
        {
            OnAdsClosed();
        }

        private void OnRewardAdsShowed(string arg1, AdNetwork.AdInfo arg3)
        {
            _maxDuration = 60f;
            OnAdsShowed();
        }

        private void OnRewardAdsClosed(string arg1, AdNetwork.AdInfo arg3)
        {
            OnAdsClosed();
        }

        private void OnAdsShowed()
        {
        }

        private void OnAdsClosed()
        {
            float duration = Mathf.Clamp(Time.realtimeSinceStartup - _realTimeShow, 0f, _maxDuration);
            AnalyticManager.OnUpdatePlayTimeAds?.Invoke(duration);
        }
    }
}