namespace SRF.UI
{
    using Internal;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// Detects when a screen dpi exceeds what the developer considers
    /// a "retina" level display, and scales the canvas accordingly.
    /// </summary>
    [RequireComponent(typeof(CanvasScaler))]
    [AddComponentMenu(ComponentMenuPaths.RetinaScaler)]
    public class SRRetinaScaler : SRMonoBehaviour
    {
        [SerializeField] private bool _disablePixelPerfect = false;

        [SerializeField] private int _designDpi = 120;

        private void Start()
        {
            this.ApplyScaling();
        }

        private void ApplyScaling()
        {
            var dpi = Screen.dpi;

            this._lastDpi = dpi;

            if (dpi <= 0)
            {
                return;
            }

#if !UNITY_EDITOR && UNITY_IOS
            // No iOS device has had low dpi for many years - Unity must be reporting it wrong.
            if(dpi < 120)
            {
                dpi = 321;
            }
#endif
            var scaler = this.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;

            // Round scale to nearest 0.5
            var scale = dpi / this._designDpi;
            scale = Mathf.Max(1, Mathf.Round(scale * 2) / 2.0f);

            scaler.scaleFactor = scale;

            if (this._disablePixelPerfect)
            {
                this.GetComponent<Canvas>().pixelPerfect = false;
            }
        }

        private float _lastDpi;

        private void Update()
        {
            if (Screen.dpi != this._lastDpi)
            {
                this.ApplyScaling();
            }
        }

    }
}
