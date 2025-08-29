namespace SRDebugger.UI.Controls
{
    using UnityEngine;
    using UnityEngine.EventSystems;

    public class MultiTapButton : UnityEngine.UI.Button
    {
        private float _lastTap;
        private int _tapCount;
        public int RequiredTapCount = 3;
        public float ResetTime = 0.5f;

        public override void OnPointerClick(PointerEventData eventData)
        {
            if (Time.unscaledTime - this._lastTap > this.ResetTime)
            {
                this._tapCount = 0;
            }

            this._lastTap = Time.unscaledTime;
            this._tapCount++;

            if (this._tapCount == this.RequiredTapCount)
            {
                base.OnPointerClick(eventData);
                this._tapCount = 0;
            }
        }
    }
}
