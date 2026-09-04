using System;
using System.Threading.Tasks;
using GIKCore;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Demo
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Canvas))]
    [RequireComponent(typeof(GraphicRaycaster))]
    [RequireComponent(typeof(CanvasGroup))]
    public class PopupTicketShop : PopupBase
    {
        [Header("Ticket shop refs")]
        [Tooltip("Closes the popup. Hooked up in OnShow and unhooked in OnClose.")]
        [SerializeField] private Button _closeButton;

        [Tooltip("One buy button per ticket pack, ordered left to right. The index is what PackSelected carries.")]
        [SerializeField] private Button[] _packButtons;

        [Tooltip("Panel that scales during the entrance animation.")]
        [SerializeField] private RectTransform _frame;

        [Header("Entrance animation")]
        [Tooltip("Seconds the fade and scale entrance takes. Counted in unscaled time so it still plays while the game sits at timeScale 0.")]
        [Min(0f)]
        [SerializeField] private float _fadeInSeconds = 0.24f;

        [Tooltip("Starting scale of the panel before the entrance animation.")]
        [Range(0.8f, 1f)]
        [SerializeField] private float _frameStartScale = 0.92f;

        private const float EntranceOvershoot = 1.70158f;

        private Canvas _canvas;

        private CanvasGroup _group;

        private float _fadeElapsed;

        private bool _isFadingIn;

        private UnityAction[] _packButtonActions;

        public event Action<int> PackSelected;

        public static bool IsShowing => UIManager.IsPopupOpen(DemoPopupId.PopupTicketShop);

        public static Task<PopupTicketShop> Show(Action<PopupTicketShop> onLoaded = null)
        {
            return UIManager.ShowPopup(DemoPopupId.PopupTicketShop, onLoaded);
        }

        public static void Hide()
        {
            var popup = UIManager.TopPopup as PopupTicketShop;

            if (popup != null)
                popup.Close();
        }

        protected override void Awake()
        {
            base.Awake();

            _canvas = GetComponent<Canvas>();
            _group = GetComponent<CanvasGroup>();
            _packButtonActions = new UnityAction[_packButtons == null ? 0 : _packButtons.Length];
        }

        protected override void OnShow()
        {
            ConfigureCanvas();
            ConfigureFade();
            AddButtonListeners();
        }

        private void Update()
        {
            if (!_isFadingIn)
                return;

            _fadeElapsed += Time.unscaledDeltaTime;
            float progress = _fadeInSeconds <= 0f ? 1f : Mathf.Clamp01(_fadeElapsed / _fadeInSeconds);
            _group.alpha = progress;

            if (_frame != null)
                _frame.localScale = Vector3.one * CalculateFrameScale(progress);

            if (progress < 1f)
                return;

            _group.alpha = 1f;

            if (_frame != null)
                _frame.localScale = Vector3.one;

            _isFadingIn = false;
        }

        protected override void OnClose()
        {
            _isFadingIn = false;
            _group.blocksRaycasts = false;

            if (_frame != null)
                _frame.localScale = Vector3.one;

            RemoveButtonListeners();
            PackSelected = null;
        }

        private void ConfigureCanvas()
        {
            _canvas.enabled = true;
            _canvas.renderMode = RenderMode.ScreenSpaceCamera;

            if (UIManager.Instance == null)
            {
                Debug.LogError("[PopupTicketShop] UIManager is not available, the popup camera cannot be assigned.");
                _canvas.worldCamera = null;
                return;
            }

            _canvas.worldCamera = UIManager.Instance.UICamera;
        }

        private void ConfigureFade()
        {
            _group.blocksRaycasts = true;
            _fadeElapsed = 0f;
            _isFadingIn = _fadeInSeconds > 0f;
            _group.alpha = _isFadingIn ? 0f : 1f;

            if (_frame != null)
                _frame.localScale = _isFadingIn ? Vector3.one * _frameStartScale : Vector3.one;
        }

        private float CalculateFrameScale(float progress)
        {
            float easedProgress = progress - 1f;
            float overshootProgress = easedProgress * easedProgress * ((EntranceOvershoot + 1f) * easedProgress + EntranceOvershoot) + 1f;
            return Mathf.LerpUnclamped(_frameStartScale, 1f, overshootProgress);
        }

        private void AddButtonListeners()
        {
            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveListener(Close);
                _closeButton.onClick.AddListener(Close);
            }

            if (_packButtons == null)
                return;

            for (int i = 0; i < _packButtons.Length; i++)
            {
                var button = _packButtons[i];

                if (button == null)
                    continue;

                int index = i;
                UnityAction action = _packButtonActions[i];

                if (action == null)
                {
                    action = () => RaisePackSelected(index);
                    _packButtonActions[i] = action;
                }

                button.onClick.RemoveListener(action);
                button.onClick.AddListener(action);
            }
        }

        private void RemoveButtonListeners()
        {
            if (_closeButton != null)
                _closeButton.onClick.RemoveListener(Close);

            if (_packButtons == null || _packButtonActions == null)
                return;

            int count = Mathf.Min(_packButtons.Length, _packButtonActions.Length);

            for (int i = 0; i < count; i++)
            {
                if (_packButtons[i] != null && _packButtonActions[i] != null)
                    _packButtons[i].onClick.RemoveListener(_packButtonActions[i]);
            }
        }

        private void RaisePackSelected(int index)
        {
            var handler = PackSelected;

            if (handler != null)
                handler.Invoke(index);
        }
    }
}
