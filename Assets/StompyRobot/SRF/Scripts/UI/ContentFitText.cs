namespace SRF.UI
{
    using Internal;
    using UnityEngine;
    using UnityEngine.EventSystems;
    using UnityEngine.UI;

    [RequireComponent(typeof(RectTransform))]
    [ExecuteInEditMode]
    [AddComponentMenu(ComponentMenuPaths.ContentFitText)]
    public class ContentFitText : UIBehaviour, ILayoutElement
    {
        public SRText CopySource;
        public Vector2 Padding;

        public float minWidth
        {
            get
            {
                if (this.CopySource == null)
                {
                    return -1f;
                }
                return LayoutUtility.GetMinWidth(this.CopySource.rectTransform) + this.Padding.x;
            }
        }

        public float preferredWidth
        {
            get
            {
                if (this.CopySource == null)
                {
                    return -1f;
                }
                return LayoutUtility.GetPreferredWidth(this.CopySource.rectTransform) + this.Padding.x;
            }
        }

        public float flexibleWidth
        {
            get
            {
                if (this.CopySource == null)
                {
                    return -1f;
                }
                return LayoutUtility.GetFlexibleWidth(this.CopySource.rectTransform);
            }
        }

        public float minHeight
        {
            get
            {
                if (this.CopySource == null)
                {
                    return -1f;
                }
                return LayoutUtility.GetFlexibleHeight(this.CopySource.rectTransform) + this.Padding.y;
            }
        }

        public float preferredHeight
        {
            get
            {
                if (this.CopySource == null)
                {
                    return -1f;
                }
                return LayoutUtility.GetPreferredHeight(this.CopySource.rectTransform) + this.Padding.y;
            }
        }

        public float flexibleHeight
        {
            get
            {
                if (this.CopySource == null)
                {
                    return -1f;
                }
                return LayoutUtility.GetFlexibleHeight(this.CopySource.rectTransform);
            }
        }

        public int layoutPriority
        {
            get { return 0; }
        }

        public void CalculateLayoutInputHorizontal()
        {
            this.CopySource.CalculateLayoutInputHorizontal();
        }

        public void CalculateLayoutInputVertical()
        {
            this.CopySource.CalculateLayoutInputVertical();
        }

        protected override void OnEnable()
        {
            this.SetDirty();
            this.CopySource.LayoutDirty += this.CopySourceOnLayoutDirty;
        }

        private void CopySourceOnLayoutDirty(SRText srText)
        {
            this.SetDirty();
        }

        protected override void OnTransformParentChanged()
        {
            this.SetDirty();
        }

        protected override void OnDisable()
        {
            this.CopySource.LayoutDirty -= this.CopySourceOnLayoutDirty;
            this.SetDirty();
        }

        protected override void OnDidApplyAnimationProperties()
        {
            this.SetDirty();
        }

        protected override void OnBeforeTransformParentChanged()
        {
            this.SetDirty();
        }

        protected void SetDirty()
        {
            if (!this.IsActive())
            {
                return;
            }

            LayoutRebuilder.MarkLayoutForRebuild(this.transform as RectTransform);
        }
    }
}
