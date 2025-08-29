namespace SRDebugger.UI.Controls
{
    using SRF.Helpers;
    using System;
    using UnityEngine;

    public abstract class DataBoundControl : OptionsControlBase
    {
        private bool _hasStarted;
        private bool _isReadOnly;
        private object _prevValue;
        private SRF.Helpers.PropertyReference _prop;

        public SRF.Helpers.PropertyReference Property
        {
            get { return this._prop; }
        }

        public bool IsReadOnly
        {
            get { return this._isReadOnly; }
        }

        public string PropertyName { get; private set; }

        #region Data Binding

        public void Bind(string propertyName, SRF.Helpers.PropertyReference prop)
        {
            this.PropertyName = propertyName;
            this._prop = prop;

            this._isReadOnly = !prop.CanWrite;

            prop.ValueChanged += this.OnValueChanged;

            this.OnBind(propertyName, prop.PropertyType);
            this.Refresh();
        }

        private void OnValueChanged(PropertyReference property)
        {
            this.Refresh();
        }

        protected void UpdateValue(object newValue)
        {
            if (newValue == this._prevValue)
            {
                return;
            }

            if (this.IsReadOnly)
            {
                return;
            }

            this._prop.SetValue(newValue);
            this._prevValue = newValue;
        }

        public override void Refresh()
        {
            if (this._prop == null)
            {
                return;
            }

            var currentValue = this._prop.GetValue();

            if (currentValue != this._prevValue)
            {
                try
                {
                    this.OnValueUpdated(currentValue);
                }
                catch (Exception e)
                {
                    Debug.LogError("[SROptions] Error refreshing binding.");
                    Debug.LogException(e);
                }
            }

            this._prevValue = currentValue;
        }

        protected virtual void OnBind(string propertyName, Type t) { }
        protected abstract void OnValueUpdated(object newValue);

        public abstract bool CanBind(Type type, bool isReadOnly);

        #endregion

        #region Unity

        protected override void Start()
        {
            base.Start();

            this.Refresh();
            this._hasStarted = true;
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            if (this._hasStarted)
            {
                if (this._prop != null)
                {
                    this._prop.ValueChanged += this.OnValueChanged;
                }

                this.Refresh();
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            if (this._prop != null)
            {
                this._prop.ValueChanged -= this.OnValueChanged;
            }
        }

        #endregion
    }
}
