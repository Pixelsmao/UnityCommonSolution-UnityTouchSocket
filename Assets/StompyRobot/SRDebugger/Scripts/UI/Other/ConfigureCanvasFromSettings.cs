using System.ComponentModel;
using UnityEngine.UI;

namespace SRDebugger.UI.Other
{
    using Internal;
    using SRF;
    using UnityEngine;

    [RequireComponent(typeof(Canvas))]
    public class ConfigureCanvasFromSettings : SRMonoBehaviour
    {
        private Canvas _canvas;
        private CanvasScaler _canvasScaler;

        private float _originalScale;
        private float _lastSetScale;
        private Settings _settings;

        private void Start()
        {
            this._canvas = this.GetComponent<Canvas>();
            this._canvasScaler = this.GetComponent<CanvasScaler>();

            SRDebuggerUtil.ConfigureCanvas(this._canvas);

            this._settings = SRDebug.Instance.Settings;
            this._originalScale = this._canvasScaler.scaleFactor;
            this._canvasScaler.scaleFactor = this._originalScale * this._settings.UIScale;

            // Track the last set scale in case it is modified by the retina scaler.
            this._lastSetScale = this._canvasScaler.scaleFactor;

            this._settings.PropertyChanged += this.SettingsOnPropertyChanged;
        }

        private void OnDestroy()
        {
            if (this._settings != null)
            {
                this._settings.PropertyChanged -= this.SettingsOnPropertyChanged;
            }
        }

        private void SettingsOnPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            // If the last set scale does not match the current scale factor, then it is likely the retina scaler has applied a change.
            // Treat the new value as the original scale.
            if (this._canvasScaler.scaleFactor != this._lastSetScale) this._originalScale = this._canvasScaler.scaleFactor;

            this._canvasScaler.scaleFactor = this._originalScale * this._settings.UIScale;
            this._lastSetScale = this._canvasScaler.scaleFactor;
        }
    }
}
