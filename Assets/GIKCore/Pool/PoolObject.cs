using UnityEngine;
namespace GIKCore.Pool
{
    public class PoolObject
    {
        public int index { get; set; }

        public string prefabName { get; set; }

        /// <summary> TRUE: this pool object is available to reuse </summary>
        public bool isAvailable { get; set; }

        public GameObject go { get; set; }

        public PoolObject()
        {
            index = -1;
            prefabName = "";
            isAvailable = false;//true: out of bounds, false: in bounds
            go = null;
        }

        public void RecycleObject()
        {
            index = -1;
            isAvailable = true;
            go.SetActive(false);
        }
    }
}
