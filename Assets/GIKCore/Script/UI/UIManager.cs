using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace GIKCore
{
    public class UIManager : MonoBehaviour
    {
        [Header("Scene refs")]
        [Tooltip("Camera the popup canvas renders through.")]
        [SerializeField] private Camera _uiCamera;

        [Tooltip("PopupRoot RectTransform. Every popup is parented under it, on that single canvas.")]
        [SerializeField] private RectTransform _popupParent;

        [Header("Blocker")]
        [Tooltip("Colour of the shared blocker that sits under the top-most modal popup and swallows clicks below it.")]
        [SerializeField] private Color _blockerColor = new Color(0f, 0f, 0f, 0.6f);

        private readonly List<PopupBase> _openPopups = new List<PopupBase>();

        private readonly HashSet<string> _loadingPopups = new HashSet<string>();

        private Image _blocker;

        private int _reservedCount;

        private static bool _isQuitting;

        private static readonly Dictionary<int, PopupBase> AwakenedPopups = new Dictionary<int, PopupBase>();

        public static UIManager Instance { get; private set; }

        public static bool HasPopupOpen => Instance != null && Instance._openPopups.Count > 0;

        public static PopupBase TopPopup =>
            Instance != null && Instance._openPopups.Count > 0
                ? Instance._openPopups[Instance._openPopups.Count - 1]
                : null;

        public Camera UICamera => _uiCamera;

        public RectTransform PopupParent => _popupParent;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (_popupParent == null)
                Debug.LogError("[UIManager] _popupParent is not assigned, popups cannot be shown.");

            if (_uiCamera == null)
                Debug.LogError("[UIManager] _uiCamera is not assigned.");

            CreateBlocker();
        }

        private void OnApplicationQuit()
        {
            _isQuitting = true;
        }

private void OnDestroy()
        {
            if (Instance != this)
                return;

            if (!_isQuitting)
                DoCloseAllPopups();

            AwakenedPopups.Clear();
            Instance = null;
        }

        public static bool IsPopupOpen(string popupName)
        {
            if (Instance == null || string.IsNullOrEmpty(popupName))
                return false;

            if (Instance._loadingPopups.Contains(popupName))
                return true;

            for (int i = 0; i < Instance._openPopups.Count; i++)
            {
                var popup = Instance._openPopups[i];

                if (popup != null && popup.PopupName == popupName)
                    return true;
            }

            return false;
        }

        public static void ShowPopup(string popupName, Action<GameObject> onLoaded = null)
        {
            if (Instance == null)
            {
                Debug.LogError("[UIManager] No instance in the scene, cannot show '" + popupName + "'.");
                onLoaded?.Invoke(null);
                return;
            }

            Instance.DoShowPopup(popupName, popup => onLoaded?.Invoke(popup == null ? null : popup.gameObject));
        }

        public static void ShowPopup<T>(string popupName, Action<T> onLoaded = null) where T : PopupBase
        {
            if (Instance == null)
            {
                Debug.LogError("[UIManager] No instance in the scene, cannot show '" + popupName + "'.");
                onLoaded?.Invoke(null);
                return;
            }

            Instance.DoShowPopup(popupName, popup =>
            {
                if (popup == null)
                {
                    onLoaded?.Invoke(null);
                    return;
                }

                var typed = popup as T;

                if (typed == null)
                {
                    Debug.LogError("[UIManager] Popup '" + popupName + "' has no " + typeof(T).Name + " on its root.");
                    ClosePopup(popup);
                    onLoaded?.Invoke(null);
                    return;
                }

                onLoaded?.Invoke(typed);
            });
        }

        public static Task<GameObject> ShowPopupAsync(string popupName)
        {
            var tcs = new TaskCompletionSource<GameObject>();
            ShowPopup(popupName, go => tcs.TrySetResult(go));
            return tcs.Task;
        }

        public static Task<T> ShowPopupAsync<T>(string popupName) where T : PopupBase
        {
            var tcs = new TaskCompletionSource<T>();
            ShowPopup<T>(popupName, popup => tcs.TrySetResult(popup));
            return tcs.Task;
        }

        public static void ClosePopup(PopupBase popup)
        {
            if (popup != null)
                popup.Close();
        }

        public static void CloseTopPopup()
        {
            var top = TopPopup;

            if (top != null)
                top.Close();
        }

        public static void CloseAllPopups()
        {
            if (Instance != null)
                Instance.DoCloseAllPopups();
        }

internal static void RegisterAwakenedPopup(PopupBase popup)
        {
            if (popup == null)
                return;

            AwakenedPopups[popup.GetInstanceID()] = popup;
        }

internal static void UnregisterAwakenedPopup(PopupBase popup)
        {
            if (popup == null)
                return;

            AwakenedPopups.Remove(popup.GetInstanceID());
        }


private static PopupBase TakeAwakenedPopup(GameObject instance)
        {
            if (instance == null)
                return null;

            int instanceId = instance.GetInstanceID();

            if (!AwakenedPopups.TryGetValue(instanceId, out var popup))
                return null;

            AwakenedPopups.Remove(instanceId);

            if (popup != null && popup.gameObject == instance)
                return popup;

            return null;
        }

        private void CreateBlocker()
        {
            if (_popupParent == null)
                return;

            var go = new GameObject("Blocker", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.layer = _popupParent.gameObject.layer;

            var rect = (RectTransform)go.transform;
            rect.SetParent(_popupParent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;

            _blocker = go.GetComponent<Image>();
            _blocker.color = _blockerColor;
            _blocker.raycastTarget = true;

            go.SetActive(false);
        }

        private void ApplyBlocker()
        {
            if (_blocker == null)
                return;

            int topModalIndex = -1;

            for (int i = _openPopups.Count - 1; i >= 0; i--)
            {
                var popup = _openPopups[i];

                if (popup != null && popup.IsModal && !popup.IsClosing)
                {
                    topModalIndex = i;
                    break;
                }
            }

            var blockerGo = _blocker.gameObject;

            if (topModalIndex < 0)
            {
                if (blockerGo.activeSelf)
                    blockerGo.SetActive(false);

                return;
            }

            if (!blockerGo.activeSelf)
                blockerGo.SetActive(true);

            _blocker.transform.SetSiblingIndex(topModalIndex);
        }

        private void DoShowPopup(string popupName, Action<PopupBase> onLoaded)
        {
            if (string.IsNullOrEmpty(popupName))
            {
                Debug.LogError("[UIManager] Empty popup name.");
                onLoaded?.Invoke(null);
                return;
            }

            if (_popupParent == null)
            {
                Debug.LogError("[UIManager] _popupParent is not assigned, cannot show '" + popupName + "'.");
                onLoaded?.Invoke(null);
                return;
            }

            if (!_loadingPopups.Add(popupName))
            {
                onLoaded?.Invoke(null);
                return;
            }

            int order = _reservedCount++;

            AddressablePrefabLoader.Load(popupName, _popupParent, go =>
            {
                _loadingPopups.Remove(popupName);

                if (go == null)
                {
                    RewindReservationIfIdle();
                    onLoaded?.Invoke(null);
                    return;
                }

                var popup = TakeAwakenedPopup(go);

                if (popup == null)
                {
                    Debug.LogError("[UIManager] Popup '" + popupName +
                                   "' needs an active root GameObject carrying a PopupBase component.");
                    AddressablePrefabLoader.Release(go);
                    RewindReservationIfIdle();
                    onLoaded?.Invoke(null);
                    return;
                }

                popup.PopupName = popupName;
                popup.Order = order;
                popup.Closed += OnPopupClosed;

                InsertByOrder(popup);
                ApplySiblingOrder();

                popup.ShowInternal();

                onLoaded?.Invoke(popup);
            });
        }

        private void InsertByOrder(PopupBase popup)
        {
            int index = _openPopups.Count;

            while (index > 0 && _openPopups[index - 1].Order > popup.Order)
                index--;

            _openPopups.Insert(index, popup);
        }

        private void ApplySiblingOrder()
        {
            for (int i = 0; i < _openPopups.Count; i++)
            {
                var popup = _openPopups[i];

                if (popup != null)
                    popup.transform.SetSiblingIndex(i);
            }

            ApplyBlocker();
        }

        private void OnPopupClosed(PopupBase popup)
        {
            _openPopups.Remove(popup);
            ApplySiblingOrder();
            RewindReservationIfIdle();

            if (popup != null)
                AddressablePrefabLoader.Release(popup.gameObject);
        }

        private void DoCloseAllPopups()
        {
            var snapshot = _openPopups.ToArray();

            for (int i = snapshot.Length - 1; i >= 0; i--)
            {
                if (snapshot[i] != null)
                    snapshot[i].Close();
            }

            _openPopups.Clear();
            _loadingPopups.Clear();
            _reservedCount = 0;

            ApplyBlocker();
        }

        private void RewindReservationIfIdle()
        {
            if (_openPopups.Count == 0 && _loadingPopups.Count == 0)
                _reservedCount = 0;
        }
    }
}
