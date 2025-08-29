//#define SR_CONSOLE_DEBUG

using System.Collections;

namespace SRDebugger.UI.Tabs
{
    using Controls;
    using Internal;
    using Services;
    using SRF;
    using System;
    using UnityEngine;
    using UnityEngine.UI;

    public class ConsoleTabController : SRMonoBehaviourEx
    {
        private const int MaxLength = 2600;

        private Canvas _consoleCanvas;
        private bool _isDirty;

        private static bool _hasWarnedAboutLogHandler;
        private static bool _hasWarnedAboutLoggingDisabled;

        [Import]
        public IConsoleFilterState FilterState;

        [RequiredField]
        public ConsoleLogControl ConsoleLogControl;

        [RequiredField]
        public Toggle PinToggle;
        //public bool IsListening = true;

        [RequiredField]
        public ScrollRect StackTraceScrollRect;
        [RequiredField]
        public Text StackTraceText;
        [RequiredField]
        public Toggle ToggleErrors;
        [RequiredField]
        public Text ToggleErrorsText;
        [RequiredField]
        public Toggle ToggleInfo;
        [RequiredField]
        public Text ToggleInfoText;
        [RequiredField]
        public Toggle ToggleWarnings;
        [RequiredField]
        public Text ToggleWarningsText;

        [RequiredField]
        public GameObject CopyToClipboardContainer;

        [RequiredField]
        public GameObject CopyToClipboardButton;

        [RequiredField]
        public GameObject CopyToClipboardMessage;

        [RequiredField]
        public CanvasGroup CopyToClipboardMessageCanvasGroup;

        [RequiredField]
        public GameObject LoggingIsDisabledCanvasGroup;

        [RequiredField]
        public GameObject LogHandlerHasBeenOverridenGroup;

        [RequiredField]
        public Toggle FilterToggle;
        [RequiredField]
        public InputField FilterField;
        [RequiredField]
        public GameObject FilterBarContainer;

        private ConsoleEntry _selectedItem;

        private Coroutine _fadeButtonCoroutine;

        protected override void Start()
        {
            base.Start();

            this._consoleCanvas = this.GetComponent<Canvas>();

            this.ToggleErrors.isOn = this.FilterState.GetConsoleFilterState(LogType.Error);
            this.ToggleWarnings.isOn = this.FilterState.GetConsoleFilterState(LogType.Warning);
            this.ToggleInfo.isOn = this.FilterState.GetConsoleFilterState(LogType.Log);

            this.ToggleErrors.onValueChanged.AddListener(isOn =>
            {
                this.FilterState.SetConsoleFilterState(LogType.Error, isOn);
                this._isDirty = true;
            });

            this.ToggleWarnings.onValueChanged.AddListener(isOn =>
            {
                this.FilterState.SetConsoleFilterState(LogType.Warning, isOn);
                this._isDirty = true;
            });

            this.ToggleInfo.onValueChanged.AddListener(isOn =>
            {
                this.FilterState.SetConsoleFilterState(LogType.Log, isOn);
                this._isDirty = true;
            });

            this.PinToggle.onValueChanged.AddListener(this.PinToggleValueChanged);

            this.FilterToggle.onValueChanged.AddListener(this.FilterToggleValueChanged);
            this.FilterBarContainer.SetActive(this.FilterToggle.isOn);

#if UNITY_5_3_OR_NEWER
            this.FilterField.onValueChanged.AddListener(this.FilterValueChanged);
#else
            FilterField.onValueChange.AddListener(FilterValueChanged);
#endif

            this.ConsoleLogControl.SelectedItemChanged = this.ConsoleLogSelectedItemChanged;

            Service.Console.Updated += this.ConsoleOnUpdated;
            Service.Panel.VisibilityChanged += this.PanelOnVisibilityChanged;
            this.FilterState.FilterStateChange += this.OnFilterStateChange;

            this.StackTraceText.supportRichText = Settings.Instance.RichTextInConsole;
            this.PopulateStackTraceArea(null);

            this.Refresh();
        }

        private void OnFilterStateChange(LogType logtype, bool newstate)
        {
            switch (logtype)
            {
                case LogType.Error:
                    this.ToggleErrors.isOn = newstate;
                    break;
                case LogType.Warning:
                    this.ToggleWarnings.isOn = newstate;
                    break;
                case LogType.Log:
                    this.ToggleInfo.isOn = newstate;
                    break;
            }
        }

        private void FilterToggleValueChanged(bool isOn)
        {
            if (isOn)
            {
                this.FilterBarContainer.SetActive(true);
                this.ConsoleLogControl.Filter = this.FilterField.text;
            }
            else
            {
                this.ConsoleLogControl.Filter = null;
                this.FilterBarContainer.SetActive(false);
            }
        }
        private void FilterValueChanged(string filterText)
        {
            if (this.FilterToggle.isOn && !string.IsNullOrEmpty(filterText) && filterText.Trim().Length != 0)
            {
                this.ConsoleLogControl.Filter = filterText;
            }
            else
            {
                this.ConsoleLogControl.Filter = null;
            }
        }

        private void PanelOnVisibilityChanged(IDebugPanelService debugPanelService, bool b)
        {
            if (this._consoleCanvas == null)
            {
                return;
            }

            if (b)
            {
                this._consoleCanvas.enabled = true;
            }
            else
            {
                this._consoleCanvas.enabled = false;
                this.StopAnimations();
            }
        }

        private void PinToggleValueChanged(bool isOn)
        {
            Service.DockConsole.IsVisible = isOn;
        }

        protected override void OnDestroy()
        {
            this.StopAnimations();

            if (Service.Console != null)
            {
                Service.Console.Updated -= this.ConsoleOnUpdated;
            }

            this.FilterState.FilterStateChange -= this.OnFilterStateChange;


            base.OnDestroy();
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            this._isDirty = true;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            this.StopAnimations();
        }

        private void ConsoleLogSelectedItemChanged(object item)
        {
            var log = item as ConsoleEntry;
            this.PopulateStackTraceArea(log);
        }

        protected override void Update()
        {
            base.Update();

            if (this._isDirty)
            {
                this.Refresh();
            }
        }

        private void PopulateStackTraceArea(ConsoleEntry entry)
        {
            if (entry == null)
            {
                this.SetCopyToClipboardButtonState(CopyToClipboardStates.Hidden);
                this.StackTraceText.text = "";
            }
            else
            {
                if (SRDebug.CopyConsoleItemCallback != null)
                {
                    this.SetCopyToClipboardButtonState(CopyToClipboardStates.Visible);
                }

                var text = entry.Message + Environment.NewLine +
                           (!string.IsNullOrEmpty(entry.StackTrace)
                               ? entry.StackTrace
                               : SRDebugStrings.Current.Console_NoStackTrace);

                if (text.Length > MaxLength)
                {
                    text = text.Substring(0, MaxLength);
                    text += "\n" + SRDebugStrings.Current.Console_MessageTruncated;
                }

                this.StackTraceText.text = text;
            }

            this.StackTraceScrollRect.normalizedPosition = new Vector2(0, 1);
            this._selectedItem = entry;
        }

        public void CopyToClipboard()
        {
            if (this._selectedItem != null)
            {
                this.SetCopyToClipboardButtonState(CopyToClipboardStates.Activated);
                if (SRDebug.CopyConsoleItemCallback != null)
                {
                    SRDebug.CopyConsoleItemCallback(this._selectedItem);
                }
                else
                {
                    Debug.LogError("[SRDebugger] Copy to clipboard is not available.");
                }
            }
        }

        public enum CopyToClipboardStates
        {
            Hidden,
            Visible,
            Activated
        }

        private void SetCopyToClipboardButtonState(CopyToClipboardStates state)
        {
            this.StopAnimations();

            switch (state)
            {
                case CopyToClipboardStates.Hidden:
                    this.CopyToClipboardContainer.SetActive(false);
                    this.CopyToClipboardButton.SetActive(false);
                    this.CopyToClipboardMessage.SetActive(false);
                    break;
                case CopyToClipboardStates.Visible:
                    this.CopyToClipboardContainer.SetActive(true);
                    this.CopyToClipboardButton.SetActive(true);
                    this.CopyToClipboardMessage.SetActive(false);
                    break;
                case CopyToClipboardStates.Activated:
                    this.CopyToClipboardMessageCanvasGroup.alpha = 1;
                    this.CopyToClipboardContainer.SetActive(true);
                    this.CopyToClipboardButton.SetActive(false);
                    this.CopyToClipboardMessage.SetActive(true);

                    this._fadeButtonCoroutine = this.StartCoroutine(this.FadeCopyButton());
                    break;
                default:
                    throw new ArgumentOutOfRangeException("state", state, null);
            }
        }

        private IEnumerator FadeCopyButton()
        {
            yield return new WaitForSecondsRealtime(2f);

            var startTime = Time.realtimeSinceStartup;
            var endTime = Time.realtimeSinceStartup + 1f;

            while (Time.realtimeSinceStartup < endTime)
            {
                var currentAlpha = Mathf.InverseLerp(endTime, startTime, Time.realtimeSinceStartup);
                this.CopyToClipboardMessageCanvasGroup.alpha = currentAlpha;
                yield return new WaitForEndOfFrame();
            }

            this.CopyToClipboardMessageCanvasGroup.alpha = 0;
            this._fadeButtonCoroutine = null;
        }

        private void StopAnimations()
        {
            if (this._fadeButtonCoroutine != null)
            {
                this.StopCoroutine(this._fadeButtonCoroutine);
                this._fadeButtonCoroutine = null;
                this.CopyToClipboardMessageCanvasGroup.alpha = 0;
            }
        }

        private void Refresh()
        {
            // Update total counts labels
            this.ToggleInfoText.text = SRDebuggerUtil.GetNumberString(Service.Console.InfoCount, 999, "999+");
            this.ToggleWarningsText.text = SRDebuggerUtil.GetNumberString(Service.Console.WarningCount, 999, "999+");
            this.ToggleErrorsText.text = SRDebuggerUtil.GetNumberString(Service.Console.ErrorCount, 999, "999+");

            this.ConsoleLogControl.ShowErrors = this.ToggleErrors.isOn;
            this.ConsoleLogControl.ShowWarnings = this.ToggleWarnings.isOn;
            this.ConsoleLogControl.ShowInfo = this.ToggleInfo.isOn;

            this.PinToggle.isOn = Service.DockConsole.IsVisible;

            this._isDirty = false;

            if (!_hasWarnedAboutLogHandler && Service.Console.LogHandlerIsOverriden)
            {
                this.LogHandlerHasBeenOverridenGroup.SetActive(true);
                _hasWarnedAboutLogHandler = true;
            }

            if (!_hasWarnedAboutLoggingDisabled && !Service.Console.LoggingEnabled)
            {
                this.LoggingIsDisabledCanvasGroup.SetActive(true);
            }
        }

        private void ConsoleOnUpdated(IConsoleService console)
        {
            this._isDirty = true;
        }

        public void Clear()
        {
            Service.Console.Clear();
            this._isDirty = true;
        }

        public void LogHandlerHasBeenOverridenOkayButtonPress()
        {
            _hasWarnedAboutLogHandler = true;
            this.LogHandlerHasBeenOverridenGroup.SetActive(false);
        }

        public void LoggingDisableCloseAndIgnorePressed()
        {
            this.LoggingIsDisabledCanvasGroup.SetActive(false);
            _hasWarnedAboutLoggingDisabled = true;
        }

        public void LoggingDisableReenablePressed()
        {
            Service.Console.LoggingEnabled = true;
            this.LoggingIsDisabledCanvasGroup.SetActive(false);

            Debug.Log("[SRDebugger] Re-enabled logging.");
        }
    }
}
