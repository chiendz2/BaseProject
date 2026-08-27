using UnityEngine;

namespace GIKCore
{
    public class ShaderPropertyId : MonoBehaviour
    {
        public static int Color;
        public static int Opacity;
        public static int Threshold;

        private void Awake()
        {
            Color = Shader.PropertyToID("_Color");
            Opacity = Shader.PropertyToID("_Opacity");
            Threshold = Shader.PropertyToID("_Threshold");
        }
    }
}
