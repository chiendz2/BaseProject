using UnityEngine;
using System.Collections.Generic;

namespace GIKCore.Pool
{
    public abstract class HorizontalOrVerticalPoolGroup : BasePoolGroup
    {
        // Fields
        [SerializeField] protected bool m_ChildForceExpand = false;

        // Method	

        /// <summary>
        /// <para>In case of multiple size, return cell size you want to work with data at 'index' in adapter.</para>
        /// <para></para>
        /// <para>Param: delegate (int 'index of data in adapter')</para>
        /// </summary>
        public void SetCellSizeCallback(ICallBack.CallFunc6<Vector2, int> func) { cellSizeCB = func; }

        /// <summary>
        /// <para>In case of multiple size, return size of GameObject have data at 'index' in adapter.</para>
        /// <para></para>
        /// <para>Param: Index of data in adapter</para>
        /// </summary>
        protected Vector2 GetCellSizeFromMultiple(int index)
        {
            if (cellSizeCB != null)
                return cellSizeCB(index);
            return GetCellSize();
        }

        protected void CheckToAddAtleast3Cells()
        {
            int numElement = adapter.Count;
            if (numElement <= 0)
                return;

            int _1stIndex = 0;
            int _2ndIndex = 1;
            int _3rdIndex = 2;

            bool _1stFound = false;
            bool _2ndFound = false;
            bool _3rdFound = false;

            foreach (PoolObject po in listPool)
            {
                if (po.index == _1stIndex) _1stFound = true;
                else if (po.index == _2ndIndex) _2ndFound = true;
                else if (po.index == _3rdIndex) _3rdFound = true;
            }

            if (_2ndIndex > (numElement - 1)) _2ndFound = true;
            if (_3rdIndex > (numElement - 1)) _3rdFound = true;

            if (!_1stFound) MakePool(_1stIndex);
            if (!_2ndFound) MakePool(_2ndIndex);
            if (!_3rdFound) MakePool(_3rdIndex);
        }

        public override BasePoolGroup SetAdapter<T>(List<T> adapter, bool resetPosition = true)
        {
            base.SetAdapter(adapter, resetPosition);
            
            CalculateSizeDelta();
            allowUpdate = true;//just allow update after calculate size delta

            ResetPool();
            CheckToAddAtleast3Cells();
            UpdateData();

            if (resetPosition) ScrollToFirst(0);
            return this;
        }
    }
}