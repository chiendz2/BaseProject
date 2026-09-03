using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GIKCore.Pool
{
    public class PoolManagerGIK : MonoBehaviour
    {
        // Fields
        [SerializeField] private bool m_Autoload = true;
        [SerializeField] private GameObject m_PoolCustom;
        [SerializeField] private List<SpawnPoolGIK> m_LstSpawnPool = new List<SpawnPoolGIK>();

        // Values
        private int total = 0;

        // Methods
        public GameObject Spawn(string key, Transform parent = null, string label = "", bool activeImmediately = true)
        {
            SpawnPoolGIK pool = m_LstSpawnPool.Find((x) => { return x.key.Equals(key); });
            if (pool != null)
            {
                GameObject go = pool.Spawn(parent, label);
                if (activeImmediately && go != null) go.SetActive(true);
                return go;
            }
            return null;
        }
        public GameObject GetTemplate(string key)
        {
            SpawnPoolGIK pool = m_LstSpawnPool.Find((x) => { return x.key.Equals(key); });
            if (pool != null) return pool.template;
            return null;
        }
        public List<GameObject> GetListPool(string key)
        {
            SpawnPoolGIK pool = m_LstSpawnPool.Find((x) => { return x.key.Equals(key); });
            if (pool != null) return pool.lstPool;
            return null;
        }
        public List<GameObject> GetListPoolActive(string key)
        {
            List<GameObject> l1 = GetListPool(key);
            if (l1 != null)
            {
                List<GameObject> l2 = l1.FindAll((x) => { return x.activeSelf; });
                return l2;
            }
            return null;
        }
        public List<GameObject> GetListPoolInActive(string key)
        {
            List<GameObject> l1 = GetListPool(key);
            if (l1 != null)
            {
                List<GameObject> l2 = l1.FindAll((x) => { return !x.activeSelf; });
                return l2;
            }
            return null;
        }
        public void GetListPoolActive(string key, List<GameObject> output, bool clear = true)
        {
            if (output == null) output = new List<GameObject>();
            if (output != null)
            {
                if (clear) output.Clear();
                SpawnPoolGIK pool = GetSpawnPool(key);
                if (pool != null)
                {
                    for (int i = 0; i < pool.total; i++)
                    {
                        if (pool.lstPool[i].activeSelf) output.Add(pool.lstPool[i]);
                    }
                }
            }
        }
        private SpawnPoolGIK GetSpawnPool(string key)
        {
            for (int i = 0; i < total; i++)
            {
                SpawnPoolGIK pool = m_LstSpawnPool[i];
                if (pool.gameObject.activeSelf && pool.key.Equals(key)) return pool;
            }
            return null;
        }
        public PoolManagerGIK SetPoolCustom(string poolKey, GameObject poolTemplate, Transform poolParentDefault = null)
        {
            if (ConstainKey(poolKey)) return this;

            if (m_PoolCustom != null)
            {
                GameObject clone = Instantiate(m_PoolCustom, transform);
                clone.transform.localPosition = Vector3.zero;

                SpawnPoolGIK spg = clone.GetComponent<SpawnPoolGIK>();
                spg.SetPoolCustom(poolKey, poolTemplate, poolParentDefault);
                m_LstSpawnPool.Add(spg);
            }

            return this;
        }
        public PoolManagerGIK SetPoolCustom2(string poolKey, string poolTemplatePath, Transform poolParentDefault = null)
        {
            if (ConstainKey(poolKey)) return this;

            if (m_PoolCustom != null)
            {
                GameObject clone = Instantiate(m_PoolCustom, transform);
                clone.transform.localPosition = Vector3.zero;

                SpawnPoolGIK spg = clone.GetComponent<SpawnPoolGIK>();
                spg.SetPoolCustom2(poolKey, poolTemplatePath, poolParentDefault);
                m_LstSpawnPool.Add(spg);
            }

            return this;
        }
        public PoolManagerGIK DoRecycle(GameObject target)
        {
            if (target != null) target.SetActive(false);
            return this;
        }
        public PoolManagerGIK DoRecycleAll(List<string> lstIgnore = null)
        {
            foreach (SpawnPoolGIK pool in m_LstSpawnPool)
            {
                if (lstIgnore != null && lstIgnore.Contains(pool.key))
                {
                    // do nothing
                }
                else pool.DoRecycleAll();
            }
            return this;
        }
        public PoolManagerGIK DoRecycleAll2(string key)
        {
            SpawnPoolGIK pool = m_LstSpawnPool.Find((x) => { return x.key.Equals(key); });
            if (pool != null)
            {
                pool.DoRecycleAll();
            }
            return this;
        }
        public PoolManagerGIK DoDestroy(string key, GameObject target)
        {
            SpawnPoolGIK pool = m_LstSpawnPool.Find((x) => { return x.key.Equals(key); });
            if (pool != null) pool.DoDestroy(target);
            return this;
        }
        public PoolManagerGIK DoDestroyAll(List<string> lstIgnore = null)
        {
            foreach (SpawnPoolGIK pool in m_LstSpawnPool)
            {
                if (lstIgnore != null && lstIgnore.Contains(pool.key))
                {
                    // do nothing
                }
                else pool.DoDestroyAll();
            }
            return this;
        }
        public bool ConstainKey(string key, bool match = true)
        {
            foreach (SpawnPoolGIK pool in m_LstSpawnPool)
            {
                if (CheckKey(pool.key, key, match))
                    return true;
            }
            return false;
        }

        private bool CheckKey(string source, string search, bool match = true)
        {
            if (match)
            {
                if (source.Equals(search)) return true;
            }
            else
            {
                if (source.Contains(search)) return true;
            }
            return false;
        }

        void Awake()
        {
            if (m_Autoload)
            {
                m_LstSpawnPool.Clear();
                m_LstSpawnPool.AddRange(GetComponentsInChildren<SpawnPoolGIK>());
            }
            total = m_LstSpawnPool.Count;
        }

        // Start is called before the first frame update
        //void Start() { }

        // Update is called once per frame
        //void Update() { }
    }
}
