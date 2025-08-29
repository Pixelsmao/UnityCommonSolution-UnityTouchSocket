using System.Linq;

namespace SRDebugger.UI.Tabs
{
    using Controls;
    using Controls.Data;
    using Internal;
    using Other;
    using Services;
    using SRF;
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UI;

    public class OptionsTabController : SRMonoBehaviourEx
    {
        private class CategoryInstance
        {
            public CategoryGroup CategoryGroup { get; private set; }
            public readonly List<OptionsControlBase> Options = new List<OptionsControlBase>();

            public CategoryInstance(CategoryGroup group)
            {
                this.CategoryGroup = group;
            }
        }

        private readonly List<OptionsControlBase> _controls = new List<OptionsControlBase>();
        private readonly List<CategoryInstance> _categories = new List<CategoryInstance>();

        private readonly Dictionary<OptionDefinition, OptionsControlBase> _options =
            new Dictionary<OptionDefinition, OptionsControlBase>();

        private bool _queueRefresh;
        private bool _selectionModeEnabled;
        private Canvas _optionCanvas;

        [RequiredField] public ActionControl ActionControlPrefab;

        [RequiredField] public CategoryGroup CategoryGroupPrefab;

        [RequiredField] public RectTransform ContentContainer;

        [RequiredField] public GameObject NoOptionsNotice;

        [RequiredField] public Toggle PinButton;

        [RequiredField] public GameObject PinPromptSpacer;

        [RequiredField] public GameObject PinPromptText;


        protected override void Start()
        {
            base.Start();

            this.PinButton.onValueChanged.AddListener(this.SetSelectionModeEnabled);

            this.PinPromptText.SetActive(false);
            //PinPromptSpacer.SetActive(false);

            this.Populate();

            this._optionCanvas = this.GetComponent<Canvas>();

            Service.Options.OptionsUpdated += this.OnOptionsUpdated;
            Service.PinnedUI.OptionPinStateChanged += this.OnOptionPinnedStateChanged;
        }

        protected override void OnDestroy()
        {
            if (Service.Options != null)
            {
                Service.Options.OptionsUpdated -= this.OnOptionsUpdated;
            }

            if (Service.PinnedUI != null)
            {
                Service.PinnedUI.OptionPinStateChanged -= this.OnOptionPinnedStateChanged;
            }

            base.OnDestroy();
        }

        private void OnOptionPinnedStateChanged(OptionDefinition optionDefinition, bool isPinned)
        {
            if (this._options.ContainsKey(optionDefinition))
            {
                this._options[optionDefinition].IsSelected = isPinned;
            }
        }

        private void OnOptionsUpdated(object sender, EventArgs eventArgs)
        {
            this.Clear();
            this.Populate();
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            Service.Panel.VisibilityChanged += this.PanelOnVisibilityChanged;
        }

        protected override void OnDisable()
        {
            // Always end pinning mode when tabbing away
            this.SetSelectionModeEnabled(false);

            if (Service.Panel != null)
            {
                Service.Panel.VisibilityChanged -= this.PanelOnVisibilityChanged;
            }

            base.OnDisable();
        }

        protected override void Update()
        {
            base.Update();

            if (this._queueRefresh)
            {
                this._queueRefresh = false;
                this.Refresh();
            }
        }

        private void PanelOnVisibilityChanged(IDebugPanelService debugPanelService, bool b)
        {
            // Always end pinning mode when panel is closed
            if (!b)
            {
                this.SetSelectionModeEnabled(false);

                // Refresh bindings for all pinned controls
                this.Refresh();
            }
            else if (b && this.CachedGameObject.activeInHierarchy)
            {
                // If the panel is visible, and this tab is active (selected), refresh all the data bindings
                this.Refresh();
            }

            if (this._optionCanvas != null)
            {
                this._optionCanvas.enabled = b;
            }
        }

        public void SetSelectionModeEnabled(bool isEnabled)
        {
            if (this._selectionModeEnabled == isEnabled)
            {
                return;
            }

            this._selectionModeEnabled = isEnabled;

            this.PinButton.isOn = isEnabled;
            this.PinPromptText.SetActive(isEnabled);
            //PinPromptSpacer.SetActive(isEnabled);

            foreach (var kv in this._options)
            {
                kv.Value.SelectionModeEnabled = isEnabled;

                // Set IsSelected if entering selection mode.
                if (isEnabled)
                {
                    kv.Value.IsSelected = Service.PinnedUI.HasPinned(kv.Key);
                }
            }

            foreach (var cat in this._categories)
            {
                cat.CategoryGroup.SelectionModeEnabled = isEnabled;
            }

            this.RefreshCategorySelection();

            // Return if entering selection mode
            if (isEnabled)
            {
                return;
            }
        }

        private void Refresh()
        {
            for (var i = 0; i < this._options.Count; i++)
            {
                this._controls[i].Refresh();
                this._controls[i].SelectionModeEnabled = this._selectionModeEnabled;
                this._controls[i].IsSelected = Service.PinnedUI.HasPinned(this._controls[i].Option);
            }
        }

        private void CommitPinnedOptions()
        {
            foreach (var kv in this._options)
            {
                var control = kv.Value;

                if (control.IsSelected && !Service.PinnedUI.HasPinned(kv.Key))
                {
                    Service.PinnedUI.Pin(kv.Key);
                }
                else if (!control.IsSelected && Service.PinnedUI.HasPinned(kv.Key))
                {
                    Service.PinnedUI.Unpin(kv.Key);
                }
            }
        }

        private bool _isTogglingCategory;

        private void RefreshCategorySelection()
        {
            this._isTogglingCategory = true;

            foreach (var cat in this._categories)
            {
                var allSelected = true;

                for (var i = 0; i < cat.Options.Count; i++)
                {
                    if (!cat.Options[i].IsSelected)
                    {
                        allSelected = false;
                        break;
                    }
                }

                cat.CategoryGroup.IsSelected = allSelected;
            }

            this._isTogglingCategory = false;
        }

        private void OnOptionSelectionToggle(bool selected)
        {
            if (!this._isTogglingCategory)
            {
                this.RefreshCategorySelection();
                this.CommitPinnedOptions();
            }
        }

        /// <summary>
        /// When a category mode selection is changed.
        /// </summary>
        /// <param name="category"></param>
        /// <param name="selected"></param>
        private void OnCategorySelectionToggle(CategoryInstance category, bool selected)
        {
            this._isTogglingCategory = true;

            for (var i = 0; i < category.Options.Count; i++)
            {
                category.Options[i].IsSelected = selected;
            }

            this._isTogglingCategory = false;

            this.CommitPinnedOptions();
        }

        #region Initialisation

        protected void Populate()
        {
            var sortedOptions = new Dictionary<string, List<OptionDefinition>>();

            foreach (var option in Service.Options.Options)
            {
                if (!OptionControlFactory.CanCreateControl(option))
                {
                    if (option.IsProperty)
                    {
                        Debug.LogError("[SRDebugger.OptionsTab] Unsupported property type: {0} (on property {1})".Fmt(option.Property.PropertyType, option.Property));
                    }
                    else
                    {
                        Debug.LogError("[SRDebugger.OptionsTab] Unsupported method signature: {0}".Fmt(option.Name));
                    }
                    continue;
                }

                // Find a properly list for that category, or create a new one

                if (!sortedOptions.TryGetValue(option.Category, out var memberList))
                {
                    memberList = new List<OptionDefinition>();
                    sortedOptions.Add(option.Category, memberList);
                }

                memberList.Add(option);
            }

            var hasCreated = false;

            foreach (var kv in sortedOptions.OrderBy(p => p.Key))
            {
                if (kv.Value.Count == 0)
                {
                    continue;
                }

                hasCreated = true;
                this.CreateCategory(kv.Key, kv.Value);
            }

            if (hasCreated)
            {
                this.NoOptionsNotice.SetActive(false);
            }

            this.RefreshCategorySelection();
        }

        protected void CreateCategory(string title, List<OptionDefinition> options)
        {
            options.Sort((d1, d2) => d1.SortPriority.CompareTo(d2.SortPriority));

            var groupInstance = SRInstantiate.Instantiate(this.CategoryGroupPrefab);
            var categoryInstance = new CategoryInstance(groupInstance);

            this._categories.Add(categoryInstance);

            groupInstance.CachedTransform.SetParent(this.ContentContainer, false);
            groupInstance.Header.text = title;
            groupInstance.SelectionModeEnabled = this._selectionModeEnabled;

            categoryInstance.CategoryGroup.SelectionToggle.onValueChanged.AddListener(
                b => this.OnCategorySelectionToggle(categoryInstance, b));

            foreach (var option in options)
            {
                var control = OptionControlFactory.CreateControl(option, title);

                if (control == null)
                {
                    Debug.LogError("[SRDebugger.OptionsTab] Failed to create option control for {0}".Fmt(option.Name));
                    continue;
                }

                categoryInstance.Options.Add(control);
                control.CachedTransform.SetParent(groupInstance.Container, false);
                control.IsSelected = Service.PinnedUI.HasPinned(option);
                control.SelectionModeEnabled = this._selectionModeEnabled;
                control.SelectionModeToggle.onValueChanged.AddListener(this.OnOptionSelectionToggle);

                this._options.Add(option, control);
                this._controls.Add(control);
            }
        }

        private void Clear()
        {
            foreach (var categoryInstance in this._categories)
            {
                Destroy(categoryInstance.CategoryGroup.gameObject);
            }

            this._categories.Clear();
            this._controls.Clear();
            this._options.Clear();
        }

        #endregion
    }
}
