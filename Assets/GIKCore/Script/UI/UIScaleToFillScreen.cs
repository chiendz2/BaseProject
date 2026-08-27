using UnityEngine;

namespace GIKCore
{
    public class UIScaleToFillScreen : MonoBehaviour
    {
        [Tooltip("RectTransform scaled up until it covers the whole screen, keeping its aspect ratio.")]
        [SerializeField] private RectTransform _rectTrans;

        private void Awake()
        {
            if (_rectTrans == null)
            {
                Debug.LogError("[UIScaleToFillScreen] _rectTrans is not assigned on '" + name + "'.");
                return;
            }

            var screenFactor = 1f * Screen.width / Screen.height;
            var rectSize = _rectTrans.sizeDelta;
            var rectFactor = rectSize.x / rectSize.y;

            if (screenFactor < rectFactor)
            {
                rectSize *= rectFactor / screenFactor;
                _rectTrans.sizeDelta = rectSize;
            }
        }
    }
}
