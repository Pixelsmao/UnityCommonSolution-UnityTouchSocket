namespace SRDebugger.UI.Other
{
    using SRF;
    using SRF.UI;
    using UnityEngine;

    [RequireComponent(typeof(StyleComponent))]
    public class DebugPanelBackgroundBehaviour : SRMonoBehaviour
    {
        private StyleComponent _styleComponent;
        public string TransparentStyleKey = "";

        [SerializeField]
        private StyleSheet _styleSheet;

        private void Awake()
        {
            this._styleComponent = this.GetComponent<StyleComponent>();

            if (Settings.Instance.EnableBackgroundTransparency)
            {
                // Update transparent style to have the transparency set in the settings menu.
                var style = this._styleSheet.GetStyle(this.TransparentStyleKey);
                var c = style.NormalColor;
                c.a = Settings.Instance.BackgroundTransparency;
                style.NormalColor = c;

                this._styleComponent.StyleKey = this.TransparentStyleKey;
            }
        }
    }
}
