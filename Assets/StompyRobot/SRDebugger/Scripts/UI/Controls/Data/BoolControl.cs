namespace SRDebugger.UI.Controls.Data
{
    using SRF;
    using System;
    using UnityEngine.UI;

    public class BoolControl : DataBoundControl
    {
        [RequiredField] public Text Title;

        [RequiredField] public Toggle Toggle;

        protected override void Start()
        {
            base.Start();
            this.Toggle.onValueChanged.AddListener(this.ToggleOnValueChanged);
        }

        private void ToggleOnValueChanged(bool isOn)
        {
            this.UpdateValue(isOn);
        }

        protected override void OnBind(string propertyName, Type t)
        {
            base.OnBind(propertyName, t);

            this.Title.text = propertyName;

            this.Toggle.interactable = !this.IsReadOnly;
        }

        protected override void OnValueUpdated(object newValue)
        {
            var value = (bool)newValue;
            this.Toggle.isOn = value;
        }

        public override bool CanBind(Type type, bool isReadOnly)
        {
            return type == typeof(bool);
        }
    }
}
