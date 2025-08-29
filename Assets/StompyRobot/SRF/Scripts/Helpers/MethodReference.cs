using System;

namespace SRF.Helpers
{
    using System.Reflection;

    public sealed class MethodReference
    {
        private readonly Func<object[], object> _method;

        public MethodReference(object target, MethodInfo method)
        {
            SRDebugUtil.AssertNotNull(target);

            this._method = o => method.Invoke(target, o);
        }

        public MethodReference(Func<object[], object> method)
        {
            this._method = method;
        }

        public object Invoke(object[] parameters)
        {
            return this._method.Invoke(parameters);
        }

        public static implicit operator MethodReference(Action action)
        {
            return new MethodReference(args =>
            {
                action();
                return null;
            });
        }
    }
}
