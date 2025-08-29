namespace SRF.UI
{
    using Internal;
    using UnityEngine;
    using UnityEngine.EventSystems;
    using UnityEngine.UI;

    /// <summary>
    /// Instantly sets colour to FlashColor on pointer down, then fades back to DefaultColour once pointer is released.
    /// </summary>
    [AddComponentMenu(ComponentMenuPaths.FlashGraphic)]
    [ExecuteInEditMode]
    public class FlashGraphic : UIBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public float DecayTime = 0.15f;
        public Color DefaultColor = new Color(1, 1, 1, 0);
        public Color FlashColor = Color.white;
        public Graphic Target;

        private bool _isHoldingUntilNextPress;

        public void OnPointerDown(PointerEventData eventData)
        {
            this.Target.CrossFadeColor(this.FlashColor, 0f, true, true);
            this._isHoldingUntilNextPress = false;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!this._isHoldingUntilNextPress)
            {
                this.Target.CrossFadeColor(this.DefaultColor, this.DecayTime, true, true);
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            if (!this._isHoldingUntilNextPress)
            {
                this.Target.CrossFadeColor(this.DefaultColor, 0f, true, true);
            }
        }

#if UNITY_EDITOR
        protected void Update()
        {

            if (!Application.isPlaying && this.Target != null)
            {
                this.Target.CrossFadeColor(this.DefaultColor, 0, true, true);
            }

        }
#endif

        public void Flash()
        {
            this.Target.CrossFadeColor(this.FlashColor, 0f, true, true);
            this.Target.CrossFadeColor(this.DefaultColor, this.DecayTime, true, true);
            this._isHoldingUntilNextPress = false;
        }

        public void FlashAndHoldUntilNextPress()
        {
            this.Target.CrossFadeColor(this.FlashColor, 0f, true, true);
            this._isHoldingUntilNextPress = true;
        }
    }
}
