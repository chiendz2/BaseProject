using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace GIKCore.Pool
{
    [DisallowMultipleComponent, ExecuteInEditMode]
    public class SpawnPoolGIK : MonoBehaviour
    {
        // Fields
        [SerializeField] private string m_Key = "";
        [SerializeField] private Transform m_ParentDefault;
        [Tooltip("Only use in case GameObject Template is null")]
        [SerializeField] private string m_TemplatePath;
        [Header("[Priority]")]
        [SerializeField] private GameObject m_Template;

        // Values
        public int total { get; private set; } = 0;
        public List<GameObject> lstPool { get; private set; } = new List<GameObject>();

        // Methods
        public string key { get { return m_Key; } }
        public GameObject template
        {
            get
            {
                if (m_Template == null)
                    LoadTemplateByPath();
                return m_Template;
            }
        }

        public GameObject Spawn(Transform parent = null, string label = "")
        {
            if (parent == null) parent = m_ParentDefault;

            GameObject go = lstPool.Find((x) => { return !x.activeSelf; });
            if (go == null && template != null)
            {
                go = Instantiate(template, parent);
                go.SetActive(false);
                lstPool.Add(go);
            }

            if (go != null)
            {
                if (!string.IsNullOrEmpty(label)) 
                    go.name = label;
                go.transform.SetParent(parent);
                go.transform.localPosition = Vector3.zero;
            }

            return go;
        }
        public SpawnPoolGIK SetPoolCustom(string poolKey, GameObject poolTemplate, Transform poolParentDefault = null)
        {
            m_Key = poolKey;
            m_Template = poolTemplate;
            m_ParentDefault = poolParentDefault;

            if (m_Template != null)
                m_Template.SetActive(false);//inactive by default
            lstPool.Clear();
            return this;
        }
        public SpawnPoolGIK SetPoolCustom2(string poolKey, string poolTemplatePath, Transform poolParentDefault = null)
        {
            m_Key = poolKey;
            m_TemplatePath = poolTemplatePath;
            m_ParentDefault = poolParentDefault;

            LoadTemplateByPath();
            if (m_Template != null)
                m_Template.SetActive(false);//inactive by default
            lstPool.Clear();
            return this;
        }

        private void LoadTemplateByPath()
        {
            if (!string.IsNullOrEmpty(m_TemplatePath))
            {
                //m_Template = IUtil.LoadPrefabWithParent(m_TemplatePath, transform);
                //if (m_Template != null)
                //{
                //    m_Template.transform.localPosition = Vector3.zero;
                //    m_Template.SetActive(false);
                //}
            }
        }

        public SpawnPoolGIK DoDestroy(GameObject target)
        {
            if (target != null)
            {
                lstPool.Remove(target);
                Destroy(target);
            }
            return this;
        }
        public SpawnPoolGIK DoDestroyAll()
        {
            foreach (GameObject pool in lstPool)
            {
                Destroy(pool);
            }
            lstPool.Clear();
            return this;
        }
        public SpawnPoolGIK DoRecycleAll()
        {
            foreach (GameObject pool in lstPool)
            {
                pool.SetActive(false);
            }
            return this;
        }

        void Awake()
        {
            if (m_ParentDefault == null) m_ParentDefault = transform;
            if (string.IsNullOrEmpty(m_Key)) m_Key = string.Format("Pool-{0}", System.DateTime.Now.Ticks);
            if (m_TemplatePath == null)
                LoadTemplateByPath();
        }

        // Start is called before the first frame update
        //void Start() { }

        // Update is called once per frame
        //void Update() { }
    }
}
