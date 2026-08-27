using Inwave.Production.Ads;
using System.Collections.Generic;
using UnityEngine;

namespace Inwave.Production.Connectors
{
    public class RemoteConfigAndAdsManagerConnector : MonoBehaviour
    {
        void Start()
        {
            Init();
        }

        void Init()
        {
            AdsEvents.OnSetCustomSegmentData += AdsManager_OnSetCustomSegmentData;
            RemoteConfig.Instance.OnEndLoad += RemoteConfig_OnEndLoad;
        }

        private void AdsManager_OnSetCustomSegmentData(string key, string value)
        {
            AnalyticHelper.LogEvent(key);
        }

        private void RemoteConfig_OnEndLoad()
        {
            AdsManager.Instance.SetCustomSegmentData(new List<string> { RemoteConfig.Instance.MediationDataSegment });

            // AOA: cooldown giữa 2 lần AOA + kill switch, lấy từ remote config thay vì hardcode Inspector.
            AppOpenConfig appOpen = RemoteConfig.Instance.AdsConfig.AppOpen;
            AdsManager.Instance.AppOpenShowingInterval = appOpen.Cooldown;
            AdsManager.Instance.EnableAppOpenAd = appOpen.Enable;
        }
    }
}

