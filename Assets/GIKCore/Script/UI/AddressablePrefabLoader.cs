using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace GIKCore
{
    public static class AddressablePrefabLoader
    {
        public static void Load(string key, Transform parent, Action<GameObject> onLoaded)
        {
            if (string.IsNullOrEmpty(key))
            {
                Debug.LogError("[AddressablePrefabLoader] Empty addressable key.");
                onLoaded?.Invoke(null);
                return;
            }

            AsyncOperationHandle<GameObject> handle;

            try
            {
                handle = Addressables.InstantiateAsync(key, parent, false);
            }
            catch (Exception e)
            {
                Debug.LogError("[AddressablePrefabLoader] Cannot instantiate '" + key + "': " + e.Message);
                onLoaded?.Invoke(null);
                return;
            }

            handle.Completed += op =>
            {
                if (op.Status == AsyncOperationStatus.Succeeded && op.Result != null)
                {
                    onLoaded?.Invoke(op.Result);
                    return;
                }

                var reason = op.OperationException == null ? "unknown error" : op.OperationException.Message;
                Debug.LogError("[AddressablePrefabLoader] Failed to load '" + key + "': " + reason);

                if (op.IsValid())
                    Addressables.Release(op);

                onLoaded?.Invoke(null);
            };
        }

        public static Task<GameObject> LoadAsync(string key, Transform parent)
        {
            var tcs = new TaskCompletionSource<GameObject>();
            Load(key, parent, go => tcs.TrySetResult(go));
            return tcs.Task;
        }

        public static void Release(GameObject instance)
        {
            if (instance == null)
                return;

            if (!Addressables.ReleaseInstance(instance))
                UnityEngine.Object.Destroy(instance);
        }
    }
}
