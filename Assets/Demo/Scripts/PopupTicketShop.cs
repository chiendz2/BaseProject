using System;
using GIKCore;
using UnityEngine;
using UnityEngine.UI;

namespace Demo
{
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

        [Header("Appearance")]
        [Tooltip("Seconds the fade in takes. Counted in unscaled time so it still plays while the game sits at timeScale 0.")]
        [SerializeField] private float _fadeInSeconds = 0.15f;

        private Canvas _canvas;

        private CanvasGroup _group;

        private float _fadeElapsed;

        private bool _isFadingIn;

        private UnityEngine.Events.UnityAction[] _packButtonActions;


        public event Action<int> PackSelected;

        public static bool IsShowing => UIManager.IsPopupOpen(DemoPopupId.PopupTicketShop);

        public static void Show(Action<PopupTicketShop> onLoaded = null)
        {
            UIManager.ShowPopup(DemoPopupId.PopupTicketShop, onLoaded);
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

            if (_packButtons != null)
                _packButtonActions = new UnityEngine.Events.UnityAction[_packButtons.Length];
        }

protected override void OnShow()
        {
            _canvas.enabled = true;
            _canvas.renderMode = RenderMode.ScreenSpaceCamera;
            _canvas.worldCamera = UIManager.Instance == null ? null : UIManager.Instance.UICamera;
            _group.blocksRaycasts = true;
            _fadeElapsed = 0f;
            _isFadingIn = _fadeInSeconds > 0f;
            _group.alpha = _isFadingIn ? 0f : 1f;

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
                var action = _packButtonActions[i];

                if (action == null)
                {
                    action = () => RaisePackSelected(index);
                    _packButtonActions[i] = action;
                }

                button.onClick.RemoveListener(action);
                button.onClick.AddListener(action);
            }
        }

        private void Update()
        {
            if (!_isFadingIn)
                return;

            _fadeElapsed += Time.unscaledDeltaTime;

            if (_fadeElapsed >= _fadeInSeconds)
            {
                _group.alpha = 1f;
                _isFadingIn = false;
                return;
            }

            _group.alpha = _fadeElapsed / _fadeInSeconds;
        }

protected override void OnClose()
        {
            _isFadingIn = false;
            _group.blocksRaycasts = false;

            if (_closeButton != null)
                _closeButton.onClick.RemoveListener(Close);

            if (_packButtons != null && _packButtonActions != null)
            {
                int count = Mathf.Min(_packButtons.Length, _packButtonActions.Length);

                for (int i = 0; i < count; i++)
                {
                    if (_packButtons[i] != null && _packButtonActions[i] != null)
                        _packButtons[i].onClick.RemoveListener(_packButtonActions[i]);
                }
            }

            PackSelected = null;
        }

        private void RaisePackSelected(int index)
        {
            var handler = PackSelected;

            if (handler != null)
                handler.Invoke(index);
        }
    }
}
