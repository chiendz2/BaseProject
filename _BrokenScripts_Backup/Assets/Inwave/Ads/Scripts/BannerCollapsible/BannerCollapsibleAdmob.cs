//#define DEBUG_ADS_INTERNAL
using System;
using System.Collections;
#if BANNER_COLLAPSIBLE
using GoogleMobileAds.Api;
#endif
using Inwave.Production.Utils;
using UnityEngine;

namespace Inwave.Production.Ads
{
    public class BannerCollapsibleAdmob : BannerCollapsibleNetwork
    {
#if DEBUG_ADS
        private const string LogTag = "BannerCollapsibleAdmob";
#endif
        [SerializeField]
        private string _adUnitIdAndroid = "YOUR_AD_UNIT_ID";
        [SerializeField]
        private string _adUnitIdIOS = "YOUR_AD_UNIT_ID";

        [SerializeField] private float _microValueRatio = 1000000;

        [SerializeField] private bool _useTestId = false;

        private string _adUnitId = "unused";
#if BANNER_COLLAPSIBLE
        private BannerView _bannerView = null;
#endif

        private bool _isCollapsibleClosed = false;

        private float _lastOpenedTime;

        public override void Initialize()
        {
            base.Initialize();
#if UNITY_ANDROID
            _adUnitId = _adUnitIdAndroid;
#elif UNITY_IOS
            _adUnitId = _adUnitIdIOS;
#endif

            if (_useTestId)
            {
                _adUnitId = "ca-app-pub-3940256099942544/2014213617";
            }
            _waitOneSecond = new WaitForSeconds(1);
        }

        public override void Create(BannerCollapsiblePosition position = BannerCollapsiblePosition.BOTTOM)
        {
#if BANNER_COLLAPSIBLE
            base.Create(position);
            if (_bannerView != null)
            {
                DestroyAd();
            }

            var adPosition = BannerPositionToAdPosition(position);
            AdSize adaptiveSize =
                AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(AdSize.FullWidth);
            _bannerView = new BannerView(_adUnitId, adaptiveSize, adPosition);
            EventHandlers();
#endif
        }

        protected BannerCollapsiblePosition _currentPosition;

        WaitForSeconds _waitOneSecond;
        private void EventHandlers()
        {
#if BANNER_COLLAPSIBLE
            _bannerView.OnBannerAdLoaded += () =>
            {
                if (_useReplacementWhenFailed)
                {
                    _isReplacement = false;
                }

                _loaded = true;
                var adInfo = CreateAdInfo();
                OnAdLoadedEvent(adInfo);
                _isLoading = false;
            };

            _bannerView.OnBannerAdLoadFailed += (LoadAdError error) =>
            {
                if (_useReplacementWhenFailed)
                {
                    _isReplacement = true;
                }

                _loaded = false;
                var adError = CreateAdError(error);
                OnAdLoadFailedEvent(adError);
                _isLoading = false;
            };

            _bannerView.OnAdPaid += (AdValue adValue) =>
            {
                StartCoroutine(_Delay());
                //Debug.LogError($"BannerCollapsible - AdValue: {adValue.Value} {adValue.CurrencyCode}");
                IEnumerator _Delay()
                {
                    yield return _waitOneSecond;
                    AdNetwork.AdImpressionData impressionData = new AdNetwork.AdImpressionData()
                    {
                        Currency = adValue.CurrencyCode,//AdsValues.AD_REVENUE_CURRENCY,
                        Revenue = adValue.Value/_microValueRatio,
                        UnitName = _adUnitId,
                        Format = AdsValues.AD_FORMAT_BANNER_COLLAPSIBLE,
                        Platform = AdsValues.AD_PLATFORM_ADMOB
                    };
                    OnAdPaidEvent(impressionData, IsCollapsible);
                }
            };

            _bannerView.OnAdImpressionRecorded += () =>
            {
                StartCoroutine(_Delay());

                IEnumerator _Delay()
                {
                    yield return _waitOneSecond;
                    AdNetwork.AdInfo adInfo = CreateAdInfo();
                    OnAdImpressionRecordedEvent(adInfo, IsCollapsible);
                }
            };

            _bannerView.OnAdClicked += () =>
            {
                AdNetwork.AdInfo adInfo = CreateAdInfo();
                OnAdClickedEvent(adInfo);
            };
            
            _bannerView.OnAdFullScreenContentOpened += () =>
            {
                //trick
                //If banner open full content in 1s, may be a Collapsible Banner
                if (IsCollapsible || _isCollapsibleClosed)
                {
                    return;
                }
                if (Time.time - _lastOpenedTime < 1)
                {
                    IsCollapsible = true;
                }

                AdNetwork.AdInfo adInfo = CreateAdInfo();
                OnAdShowedEvent(adInfo);
            };

            _bannerView.OnAdFullScreenContentClosed += () =>
            {
                if (!IsCollapsible)
                {
                    return;
                }
                IsCollapsible = false;
                _isCollapsibleClosed = true;
                AdNetwork.AdInfo adInfo = CreateAdInfo();
                OnAdClosedEvent(adInfo);
            };
#endif
        }

#if BANNER_COLLAPSIBLE
        protected AdPosition BannerPositionToAdPosition(BannerCollapsiblePosition position)
        {
            if (position == BannerCollapsiblePosition.BOTTOM)
            {
                return AdPosition.Bottom;
            }
            if (position == BannerCollapsiblePosition.TOP)
            {
                return AdPosition.Top;
            }
            return AdPosition.Center;
        }
#endif
        private AdNetwork.AdInfo CreateAdInfo()
        {
            AdNetwork.AdInfo adInfo = new AdNetwork.AdInfo
            {
                Format = AdsValues.AD_FORMAT_BANNER_COLLAPSIBLE,
                UnitId = _adUnitId,
                Platform = AdsValues.AD_PLATFORM_ADMOB
            };
            return adInfo;
        }

#if BANNER_COLLAPSIBLE
        private AdNetwork.AdError CreateAdError(LoadAdError error)
        {
            AdNetwork.AdError adError = new AdNetwork.AdError
            {
                UnitId = _adUnitId,
                Code = error.GetCode(),
                Message = error.GetMessage()
            };
            return adError;
        }
#endif
        public override void DestroyAd()
        {
            #if BANNER_COLLAPSIBLE
            base.DestroyAd();
            if (_bannerView != null)
            {
                _bannerView.Destroy();
                _bannerView = null;
            }
            #endif
        }

        private bool _isLoading = false;

        public override void Load(BannerCollapsiblePosition position = BannerCollapsiblePosition.BOTTOM,
            bool hideAfterLoad = true)
        {
            #if BANNER_COLLAPSIBLE
            //if (_isLoading)
            //{
            //    return;
            //}
            OnRequestLoadEvent();
            IsCollapsible = false;
            _isCollapsibleClosed = false;
            _isLoading = true;
            _currentPosition = position;
            base.Load(position);
            if (_bannerView == null)
            {
                Create(position);
            }

            _loaded = false;
            var adRequest = new AdRequest();
            string positionString = "bottom";
            if (position == BannerCollapsiblePosition.TOP)
            {
                positionString = "top";
            }


            adRequest.Extras.Add("collapsible", positionString);
            adRequest.Extras.Add("collapsible_request_id", System.Guid.NewGuid().ToString());

            _bannerView.LoadAd(adRequest);
            if (hideAfterLoad)
            {
                _bannerView.Hide();
            }
#endif
        }

        public override void Show(BannerCollapsiblePosition position = BannerCollapsiblePosition.BOTTOM)
        {
#if BANNER_COLLAPSIBLE
            _currentPosition = position;
            base.Show(position);

            if (_isReplacement && _useReplacementWhenFailed)
            {
                OnNeedReplacementEvent();
            }
            else
            {
                _lastOpenedTime = Time.time;
                if (_bannerView != null)
                {
                    if (_loaded)
                    {
#if DEBUG_ADS && DEBUG_ADS_INTERNAL
                        Log("Show Immediately");
#endif
                        var adPosition = BannerPositionToAdPosition(position);
                        _bannerView.SetPosition(adPosition);
                        //request to show banner
                        _bannerView.Show();
                    }
                    else
                    {
                        //request new ad
                        Load(position,false);
#if DEBUG_ADS && DEBUG_ADS_INTERNAL
                        Log("Request new Ad");
#endif
                    }
                }
                else
                {
                    //create and load
                    Load(position,false);
#if DEBUG_ADS && DEBUG_ADS_INTERNAL
                    Log("Create and Show");
#endif
                }
                //for calculating fill rate of collapsible banner
                //start_show = collapse + normal
                AdsEvents.BannerCollapsible.OnStartShowCollapseBannerEvent();
            }

            
            //Debug.LogError(position.ToString());
#endif
        }

        public override void Hide()
        {
#if BANNER_COLLAPSIBLE
            base.Hide();
            _bannerView?.Hide();
#endif
        }

#if BANNER_COLLAPSIBLE
        public override bool IsAvailable => _bannerView !=null &&_loaded;
#endif
#if DEBUG_ADS &&DEBUG_ADS_INTERNAL
        private void Log(string content)
        {
            LogUtils.LogWarning(LogTag,content);
        }
#endif
    }
}

