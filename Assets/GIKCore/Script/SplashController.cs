using UnityEngine;
using UnityEngine.SceneManagement;

namespace GIKCore
{
    public class SplashController : MonoBehaviour
    {
        [Header("Flow")]
        [Tooltip("Splash stays on screen at least this long, so it never flashes for a single frame on a fast device.")]
        [SerializeField] private float _minDisplaySeconds = 1.5f;

        [Tooltip("Scene loaded after the splash. Must be in Build Settings. Leave empty to stay on the splash.")]
        [SerializeField] private string _nextScene;

        private AsyncOperation _loadOperation;

        private float _elapsedSeconds;

        private bool _activated;

        public float Progress => _loadOperation == null ? 0f : Mathf.Clamp01(_loadOperation.progress / 0.9f);

        private void Start()
        {
            if (string.IsNullOrEmpty(_nextScene))
            {
                Debug.Log("[SplashController] No next scene assigned, staying on the splash.");
                return;
            }

            _loadOperation = SceneManager.LoadSceneAsync(_nextScene);

            if (_loadOperation == null)
            {
                Debug.LogError("[SplashController] Cannot load '" + _nextScene + "'. Add it to Build Settings.");
                return;
            }

            _loadOperation.allowSceneActivation = false;
            _loadOperation.completed += OnNextSceneLoaded;
        }

        private void Update()
        {
            if (_activated || _loadOperation == null)
                return;

            _elapsedSeconds += Time.unscaledDeltaTime;

            if (_elapsedSeconds < _minDisplaySeconds)
                return;

            if (_loadOperation.progress < 0.9f)
                return;

            _activated = true;
            _loadOperation.allowSceneActivation = true;
        }

        private void OnNextSceneLoaded(AsyncOperation operation)
        {
            operation.completed -= OnNextSceneLoaded;
            _loadOperation = null;
        }
    }
}
