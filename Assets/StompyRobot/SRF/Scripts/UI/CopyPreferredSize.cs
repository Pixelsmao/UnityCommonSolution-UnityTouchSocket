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
    [AddComponentMenu(ComponentMenuPaths.CopyPreferredSize)]
    public class CopyPreferredSize : LayoutElement
    {
        public RectTransform CopySource;
        public float PaddingHeight;
        public float PaddingWidth;

        public override float preferredWidth
        {
            get
            {
                if (this.CopySource == null || !this.IsActive())
                {
                    return -1f;
                }
                return LayoutUtility.GetPreferredWidth(this.CopySource) + this.PaddingWidth;
            }
        }

        public override float preferredHeight
        {
            get
            {
                if (this.CopySource == null || !this.IsActive())
                {
                    return -1f;
                }
                return LayoutUtility.GetPreferredHeight(this.CopySource) + this.PaddingHeight;
            }
        }

        public override int layoutPriority
        {
            get { return 2; }
        }
    }
}
