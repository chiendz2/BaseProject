using UnityEngine;

namespace GIKCore
{
    public class AnimationId : MonoBehaviour
    {
        public static int Alive;

        private void Awake()
        {
            Alive = Animator.StringToHash("Alive");
        }
    }
}
