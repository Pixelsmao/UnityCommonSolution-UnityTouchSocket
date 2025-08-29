// ReSharper disable once RedundantUsingDirective
namespace SRDebugger.UI.Controls.Data
{
    using SRF;
    using SRF.UI;
    using System;
    using UnityEngine;
    using UnityEngine.UI;

    public class EnumControl : DataBoundControl
    {
        private object _lastValue;
        private string[] _names;
        private Array _values;

        [RequiredField] public LayoutElement ContentLayoutElement;

        public GameObject[] DisableOnReadOnly;

        [RequiredField] public SRSpinner Spinner;

        [RequiredField] public Text Title;

        [RequiredField] public Text Value;

        protected override void Start()
        {
            base.Start();
        }

        protected override void OnBind(string propertyName, Type t)
        {
            base.OnBind(propertyName, t);

            this.Title.text = propertyName;

            this.Spinner.interactable = !this.IsReadOnly;

            if (this.DisableOnReadOnly != null)
            {
                foreach (var child in this.DisableOnReadOnly)
                {
                    child.SetActive(!this.IsReadOnly);
                }
            }

            this._names = Enum.GetNames(t);
            this._values = Enum.GetValues(t);

            var longestName = "";

            for (var i = 0; i < this._names.Length; i++)
            {
                if (this._names[i].Length > longestName.Length)
                {
                    longestName = this._names[i];
                }
            }

            if (this._names.Length == 0)
            {
                return;
            }

            // Set preferred width of content to the largest possible value size

            var width = this.Value.cachedTextGeneratorForLayout.GetPreferredWidth(longestName,
                this.Value.GetGenerationSettings(new Vector2(float.MaxValue, this.Value.preferredHeight)));

            this.ContentLayoutElement.preferredWidth = width;
        }

        protected override void OnValueUpdated(object newValue)
        {
            this._lastValue = newValue;
            this.Value.text = newValue.ToString();
            LayoutRebuilder.MarkLayoutForRebuild(this.GetComponent<RectTransform>());
        }

        public override bool CanBind(Type type, bool isReadOnly)
        {
#if NETFX_CORE
			return type.GetTypeInfo().IsEnum;
#else
            return type.IsEnum;
#endif
        }

        private void SetIndex(int i)
        {
            this.UpdateValue(this._values.GetValue(i));
            this.Refresh();
        }

        public void GoToNext()
        {
            var currentIndex = Array.IndexOf(this._values, this._lastValue);
            this.SetIndex(SRMath.Wrap(this._values.Length, currentIndex + 1));
        }

        public void GoToPrevious()
        {
            var currentIndex = Array.IndexOf(this._values, this._lastValue);
            this.SetIndex(SRMath.Wrap(this._values.Length, currentIndex - 1));
        }
    }
}
