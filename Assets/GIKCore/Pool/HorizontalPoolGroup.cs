using UnityEngine;
namespace GIKCore.Pool
{
    public class HorizontalPoolGroup : HorizontalOrVerticalPoolGroup
    {
        public enum Alignment { UpperLeft, MiddleLeft, LowerLeft, UpperCenter, MiddleCenter, LowerCenter, UpperRight, MiddleRight, LowerRight }

        // Fields
        [SerializeField][Min(0)] private float m_PaddingLeft = 0f;
        [SerializeField][Min(0)] private float m_PaddingRight = 0f;
        [SerializeField] private Alignment m_ChildAlignment = Alignment.UpperLeft;

        // Method
        public override Vector2 GetCellSize()
        {
            float width = m_CellSize.x;
            if (width <= 0f) width = cellSizeDefault.x;

            float height = m_ChildForceExpand ? viewport.rect.height : m_CellSize.y;
            if (height <= 0f) height = cellSizeDefault.y;

            return new Vector2(width, height);
        }

        protected override void CalculateSizeDelta()
        {
            base.CalculateSizeDelta();

            int numElement = adapter.Count;
            float width = viewport.rect.width;
            float height = viewport.rect.height;

            //calculate content size
            float wConstraint = m_PaddingLeft + m_PaddingRight;
            for (int i = 0; i < numElement; i++)
            {
                Vector2 elmSize = GetCellSizeFromMultiple(i);
                AddToListCellSize(elmSize);
                wConstraint += (elmSize.x + m_Spacing.x);
            }
            wConstraint = Mathf.Max(0, wConstraint - m_Spacing.x);

            float xTo = m_PaddingLeft;
            if (wConstraint <= width)
            {
                if (m_ChildAlignment == Alignment.UpperCenter || m_ChildAlignment == Alignment.MiddleCenter || m_ChildAlignment == Alignment.LowerCenter)
                {
                    xTo += (width * 0.5f - wConstraint * 0.5f);
                    wConstraint = width;
                }
                else if (m_ChildAlignment == Alignment.UpperRight || m_ChildAlignment == Alignment.MiddleRight || m_ChildAlignment == Alignment.LowerRight)
                {
                    xTo += (width - wConstraint);
                    wConstraint = width;
                }
            }

            //set content size delta
            TrySetLayoutSizeDelta(wConstraint, height);

            //calculate init local position of each cell in group.
            //anchors min, max, pivot is at(0, 1)
            float yTo = 0f;

            for (int i = 0; i < numElement; i++)
            {
                Vector2 cellSize = GetCellSizeAt(i);

                if (!m_ChildForceExpand)
                {
                    if (m_ChildAlignment == Alignment.UpperLeft || m_ChildAlignment == Alignment.UpperCenter || m_ChildAlignment == Alignment.UpperRight)
                        yTo = 0f;
                    else if (m_ChildAlignment == Alignment.MiddleLeft || m_ChildAlignment == Alignment.MiddleCenter || m_ChildAlignment == Alignment.MiddleRight)
                        yTo = cellSize.y * 0.5f - height * 0.5f;
                    else if (m_ChildAlignment == Alignment.LowerLeft || m_ChildAlignment == Alignment.LowerCenter || m_ChildAlignment == Alignment.LowerRight)
                        yTo = cellSize.y - height;
                }

                AddToListLocalCellPos(new Vector2(xTo, yTo + m_Spacing.y));
                xTo += (cellSize.x + m_Spacing.x);
            }
        }
        protected override void UpdateData()
        {
            base.UpdateData();

            if (!allowUpdate) return;

            //Calculate distance between current pivot's position and init pivot's position of layput group
            float offsetX = content.anchoredPosition.x;//init posX is at x = 0 (local)

            //check pool, inactive object if it's out of bound
            foreach (PoolObject po in listPool)
            {
                if (!po.isAvailable)
                {
                    float xLeft = GetWorldCellPos(po.index).x + offsetX;
                    float xRight = xLeft + GetCellSizeAt(po.index).x;
                    if (xRight < 0 || xLeft > viewport.rect.width)
                        po.RecycleObject();
                }
            }

            //data
            int numElement = adapter.Count;
            for (int i = 0; i < numElement; i++)
            {
                float xLeft = GetWorldCellPos(i).x + offsetX;
                float xRight = xLeft + GetCellSizeAt(i).x;
                if (xRight < 0 || xLeft > viewport.rect.width || IsPoolWorking(i))
                {
                    continue;
                }

                //add cell
                MakePool(i);
            }
        }
    }
}