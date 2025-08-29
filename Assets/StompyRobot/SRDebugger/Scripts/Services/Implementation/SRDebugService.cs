namespace SRDebugger.Services.Implementation
{
    using Internal;
    using SRF;
    using SRF.Service;
    using SRF.UI;
    using System;
    using UnityEngine;
    using UnityEngine.UI;
    using Object = UnityEngine.Object;

    [Service(typeof(IDebugService))]
    public class SRDebugService : IDebugService
    {
        public IDockConsoleService DockConsole
        {
            get { return Service.DockConsole; }
        }

        public IConsoleFilterState ConsoleFilter
        {
            get
            {
                if (this._consoleFilterState == null)
                {
                    this._consoleFilterState = SRServiceManager.GetService<IConsoleFilterState>();
                }
                return this._consoleFilterState;
            }
        }

        public event VisibilityChangedDelegate PanelVisibilityChanged;
        public event PinnedUiCanvasCreated PinnedUiCanvasCreated;

        private readonly IDebugPanelService _debugPanelService;
        private readonly IDebugTriggerService _debugTrigger;
        private readonly ISystemInformationService _informationService;
        private readonly IOptionsService _optionsService;
        private readonly IPinnedUIService _pinnedUiService;
        private IConsoleFilterState _consoleFilterState;

        private EntryCode? _entryCode;
        private bool _hasAuthorised;

        private DefaultTabs? _queuedTab;
        private RectTransform _worldSpaceTransform;
        private DynamicOptionContainer _looseOptionContainer;


        public SRDebugService()
        {
            SRServiceManager.RegisterService<IDebugService>(this);

            // Load profiler
            SRServiceManager.GetService<IProfilerService>();

            // Setup trigger service
            this._debugTrigger = SRServiceManager.GetService<IDebugTriggerService>();

            this._informationService = SRServiceManager.GetService<ISystemInformationService>();

            this._pinnedUiService = SRServiceManager.GetService<IPinnedUIService>();
            this._pinnedUiService.OptionsCanvasCreated += transform =>
            {
                if (PinnedUiCanvasCreated == null) return;
                try
                {
                    PinnedUiCanvasCreated(transform);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            };

            this._optionsService = SRServiceManager.GetService<IOptionsService>();

            // Create debug panel service (this does not actually load any UI resources until opened)
            this._debugPanelService = SRServiceManager.GetService<IDebugPanelService>();

            // Subscribe to visibility changes to provide API-facing event for panel open/close
            this._debugPanelService.VisibilityChanged += this.DebugPanelServiceOnVisibilityChanged;

            this._debugTrigger.IsEnabled = this.Settings.EnableTrigger == Settings.TriggerEnableModes.Enabled ||
                                      this.Settings.EnableTrigger == Settings.TriggerEnableModes.MobileOnly && Application.isMobilePlatform ||
                                      this.Settings.EnableTrigger == Settings.TriggerEnableModes.DevelopmentBuildsOnly && Debug.isDebugBuild;

            this._debugTrigger.Position = this.Settings.TriggerPosition;

            if (this.Settings.EnableKeyboardShortcuts)
            {
                SRServiceManager.GetService<KeyboardShortcutListenerService>();
            }

            if (Settings.Instance.RequireCode)
            {
                if (Settings.Instance.EntryCode.Count != 4)
                {
                    Debug.LogError("[SRDebugger] RequireCode is enabled, but pin is not 4 digits");
                }
                else
                {
                    this._entryCode = new EntryCode(Settings.Instance.EntryCode[0], Settings.Instance.EntryCode[1],
                        Settings.Instance.EntryCode[2], Settings.Instance.EntryCode[3]);
                }
            }

            // Ensure that root object cannot be destroyed on scene loads
            var srDebuggerParent = Hierarchy.Get("SRDebugger");
            Object.DontDestroyOnLoad(srDebuggerParent.gameObject);

            // Add any options containers that were created on init
            var internalRegistry = SRServiceManager.GetService<InternalOptionsRegistry>();
            internalRegistry.SetHandler(this._optionsService.AddContainer);
        }

        public Settings Settings
        {
            get { return Settings.Instance; }
        }

        public bool IsDebugPanelVisible
        {
            get { return this._debugPanelService.IsVisible; }
        }

        public bool IsTriggerEnabled
        {
            get { return this._debugTrigger.IsEnabled; }
            set { this._debugTrigger.IsEnabled = value; }
        }

        public bool IsTriggerErrorNotificationEnabled
        {
            get { return this._debugTrigger.ShowErrorNotification; }
            set { this._debugTrigger.ShowErrorNotification = value; }
        }

        public bool IsProfilerDocked
        {
            get { return Service.PinnedUI.IsProfilerPinned; }
            set { Service.PinnedUI.IsProfilerPinned = value; }
        }

        public void AddSystemInfo(InfoEntry entry, string category = "Default")
        {
            this._informationService.Add(entry, category);
        }

        public void ShowDebugPanel(bool requireEntryCode = true)
        {
            if (requireEntryCode && this._entryCode.HasValue && !this._hasAuthorised)
            {
                this.PromptEntryCode();
                return;
            }

            this._debugPanelService.IsVisible = true;
        }

        public void ShowDebugPanel(DefaultTabs tab, bool requireEntryCode = true)
        {
            if (requireEntryCode && this._entryCode.HasValue && !this._hasAuthorised)
            {
                this._queuedTab = tab;
                this.PromptEntryCode();
                return;
            }

            this._debugPanelService.IsVisible = true;
            this._debugPanelService.OpenTab(tab);
        }

        public void HideDebugPanel()
        {
            this._debugPanelService.IsVisible = false;
        }

        public void SetEntryCode(EntryCode newCode)
        {
            this._hasAuthorised = false;
            this._entryCode = newCode;
        }

        public void DisableEntryCode()
        {
            this._entryCode = null;
        }

        public void DestroyDebugPanel()
        {
            this._debugPanelService.IsVisible = false;
            this._debugPanelService.Unload();
        }

        #region Options

        public void AddOptionContainer(object container)
        {
            this._optionsService.AddContainer(container);
        }

        public void RemoveOptionContainer(object container)
        {
            this._optionsService.RemoveContainer(container);
        }

        public void AddOption(OptionDefinition option)
        {
            if (this._looseOptionContainer == null)
            {
                this._looseOptionContainer = new DynamicOptionContainer();
                this._optionsService.AddContainer(this._looseOptionContainer);
            }

            this._looseOptionContainer.AddOption(option);
        }

        public bool RemoveOption(OptionDefinition option)
        {
            if (this._looseOptionContainer != null)
            {
                return this._looseOptionContainer.RemoveOption(option);
            }

            return false;
        }

        public void PinAllOptions(string category)
        {
            foreach (var op in this._optionsService.Options)
            {
                if (op.Category == category)
                {
                    this._pinnedUiService.Pin(op);
                }
            }
        }

        public void UnpinAllOptions(string category)
        {
            foreach (var op in this._optionsService.Options)
            {
                if (op.Category == category)
                {
                    this._pinnedUiService.Unpin(op);
                }
            }
        }

        public void PinOption(string name)
        {
            foreach (var op in this._optionsService.Options)
            {
                if (op.Name == name)
                {
                    this._pinnedUiService.Pin(op);
                }
            }
        }

        public void UnpinOption(string name)
        {
            foreach (var op in this._optionsService.Options)
            {
                if (op.Name == name)
                {
                    this._pinnedUiService.Unpin(op);
                }
            }
        }

        public void ClearPinnedOptions()
        {
            this._pinnedUiService.UnpinAll();
        }

        #endregion

        #region Bug Reporter

        public void ShowBugReportSheet(ActionCompleteCallback onComplete = null, bool takeScreenshot = true,
            string descriptionContent = null)
        {
            var popoverService = SRServiceManager.GetService<BugReportPopoverService>();

            if (popoverService.IsShowingPopover)
            {
                return;
            }

            popoverService.ShowBugReporter((succeed, message) =>
            {
                if (onComplete != null)
                {
                    onComplete(succeed);
                }
            }, takeScreenshot, descriptionContent);
        }

        #endregion

        private void DebugPanelServiceOnVisibilityChanged(IDebugPanelService debugPanelService, bool b)
        {
            if (PanelVisibilityChanged == null)
            {
                return;
            }

            try
            {
                PanelVisibilityChanged(b);
            }
            catch (Exception e)
            {
                Debug.LogError("[SRDebugger] Event target threw exception (IDebugService.PanelVisiblityChanged)");
                Debug.LogException(e);
            }
        }

        private void PromptEntryCode()
        {
            SRServiceManager.GetService<IPinEntryService>()
                .ShowPinEntry(this._entryCode.Value, SRDebugStrings.Current.PinEntryPrompt,
                    entered =>
                    {
                        if (entered)
                        {
                            if (!Settings.Instance.RequireEntryCodeEveryTime)
                            {
                                this._hasAuthorised = true;
                            }

                            if (this._queuedTab.HasValue)
                            {
                                var t = this._queuedTab.Value;

                                this._queuedTab = null;
                                this.ShowDebugPanel(t, false);
                            }
                            else
                            {
                                this.ShowDebugPanel(false);
                            }
                        }

                        this._queuedTab = null;
                    });
        }

        public RectTransform EnableWorldSpaceMode()
        {
            if (this._worldSpaceTransform != null)
            {
                return this._worldSpaceTransform;
            }

            if (Settings.Instance.UseDebugCamera)
            {
                throw new InvalidOperationException("UseDebugCamera cannot be enabled at the same time as EnableWorldSpaceMode.");
            }

            this._debugPanelService.IsVisible = true;

            var root = ((DebugPanelServiceImpl)this._debugPanelService).RootObject;
            root.Canvas.gameObject.RemoveComponentIfExists<SRRetinaScaler>();
            root.Canvas.gameObject.RemoveComponentIfExists<CanvasScaler>();
            root.Canvas.renderMode = RenderMode.WorldSpace;

            var rectTransform = root.Canvas.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(1024, 768);
            rectTransform.position = Vector3.zero;

            return this._worldSpaceTransform = rectTransform;
        }

        public void SetBugReporterHandler(IBugReporterHandler bugReporterHandler)
        {
            SRServiceManager.GetService<IBugReportService>().SetHandler(bugReporterHandler);
        }
    }
}
