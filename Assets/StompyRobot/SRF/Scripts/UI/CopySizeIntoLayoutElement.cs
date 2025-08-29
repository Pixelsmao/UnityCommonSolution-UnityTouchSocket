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
    [AddComponentMenu(ComponentMenuPaths.CopySizeIntoLayoutElement)]
    public class CopySizeIntoLayoutElement : LayoutElement
    {
        public RectTransform CopySource;
        public float PaddingHeight;
        public float PaddingWidth;

        public bool SetPreferredSize = false;
        public bool SetMinimumSize = false;

        public override float preferredWidth
        {
            get
            {
                if (!this.SetPreferredSize || this.CopySource == null || !this.IsActive())
                {
                    return -1f;
                }
                return this.CopySource.rect.width + this.PaddingWidth;
            }
        }

        public override float preferredHeight
        {
            get
            {
                if (!this.SetPreferredSize || this.CopySource == null || !this.IsActive())
                {
                    return -1f;
                }
                return this.CopySource.rect.height + this.PaddingHeight;
            }
        }
        public override float minWidth
        {
            get
            {
                if (!this.SetMinimumSize || this.CopySource == null || !this.IsActive())
                {
                    return -1f;
                }
                return this.CopySource.rect.width + this.PaddingWidth;
            }
        }

        public override float minHeight
        {
            get
            {
                if (!this.SetMinimumSize || this.CopySource == null || !this.IsActive())
                {
                    return -1f;
                }
                return this.CopySource.rect.height + this.PaddingHeight;
            }
        }

        public override int layoutPriority
        {
            get { return 2; }
        }
    }
}
