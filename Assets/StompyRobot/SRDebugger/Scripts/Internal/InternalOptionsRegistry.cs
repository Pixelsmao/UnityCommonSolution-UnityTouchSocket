using SRF.Service;
using System;
using System.Collections.Generic;

namespace SRDebugger.Internal
{
    /// <summary>
    /// Workaround for the debug panel not being initialized on startup.
    /// SROptions needs to register itself but not cause auto-initialization.
    /// This class buffers requests to register contains until there is a handler in place to deal with them.
    /// Once the handler is in place, all buffered requests are passed in and future requests invoke the handler directly.
    /// </summary>
    [Service(typeof(InternalOptionsRegistry))]
    public sealed class InternalOptionsRegistry
    {
        private List<object> _registeredContainers = new List<object>();
        private Action<object> _handler;

        public void AddOptionContainer(object obj)
        {
            if (this._handler != null)
            {
                this._handler(obj);
                return;
            }

            this._registeredContainers.Add(obj);
        }

        public void SetHandler(Action<object> action)
        {
            this._handler = action;

            foreach (var o in this._registeredContainers)
            {
                this._handler(o);
            }

            this._registeredContainers = null;
        }
    }
}