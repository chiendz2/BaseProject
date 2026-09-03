using UnityEngine;

namespace GIKCore
{
    public class SplashController : MonoBehaviour
    {
        [Header("Flow")]
        [Tooltip("Splash stays on screen at least this long, so it never flashes for a single frame on a fast device.")]
        [SerializeField] private float _minDisplaySeconds = 1.5f;

        [Tooltip("Scene loaded after the splash. Must be in Build Settings. Leave empty to stay on the splash.")]
        [SerializeField] private string _nextScene;

        public float Progress => SceneLoader.Progress;

        private void Start()
        {
            if (string.IsNullOrEmpty(_nextScene))
            {
                Debug.Log("[SplashController] No next scene assigned, staying on the splash.");
                return;
            }

            SceneLoader.Load(_nextScene, _minDisplaySeconds);
        }
    }
}
