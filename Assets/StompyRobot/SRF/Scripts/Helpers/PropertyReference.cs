using JetBrains.Annotations;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

namespace SRF.Helpers
{
    using System;
    using System.Linq;
    using System.Reflection;

    public delegate void PropertyValueChangedHandler(PropertyReference property);

    public sealed class PropertyReference
    {
        public event PropertyValueChangedHandler ValueChanged
        {
            add
            {
                if (this._valueChangedListeners == null)
                {
                    this._valueChangedListeners = new List<PropertyValueChangedHandler>();
                }

                this._valueChangedListeners.Add(value);
                if (this._valueChangedListeners.Count == 1 && this._target is INotifyPropertyChanged)
                {
                    // Subscribe to value changed event on target.
                    ((INotifyPropertyChanged)this._target).PropertyChanged += this.OnTargetPropertyChanged;
                }
            }

            remove
            {
                if (this._valueChangedListeners == null)
                {
                    return;
                }

                if (this._valueChangedListeners.Remove(value) && this._valueChangedListeners.Count == 0 &&
                    this._target is INotifyPropertyChanged)
                {
                    // Unsubscribe from value changed event on target.
                    ((INotifyPropertyChanged)this._target).PropertyChanged -= this.OnTargetPropertyChanged;
                }
            }
        }

        [CanBeNull] private readonly PropertyInfo _property;
        [CanBeNull] private readonly object _target;

        [CanBeNull] private readonly Attribute[] _attributes;

        [CanBeNull] private readonly Func<object> _getter;

        [CanBeNull] private readonly Action<object> _setter;

        [CanBeNull] private List<PropertyValueChangedHandler> _valueChangedListeners;


        public static PropertyReference FromLambda<T>(Func<T> getter, Action<T> setter = null, params Attribute[] attributes)
        {
            Action<object> internalSetter = null;
            if (setter != null)
            {
                internalSetter = o => setter((T)o);
            }
            return new PropertyReference(typeof(T), () => getter(), internalSetter, attributes);
        }

        /// <summary>
        /// Create a property reference from an object target and reflection PropertyInfo.
        /// This represents a property on an object.
        /// </summary>
        public PropertyReference(object target, PropertyInfo property)
        {
            SRDebugUtil.AssertNotNull(target);
            SRDebugUtil.AssertNotNull(property);

            this.PropertyType = property.PropertyType;
            this._property = property;
            this._target = target;

#if NETFX_CORE
            if(_property.GetMethod != null && _property.GetMethod.IsPublic)
#else
            if (property.GetGetMethod() != null)
#endif
            {
                this._getter = () => SRReflection.GetPropertyValue(target, property);
            }


#if NETFX_CORE
            if(_property.SetMethod != null && _property.SetMethod.IsPublic)
#else
            if (property.GetSetMethod() != null)
#endif
            {
                this._setter = (v) => SRReflection.SetPropertyValue(target, property, v);
            }
        }

        /// <summary>
        /// Create a property reference from lambdas. This has no underlying reflection or object associated with it.
        /// </summary>
        public PropertyReference(Type type, Func<object> getter = null, Action<object> setter = null, Attribute[] attributes = null)
        {
            SRDebugUtil.AssertNotNull(type);

            this.PropertyType = type;
            this._attributes = attributes;
            this._getter = getter;
            this._setter = setter;
        }

        public Type PropertyType { get; private set; }

        public bool CanRead
        {
            get
            {
                return this._getter != null;
            }
        }

        public bool CanWrite
        {
            get
            {
                return this._setter != null;
            }
        }

        /// <summary>
        /// Notify any listeners to <see cref="ValueChanged"/> that the value has been updated.
        /// </summary>
        public void NotifyValueChanged()
        {
            if (this._valueChangedListeners == null)
            {
                return;
            }

            foreach (var handler in this._valueChangedListeners)
            {
                handler(this);
            }
        }

        public object GetValue()
        {
            if (this._getter != null)
            {
                return this._getter();
            }

            return null;
        }

        public void SetValue(object value)
        {
            if (this._setter != null)
            {
                this._setter(value);
            }
            else
            {
                throw new InvalidOperationException("Can not write to property");
            }
        }

        public T GetAttribute<T>() where T : Attribute
        {
            if (this._attributes != null)
            {
                return this._attributes.FirstOrDefault(p => p is T) as T;
            }

            if (this._property != null)
            {
                return this._property.GetCustomAttributes(typeof(T), true).FirstOrDefault() as T;
            }

            return null;
        }
        private void OnTargetPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (this._valueChangedListeners == null || this._valueChangedListeners.Count == 0)
            {
                Debug.LogWarning("[PropertyReference] Received property value changed event when there are no listeners. Did the event not get unsubscribed correctly?");
                return;
            }

            this.NotifyValueChanged();
        }

        public override string ToString()
        {
            if (this._property != null)
            {
                return "{0}.{1}".Fmt(this._property.DeclaringType.Name, this._property.Name);
            }

            return "<delegate>";
        }
    }
}
