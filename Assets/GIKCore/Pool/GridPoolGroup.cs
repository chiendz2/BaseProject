using UnityEngine;
using System.Collections.Generic;

namespace GIKCore.Pool
{
    public class GridPoolGroup : BasePoolGroup
    {
        public enum StartAxis
        {
            //fill all column(x) before fill new row(y)
            Horizontal,
            //fill all row(y) before fill new column(x)
            Vertical
        }
        public enum Alignment { UpperLeft, UpperCenter, UpperRight, MiddleLeft, MiddleCenter, MiddleRight, LowerLeft, LowerCenter, LowerRight }

        // Fields
        [SerializeField][Min(0)] private float m_PaddingLeft = 0f;
        [SerializeField][Min(0)] private float m_PaddingRight = 0f;
        [SerializeField][Min(0)] private float m_PaddingTop = 0f;
        [SerializeField][Min(0)] private float m_PaddingBottom = 0f;
        [SerializeField] private StartAxis m_StartAxis = StartAxis.Horizontal;
        [SerializeField] private Alignment m_ChildAlignment = Alignment.UpperLeft;
        [SerializeField][Min(1)] private int m_ConstraintCount = 1; //fixed column or row count                

        // Method
        public override BasePoolGroup SetAdapter<T>(List<T> adapter, bool resetPosition = true)
        {
            base.SetAdapter(adapter, resetPosition);
            
            CalculateSizeDelta();
            allowUpdate = true;//just allow update after calculate size delta

            ResetPool();
            UpdateData();

            if (resetPosition) ScrollToFirst(0);
            return this;
        }

        public void SetStartAxis(StartAxis start)
        {
            m_StartAxis = start;
        }

        public void SetChildAlignment(Alignment alignment)
        {
            m_ChildAlignment = alignment;
        }

        public void SetPaddingLeft(int values)
        {
            m_PaddingLeft = values;
        }

        protected override void CalculateSizeDelta()
        {
            base.CalculateSizeDelta();

            int numElement = adapter.Count;
            int numOfGroup = 0;

            //calculate number of group cell
            for (int i = 0; i < numElement; i += m_ConstraintCount)
            {
                numOfGroup++;
            }

            //add cell size
            for (int i = 0; i < numElement; i++)
            {
                AddToListCellSize(GetCellSize());
            }

            float width = viewport.rect.width;
            float height = viewport.rect.height;
            float wConstraint = m_PaddingLeft + m_PaddingRight;
            float hConstraint = m_PaddingTop + m_PaddingBottom;

            Vector2 cellSize = GetCellSize();
            int numElementOfGroup = numElement > m_ConstraintCount ? m_ConstraintCount : numElement;
            //calculate content size
            if (m_StartAxis == StartAxis.Horizontal)
            {
                wConstraint += ((cellSize.x + m_Spacing.x) * numElementOfGroup - m_Spacing.x);
                hConstraint += ((cellSize.y + m_Spacing.y) * numOfGroup - m_Spacing.y);
            }
            else if (m_StartAxis == StartAxis.Vertical)
            {
                wConstraint += ((cellSize.x + m_Spacing.x) * numOfGroup - m_Spacing.x);
                hConstraint += ((cellSize.y + m_Spacing.y) * numElementOfGroup - m_Spacing.y);
            }

            float xInit = m_PaddingLeft, yInit = -m_PaddingTop;
            if (wConstraint <= width)
            {
                if (m_ChildAlignment == Alignment.UpperCenter || m_ChildAlignment == Alignment.MiddleCenter || m_ChildAlignment == Alignment.LowerCenter)
                {
                    xInit += (width * 0.5f - wConstraint * 0.5f);
                    wConstraint = width;
                }
                else if (m_ChildAlignment == Alignment.UpperRight || m_ChildAlignment == Alignment.MiddleRight || m_ChildAlignment == Alignment.LowerRight)
                {
                    xInit += (width - wConstraint);
                    wConstraint = width;
                }
            }
            if (hConstraint <= height)
            {
                if (m_ChildAlignment == Alignment.MiddleLeft || m_ChildAlignment == Alignment.MiddleCenter || m_ChildAlignment == Alignment.MiddleRight)
                {
                    yInit -= (height * 0.5f - hConstraint * 0.5f);
                    hConstraint = height;
                }
                else if (m_ChildAlignment == Alignment.LowerLeft || m_ChildAlignment == Alignment.LowerCenter || m_ChildAlignment == Alignment.LowerRight)
                {
                    yInit -= (height - hConstraint);
                    hConstraint = height;
                }
            }

            //set size delta
            TrySetLayoutSizeDelta(wConstraint, hConstraint);

            /*
             * calculate init local position of each cell in group.		 
             * anchors min, max, pivot is at (0, 1).
             * start corner is always upper left.
             */
            int index = -1;

            // fill all column with constraint count before break to new row 
            if (m_StartAxis == StartAxis.Horizontal)
            {
                for (int i = 0; i < numOfGroup; i++)
                {
                    float xTo = xInit;
                    float yTo = yInit - i * (cellSize.y + m_Spacing.y);

                    for (int j = 0; j < m_ConstraintCount; j++)
                    {
                        index++;
                        if (index > (numElement - 1))
                            break;
                        AddToListLocalCellPos(new Vector2(xTo, yTo));
                        xTo = xTo + (cellSize.x + m_Spacing.x);
                    }
                }
            }
            // fill all row with constraint count before break to new column
            else if (m_StartAxis == StartAxis.Vertical)
            {
                for (int i = 0; i < numOfGroup; i++)
                {
                    float xTo = xInit + i * (cellSize.x + m_Spacing.x);
                    float yTo = yInit;

                    for (int j = 0; j < m_ConstraintCount; j++)
                    {
                        index++;
                        if (index > (numElement - 1))
                            break;
                        AddToListLocalCellPos(new Vector2(xTo, yTo));
                        yTo = yTo - (cellSize.y + m_Spacing.y);
                    }
                }
            }
        }
        protected override void UpdateData()
        {
            base.UpdateData();

            if (!allowUpdate) return;

            //Calculate distance between current pivot's position and init pivot's position of layput group
            //init pos is at (0, 0) (local)
            float offsetX = content.anchoredPosition.x;
            float offsetY = content.anchoredPosition.y;

            //check pool, inactive object if it's out of bound
            foreach (PoolObject po in listPool)
            {
                if (!po.isAvailable)
                {
                    Vector2 _cellSize = GetCellSizeAt(po.index);
                    Vector2 _cellPos = GetWorldCellPos(po.index);

                    float xLeft = _cellPos.x + offsetX;
                    float xRight = xLeft + _cellSize.x;

                    float yTop = _cellPos.y + offsetY;
                    float yBot = yTop - _cellSize.y;

                    if (xRight < 0 || xLeft > viewport.rect.width
                        || yBot > 0 || yTop < -viewport.rect.height)
                        po.RecycleObject();
                }
            }

            //data
            int numElement = adapter.Count;
            for (int i = 0; i < numElement; i++)
            {
                Vector2 _cellSize = GetCellSizeAt(i);
                Vector2 _cellPos = GetWorldCellPos(i);

                float xLeft = _cellPos.x + offsetX;
                float xRight = xLeft + _cellSize.x;

                float yTop = _cellPos.y + offsetY;
                float yBot = yTop - _cellSize.y;

                if (xRight < 0 || xLeft > viewport.rect.width
                    || yBot > 0 || yTop < -viewport.rect.height
                    || IsPoolWorking(i))
                    continue;

                //add cell
                MakePool(i);
            }
        }
    }
}