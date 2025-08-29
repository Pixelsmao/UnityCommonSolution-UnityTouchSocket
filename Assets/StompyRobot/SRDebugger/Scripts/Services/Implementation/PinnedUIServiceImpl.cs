namespace SRDebugger.Services.Implementation
{
    using Internal;
    using SRF;
    using SRF.Service;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UI.Controls;
    using UI.Other;
    using UnityEngine;

    [Service(typeof(IPinnedUIService))]
    public class PinnedUIServiceImpl : SRServiceBase<IPinnedUIService>, IPinnedUIService
    {
        private readonly List<OptionsControlBase> _controlList = new List<OptionsControlBase>();

        private readonly Dictionary<OptionDefinition, OptionsControlBase> _pinnedObjects =
            new Dictionary<OptionDefinition, OptionsControlBase>();

        private bool _queueRefresh;
        private PinnedUIRoot _uiRoot;

        public DockConsoleController DockConsoleController
        {
            get
            {
                if (this._uiRoot == null)
                {
                    this.Load();
                }
                return this._uiRoot.DockConsoleController;
            }
        }

        public event Action<OptionDefinition, bool> OptionPinStateChanged;
        public event Action<RectTransform> OptionsCanvasCreated;

        public bool IsProfilerPinned
        {
            get
            {
                if (this._uiRoot == null)
                {
                    return false;
                }
                return this._uiRoot.Profiler.activeSelf;
            }
            set
            {
                if (this._uiRoot == null)
                {
                    this.Load();
                }
                this._uiRoot.Profiler.SetActive(value);
            }
        }

        public void Pin(OptionDefinition obj, int order = -1)
        {
            if (this._uiRoot == null)
            {
                this.Load();
            }

            if (this._pinnedObjects.ContainsKey(obj))
            {
                return;
            }

            var control = OptionControlFactory.CreateControl(obj);

            control.CachedTransform.SetParent(this._uiRoot.Container, false);

            if (order >= 0)
            {
                control.CachedTransform.SetSiblingIndex(order);
            }

            this._pinnedObjects.Add(obj, control);
            this._controlList.Add(control);

            this.OnPinnedStateChanged(obj, true);
        }

        public void Unpin(OptionDefinition obj)
        {
            if (!this._pinnedObjects.ContainsKey(obj))
            {
                return;
            }

            var control = this._pinnedObjects[obj];

            this._pinnedObjects.Remove(obj);
            this._controlList.Remove(control);

            Destroy(control.CachedGameObject);

            this.OnPinnedStateChanged(obj, false);
        }

        private void OnPinnedStateChanged(OptionDefinition option, bool isPinned)
        {
            if (OptionPinStateChanged != null)
            {
                OptionPinStateChanged(option, isPinned);
            }
        }

        public void UnpinAll()
        {
            foreach (var op in this._pinnedObjects)
            {
                Destroy(op.Value.CachedGameObject);
            }

            this._pinnedObjects.Clear();
            this._controlList.Clear();
        }

        public bool HasPinned(OptionDefinition option)
        {
            return this._pinnedObjects.ContainsKey(option);
        }

        protected override void Awake()
        {
            base.Awake();

            this.CachedTransform.SetParent(Hierarchy.Get("SRDebugger"));
        }

        private void Load()
        {
            var prefab = Resources.Load<PinnedUIRoot>(SRDebugPaths.PinnedUIPrefabPath);

            if (prefab == null)
            {
                Debug.LogError("[SRDebugger.PinnedUI] Error loading ui prefab");
                return;
            }

            var instance = SRInstantiate.Instantiate(prefab);
            instance.CachedTransform.SetParent(this.CachedTransform, false);

            this._uiRoot = instance;
            this.UpdateAnchors();
            SRDebug.Instance.PanelVisibilityChanged += this.OnDebugPanelVisibilityChanged;

            Service.Options.OptionsUpdated += this.OnOptionsUpdated;

            if (OptionsCanvasCreated != null)
            {
                OptionsCanvasCreated(this._uiRoot.Canvas.GetComponent<RectTransform>());
            }
        }

        private void UpdateAnchors()
        {
            // Setup alignment of Profiler/Options splitter
            switch (Settings.Instance.ProfilerAlignment)
            {
                case PinAlignment.BottomLeft:
                case PinAlignment.TopLeft:
                case PinAlignment.CenterLeft:
                    this._uiRoot.Profiler.transform.SetSiblingIndex(0);
                    break;

                case PinAlignment.BottomRight:
                case PinAlignment.TopRight:
                case PinAlignment.CenterRight:
                    this._uiRoot.Profiler.transform.SetSiblingIndex(1);
                    break;
            }

            // Setup alignment of Profiler vertical layout group
            switch (Settings.Instance.ProfilerAlignment)
            {
                case PinAlignment.TopRight:
                case PinAlignment.TopLeft:
                    this._uiRoot.ProfilerVerticalLayoutGroup.childAlignment = TextAnchor.UpperCenter;
                    break;

                case PinAlignment.BottomRight:
                case PinAlignment.BottomLeft:
                    this._uiRoot.ProfilerVerticalLayoutGroup.childAlignment = TextAnchor.LowerCenter;
                    break;

                case PinAlignment.CenterLeft:
                case PinAlignment.CenterRight:
                    this._uiRoot.ProfilerVerticalLayoutGroup.childAlignment = TextAnchor.MiddleCenter;
                    break;
            }

            this._uiRoot.ProfilerHandleManager.SetAlignment(Settings.Instance.ProfilerAlignment);

            // Setup alignment of options flow layout group
            switch (Settings.Instance.OptionsAlignment)
            {
                case PinAlignment.BottomLeft: // OptionsBottomLeft
                    this._uiRoot.OptionsLayoutGroup.childAlignment = TextAnchor.LowerLeft;
                    break;
                case PinAlignment.TopLeft:
                    this._uiRoot.OptionsLayoutGroup.childAlignment = TextAnchor.UpperLeft;
                    break;
                case PinAlignment.BottomRight:
                    this._uiRoot.OptionsLayoutGroup.childAlignment = TextAnchor.LowerRight;
                    break;
                case PinAlignment.TopRight:
                    this._uiRoot.OptionsLayoutGroup.childAlignment = TextAnchor.UpperRight;
                    break;
                case PinAlignment.BottomCenter:
                    this._uiRoot.OptionsLayoutGroup.childAlignment = TextAnchor.LowerCenter;
                    break;
                case PinAlignment.TopCenter:
                    this._uiRoot.OptionsLayoutGroup.childAlignment = TextAnchor.UpperCenter;
                    break;
                case PinAlignment.CenterLeft:
                    this._uiRoot.OptionsLayoutGroup.childAlignment = TextAnchor.MiddleLeft;
                    break;
                case PinAlignment.CenterRight:
                    this._uiRoot.OptionsLayoutGroup.childAlignment = TextAnchor.MiddleRight;
                    break;
            }
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

        private void OnOptionsUpdated(object sender, EventArgs eventArgs)
        {
            // Check for removed options.
            var pinned = this._pinnedObjects.Keys.ToList();

            foreach (var op in pinned)
            {
                if (!Service.Options.Options.Contains(op))
                {
                    this.Unpin(op);
                }
            }
        }

        private void OnDebugPanelVisibilityChanged(bool isVisible)
        {
            // Refresh bindings when debug panel is no longer visible
            if (!isVisible)
            {
                this._queueRefresh = true;
            }
        }

        private void Refresh()
        {
            for (var i = 0; i < this._controlList.Count; i++)
            {
                this._controlList[i].Refresh();
            }
        }
    }
}
