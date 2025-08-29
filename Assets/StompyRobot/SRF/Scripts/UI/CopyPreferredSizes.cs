using System;

namespace SRF.UI
{
    using Internal;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// Copies the preferred size of another layout element (useful for a parent object basing its sizing from a child
    /// element).
    /// This does have very quirky behaviour, though.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [ExecuteInEditMode]
    [AddComponentMenu(ComponentMenuPaths.CopyPreferredSizes)]
    public class CopyPreferredSizes : LayoutElement
    {
        public enum Operations
        {
            Max,
            Min
        }

        [Serializable]
        public class CopySource
        {
            public RectTransform Rect;

            public float PaddingHeight;
            public float PaddingWidth;
        }

        public CopySource[] CopySources;
        public Operations Operation;


        public override float preferredWidth
        {
            get
            {
                if (this.CopySources == null || this.CopySources.Length == 0 || !this.IsActive())
                {
                    return -1f;
                }

                var current = this.Operation == Operations.Max ? float.MinValue : float.MaxValue;

                for (var i = 0; i < this.CopySources.Length; i++)
                {
                    if (this.CopySources[i].Rect == null)
                        continue;

                    var width = LayoutUtility.GetPreferredWidth(this.CopySources[i].Rect) + this.CopySources[i].PaddingWidth;
                    if (this.Operation == Operations.Max && width > current)
                        current = width;
                    else if (this.Operation == Operations.Min && width < current)
                        current = width;
                }

                // ReSharper disable CompareOfFloatsByEqualityOperator
                if (this.Operation == Operations.Max && current == float.MinValue) return -1;
                if (this.Operation == Operations.Min && current == float.MaxValue) return -1;
                // ReSharper restore CompareOfFloatsByEqualityOperator

                return current;
            }
        }

        public override float preferredHeight
        {
            get
            {
                if (this.CopySources == null || this.CopySources.Length == 0 || !this.IsActive())
                {
                    return -1f;
                }

                var current = this.Operation == Operations.Max ? float.MinValue : float.MaxValue;

                for (var i = 0; i < this.CopySources.Length; i++)
                {
                    if (this.CopySources[i].Rect == null)
                        continue;

                    var height = LayoutUtility.GetPreferredHeight(this.CopySources[i].Rect) + this.CopySources[i].PaddingHeight;
                    if (this.Operation == Operations.Max && height > current)
                        current = height;
                    else if (this.Operation == Operations.Min && height < current)
                        current = height;
                }

                // ReSharper disable CompareOfFloatsByEqualityOperator
                if (this.Operation == Operations.Max && current == float.MinValue) return -1;
                if (this.Operation == Operations.Min && current == float.MaxValue) return -1;
                // ReSharper restore CompareOfFloatsByEqualityOperator

                return current;
            }
        }

        public override int layoutPriority
        {
            get { return 2; }
        }
    }
}