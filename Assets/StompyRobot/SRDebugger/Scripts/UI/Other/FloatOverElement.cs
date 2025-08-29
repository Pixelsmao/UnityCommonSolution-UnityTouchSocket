using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SRDebugger.UI.Other
{
    [RequireComponent(typeof(RectTransform)), ExecuteAlways]
    public class FloatOverElement : UIBehaviour, ILayoutSelfController
    {
        public RectTransform CopyFrom;

        private DrivenRectTransformTracker _tracker;

        private void Copy()
        {
            if (this.CopyFrom == null) return;

            this._tracker.Clear();

            var r = this.GetComponent<RectTransform>();
            r.anchorMin = this.CopyFrom.anchorMin;
            r.anchorMax = this.CopyFrom.anchorMax;
            r.anchoredPosition = this.CopyFrom.anchoredPosition;
            r.offsetMin = this.CopyFrom.offsetMin;
            r.offsetMax = this.CopyFrom.offsetMax;
            r.sizeDelta = this.CopyFrom.sizeDelta;
            r.localScale = this.CopyFrom.localScale;
            r.pivot = this.CopyFrom.pivot;

            this._tracker.Add(this, r, DrivenTransformProperties.All);
        }

        public void SetLayoutHorizontal()
        {
            this.Copy();
        }

        public void SetLayoutVertical()
        {
            this.Copy();
        }

    }
}