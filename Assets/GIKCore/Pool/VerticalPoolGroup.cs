using UnityEngine;
namespace GIKCore.Pool
{
    public class VerticalPoolGroup : HorizontalOrVerticalPoolGroup
    {
        public enum Alignment { UpperLeft, UpperCenter, UpperRight, MiddleLeft, MiddleCenter, MiddleRight, LowerLeft, LowerCenter, LowerRight }

        // Fields
        [SerializeField][Min(0)] private float m_PaddingTop = 0f;
        [SerializeField][Min(0)] private float m_PaddingBottom = 0f;
        [SerializeField] private Alignment m_ChildAlignment = Alignment.UpperLeft;

        // Method
        public override Vector2 GetCellSize()
        {
            float width = m_ChildForceExpand ? viewport.rect.width : m_CellSize.x;
            if (width <= 0f) width = cellSizeDefault.x;

            float height = m_CellSize.y;
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
            float hConstraint = m_PaddingTop + m_PaddingBottom;
            for (int i = 0; i < numElement; i++)
            {
                Vector2 elmSize = GetCellSizeFromMultiple(i);
                AddToListCellSize(elmSize);
                hConstraint += (elmSize.y + m_Spacing.y);
            }
            hConstraint = Mathf.Max(0, hConstraint - m_Spacing.y);

            float yTo = -m_PaddingTop;
            if (hConstraint <= height)
            {
                if (m_ChildAlignment == Alignment.MiddleLeft || m_ChildAlignment == Alignment.MiddleCenter || m_ChildAlignment == Alignment.MiddleRight)
                {
                    yTo -= (height * 0.5f - hConstraint * 0.5f);
                    hConstraint = height;
                }
                else if (m_ChildAlignment == Alignment.LowerLeft || m_ChildAlignment == Alignment.LowerCenter || m_ChildAlignment == Alignment.LowerRight)
                {
                    yTo -= (height - hConstraint);
                    hConstraint = height;
                }
            }

            //set content size delta
            TrySetLayoutSizeDelta(width, hConstraint);

            //calculate init local position of each cell in group.
            //anchors min, max, pivot is at(0, 1)
            float xTo = 0f;

            for (int i = 0; i < numElement; i++)
            {
                Vector2 cellSize = GetCellSizeAt(i);

                if (!m_ChildForceExpand)
                {
                    if (m_ChildAlignment == Alignment.UpperLeft || m_ChildAlignment == Alignment.MiddleLeft || m_ChildAlignment == Alignment.LowerLeft)
                        xTo = 0f;
                    else if (m_ChildAlignment == Alignment.UpperCenter || m_ChildAlignment == Alignment.MiddleCenter || m_ChildAlignment == Alignment.LowerCenter)
                        xTo = width * 0.5f - cellSize.x * 0.5f;
                    else if (m_ChildAlignment == Alignment.UpperRight || m_ChildAlignment == Alignment.MiddleRight || m_ChildAlignment == Alignment.LowerRight)
                        xTo = width - cellSize.x;
                }

                AddToListLocalCellPos(new Vector2(xTo + m_Spacing.x, yTo));
                yTo -= (cellSize.y + m_Spacing.y);
            }
        }
        protected override void UpdateData()
        {
            base.UpdateData();

            if (!allowUpdate) return;

            //Calculate distance between current pivot's position and init pivot's position of layput group
            float offsetY = content.anchoredPosition.y; //init posY is at y = 0 (local)

            //check pool, inactive object if it's out of bound
            foreach (PoolObject po in listPool)
            {
                if (!po.isAvailable)
                {
                    float yTop = GetWorldCellPos(po.index).y + offsetY;
                    float yBot = yTop - GetCellSizeAt(po.index).y;
                    if (yBot > 0 || yTop < -viewport.rect.height)
                        po.RecycleObject();
                }
            }

            //data
            int numElement = adapter.Count;
            for (int i = 0; i < numElement; i++)
            {
                float yTop = GetWorldCellPos(i).y + offsetY;
                float yBot = yTop - GetCellSizeAt(i).y;
                if (yBot > 0 || yTop < -viewport.rect.height || IsPoolWorking(i))
                    continue;

                //add cell
                MakePool(i);
            }
        }
    }
}