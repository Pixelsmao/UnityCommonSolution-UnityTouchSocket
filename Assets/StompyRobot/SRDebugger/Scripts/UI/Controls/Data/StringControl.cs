namespace SRDebugger.UI.Controls.Data
{
    using SRF;
    using System;
    using UnityEngine.UI;

    public class StringControl : DataBoundControl
    {
        [RequiredField] public InputField InputField;

        [RequiredField] public Text Title;

        protected override void Start()
        {
            base.Start();
#if UNITY_4_6 || UNITY_4_7 || UNITY_5_0 || UNITY_5_1 || UNITY_5_2
            InputField.onValueChange.AddListener(OnValueChanged);
#else
            this.InputField.onValueChanged.AddListener(this.OnValueChanged);
#endif
        }

        private void OnValueChanged(string newValue)
        {
            this.UpdateValue(newValue);
        }

        protected override void OnBind(string propertyName, Type t)
        {
            base.OnBind(propertyName, t);
            this.Title.text = propertyName;
            this.InputField.text = "";
            this.InputField.interactable = !this.IsReadOnly;
        }

        protected override void OnValueUpdated(object newValue)
        {
            var value = newValue == null ? "" : (string)newValue;
            this.InputField.text = value;
        }

        public override bool CanBind(Type type, bool isReadOnly)
        {
            return type == typeof(string) && !isReadOnly;
        }
    }
}
