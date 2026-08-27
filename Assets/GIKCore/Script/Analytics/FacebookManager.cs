using UnityEngine;
#if FACEBOOK_SDK
using Facebook.Unity;
#endif

namespace GIKCore
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-85)]
    public class FacebookManager : MonoBehaviour
    {
        private const string LogTag = "[FacebookManager]";

        public static FacebookManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            StartSdk();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void StartSdk()
        {
#if FACEBOOK_SDK
            if (FB.IsInitialized)
            {
                ActivateApp();
                return;
            }

            FB.Init(OnInitComplete, OnHideUnity);
#else
            Debug.LogWarning($"{LogTag} FACEBOOK_SDK is not defined, Facebook events are dropped.");
#endif
        }

#if FACEBOOK_SDK
        private void OnInitComplete()
        {
            if (!FB.IsInitialized)
            {
                Debug.LogError($"{LogTag} FB.Init completed but the SDK is not initialized.");
                return;
            }

            ActivateApp();
        }

        private void ActivateApp()
        {
            FB.ActivateApp();
            FacebookProvider.SetReady();
        }

        private void OnHideUnity(bool isGameShown)
        {
            Time.timeScale = isGameShown ? 1f : 0f;
        }
#endif
    }
}
