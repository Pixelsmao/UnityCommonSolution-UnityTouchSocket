using System.Globalization;

namespace SRDebugger.UI.Controls.Data
{
    using SRF;
    using SRF.UI;
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UI;

    public class NumberControl : DataBoundControl
    {
        private static readonly Type[] IntegerTypes =
        {
            typeof (int), typeof (short), typeof (byte), typeof (sbyte), typeof (uint), typeof (ushort)
        };

        private static readonly Type[] DecimalTypes =
        {
            typeof (float), typeof (double)
        };

        public static readonly Dictionary<Type, ValueRange> ValueRanges = new Dictionary<Type, ValueRange>
        {
            {typeof (int), new ValueRange {MaxValue = int.MaxValue, MinValue = int.MinValue}},
            {typeof (short), new ValueRange {MaxValue = short.MaxValue, MinValue = short.MinValue}},
            {typeof (byte), new ValueRange {MaxValue = byte.MaxValue, MinValue = byte.MinValue}},
            {typeof (sbyte), new ValueRange {MaxValue = sbyte.MaxValue, MinValue = sbyte.MinValue}},
            {typeof (uint), new ValueRange {MaxValue = uint.MaxValue, MinValue = uint.MinValue}},
            {typeof (ushort), new ValueRange {MaxValue = ushort.MaxValue, MinValue = ushort.MinValue}},
            {typeof (float), new ValueRange {MaxValue = float.MaxValue, MinValue = float.MinValue}},
            {typeof (double), new ValueRange {MaxValue = double.MaxValue, MinValue = double.MinValue}}
        };

        private string _lastValue;
        private Type _type;
        public GameObject[] DisableOnReadOnly;
        public SRNumberButton DownNumberButton;

        [RequiredField] public SRNumberSpinner NumberSpinner;

        [RequiredField] public Text Title;

        public SRNumberButton UpNumberButton;

        protected override void Start()
        {
            base.Start();
            this.NumberSpinner.onEndEdit.AddListener(this.OnValueChanged);
        }

        private void OnValueChanged(string newValue)
        {
            try
            {
                var num = Convert.ChangeType(newValue, this._type, CultureInfo.InvariantCulture);
                this.UpdateValue(num);
            }
            catch (Exception)
            {
                this.NumberSpinner.text = this._lastValue;
            }

            LayoutRebuilder.MarkLayoutForRebuild(this.GetComponent<RectTransform>());
        }

        protected override void OnBind(string propertyName, Type t)
        {
            base.OnBind(propertyName, t);
            this.Title.text = propertyName;

            if (IsIntegerType(t))
            {
                this.NumberSpinner.contentType = InputField.ContentType.IntegerNumber;
            }
            else if (IsDecimalType(t))
            {
                this.NumberSpinner.contentType = InputField.ContentType.DecimalNumber;
            }
            else
            {
                throw new ArgumentException("Type must be one of expected types", "t");
            }

            var rangeAttrib = this.Property.GetAttribute<NumberRangeAttribute>();

            this.NumberSpinner.MaxValue = this.GetMaxValue(t);
            this.NumberSpinner.MinValue = this.GetMinValue(t);

            if (rangeAttrib != null)
            {
                this.NumberSpinner.MaxValue = Math.Min(rangeAttrib.Max, this.NumberSpinner.MaxValue);
                this.NumberSpinner.MinValue = Math.Max(rangeAttrib.Min, this.NumberSpinner.MinValue);
            }

            var incrementAttribute = this.Property.GetAttribute<IncrementAttribute>();

            if (incrementAttribute != null)
            {
                if (this.UpNumberButton != null)
                {
                    this.UpNumberButton.Amount = incrementAttribute.Increment;
                }

                if (this.DownNumberButton != null)
                {
                    this.DownNumberButton.Amount = -incrementAttribute.Increment;
                }
            }

            this._type = t;

            this.NumberSpinner.interactable = !this.IsReadOnly;

            if (this.DisableOnReadOnly != null)
            {
                foreach (var childControl in this.DisableOnReadOnly)
                {
                    childControl.SetActive(!this.IsReadOnly);
                }
            }
        }

        protected override void OnValueUpdated(object newValue)
        {
            var value = Convert.ToDecimal(newValue, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);

            if (value != this._lastValue)
            {
                this.NumberSpinner.text = value;
            }

            this._lastValue = value;
        }

        public override bool CanBind(Type type, bool isReadOnly)
        {
            return IsDecimalType(type) || IsIntegerType(type);
        }

        protected static bool IsIntegerType(Type t)
        {
            for (var i = 0; i < IntegerTypes.Length; i++)
            {
                if (IntegerTypes[i] == t)
                {
                    return true;
                }
            }

            return false;
        }

        protected static bool IsDecimalType(Type t)
        {
            for (var i = 0; i < DecimalTypes.Length; i++)
            {
                if (DecimalTypes[i] == t)
                {
                    return true;
                }
            }

            return false;
        }

        protected double GetMaxValue(Type t)
        {
            if (ValueRanges.TryGetValue(t, out var value))
            {
                return value.MaxValue;
            }

            Debug.LogWarning("[NumberControl] No MaxValue stored for type {0}".Fmt(t));

            return double.MaxValue;
        }

        protected double GetMinValue(Type t)
        {
            if (ValueRanges.TryGetValue(t, out var value))
            {
                return value.MinValue;
            }

            Debug.LogWarning("[NumberControl] No MinValue stored for type {0}".Fmt(t));

            return double.MinValue;
        }

        public struct ValueRange
        {
            public double MaxValue;
            public double MinValue;
        }
    }
}
