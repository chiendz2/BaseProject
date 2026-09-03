//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.UI;
//namespace GIKCore.Pool
//{
//    [DisallowMultipleComponent, ExecuteInEditMode]
//    public class RecycleLayoutGroup : MonoBehaviour
//    {
//        // Fields
//        [SerializeField] private ScrollHelper m_ScrollHelper;
//        [SerializeField] private Transform m_Group;
//        [SerializeField] private List<GameObject> m_CellPrefabs;

//        // Values
//        protected ICallBack.CallFunc3<GameObject, int> cellSiblingCB = null;
//        private ICallBack.CallFunc6<GameObject, int> cellPrefabCB = null;
//        private ICallBack.CallFunc4<GameObject, object, int> cellDataCB = null;
//        private ICallBack.CallFunc2<GameObject> cellDataClearCB = null;

//        protected List<object> _adapter = new List<object>();
//        protected List<GameObject> lstCellOfAdapter = new List<GameObject>();
//        protected List<GameObject> lstCell = new List<GameObject>();
//        public bool isInitialized { get; private set; } = false;
//        public int dataCount { get; private set; } = 0;

//        // Methods
//        public ScrollHelper scrollHelper { get { return m_ScrollHelper; } }
//        public Transform layoutGroup { get { return m_Group; } }
//        public RecycleLayoutGroup SetLayoutGroup(Transform t) { m_Group = t; return this; }
//        public RecycleLayoutGroup SetActive(bool on) { gameObject.SetActive(on); return this; }
//        public RecycleLayoutGroup SetActiveLayoutGroup(bool on) { layoutGroup.gameObject.SetActive(on);return this; }

//        public T CastData<T>(object data)
//        {
//            try
//            {
//                T ret = (T)data;
//                return ret;
//                //return (T)System.Convert.ChangeType(data, typeof(T));
//            }
//            catch (System.InvalidCastException e)
//            {
//                return default(T);
//            }
//        }

//        /// <summary>
//        /// Set the sibling of the cell
//        /// </summary>
//        /// <param name="func"></param>
//        /// <returns></returns>
//        public RecycleLayoutGroup SetCellSiblingCallback(ICallback.CallFunc3<GameObject, int> func) { cellSiblingCB = func; return this; }

//        /// <summary>
//        /// <para>In case of multiple prefabs, return prefab you want to work with data at 'index' in adapter.</para>
//        /// <para></para>
//        /// <para>Param: delegate (int 'index of data in adapter')</para>
//        /// <para>Notice: use function GetDeclarePrefab to get prefab you set in Unity Editor</para>
//        /// </summary>
//        public RecycleLayoutGroup SetCellPrefabCallback(ICallback.CallFunc6<GameObject, int> func) { cellPrefabCB = func; return this; }

//        /// <summary>
//        /// <para>Define the way how you work with each cell data.</para>
//        /// <para></para>
//        /// <para>Param: delegate (GameObject go, T data, int 'index of data in adapter')</para>
//        /// </summary>
//        public RecycleLayoutGroup SetCellDataCallback<T>(ICallback.CallFunc4<GameObject, T, int> func)
//        {
//            Init();
//            cellDataCB = (GameObject go, object data, int index) =>
//            {
//                if (func != null) func(go, CastData<T>(data), index);
//            };
//            return this;
//        }
//        public RecycleLayoutGroup SetCellDataCallback2(ICallback.CallFunc4<GameObject, object, int> func)
//        {
//            Init();
//            cellDataCB = func;
//            return this;
//        }
//        /** excecute when GameObject become inactive to clear all data of cell */
//        public RecycleLayoutGroup SetCellDataClearCallback(ICallback.CallFunc2<GameObject> func) { cellDataClearCB = func; return this; }

//        public GameObject FindGameObject(int index)
//        {
//            if (index >= 0 && index < lstCellOfAdapter.Count)
//            {
//                return lstCellOfAdapter[index];
//            }
//            return null;
//        }
//        public RecycleLayoutGroup QueryAdapter(ICallback.CallFunc3<int, GameObject> callback, bool reverse = false)
//        {
//            int count = lstCellOfAdapter.Count;
//            if (reverse)
//            {
//                for (int i = count - 1; i >= 0; i--)
//                {
//                    int index = i;
//                    GameObject go = lstCellOfAdapter[i];
//                    callback?.Invoke(index, go);
//                }
//            }
//            else
//            {
//                for (int i = 0; i < count; i++)
//                {
//                    int index = i;
//                    GameObject go = lstCellOfAdapter[i];
//                    callback?.Invoke(index, go);
//                }
//            }
//            return this;
//        }
//        public RecycleLayoutGroup QueryAdapterWithBreak(ICallback.CallFunc7<bool, int, GameObject> callback, bool reverse = false)
//        {
//            int count = lstCellOfAdapter.Count;
//            if (reverse)
//            {
//                for (int i = count - 1; i >= 0; i--)
//                {
//                    if (callback != null)
//                    {
//                        int index = i;
//                        GameObject go = lstCellOfAdapter[i];
//                        bool b = callback.Invoke(index, go);
//                        if (b) break;
//                    }
//                }
//            }
//            else
//            {
//                for (int i = 0; i < count; i++)
//                {
//                    if (callback != null)
//                    {
//                        int index = i;
//                        GameObject go = lstCellOfAdapter[i];
//                        bool b = callback.Invoke(index, go);
//                        if (b) break;
//                    }
//                }
//            }
//            return this;
//        }

//        /// <summary>
//        /// <para>Return the prefab you set in Unity Editor. Return null if not found</para>
//        /// <para></para>
//        /// <para>Param: Index of prefab in Unity Editor</para>
//        /// </summary>
//        public GameObject GetDeclarePrefab(int index)
//        {
//            if (m_CellPrefabs != null && index >= 0 && index < m_CellPrefabs.Count)
//                return m_CellPrefabs[index];
//            return null;
//        }

//        public RecycleLayoutGroup SetDeclarePrefab(params GameObject[] values)
//        {
//            m_CellPrefabs.Clear();
//            foreach (GameObject go in values)
//                m_CellPrefabs.Add(go);
//            return this;
//        }

//        /// <summary>
//        /// <para>Return a GameObject have data at 'index' in adapter. Return null if not found.</para>
//        /// <para></para>
//        /// <para>Param: Index of data in adapter.</para>
//        /// </summary>
//        private GameObject GetCellPrefab(int index)
//        {
//            if (cellPrefabCB != null)
//                return cellPrefabCB(index);
//            return GetDeclarePrefab(0);
//        }

//        /// <summary>
//        /// <para>Return data at 'index' in adapter.</para>
//        /// <para></para>
//        /// <para>Param: Index of data in adapter</para>
//        /// <para>Notice: Make sure you call function SetAdapter first.</para>
//        /// </summary>
//        public T GetCellData<T>(int index)
//        {
//            object data = GetCellData2(index);
//            return CastData<T>(data);
//        }
//        public object GetCellData2(int index)
//        {
//            if (index >= 0 && index < _adapter.Count)
//                return _adapter[index];
//            return null;
//        }
//        protected void SetCellData(GameObject go, object data, int index)
//        {
//            if (cellDataCB != null) cellDataCB(go, data, index);
//        }

//        /// <summary>
//        /// <para>Return index of data in adapter. Return -1 if not found</para>
//        /// </summary>
//        /// <returns>The index.</returns>
//        /// <param name="data">Data.</param>
//        public int GetDataIndex(object data)
//        {
//            if (data == null)
//                return -1;

//            int numElement = _adapter.Count;
//            for (int i = 0; i < numElement; i++)
//            {
//                if (data.Equals(_adapter[i]))
//                    return i;
//            }

//            return -1;
//        }
//        public int GetLastDataIndex()
//        {
//            return (_adapter.Count - 1);
//        }

//        private void AddCell(GameObject go, int index)
//        {
//            go.name = "item_" + index;
//            go.SetActive(true);

//            if (cellSiblingCB != null)
//                cellSiblingCB.Invoke(go, index);
//            else go.transform.SetSiblingIndex(index);

//            SetCellData(go, GetCellData2(index), index);
//            lstCellOfAdapter.Add(go);
//        }

//        /// <summary>
//        /// <para>Set Adapter</para>
//        /// <para>Param: List data with type T.</para>        
//        /// </summary>
//        public virtual RecycleLayoutGroup SetAdapter<T>(List<T> adapter, bool forceRebuildLayoutImmediate = false)
//        {
//            Init();
//            _adapter.Clear();
//            lstCellOfAdapter.Clear();

//            if (adapter != null)
//            {
//                foreach (T data in adapter)
//                    _adapter.Add(data);
//            }

//            int cellCount = lstCell.Count;
//            for (int i = 0; i < cellCount; i++)
//            {
//                GameObject go = lstCell[i];
//                go.SetActive(false);
//                if (cellDataClearCB != null) cellDataClearCB(go);
//            }

//            dataCount = _adapter.Count;
//            for (int i = 0; i < dataCount; i++)
//            {
//                Transform child = (i < cellCount) ? lstCell[i].transform : null;
//                if (child == null)
//                {
//                    GameObject go = Instantiate(GetCellPrefab(i), m_Group);
//                    lstCell.Add(go);
//                    child = go.transform;
//                }

//                if (child != null)
//                {
//                    AddCell(child.gameObject, i);
//                }
//            }

//            if (forceRebuildLayoutImmediate)
//                LayoutRebuilder.ForceRebuildLayoutImmediate(m_Group as RectTransform);
//            return this;
//        }
//        public RecycleLayoutGroup SetAdapter2(List<object> adapter, bool forceRebuildLayoutImmediate = false)
//        {
//            SetAdapter(adapter);
//            return this;
//        }
//        public RecycleLayoutGroup ClearAdapter() { SetAdapter<object>(null); return this; }
//        public RecycleLayoutGroup ResetPosition()
//        {
//            (layoutGroup as RectTransform).anchoredPosition = Vector2.zero;
//            return this;
//        }
//        private void Init()
//        {
//            if (!isInitialized)
//            {
//                isInitialized = true;
//            }
//        }

//        private void Awake()
//        {
//            if (m_Group == null) m_Group = transform;
//            Init();
//        }

//        // Start is called before the first frame update
//        //void Start() { }

//        // Update is called once per frame
//        //void Update() { }
//    }
//}
