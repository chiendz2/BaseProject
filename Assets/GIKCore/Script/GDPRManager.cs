using System;
using UnityEngine;
#if GOOGLE_MOBILE_ADS
using GoogleMobileAds.Ump;
using GoogleMobileAds.Ump.Api;
#endif

namespace GIKCore
{
    public class GDPRManager : MonoBehaviour
    {
        public Action<bool> OnLoadedFormDone;

#if GOOGLE_MOBILE_ADS
        private ConsentForm _consentForm;

        public static bool IsConsentReady =>
            ConsentInformation.ConsentStatus == ConsentStatus.Required ||
            ConsentInformation.ConsentStatus == ConsentStatus.Obtained;

        private void Start()
        {
            var request = new ConsentRequestParameters
            {
                TagForUnderAgeOfConsent = false
            };

            ConsentInformation.Update(request, OnConsentInfoUpdated);
        }

        public void ReloadPrivacyOption()
        {
            UnityEngine.Debug.Log("[GDPRManager] Showing privacy options form.");
            ConsentForm.Load(OnLoadConsentForm);
        }

        public void ShowConsentForm()
        {
            _consentForm.Show(OnShowForm);
        }

        private void OnConsentInfoUpdated(FormError error)
        {
            if (error != null)
            {
                UnityEngine.Debug.LogError(error);
                return;
            }

            if (ConsentInformation.IsConsentFormAvailable())
                LoadConsentForm();
        }

        private void LoadConsentForm()
        {
            ConsentForm.Load(OnLoadAndShowConsentForm);
        }

        private void OnLoadAndShowConsentForm(ConsentForm consentForm, FormError error)
        {
            if (error != null)
            {
                UnityEngine.Debug.LogError(error);
                return;
            }

            _consentForm = consentForm;

            if (ConsentInformation.ConsentStatus == ConsentStatus.Required)
                _consentForm.Show(OnShowForm);
        }

        private void OnLoadConsentForm(ConsentForm consentForm, FormError error)
        {
            if (error != null)
            {
                UnityEngine.Debug.LogError(error);
                return;
            }

            _consentForm = consentForm;
            OnLoadedFormDone?.Invoke(IsConsentReady);
        }

        private void OnShowForm(FormError error)
        {
            if (error != null)
            {
                UnityEngine.Debug.LogError(error);
                return;
            }

            LoadConsentForm();
        }
#else
        public static bool IsConsentReady => false;

        public void ReloadPrivacyOption()
        {
            Debug.LogWarning("[GDPRManager] Google Mobile Ads (UMP) SDK is not installed - privacy options form unavailable.");
            OnLoadedFormDone?.Invoke(IsConsentReady);
        }

        public void ShowConsentForm()
        {
            Debug.LogWarning("[GDPRManager] Google Mobile Ads (UMP) SDK is not installed - consent form unavailable.");
        }
#endif
    }
}
