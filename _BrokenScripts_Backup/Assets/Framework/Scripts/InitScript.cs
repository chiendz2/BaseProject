using DG.Tweening;
using GameUI;
using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class InitScript : MonoBehaviour
{
    [SerializeField] private AssetReference _initAssets;

    private void Awake()
    {
        Input.multiTouchEnabled = false;
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        DOTween.defaultTimeScaleIndependent = false;
        GlobalValues.Init();
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        FirebaseSDK.Instance.OnFirebaseReady -= OnFirebaseReady;
    }

    private IEnumerator Start()
    {
        FirebaseSDK.Instance.OnFirebaseReady += OnFirebaseReady;

        GameEvents.ShowSceneLoading?.Invoke();

        float loadingStartTime = Time.time;

        var initAssetsHandle = _initAssets.InstantiateAsync();
        UIManager.Instance.TrackSceneLoading(initAssetsHandle, 0f, .5f);

        while (!RemoteConfig.Loaded
                || !GlobalValues.SFXReady
                || !GlobalValues.InitAssetsReady
                || Time.time - loadingStartTime < .5f)
            yield return null;

        GlobalValues.InitByRemoteConfig();

        AnalyticsFTUE.LogEvent(EventName.FTUE1_loading_end);

#if UNITY_EDITOR
        if (SessionState.GetBool("ToolMode", false))
        {
            GameEvents.LoadScene?.Invoke(SceneId.Game);
            UIManager.Instance.HideSceneLoading();
            yield break;
        }
#endif
        if (GamePrefs.Level >= RemoteConfig.Instance.LevelShowHome || GamePrefs.Life == 0)
        {
            GameEvents.LoadScene?.Invoke(SceneId.Home);
            Debug.Log("Load Scene Home: "+ GamePrefs.Level+ " Remote: "+ RemoteConfig.Instance.LevelShowHome);
        }
        else
        {
            GameEvents.LoadScene?.Invoke(SceneId.Game);
        }
    }

    private void OnFirebaseReady()
    {
        AnalyticHelper.LogEvent(EventName.af_session);
        AnalyticsFTUE.LogEvent(EventName.FTUE1_loading_start);
    }
}
