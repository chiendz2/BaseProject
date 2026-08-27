using FoodMaster;
using GamePopup;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace GameUI
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance;

        public Camera UICamera;

        [SerializeField] private GameObject _initBG;

        [Header("Preload popup")]
        [Tooltip("Addressable key của các popup hay hiện trong màn chơi. Warm sẵn asset (giữ bundle " +
                 "nóng) lúc khởi động -> mỗi lần hiện popup KHÔNG phải đọc bundle từ đĩa nữa, bớt giật.")]
        [SerializeField] private string[] _warmPopups;

        private readonly List<AsyncOperationHandle<GameObject>> _warmHandles =
            new List<AsyncOperationHandle<GameObject>>();

        private UISceneLoading _uiSceneLoading;
        private UISceneTransition _uiSceneTransition;
        private PopupNoticeFloat _noticeFloat;
        private PopupWait _popupWait;
        private readonly HashSet<string> _loadingPopups = new HashSet<string>();
        private AsyncOperationHandle _trackedLoadingHandle;
        private bool _hasTrackedLoadingHandle;
        private float _trackedProgressFrom;
        private float _trackedProgressTo = 1f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            GameEvents.LoadScene += LoadScene;
            GameEvents.ShowSceneLoading += ShowSceneLoading;
            GameEvents.HideSceneLoading += HideSceneLoading;
            GameEvents.ShowNoticeFloat += ShowNoticeFloat;
            GameEvents.ShowNoticeFloatCustom += ShowNoticeFloatCustom;
            GameEvents.ShowPopup += ShowPopup;
            GameEvents.ShowWait += ShowWait;

            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance != this)
                return;

            GameEvents.LoadScene -= LoadScene;
            GameEvents.ShowSceneLoading -= ShowSceneLoading;
            GameEvents.HideSceneLoading -= HideSceneLoading;
            GameEvents.ShowNoticeFloat -= ShowNoticeFloat;
            GameEvents.ShowNoticeFloatCustom -= ShowNoticeFloatCustom;
            GameEvents.ShowPopup -= ShowPopup;
            GameEvents.ShowWait -= ShowWait;

            for (int i = 0; i < _warmHandles.Count; i++)
                if (_warmHandles[i].IsValid()) Addressables.Release(_warmHandles[i]);
            _warmHandles.Clear();

            Instance = null;
        }

        private void Start()
        {
            Addressables.InstantiateAsync(PopupId.UISceneTransition, transform).Completed += (handle) =>
            {
                if (handle.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
                {
                    handle.Result.GetComponent<Canvas>().worldCamera = UICamera;
                    _uiSceneTransition = handle.Result.GetComponent<UISceneTransition>();
                    _uiSceneTransition.gameObject.SetActive(false);
                }
            };
            Addressables.InstantiateAsync(PopupId.PopupNoticeFloat, transform).Completed += (handle) =>
            {
                if (handle.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
                {
                    _noticeFloat = handle.Result.GetComponent<PopupNoticeFloat>();
                    _noticeFloat.gameObject.SetActive(false);
                }
            };
            Addressables.InstantiateAsync(PopupId.PopupWait, transform).Completed += (handle) =>
            {
                if (handle.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
                {
                    _popupWait = handle.Result.GetComponent<PopupWait>();
                    _popupWait.gameObject.SetActive(false);
                }
            };

            WarmPopups();
        }

        // Nạp trước và GIỮ asset của các popup hay dùng -> bundle luôn nóng, InstantiateAsync lúc mở
        // popup không phải load bundle từ đĩa nữa (bớt khựng). Chỉ warm asset (prefab), KHÔNG tạo sẵn
        // instance nên vòng đời từng popup giữ nguyên (Awake/Start vẫn chạy mỗi lần mở như cũ).
        private void WarmPopups()
        {
            if (_warmPopups == null) return;
            for (int i = 0; i < _warmPopups.Length; i++)
            {
                var key = _warmPopups[i];
                if (string.IsNullOrEmpty(key)) continue;
                try
                {
                    _warmHandles.Add(Addressables.LoadAssetAsync<GameObject>(key));
                }
                catch (InvalidKeyException)
                {
                    // Popup chưa được đánh addressable -> bỏ qua, KHÔNG để 1 key sai làm hỏng cả Start.
                    Debug.LogWarning($"[UIManager] Bỏ warm popup không có addressable: {key}");
                }
            }
        }

        private void LoadScene(string sceneId)
        {
            if (_uiSceneLoading == null && _uiSceneTransition != null)
            {
                _uiSceneTransition.Show(() => StartSceneLoad(sceneId));
            }
            else
            {
                StartSceneLoad(sceneId);
            }
        }

        private void StartSceneLoad(string sceneId)
        {
            var handle = Addressables.LoadSceneAsync(sceneId);
            TrackSceneLoading(handle, .5f, .95f);
        }

        public void TrackSceneLoading(
            AsyncOperationHandle handle,
            float progressFrom = 0f,
            float progressTo = 1f)
        {
            _trackedLoadingHandle = handle;
            _hasTrackedLoadingHandle = true;
            _trackedProgressFrom = progressFrom;
            _trackedProgressTo = progressTo;

            _uiSceneLoading?.Track(handle, progressFrom, progressTo);
        }

        private void ShowSceneLoading()
        {
            if (_uiSceneLoading == null)
            {
                ShowPopup(PopupId.UISceneLoading, (go) =>
                {
                    _uiSceneLoading = go.GetComponent<UISceneLoading>();
                    if (_hasTrackedLoadingHandle)
                    {
                        _uiSceneLoading.Track(
                            _trackedLoadingHandle,
                            _trackedProgressFrom,
                            _trackedProgressTo);
                    }

                    if (_initBG != null)
                    {
                        Destroy(_initBG);
                    }
                });
            }
        }

        public void HideSceneLoading()
        {
            _hasTrackedLoadingHandle = false;
            if (_uiSceneLoading != null)
            {
                _uiSceneLoading.Complete();
            }
            else
            {
                _uiSceneTransition?.Hide();
            }
        }

        private void ShowPopup(string popupName, Action<GameObject> callback)
        {
            if (string.IsNullOrEmpty(popupName) || !_loadingPopups.Add(popupName))
                return;

            // Giữ order tại thời điểm user yêu cầu mở. Nếu hai Addressables hoàn tất ngược thứ tự,
            // popup được nhấn sau vẫn phải nằm trên popup được nhấn trước.
            var reservedSortingOrder = PopupBase.ReserveSortingOrder();
            Addressables.InstantiateAsync(popupName, transform).Completed += (handle) =>
            {
                _loadingPopups.Remove(popupName);
                if (handle.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
                {
                    var popup = handle.Result.GetComponent<PopupBase>();
                    if (popup != null)
                        popup.ApplySortingOrder(reservedSortingOrder);

                    callback?.Invoke(handle.Result);
                }
            };
        }

        private void ShowNoticeFloat(string content)
        {
            if (_noticeFloat != null)
            {
                _noticeFloat.Show(content);
            }
        }

        private void ShowNoticeFloatCustom(string content, float lifetime, Color color)
        {
            if (_noticeFloat != null)
            {
                _noticeFloat.Show(content, lifetime, color);
            }
        }

        private void ShowWait(bool show)
        {
            if (_popupWait != null)
            {
                if (show) _popupWait.Show();
                else _popupWait.Hide();
            }
        }
    }
}
