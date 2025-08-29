namespace SRF.UI
{
    using Internal;
    using System;
    using UnityEngine;
    using UnityEngine.Events;
    using UnityEngine.EventSystems;
    using UnityEngine.UI;

    [AddComponentMenu(ComponentMenuPaths.SRSpinner)]
    public class SRSpinner : Selectable, IDragHandler, IBeginDragHandler
    {
        private float _dragDelta;

        [SerializeField] private SpinEvent _onSpinDecrement = new SpinEvent();

        [SerializeField] private SpinEvent _onSpinIncrement = new SpinEvent();

        /// <summary>
        /// Number of units a drag must accumulate to trigger a change
        /// </summary>
        public float DragThreshold = 20f;

        public SpinEvent OnSpinIncrement
        {
            get { return this._onSpinIncrement; }
            set { this._onSpinIncrement = value; }
        }

        public SpinEvent OnSpinDecrement
        {
            get { return this._onSpinDecrement; }
            set { this._onSpinDecrement = value; }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            this._dragDelta = 0;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!this.interactable)
            {
                return;
            }

            this._dragDelta += eventData.delta.x;

            if (Mathf.Abs(this._dragDelta) > this.DragThreshold)
            {
                var direction = Mathf.Sign(this._dragDelta);
                var quantity = Mathf.FloorToInt(Mathf.Abs(this._dragDelta) / this.DragThreshold);

                if (direction > 0)
                {
                    this.OnIncrement(quantity);
                }
                else
                {
                    this.OnDecrement(quantity);
                }

                this._dragDelta -= quantity * this.DragThreshold * direction;
            }
        }

        private void OnIncrement(int amount)
        {
            for (var i = 0; i < amount; i++)
            {
                this.OnSpinIncrement.Invoke();
            }
        }

        private void OnDecrement(int amount)
        {
            for (var i = 0; i < amount; i++)
            {
                this.OnSpinDecrement.Invoke();
            }
        }

        [Serializable]
        public class SpinEvent : UnityEvent { }
    }
}
