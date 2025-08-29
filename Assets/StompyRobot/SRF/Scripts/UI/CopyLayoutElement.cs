namespace SRF.UI
{
    using Internal;
    using UnityEngine;
    using UnityEngine.EventSystems;
    using UnityEngine.UI;

    /// <summary>
    /// Copies the preferred size of another layout element (useful for a parent object basing its sizing from a child
    /// element).
    /// This does have very quirky behaviour, though.
    /// TODO: Write custom editor for this to match layout element editor
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [ExecuteInEditMode]
    [AddComponentMenu(ComponentMenuPaths.CopyLayoutElement)]
    public class CopyLayoutElement : UIBehaviour, ILayoutElement
    {
        public bool CopyMinHeight;
        public bool CopyMinWidth;
        public bool CopyPreferredHeight;
        public bool CopyPreferredWidth;
        public RectTransform CopySource;
        public float PaddingMinHeight;
        public float PaddingMinWidth;
        public float PaddingPreferredHeight;
        public float PaddingPreferredWidth;

        public float preferredWidth
        {
            get
            {
                if (!this.CopyPreferredWidth || this.CopySource == null || !this.IsActive())
                {
                    return -1f;
                }
                return LayoutUtility.GetPreferredWidth(this.CopySource) + this.PaddingPreferredWidth;
            }
        }

        public float preferredHeight
        {
            get
            {
                if (!this.CopyPreferredHeight || this.CopySource == null || !this.IsActive())
                {
                    return -1f;
                }
                return LayoutUtility.GetPreferredHeight(this.CopySource) + this.PaddingPreferredHeight;
            }
        }

        public float minWidth
        {
            get
            {
                if (!this.CopyMinWidth || this.CopySource == null || !this.IsActive())
                {
                    return -1f;
                }
                return LayoutUtility.GetMinWidth(this.CopySource) + this.PaddingMinWidth;
            }
        }

        public float minHeight
        {
            get
            {
                if (!this.CopyMinHeight || this.CopySource == null || !this.IsActive())
                {
                    return -1f;
                }
                return LayoutUtility.GetMinHeight(this.CopySource) + this.PaddingMinHeight;
            }
        }

        public int layoutPriority
        {
            get { return 2; }
        }

        public float flexibleHeight
        {
            get { return -1; }
        }

        public float flexibleWidth
        {
            get { return -1; }
        }

        public void CalculateLayoutInputHorizontal() { }
        public void CalculateLayoutInputVertical() { }
    }
}
