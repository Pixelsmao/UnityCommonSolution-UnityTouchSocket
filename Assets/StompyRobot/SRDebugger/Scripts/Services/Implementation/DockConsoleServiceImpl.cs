namespace SRDebugger.Services.Implementation
{
    using Internal;
    using SRF.Service;
    using UI.Other;
    using UnityEngine;

    [Service(typeof(IDockConsoleService))]
    public class DockConsoleServiceImpl : IDockConsoleService
    {
        private ConsoleAlignment _alignment;
        private DockConsoleController _consoleRoot;
        private bool _didSuspendTrigger;
        private bool _isExpanded = true;
        private bool _isVisible;

        public DockConsoleServiceImpl()
        {
            this._alignment = Settings.Instance.ConsoleAlignment;
        }

        public bool IsVisible
        {
            get { return this._isVisible; }

            set
            {
                if (value == this._isVisible)
                {
                    return;
                }

                this._isVisible = value;

                if (this._consoleRoot == null && value)
                {
                    this.Load();
                }
                else
                {
                    this._consoleRoot.CachedGameObject.SetActive(value);
                }

                this.CheckTrigger();
            }
        }

        public bool IsExpanded
        {
            get { return this._isExpanded; }

            set
            {
                if (value == this._isExpanded)
                {
                    return;
                }

                this._isExpanded = value;

                if (this._consoleRoot == null && value)
                {
                    this.Load();
                }
                else
                {
                    this._consoleRoot.SetDropdownVisibility(value);
                }

                this.CheckTrigger();
            }
        }

        public ConsoleAlignment Alignment
        {
            get { return this._alignment; }
            set
            {
                this._alignment = value;

                if (this._consoleRoot != null)
                {
                    this._consoleRoot.SetAlignmentMode(value);
                }

                this.CheckTrigger();
            }
        }

        private void Load()
        {
            var dockService = SRServiceManager.GetService<IPinnedUIService>();

            if (dockService == null)
            {
                Debug.LogError("[DockConsoleService] PinnedUIService not found");
                return;
            }

            var pinService = dockService as PinnedUIServiceImpl;

            if (pinService == null)
            {
                Debug.LogError("[DockConsoleService] Expected IPinnedUIService to be PinnedUIServiceImpl");
                return;
            }

            this._consoleRoot = pinService.DockConsoleController;

            this._consoleRoot.SetDropdownVisibility(this._isExpanded);
            this._consoleRoot.IsVisible = this._isVisible;
            this._consoleRoot.SetAlignmentMode(this._alignment);

            this.CheckTrigger();
        }

        private void CheckTrigger()
        {
            ConsoleAlignment? triggerAlignment = null;
            var pinAlignment = Service.Trigger.Position;

            if (pinAlignment == PinAlignment.TopLeft ||
                pinAlignment == PinAlignment.TopRight || pinAlignment == PinAlignment.TopCenter)
            {
                triggerAlignment = ConsoleAlignment.Top;
            }
            else if (pinAlignment == PinAlignment.BottomLeft ||
                       pinAlignment == PinAlignment.BottomRight ||
                       pinAlignment == PinAlignment.BottomCenter)
            {
                triggerAlignment = ConsoleAlignment.Bottom;
            }

            var shouldHide = triggerAlignment.HasValue && this.IsVisible && this.Alignment == triggerAlignment.Value;

            // Show trigger if we have hidden it, and we no longer need to hide it.
            if (this._didSuspendTrigger && !shouldHide)
            {
                Service.Trigger.IsEnabled = true;
                this._didSuspendTrigger = false;
            }
            else if (Service.Trigger.IsEnabled && shouldHide)
            {
                Service.Trigger.IsEnabled = false;
                this._didSuspendTrigger = true;
            }
        }
    }
}
