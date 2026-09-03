using UnityEngine;

namespace GIKCore
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-200)]
    public class PersistentRoot : MonoBehaviour
    {
        private static PersistentRoot _instance;

        private void Awake()
        {
            if (transform.parent != null)
            {
                Debug.LogError("[PersistentRoot] Must sit on a root GameObject, '" + name + "' is a child.");
                return;
            }

            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }
    }
}
