using System;
using UnityEngine;
using UnityEngine.UI;

namespace GIKCore
{
    public class PopupTemplate : PopupBase
    {
        [Header("Template refs")]
        [Tooltip("Optional. Wire a close button here and it is hooked up automatically.")]
        [SerializeField] private Button _closeButton;

        public static bool IsShowing => UIManager.IsPopupOpen("PopupTemplate");

        public static void Show(Action<PopupTemplate> onLoaded = null)
        {
            UIManager.ShowPopup("PopupTemplate", onLoaded);
        }

        public static void Hide()
        {
            var popup = UIManager.TopPopup as PopupTemplate;

            if (popup != null)
                popup.Close();
        }

        protected override void OnShow()
        {
            if (_closeButton == null)
                return;

            _closeButton.onClick.RemoveListener(Close);
            _closeButton.onClick.AddListener(Close);
        }

        protected override void OnClose()
        {
            if (_closeButton == null)
                return;

            _closeButton.onClick.RemoveListener(Close);
        }
    }
}
