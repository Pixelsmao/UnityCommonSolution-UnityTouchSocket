namespace SRF.UI
{
    using System;
    using UnityEngine;
    using UnityEngine.EventSystems;
    using UnityEngine.UI;

    public class DragHandle : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler
    {
        private CanvasScaler _canvasScaler;
        private float _delta;
        private float _startValue;
        public RectTransform.Axis Axis = RectTransform.Axis.Horizontal;
        public bool Invert = false;
        public float MaxSize = -1;
        public LayoutElement TargetLayoutElement;
        public RectTransform TargetRectTransform;

        private float Mult
        {
            get { return this.Invert ? -1 : 1; }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!this.Verify())
            {
                return;
            }

            //Debug.Log("OnBeginDrag");

            this._startValue = this.GetCurrentValue();
            this._delta = 0;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!this.Verify())
            {
                return;
            }

            //Debug.Log("OnDrag");

            var delta = 0f;

            if (this.Axis == RectTransform.Axis.Horizontal)
            {
                delta += eventData.delta.x;
            }
            else
            {
                delta += eventData.delta.y;
            }

            if (this._canvasScaler != null)
            {
                delta /= this._canvasScaler.scaleFactor;
            }

            delta *= this.Mult;
            this._delta += delta;

            this.SetCurrentValue(Mathf.Clamp(this._startValue + this._delta, this.GetMinSize(), this.GetMaxSize()));
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!this.Verify())
            {
                return;
            }

            //Debug.Log("OnEndDrag");

            this.SetCurrentValue(Mathf.Max(this._startValue + this._delta, this.GetMinSize()));
            this._delta = 0;
            this.CommitCurrentValue();
        }

        private void Start()
        {
            this.Verify();
            this._canvasScaler = this.GetComponentInParent<CanvasScaler>();
        }

        private bool Verify()
        {
            if (this.TargetLayoutElement == null && this.TargetRectTransform == null)
            {
                Debug.LogWarning(
                    "DragHandle: TargetLayoutElement and TargetRectTransform are both null. Disabling behaviour.");
                this.enabled = false;
                return false;
            }

            return true;
        }

        private float GetCurrentValue()
        {
            if (this.TargetLayoutElement != null)
            {
                return this.Axis == RectTransform.Axis.Horizontal
                    ? this.TargetLayoutElement.preferredWidth
                    : this.TargetLayoutElement.preferredHeight;
            }

            if (this.TargetRectTransform != null)
            {
                return this.Axis == RectTransform.Axis.Horizontal
                    ? this.TargetRectTransform.sizeDelta.x
                    : this.TargetRectTransform.sizeDelta.y;
            }

            throw new InvalidOperationException();
        }

        private void SetCurrentValue(float value)
        {
            if (this.TargetLayoutElement != null)
            {
                if (this.Axis == RectTransform.Axis.Horizontal)
                {
                    this.TargetLayoutElement.preferredWidth = value;
                }
                else
                {
                    this.TargetLayoutElement.preferredHeight = value;
                }

                return;
            }

            if (this.TargetRectTransform != null)
            {
                var d = this.TargetRectTransform.sizeDelta;

                if (this.Axis == RectTransform.Axis.Horizontal)
                {
                    d.x = value;
                }
                else
                {
                    d.y = value;
                }

                this.TargetRectTransform.sizeDelta = d;

                return;
            }

            throw new InvalidOperationException();
        }

        private void CommitCurrentValue()
        {
            if (this.TargetLayoutElement != null)
            {
                if (this.Axis == RectTransform.Axis.Horizontal)
                {
                    this.TargetLayoutElement.preferredWidth = ((RectTransform)this.TargetLayoutElement.transform).sizeDelta.x;
                }
                else
                {
                    this.TargetLayoutElement.preferredHeight = ((RectTransform)this.TargetLayoutElement.transform).sizeDelta.y;
                }
            }
        }

        private float GetMinSize()
        {
            if (this.TargetLayoutElement == null)
            {
                return 0;
            }
            return this.Axis == RectTransform.Axis.Horizontal ? this.TargetLayoutElement.minWidth : this.TargetLayoutElement.minHeight;
        }

        private float GetMaxSize()
        {
            if (this.MaxSize > 0)
            {
                return this.MaxSize;
            }
            return float.MaxValue;
        }
    }
}
