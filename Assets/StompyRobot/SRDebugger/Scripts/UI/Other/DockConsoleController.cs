namespace SRDebugger.UI.Other
{
    using Controls;
    using Internal;
    using Services;
    using SRF;
    using UnityEngine;
    using UnityEngine.EventSystems;
    using UnityEngine.UI;

    public class DockConsoleController : SRMonoBehaviourEx, IPointerEnterHandler, IPointerExitHandler
    {
        public const float NonFocusOpacity = 0.65f;
        private bool _isDirty;
        private bool _isDragging;
        private int _pointersOver;

        [Import]
        public IConsoleFilterState FilterState;

        [RequiredField] public GameObject BottomHandle;

        [RequiredField] public CanvasGroup CanvasGroup;

        [RequiredField] public ConsoleLogControl Console;

        [RequiredField] public GameObject Dropdown;

        [RequiredField] public Image DropdownToggleSprite;

        [RequiredField] public Text TextErrors;

        [RequiredField] public Text TextInfo;

        [RequiredField] public Text TextWarnings;

        [RequiredField] public Toggle ToggleErrors;

        [RequiredField] public Toggle ToggleInfo;

        [RequiredField] public Toggle ToggleWarnings;

        [RequiredField] public GameObject TopBar;

        [RequiredField] public GameObject TopHandle;

        [RequiredField] public GameObject TopSafeAreaSpacer;
        [RequiredField] public GameObject BottomSafeAreaSpacer;

        public bool IsVisible
        {
            get { return this.CachedGameObject.activeSelf; }
            set { this.CachedGameObject.SetActive(value); }
        }

        protected override void Start()
        {
            base.Start();

            //_canvasScaler = Canvas.GetComponent<CanvasScaler>();
            Service.Console.Updated += this.ConsoleOnUpdated;

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

            this.FilterState.FilterStateChange += this.OnFilterStateChange;

            this.Refresh();
            this.RefreshAlpha();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (Service.Console != null)
            {
                Service.Console.Updated -= this.ConsoleOnUpdated;
            }

            this.FilterState.FilterStateChange -= this.OnFilterStateChange;
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            this._pointersOver = 0;
            this._isDragging = false;
            this.RefreshAlpha();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            this._pointersOver = 0;
        }

        protected override void Update()
        {
            base.Update();

            if (this._isDirty)
            {
                this.Refresh();
            }
        }

        private void OnFilterStateChange(LogType logType, bool newState)
        {
            switch (logType)
            {
                case LogType.Error:
                    this.ToggleErrors.isOn = newState;
                    break;
                case LogType.Warning:
                    this.ToggleWarnings.isOn = newState;
                    break;
                case LogType.Log:
                    this.ToggleInfo.isOn = newState;
                    break;
            }
        }

        private void ConsoleOnUpdated(IConsoleService console)
        {
            this._isDirty = true;
        }

        public void SetDropdownVisibility(bool visible)
        {
            this.Dropdown.SetActive(visible);
            this.DropdownToggleSprite.rectTransform.localRotation = Quaternion.Euler(0, 0, visible ? 0f : 180f);
        }

        public void SetAlignmentMode(ConsoleAlignment alignment)
        {
            switch (alignment)
            {
                case ConsoleAlignment.Top:
                    {
                        this.TopBar.transform.SetSiblingIndex(0);
                        this.Dropdown.transform.SetSiblingIndex(2);
                        this.TopHandle.SetActive(false);
                        this.BottomHandle.SetActive(true);
                        this.transform.SetSiblingIndex(0);
                        this.DropdownToggleSprite.rectTransform.parent.localRotation = Quaternion.Euler(0, 0, 0f);
                        this.TopSafeAreaSpacer.SetActive(true);
                        this.BottomSafeAreaSpacer.SetActive(false);
                        break;
                    }

                case ConsoleAlignment.Bottom:
                    {
                        this.Dropdown.transform.SetSiblingIndex(0);
                        this.TopBar.transform.SetSiblingIndex(2);
                        this.TopHandle.SetActive(true);
                        this.BottomHandle.SetActive(false);
                        this.transform.SetSiblingIndex(1);
                        this.DropdownToggleSprite.rectTransform.parent.localRotation = Quaternion.Euler(0, 0, 180f);
                        this.TopSafeAreaSpacer.SetActive(false);
                        this.BottomSafeAreaSpacer.SetActive(true);
                        break;
                    }
            }
        }

        private void Refresh()
        {
            // Update total counts labels
            this.TextInfo.text = SRDebuggerUtil.GetNumberString(Service.Console.InfoCount, 999, "999+");
            this.TextWarnings.text = SRDebuggerUtil.GetNumberString(Service.Console.WarningCount, 999, "999+");
            this.TextErrors.text = SRDebuggerUtil.GetNumberString(Service.Console.ErrorCount, 999, "999+");

            this.ToggleErrors.isOn = this.FilterState.GetConsoleFilterState(LogType.Error);
            this.ToggleWarnings.isOn = this.FilterState.GetConsoleFilterState(LogType.Warning);
            this.ToggleInfo.isOn = this.FilterState.GetConsoleFilterState(LogType.Log);

            this._isDirty = false;
        }

        private void RefreshAlpha()
        {
            if (this._isDragging || this._pointersOver > 0)
            {
                this.CanvasGroup.alpha = 1.0f;
            }
            else
            {
                this.CanvasGroup.alpha = NonFocusOpacity;
            }
        }

        #region Event Callbacks

        public void ToggleDropdownVisible()
        {
            this.SetDropdownVisibility(!this.Dropdown.activeSelf);
        }

        public void MenuButtonPressed()
        {
            SRDebug.Instance.ShowDebugPanel(DefaultTabs.Console);
        }

        public void ClearButtonPressed()
        {
            Service.Console.Clear();
        }

        public void TogglesUpdated()
        {
            this.Console.ShowErrors = this.ToggleErrors.isOn;
            this.Console.ShowWarnings = this.ToggleWarnings.isOn;
            this.Console.ShowInfo = this.ToggleInfo.isOn;

            this.SetDropdownVisibility(true);
        }

        public void OnPointerEnter(PointerEventData e)
        {
            this._pointersOver = 1;
            this.RefreshAlpha();
        }

        public void OnPointerExit(PointerEventData e)
        {
            this._pointersOver = 0; //Mathf.Max(0, _pointersOver - 1);
            this.RefreshAlpha();
        }

        public void OnBeginDrag()
        {
            this._isDragging = true;
            this.RefreshAlpha();
        }

        public void OnEndDrag()
        {
            this._isDragging = false;
            this._pointersOver = 0;
            this.RefreshAlpha();
        }

        #endregion
    }
}
