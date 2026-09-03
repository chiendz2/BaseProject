using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GIKCore
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-95)]
    public class SceneLoader : MonoBehaviour
    {
        private const string LogTag = "[SceneLoader]";

        public static SceneLoader Instance { get; private set; }

        public static bool IsLoading => Instance != null && Instance._operation != null;

        public static float Progress => Instance == null ? 0f : Instance._progress;

        public event Action<string> LoadStarted;

        public event Action<string> LoadCompleted;

        [Header("Flow")]
        [Tooltip("A load never completes faster than this, so a loading screen cannot flash for a single frame.")]
        [SerializeField] private float _minLoadSeconds = 0.5f;

        private AsyncOperation _operation;

        private string _pendingScene;

        private float _elapsedSeconds;

        private float _minSecondsForThisLoad;

        private float _progress;

        private Action _onLoaded;

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

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (_operation == null)
                return;

            Complete();
        }

        public static void Load(string sceneName, Action onLoaded = null)
        {
            if (Instance == null)
            {
                Debug.LogError(LogTag + " No SceneLoader in the scene, cannot load '" + sceneName + "'.");
                onLoaded?.Invoke();
                return;
            }

            Instance.DoLoad(sceneName, Instance._minLoadSeconds, onLoaded);
        }

        public static void Load(string sceneName, float minSeconds, Action onLoaded = null)
        {
            if (Instance == null)
            {
                Debug.LogError(LogTag + " No SceneLoader in the scene, cannot load '" + sceneName + "'.");
                onLoaded?.Invoke();
                return;
            }

            Instance.DoLoad(sceneName, minSeconds, onLoaded);
        }

        public static Task LoadAsync(string sceneName)
        {
            var tcs = new TaskCompletionSource<bool>();
            Load(sceneName, () => tcs.TrySetResult(true));
            return tcs.Task;
        }

        private void DoLoad(string sceneName, float minSeconds, Action onLoaded)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogError(LogTag + " Empty scene name.");
                onLoaded?.Invoke();
                return;
            }

            if (_operation != null)
            {
                Debug.LogError(LogTag + " Already loading '" + _pendingScene + "', ignoring '" + sceneName + "'.");
                onLoaded?.Invoke();
                return;
            }

            _operation = SceneManager.LoadSceneAsync(sceneName);

            if (_operation == null)
            {
                Debug.LogError(LogTag + " Cannot load '" + sceneName + "'. Add it to Build Settings.");
                onLoaded?.Invoke();
                return;
            }

            _operation.allowSceneActivation = false;
            _pendingScene = sceneName;
            _minSecondsForThisLoad = minSeconds;
            _elapsedSeconds = 0f;
            _progress = 0f;
            _onLoaded = onLoaded;

            LoadStarted?.Invoke(sceneName);
        }

        private void Update()
        {
            if (_operation == null)
                return;

            _elapsedSeconds += Time.unscaledDeltaTime;
            _progress = Mathf.Clamp01(_operation.progress / 0.9f);

            if (_elapsedSeconds < _minSecondsForThisLoad)
                return;

            if (_operation.progress < 0.9f)
                return;

            _operation.allowSceneActivation = true;
        }

        private void Complete()
        {
            string loadedScene = _pendingScene;
            var handler = _onLoaded;

            _operation = null;
            _pendingScene = null;
            _onLoaded = null;
            _progress = 1f;

            handler?.Invoke();
            LoadCompleted?.Invoke(loadedScene);
        }
    }
}
