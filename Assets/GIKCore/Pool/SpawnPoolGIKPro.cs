using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GIKCore.Pool
{
    [DisallowMultipleComponent, ExecuteInEditMode]
    public class SpawnPoolGIKPro : MonoBehaviour
    {
        // Fields
        [SerializeField] private string m_Key = "";
        [SerializeField] private Transform m_ParentDefault;
        [SerializeField] private GameObject m_Template;
        // Values
        public List<GameObject> lstPoolBusy { get; private set; } = new List<GameObject>();
        public List<GameObject> lstPoolFree { get; private set; } = new List<GameObject>();

        // Methods
        public string key { get { return m_Key; } }
        public GameObject template { get { return m_Template; } }

        public GameObject Spawn(Transform parent = null, string label = "", string search = "", ICallBack.CallFunc2<GameObject> onCached = null)
        {
            if (parent == null) parent = m_ParentDefault;

            GameObject go = GetPoolFree(search);
            if (go == null && m_Template != null)
            {
                go = Instantiate(m_Template, parent);
                go.SetActive(false);
                lstPoolFree.Add(go);
                onCached?.Invoke(go);
            }

            if (go != null)
            {
                if (!string.IsNullOrEmpty(label)) go.name = label;
                go.transform.SetParent(parent);
                go.transform.localPosition = Vector3.zero;
            }

            return go;
        }
        public void Preload(int preload, Transform parent = null, string label = "", ICallBack.CallFunc2<GameObject> onCached = null)
        {
            if (parent == null) parent = m_ParentDefault;

            if (m_Template != null)
            {
                for (int i = 0; i < preload; i++)
                {
                    GameObject go = Instantiate(m_Template, parent);
                    if (!string.IsNullOrEmpty(label)) go.name = label;
                    go.transform.localPosition = Vector3.zero;
                    go.SetActive(false);

                    lstPoolFree.Add(go);
                    onCached?.Invoke(go);
                }
            }
        }
        public SpawnPoolGIKPro DoDestroy(GameObject target)
        {
            if (target != null)
            {
                lstPoolBusy.Remove(target);
                lstPoolFree.Remove(target);
                Destroy(target);
            }
            return this;
        }
        public SpawnPoolGIKPro DoInvoke(GameObject target)
        {
            if (target != null)
            {
                if (!target.activeSelf)
                {
                    lstPoolFree.Remove(target);
                    lstPoolBusy.Add(target);
                }
                target.SetActive(true);
            }
            return this;
        }
        public SpawnPoolGIKPro DoRecycle(GameObject target)
        {
            if (target != null)
            {
                if (target.activeSelf)
                {
                    lstPoolBusy.Remove(target);
                    lstPoolFree.Add(target);
                }
                target.SetActive(false);
            }
            return this;
        }
        public SpawnPoolGIKPro DoRecycleAll()
        {
            int count = lstPoolBusy.Count;
            for (int i = 0; i < count; i++)
            {
                lstPoolBusy[i].SetActive(false);
                lstPoolFree.Add(lstPoolBusy[i]);
            }
            lstPoolBusy.Clear();
            return this;
        }

        private GameObject GetPoolFree(string search = "")
        {
            bool specialSearch = !string.IsNullOrEmpty(search);
            int count = lstPoolFree.Count;
            for (int i = 0; i < count; i++)
            {
                if (!lstPoolFree[i].activeSelf)
                {
                    if (specialSearch)
                    {
                        if (lstPoolFree[i].name.Contains(search))
                        {
                            return lstPoolFree[i];
                        }
                    }
                    else
                    {
                        return lstPoolFree[i];
                    }
                }
            }

            return null;
        }

        void Awake()
        {
            if (m_ParentDefault == null) m_ParentDefault = transform;
            if (string.IsNullOrEmpty(m_Key)) m_Key = string.Format("Pool-{0}", System.DateTime.Now.Ticks);
        }

        // Start is called before the first frame update
        //void Start() { }

        // Update is called once per frame
        //void Update() { }
    }
}
