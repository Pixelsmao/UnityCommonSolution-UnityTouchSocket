namespace SRF.UI
{
    using Internal;
    using UnityEngine;

    [AddComponentMenu(ComponentMenuPaths.LongPressButton)]
    public class LongPressButton : UnityEngine.UI.Button
    {
        private bool _handled;
        [SerializeField] private ButtonClickedEvent _onLongPress = new ButtonClickedEvent();
        private bool _pressed;
        private float _pressedTime;
        public float LongPressDuration = 0.9f;

        public ButtonClickedEvent onLongPress
        {
            get { return this._onLongPress; }
            set { this._onLongPress = value; }
        }

        public override void OnPointerExit(UnityEngine.EventSystems.PointerEventData eventData)
        {
            base.OnPointerExit(eventData);
            this._pressed = false;
        }

        public override void OnPointerDown(UnityEngine.EventSystems.PointerEventData eventData)
        {
            base.OnPointerDown(eventData);

            if (eventData.button != UnityEngine.EventSystems.PointerEventData.InputButton.Left)
            {
                return;
            }

            this._pressed = true;
            this._handled = false;
            this._pressedTime = Time.realtimeSinceStartup;
        }

        public override void OnPointerUp(UnityEngine.EventSystems.PointerEventData eventData)
        {
            if (!this._handled)
            {
                base.OnPointerUp(eventData);
            }

            this._pressed = false;
        }

        public override void OnPointerClick(UnityEngine.EventSystems.PointerEventData eventData)
        {
            if (!this._handled)
            {
                base.OnPointerClick(eventData);
            }
        }

        private void Update()
        {
            if (!this._pressed)
            {
                return;
            }

            if (Time.realtimeSinceStartup - this._pressedTime >= this.LongPressDuration)
            {
                this._pressed = false;
                this._handled = true;
                this.onLongPress.Invoke();
            }
        }
    }
}
