using System;
using UnityEngine;

namespace GIKCore
{
    [DisallowMultipleComponent]
    public class PopupBase : MonoBehaviour
    {
        [Header("Popup refs")]
        [Tooltip("Root RectTransform of this popup. Must stretch to fill the popup canvas.")]
        [SerializeField] private RectTransform _rect;

        [Header("Behaviour")]
        [Tooltip("Modal popups block every click underneath them. UIManager owns one shared blocker for all popups, " +
                 "so nothing has to be wired here. Untick for a floating notice or toast that must let clicks through.")]
        [SerializeField] private bool _isModal = true;

        public event Action<PopupBase> Closed;

        public string PopupName { get; internal set; }

        public int Order { get; internal set; }

        public bool IsClosing { get; private set; }

        public bool IsModal => _isModal;

        public RectTransform Rect => _rect;

protected virtual void Awake()
        {
            UIManager.RegisterAwakenedPopup(this);
        }

protected virtual void OnDestroy()
        {
            UIManager.UnregisterAwakenedPopup(this);
        }


        internal void ShowInternal()
        {
            if (!gameObject.activeSelf)
                gameObject.SetActive(true);

            OnShow();
        }

        public void Close()
        {
            if (IsClosing)
                return;

            IsClosing = true;
            OnClose();

            var handler = Closed;
            Closed = null;

            if (handler != null)
                handler.Invoke(this);
            else
                AddressablePrefabLoader.Release(gameObject);
        }

        protected virtual void OnShow()
        {
        }

        protected virtual void OnClose()
        {
        }
    }
}
