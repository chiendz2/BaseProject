using UnityEngine;
using UnityEngine.UI;

namespace Demo
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public class PopupTicketShopTestButton : MonoBehaviour
    {
        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _button.onClick.RemoveListener(OpenPopup);
            _button.onClick.AddListener(OpenPopup);
        }

        private void OnDestroy()
        {
            if (_button != null)
                _button.onClick.RemoveListener(OpenPopup);
        }

        private void OpenPopup()
        {
            _ = PopupTicketShop.Show();
        }
    }
}
