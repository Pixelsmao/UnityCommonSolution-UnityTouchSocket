namespace SRF.UI
{
    using Internal;
    using System;
    using UnityEngine;
    using UnityEngine.EventSystems;
    using UnityEngine.UI;

    [AddComponentMenu(ComponentMenuPaths.NumberSpinner)]
    public class SRNumberSpinner : InputField
    {
        private double _currentValue;
        private double _dragStartAmount;
        private double _dragStep;
        public float DragSensitivity = 0.01f;
        public double MaxValue = double.MaxValue;
        public double MinValue = double.MinValue;

        protected override void Awake()
        {
            base.Awake();

            if (this.contentType != ContentType.IntegerNumber && this.contentType != ContentType.DecimalNumber)
            {
                Debug.LogError("[SRNumberSpinner] contentType must be integer or decimal. Defaulting to integer");
                this.contentType = ContentType.DecimalNumber;
            }
        }

        public override void OnPointerClick(PointerEventData eventData)
        {
            //Debug.Log("OnPointerClick (isFocused: {0}, isUsed: {1}, isDragging: {2})".Fmt(isFocused, eventData.used, eventData.dragging));

            if (!this.interactable)
            {
                return;
            }

            if (eventData.dragging)
            {
                return;
            }

            EventSystem.current.SetSelectedGameObject(this.gameObject, eventData);

            base.OnPointerClick(eventData);

            if ((this.m_Keyboard == null || !this.m_Keyboard.active))
            {
                this.OnSelect(eventData);
            }
            else
            {
                this.UpdateLabel();
                eventData.Use();
            }
        }

        public override void OnPointerDown(PointerEventData eventData)
        {
            //Debug.Log("OnPointerDown (isFocused: {0}, isUsed: {1})".Fmt(isFocused, eventData.used));

            //base.OnPointerDown(eventData);
        }

        public override void OnPointerUp(PointerEventData eventData)
        {
            //Debug.Log("OnPointerUp (isFocused: {0}, isUsed: {1})".Fmt(isFocused, eventData.used));

            //base.OnPointerUp(eventData);
        }

        public override void OnBeginDrag(PointerEventData eventData)
        {
            if (!this.interactable)
            {
                return;
            }

            //Debug.Log("OnBeginDrag (isFocused: {0}, isUsed: {1}, delta: {2})".Fmt(isFocused, eventData.used, eventData.delta));

            // Pass event to parent if it is a vertical drag
            if (Mathf.Abs(eventData.delta.y) > Mathf.Abs(eventData.delta.x))
            {
                //Debug.Log("Passing To Parent");

                var parent = this.transform.parent;

                if (parent != null)
                {
                    eventData.pointerDrag = ExecuteEvents.GetEventHandler<IBeginDragHandler>(parent.gameObject);

                    if (eventData.pointerDrag != null)
                    {
                        ExecuteEvents.Execute(eventData.pointerDrag, eventData, ExecuteEvents.beginDragHandler);
                    }
                }

                return;
            }
            eventData.Use();

            this._dragStartAmount = double.Parse(this.text);
            this._currentValue = this._dragStartAmount;

            var minStep = 1f;

            // Use a larger minimum step for integer numbers, since there are no fractional values
            if (this.contentType == ContentType.IntegerNumber)
            {
                minStep *= 10;
            }

            this._dragStep = Math.Max(minStep, this._dragStartAmount * 0.05f);

            if (this.isFocused)
            {
                this.DeactivateInputField();
            }
        }

        public override void OnDrag(PointerEventData eventData)
        {
            if (!this.interactable)
            {
                return;
            }

            //Debug.Log("OnDrag (isFocused: {0}, isUsed: {1})".Fmt(isFocused, eventData.used));

            var diff = eventData.delta.x;

            this._currentValue += Math.Abs(this._dragStep) * diff * this.DragSensitivity;
            this._currentValue = Math.Round(this._currentValue, 2);

            if (this._currentValue > this.MaxValue)
            {
                this._currentValue = this.MaxValue;
            }

            if (this._currentValue < this.MinValue)
            {
                this._currentValue = this.MinValue;
            }

            if (this.contentType == ContentType.IntegerNumber)
            {
                this.text = ((int)Math.Round(this._currentValue)).ToString();
            }
            else
            {
                this.text = this._currentValue.ToString();
            }
        }

        public override void OnEndDrag(PointerEventData eventData)
        {
            if (!this.interactable)
            {
                return;
            }

            //Debug.Log("OnEndDrag (isFocused: {0}, isUsed: {1})".Fmt(isFocused, eventData.used));

            //base.OnEndDrag(eventData);

            eventData.Use();

            if (this._dragStartAmount != this._currentValue)
            {
                this.DeactivateInputField();
                this.SendOnSubmit();
            }
        }
    }
}
