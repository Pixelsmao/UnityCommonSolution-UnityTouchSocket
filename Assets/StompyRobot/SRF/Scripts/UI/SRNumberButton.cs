namespace SRF.UI
{
    using Internal;
    using UnityEngine;
    using UnityEngine.EventSystems;

    [AddComponentMenu(ComponentMenuPaths.NumberButton)]
    public class SRNumberButton : UnityEngine.UI.Button, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler
    {
        private const float ExtraThreshold = 3f;
        public const float Delay = 0.4f;
        private float _delayTime;
        private float _downTime;
        private bool _isDown;
        public double Amount = 1;
        public SRNumberSpinner TargetField;

        public override void OnPointerDown(PointerEventData eventData)
        {
            base.OnPointerDown(eventData);

            if (!this.interactable)
            {
                return;
            }

            this.Apply();

            this._isDown = true;
            this._downTime = Time.realtimeSinceStartup;
            this._delayTime = this._downTime + Delay;
        }

        public override void OnPointerUp(PointerEventData eventData)
        {
            base.OnPointerUp(eventData);

            this._isDown = false;
        }

        protected virtual void Update()
        {
            if (this._isDown)
            {
                if (this._delayTime <= Time.realtimeSinceStartup)
                {
                    this.Apply();

                    var newDelay = Delay * 0.5f;

                    var extra = Mathf.RoundToInt((Time.realtimeSinceStartup - this._downTime) / ExtraThreshold);

                    for (var i = 0; i < extra; i++)
                    {
                        newDelay *= 0.5f;
                    }

                    this._delayTime = Time.realtimeSinceStartup + newDelay;
                }
            }
        }

        private void Apply()
        {
            var currentValue = double.Parse(this.TargetField.text);
            currentValue += this.Amount;

            if (currentValue > this.TargetField.MaxValue)
            {
                currentValue = this.TargetField.MaxValue;
            }
            if (currentValue < this.TargetField.MinValue)
            {
                currentValue = this.TargetField.MinValue;
            }

            this.TargetField.text = currentValue.ToString();
            this.TargetField.onEndEdit.Invoke(this.TargetField.text);
        }
    }
}
