namespace SRF.UI.Layout
{
    using Internal;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// Layout Group controller that arranges children in rows, fitting as many on a line until total width exceeds parent
    /// bounds
    /// </summary>
    [AddComponentMenu(ComponentMenuPaths.FlowLayoutGroup)]
    public class FlowLayoutGroup : LayoutGroup
    {
        /// <summary>
        /// Holds the rects that will make up the current row being processed
        /// </summary>
        private readonly IList<RectTransform> _rowList = new List<RectTransform>();

        private float _layoutHeight;
        public bool ChildForceExpandHeight = false;
        public bool ChildForceExpandWidth = false;
        public float Spacing = 0f;

        protected bool IsCenterAlign
        {
            get
            {
                return this.childAlignment == TextAnchor.LowerCenter || this.childAlignment == TextAnchor.MiddleCenter ||
                       this.childAlignment == TextAnchor.UpperCenter;
            }
        }

        protected bool IsRightAlign
        {
            get
            {
                return this.childAlignment == TextAnchor.LowerRight || this.childAlignment == TextAnchor.MiddleRight ||
                       this.childAlignment == TextAnchor.UpperRight;
            }
        }

        protected bool IsMiddleAlign
        {
            get
            {
                return this.childAlignment == TextAnchor.MiddleLeft || this.childAlignment == TextAnchor.MiddleRight ||
                       this.childAlignment == TextAnchor.MiddleCenter;
            }
        }

        protected bool IsLowerAlign
        {
            get
            {
                return this.childAlignment == TextAnchor.LowerLeft || this.childAlignment == TextAnchor.LowerRight ||
                       this.childAlignment == TextAnchor.LowerCenter;
            }
        }

        public override void CalculateLayoutInputHorizontal()
        {
            base.CalculateLayoutInputHorizontal();

            var minWidth = this.GetGreatestMinimumChildWidth() + this.padding.left + this.padding.right;

            this.SetLayoutInputForAxis(minWidth, -1, -1, 0);
        }

        public override void SetLayoutHorizontal()
        {
            this.SetLayout(this.rectTransform.rect.width, 0, false);
        }

        public override void SetLayoutVertical()
        {
            this.SetLayout(this.rectTransform.rect.width, 1, false);
        }

        public override void CalculateLayoutInputVertical()
        {
            this._layoutHeight = this.SetLayout(this.rectTransform.rect.width, 1, true);
        }

        /// <summary>
        /// Main layout method
        /// </summary>
        /// <param name="width">Width to calculate the layout with</param>
        /// <param name="axis">0 for horizontal axis, 1 for vertical</param>
        /// <param name="layoutInput">If true, sets the layout input for the axis. If false, sets child position for axis</param>
        public float SetLayout(float width, int axis, bool layoutInput)
        {
            var groupHeight = this.rectTransform.rect.height;

            // Width that is available after padding is subtracted
            var workingWidth = this.rectTransform.rect.width - this.padding.left - this.padding.right;

            // Accumulates the total height of the rows, including spacing and padding.
            var yOffset = this.IsLowerAlign ? this.padding.bottom : (float)this.padding.top;

            var currentRowWidth = 0f;
            var currentRowHeight = 0f;

            for (var i = 0; i < this.rectChildren.Count; i++)
            {
                // LowerAlign works from back to front
                var index = this.IsLowerAlign ? this.rectChildren.Count - 1 - i : i;

                var child = this.rectChildren[index];

                var childWidth = LayoutUtility.GetPreferredSize(child, 0);
                var childHeight = LayoutUtility.GetPreferredSize(child, 1);

                // Max child width is layout group with - padding
                childWidth = Mathf.Min(childWidth, workingWidth);

                // Apply spacing if not the first element in a row
                if (this._rowList.Count > 0)
                {
                    currentRowWidth += this.Spacing;
                }

                // If adding this element would exceed the bounds of the row,
                // go to a new line after processing the current row
                if (currentRowWidth + childWidth > workingWidth)
                {
                    // Undo spacing addition if we're moving to a new line (Spacing is not applied on edges)
                    currentRowWidth -= this.Spacing;

                    // Process current row elements positioning
                    if (!layoutInput)
                    {
                        var h = this.CalculateRowVerticalOffset(groupHeight, yOffset, currentRowHeight);
                        this.LayoutRow(this._rowList, currentRowWidth, currentRowHeight, workingWidth, this.padding.left, h, axis);
                    }

                    // Clear existing row
                    this._rowList.Clear();

                    // Add the current row height to total height accumulator, and reset to 0 for the next row
                    yOffset += currentRowHeight;
                    yOffset += this.Spacing;

                    currentRowHeight = 0;
                    currentRowWidth = 0;
                }

                currentRowWidth += childWidth;
                this._rowList.Add(child);

                // We need the largest element height to determine the starting position of the next line
                if (childHeight > currentRowHeight)
                {
                    currentRowHeight = childHeight;
                }
            }

            if (!layoutInput)
            {
                var h = this.CalculateRowVerticalOffset(groupHeight, yOffset, currentRowHeight);

                // Layout the final row
                this.LayoutRow(this._rowList, currentRowWidth, currentRowHeight, workingWidth, this.padding.left, h, axis);
            }

            this._rowList.Clear();

            // Add the last rows height to the height accumulator
            yOffset += currentRowHeight;
            yOffset += this.IsLowerAlign ? this.padding.top : this.padding.bottom;

            if (layoutInput)
            {
                if (axis == 1)
                {
                    this.SetLayoutInputForAxis(yOffset, yOffset, -1, axis);
                }
            }

            return yOffset;
        }

        private float CalculateRowVerticalOffset(float groupHeight, float yOffset, float currentRowHeight)
        {
            float h;

            if (this.IsLowerAlign)
            {
                h = groupHeight - yOffset - currentRowHeight;
            }
            else if (this.IsMiddleAlign)
            {
                h = groupHeight * 0.5f - this._layoutHeight * 0.5f + yOffset;
            }
            else
            {
                h = yOffset;
            }
            return h;
        }

        protected void LayoutRow(IList<RectTransform> contents, float rowWidth, float rowHeight, float maxWidth,
            float xOffset, float yOffset, int axis)
        {
            var xPos = xOffset;

            if (!this.ChildForceExpandWidth && this.IsCenterAlign)
            {
                xPos += (maxWidth - rowWidth) * 0.5f;
            }
            else if (!this.ChildForceExpandWidth && this.IsRightAlign)
            {
                xPos += (maxWidth - rowWidth);
            }

            var extraWidth = 0f;

            if (this.ChildForceExpandWidth)
            {
                var flexibleChildCount = 0;

                for (var i = 0; i < this._rowList.Count; i++)
                {
                    if (LayoutUtility.GetFlexibleWidth(this._rowList[i]) > 0f)
                    {
                        flexibleChildCount++;
                    }
                }

                if (flexibleChildCount > 0)
                {
                    extraWidth = (maxWidth - rowWidth) / flexibleChildCount;
                }
            }

            for (var j = 0; j < this._rowList.Count; j++)
            {
                var index = this.IsLowerAlign ? this._rowList.Count - 1 - j : j;

                var rowChild = this._rowList[index];

                var rowChildWidth = LayoutUtility.GetPreferredSize(rowChild, 0);

                if (LayoutUtility.GetFlexibleWidth(rowChild) > 0f)
                {
                    rowChildWidth += extraWidth;
                }

                var rowChildHeight = LayoutUtility.GetPreferredSize(rowChild, 1);

                if (this.ChildForceExpandHeight)
                {
                    rowChildHeight = rowHeight;
                }

                rowChildWidth = Mathf.Min(rowChildWidth, maxWidth);

                var yPos = yOffset;

                if (this.IsMiddleAlign)
                {
                    yPos += (rowHeight - rowChildHeight) * 0.5f;
                }
                else if (this.IsLowerAlign)
                {
                    yPos += (rowHeight - rowChildHeight);
                }

                if (axis == 0)
                {
#if UNITY_2019_1
                    SetChildAlongAxis(rowChild, 0, 1f, xPos, rowChildWidth);
#else
                    this.SetChildAlongAxis(rowChild, 0, xPos, rowChildWidth);
#endif
                }
                else
                {
#if UNITY_2019_1
                    SetChildAlongAxis(rowChild, 1, 1f, yPos, rowChildHeight);
#else
                    this.SetChildAlongAxis(rowChild, 1, yPos, rowChildHeight);
#endif
                }

                xPos += rowChildWidth + this.Spacing;
            }
        }

        public float GetGreatestMinimumChildWidth()
        {
            var max = 0f;

            for (var i = 0; i < this.rectChildren.Count; i++)
            {
                var w = LayoutUtility.GetMinWidth(this.rectChildren[i]);

                max = Mathf.Max(w, max);
            }

            return max;
        }
    }
}
