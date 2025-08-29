namespace SRDebugger.Services.Implementation
{
    using Internal;
    using SRF.Service;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using UnityEngine;

    [Service(typeof(IOptionsService))]
    public partial class OptionsServiceImpl : IOptionsService
    {
        public event EventHandler OptionsUpdated;

        public ICollection<OptionDefinition> Options
        {
            get { return this._optionsReadonly; }
        }

        private void OptionsContainerOnOptionAdded(IOptionContainer container, OptionDefinition optionDefinition)
        {
            if (!this._optionContainerLookup.TryGetValue(container, out var options))
            {
                Debug.LogWarning("[SRDebugger] Received event from unknown option container.");
                return;
            }

            if (options.Contains(optionDefinition))
            {
                Debug.LogWarning("[SRDebugger] Received option added event from option container, but option has already been added.");
                return;
            }

            options.Add(optionDefinition);
            this._options.Add(optionDefinition);
            this.OnOptionsUpdated();
        }

        private void OptionsContainerOnOptionRemoved(IOptionContainer container, OptionDefinition optionDefinition)
        {
            if (!this._optionContainerLookup.TryGetValue(container, out var options))
            {
                Debug.LogWarning("[SRDebugger] Received event from unknown option container.");
                return;
            }

            if (options.Remove(optionDefinition))
            {
                this._options.Remove(optionDefinition);
                this.OnOptionsUpdated();
            }
            else
            {
                Debug.LogWarning("[SRDebugger] Received option removed event from option container, but option does not exist.");
            }
        }

        private readonly Dictionary<IOptionContainer, List<OptionDefinition>> _optionContainerLookup = new Dictionary<IOptionContainer, List<OptionDefinition>>();

        private readonly Dictionary<IOptionContainer, OptionContainerEventHandler> _optionContainerEventHandlerLookup = new Dictionary<IOptionContainer, OptionContainerEventHandler>();

        private readonly List<OptionDefinition> _options = new List<OptionDefinition>();

        private readonly IList<OptionDefinition> _optionsReadonly;

        public OptionsServiceImpl()
        {
            this._optionsReadonly = new ReadOnlyCollection<OptionDefinition>(this._options);
        }

        public void Scan(object obj)
        {
            this.AddContainer(obj);
        }

        public void AddContainer(object obj)
        {
            var container = obj as IOptionContainer ?? new ReflectionOptionContainer(obj);
            this.AddContainer(container);
        }

        public void AddContainer(IOptionContainer optionContainer)
        {
            if (this._optionContainerLookup.ContainsKey(optionContainer))
            {
                throw new Exception("An options container should only be added once.");
            }

            var options = new List<OptionDefinition>();
            options.AddRange(optionContainer.GetOptions());

            this._optionContainerLookup.Add(optionContainer, options);

            if (optionContainer.IsDynamic)
            {
                var handler = new OptionContainerEventHandler(this, optionContainer);
                this._optionContainerEventHandlerLookup.Add(optionContainer, handler);
            }

            if (options.Count > 0)
            {
                this._options.AddRange(options);
                this.OnOptionsUpdated();
            }
        }

        public void RemoveContainer(object obj)
        {
            var container = obj as IOptionContainer ?? new ReflectionOptionContainer(obj);
            this.RemoveContainer(container);
        }

        public void RemoveContainer(IOptionContainer optionContainer)
        {
            if (!this._optionContainerLookup.ContainsKey(optionContainer))
            {
                return;
            }

            var isDirty = false;
            var list = this._optionContainerLookup[optionContainer];
            this._optionContainerLookup.Remove(optionContainer);
            foreach (var op in list)
            {
                this._options.Remove(op);
                isDirty = true;
            }

            if (this._optionContainerEventHandlerLookup.TryGetValue(optionContainer,
                out var handler))
            {
                handler.Dispose();
                this._optionContainerEventHandlerLookup.Remove(optionContainer);
            }

            if (isDirty)
            {
                this.OnOptionsUpdated();
            }
        }

        private void OnOptionsUpdated()
        {
            if (OptionsUpdated != null)
            {
                OptionsUpdated(this, EventArgs.Empty);
            }
        }

        private class OptionContainerEventHandler : IDisposable
        {
            private readonly OptionsServiceImpl _service;
            private readonly IOptionContainer _container;

            public OptionContainerEventHandler(OptionsServiceImpl service, IOptionContainer container)
            {
                this._container = container;
                this._service = service;

                container.OptionAdded += this.ContainerOnOptionAdded;
                container.OptionRemoved += this.ContainerOnOptionRemoved;
            }

            private void ContainerOnOptionAdded(OptionDefinition obj)
            {
                this._service.OptionsContainerOnOptionAdded(this._container, obj);
            }

            private void ContainerOnOptionRemoved(OptionDefinition obj)
            {
                this._service.OptionsContainerOnOptionRemoved(this._container, obj);
            }

            public void Dispose()
            {
                this._container.OptionAdded -= this.ContainerOnOptionAdded;
                this._container.OptionRemoved -= this.ContainerOnOptionRemoved;
            }
        }
    }
}
