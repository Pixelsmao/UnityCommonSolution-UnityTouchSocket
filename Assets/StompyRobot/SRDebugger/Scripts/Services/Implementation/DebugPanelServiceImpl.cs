namespace SRDebugger.Services.Implementation
{
    using Internal;
    using SRF;
    using SRF.Service;
    using System;
    using UI;
    using UnityEngine;

    [Service(typeof(IDebugPanelService))]
    public class DebugPanelServiceImpl : ScriptableObject, IDebugPanelService, IDisposable
    {
        private DebugPanelRoot _debugPanelRootObject;
        public event Action<IDebugPanelService, bool> VisibilityChanged;

        private bool _isVisible;

        private bool? _cursorWasVisible;

        private CursorLockMode? _cursorLockMode;


        public DebugPanelRoot RootObject
        {
            get { return this._debugPanelRootObject; }
        }

        public bool IsLoaded
        {
            get { return this._debugPanelRootObject != null; }
        }

        public bool IsVisible
        {
            get { return this.IsLoaded && this._isVisible; }
            set
            {
                if (this._isVisible == value)
                {
                    return;
                }

                if (value)
                {
                    if (!this.IsLoaded)
                    {
                        this.Load();
                    }

                    SRDebuggerUtil.EnsureEventSystemExists();

                    this._debugPanelRootObject.CanvasGroup.alpha = 1.0f;
                    this._debugPanelRootObject.CanvasGroup.interactable = true;
                    this._debugPanelRootObject.CanvasGroup.blocksRaycasts = true;
                    this._cursorWasVisible = Cursor.visible;
                    this._cursorLockMode = Cursor.lockState;

                    foreach (var c in this._debugPanelRootObject.GetComponentsInChildren<Canvas>())
                    {
                        c.enabled = true;
                    }

                    if (Settings.Instance.AutomaticallyShowCursor)
                    {
                        Cursor.visible = true;
                        Cursor.lockState = CursorLockMode.None;
                    }
                }
                else
                {
                    if (this.IsLoaded)
                    {
                        this._debugPanelRootObject.CanvasGroup.alpha = 0.0f;
                        this._debugPanelRootObject.CanvasGroup.interactable = false;
                        this._debugPanelRootObject.CanvasGroup.blocksRaycasts = false;

                        foreach (var c in this._debugPanelRootObject.GetComponentsInChildren<Canvas>())
                        {
                            c.enabled = false;
                        }
                    }

                    if (this._cursorWasVisible.HasValue)
                    {
                        Cursor.visible = this._cursorWasVisible.Value;
                        this._cursorWasVisible = null;
                    }

                    if (this._cursorLockMode.HasValue)
                    {
                        Cursor.lockState = this._cursorLockMode.Value;
                        this._cursorLockMode = null;
                    }
                }

                this._isVisible = value;

                if (VisibilityChanged != null)
                {
                    VisibilityChanged(this, this._isVisible);
                }
            }
        }

        public DefaultTabs? ActiveTab
        {
            get
            {
                if (this._debugPanelRootObject == null)
                {
                    return null;
                }

                return this._debugPanelRootObject.TabController.ActiveTab;
            }
        }

        public void OpenTab(DefaultTabs tab)
        {
            if (!this.IsVisible)
            {
                this.IsVisible = true;
            }

            this._debugPanelRootObject.TabController.OpenTab(tab);
        }

        public void Unload()
        {
            if (this._debugPanelRootObject == null)
            {
                return;
            }

            this.IsVisible = false;

            this._debugPanelRootObject.CachedGameObject.SetActive(false);
            Destroy(this._debugPanelRootObject.CachedGameObject);

            this._debugPanelRootObject = null;
        }

        private void Load()
        {
            var prefab = Resources.Load<DebugPanelRoot>(SRDebugPaths.DebugPanelPrefabPath);

            if (prefab == null)
            {
                Debug.LogError("[SRDebugger] Error loading debug panel prefab");
                return;
            }

            this._debugPanelRootObject = SRInstantiate.Instantiate(prefab);
            this._debugPanelRootObject.name = "Panel";

            DontDestroyOnLoad(this._debugPanelRootObject);

            this._debugPanelRootObject.CachedTransform.SetParent(Hierarchy.Get("SRDebugger"), true);

            SRDebuggerUtil.EnsureEventSystemExists();
        }

        public void Dispose()
        {
            if (this._debugPanelRootObject != null)
            {
                DestroyImmediate(this._debugPanelRootObject.gameObject);
            }
        }
    }
}
