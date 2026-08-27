using UnityEngine;

namespace FoodMaster
{
    public class UIAdmin : MonoBehaviour
    {
        private int _adminClickCount;
        
        public void ClickButtonAdmin()
        {
            if (RemoteConfig.Instance.AdminTool)
            {
                _adminClickCount++;
                if (_adminClickCount == 10)
                {
                    _adminClickCount = 0;
                    SRDebug.Instance.ShowDebugPanel(SRDebugger.DefaultTabs.Options);
                }
            }
        }
    }
}
