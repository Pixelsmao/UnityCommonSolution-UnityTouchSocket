using SRDebugger.Internal;
using System;
using System.Collections.Generic;

namespace SRDebugger.Services.Implementation
{
    public sealed partial class OptionsServiceImpl
    {
        /// <summary>
        /// Options container that is implemented via reflection.
        /// This is the normal behaviour used when options container is added as an `object`.
        /// </summary>
        private class ReflectionOptionContainer : IOptionContainer
        {
            // Options don't change, so just leave stubs that do nothing.
            public event Action<OptionDefinition> OptionAdded
            {
                add { }
                remove { }
            }

            public event Action<OptionDefinition> OptionRemoved
            {
                add { }
                remove { }
            }

            public bool IsDynamic
            {
                get { return false; }
            }

            private List<OptionDefinition> Options
            {
                get
                {
                    if (this._options == null) this._options = SRDebuggerUtil.ScanForOptions(this._target);
                    return this._options;
                }
            }

            private List<OptionDefinition> _options;

            public IEnumerable<OptionDefinition> GetOptions()
            {
                return this.Options;
            }

            private readonly object _target;

            public ReflectionOptionContainer(object target)
            {
                this._target = target;
            }

            protected bool Equals(ReflectionOptionContainer other)
            {
                return Equals(other._target, this._target);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return this.Equals((ReflectionOptionContainer)obj);
            }

            public override int GetHashCode()
            {
                return this._target.GetHashCode();
            }
        }
    }
}