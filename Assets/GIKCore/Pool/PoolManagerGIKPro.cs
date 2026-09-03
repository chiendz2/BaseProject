using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GIKCore.Pool
{
    public class PoolManagerGIKPro : MonoBehaviour
    {
        // Fields
        [SerializeField] private bool m_Autoload = true;
        [SerializeField] private List<SpawnPoolGIKPro> m_LstSpawnPool = new List<SpawnPoolGIKPro>();

        // Values
        private int total = 0;

        // Methods
        public GameObject Spawn(string key, Transform parent = null, string label = "", string search = "", bool activeImmediately = true, ICallBack.CallFunc2<GameObject> onCached = null)
        {
            SpawnPoolGIKPro pool = GetSpawnPool(key); ;
            if (pool != null)
            {
                GameObject go = pool.Spawn(parent, label, search, onCached);
                if (activeImmediately && go != null) pool.DoInvoke(go);
                return go;
            }
            return null;
        }
        public void Preload(string key, int preload, Transform parent = null, string label = "", ICallBack.CallFunc2<GameObject> onCached = null)
        {
            SpawnPoolGIKPro pool = GetSpawnPool(key);
            if (pool != null)
            {
                pool.Preload(preload, parent, label, onCached);
            }
        }
        public int CountPoolActive(string key)
        {
            SpawnPoolGIKPro pool = GetSpawnPool(key);
            if (pool != null)
            {
                return pool.lstPoolBusy.Count;
            }
            return 0;
        }
        public GameObject GetTemplate(string key)
        {
            SpawnPoolGIKPro pool = m_LstSpawnPool.Find((x) => { return x.key.Equals(key); });
            if (pool != null) return pool.template;
            return null;
        }
        public void GetListPoolActive(string key, List<GameObject> output, bool clear = true)
        {
            if (output == null) output = new List<GameObject>();
            if (output != null)
            {
                if (clear) output.Clear();
                SpawnPoolGIKPro pool = GetSpawnPool(key);
                if (pool != null)
                {
                    int count = pool.lstPoolBusy.Count;
                    for (int i = 0; i < count; i++)
                    {
                        output.Add(pool.lstPoolBusy[i]);
                    }
                }
            }
        }
        public void GetListPoolInActive(string key, List<GameObject> output, bool clear = true)
        {
            if (output == null) output = new List<GameObject>();
            if (output != null)
            {
                if (clear) output.Clear();
                SpawnPoolGIKPro pool = GetSpawnPool(key);
                if (pool != null)
                {
                    int count = pool.lstPoolFree.Count;
                    for (int i = 0; i < count; i++)
                    {
                        output.Add(pool.lstPoolFree[i]);
                    }
                }
            }
        }
        public PoolManagerGIKPro DoInvoke(string key, GameObject target)
        {
            SpawnPoolGIKPro pool = GetSpawnPool(key);
            if (pool != null) pool.DoInvoke(target);
            return this;
        }
        public PoolManagerGIKPro DoRecycle(string key, GameObject target)
        {
            SpawnPoolGIKPro pool = GetSpawnPool(key);
            if (pool != null) pool.DoRecycle(target);
            return this;
        }
        public PoolManagerGIKPro DoRecycleAll()
        {
            for (int i = 0; i < total; i++)
            {
                m_LstSpawnPool[i].DoRecycleAll();
            }
            return this;
        }
        public PoolManagerGIKPro DoRecycleAll(string key)
        {
            SpawnPoolGIKPro pool = GetSpawnPool(key);
            if (pool != null)
            {
                pool.DoRecycleAll();
            }
            return this;
        }
        public PoolManagerGIKPro DoDestroy(string key, GameObject target)
        {
            SpawnPoolGIKPro pool = GetSpawnPool(key);
            if (pool != null) pool.DoDestroy(target);
            return this;
        }
        public bool ConstainKey(string key, bool match = true)
        {
            foreach (SpawnPoolGIKPro pool in m_LstSpawnPool)
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
        private SpawnPoolGIKPro GetSpawnPool(string key)
        {
            for (int i = 0; i < total; i++)
            {
                SpawnPoolGIKPro pool = m_LstSpawnPool[i];
                if (pool.key.Equals(key)) return pool;
            }
            return null;
        }

        void Awake()
        {
            if (m_Autoload)
            {
                m_LstSpawnPool.Clear();
                m_LstSpawnPool.AddRange(GetComponentsInChildren<SpawnPoolGIKPro>());
            }
            total = m_LstSpawnPool.Count;
        }

        // Start is called before the first frame update
        //void Start() { }

        // Update is called once per frame
        //void Update() { }
    }
}
