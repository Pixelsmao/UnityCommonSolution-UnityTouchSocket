namespace SRDebugger.Services.Implementation
{
    using Internal;
    using SRF;
    using SRF.Service;
    using System;
    using UI.Other;
    using UnityEngine;

    [Service(typeof(IDebugTriggerService))]
    public class DebugTriggerImpl : SRServiceBase<IDebugTriggerService>, IDebugTriggerService
    {
        private PinAlignment _position;
        private TriggerRoot _trigger;
        private IConsoleService _consoleService;
        private bool _showErrorNotification;

        public bool IsEnabled
        {
            get { return this._trigger != null && this._trigger.CachedGameObject.activeSelf; }
            set
            {
                // Create trigger if it does not yet exist
                if (value && this._trigger == null)
                {
                    this.CreateTrigger();
                }

                if (this._trigger != null)
                {
                    this._trigger.CachedGameObject.SetActive(value);
                }
            }
        }

        public bool ShowErrorNotification
        {
            get
            {
                return this._showErrorNotification;
            }
            set
            {
                if (this._showErrorNotification == value) return;

                this._showErrorNotification = value;

                if (this._trigger == null) return;

                if (this._showErrorNotification)
                {
                    this._consoleService = SRServiceManager.GetService<IConsoleService>();
                    this._consoleService.Error += this.OnError;
                }
                else
                {
                    this._consoleService.Error -= this.OnError;
                    this._consoleService = null;
                }
            }
        }

        public PinAlignment Position
        {
            get { return this._position; }
            set
            {
                if (this._trigger != null)
                {
                    SetTriggerPosition(this._trigger.TriggerTransform, value);
                }

                this._position = value;
            }
        }

        protected override void Awake()
        {
            base.Awake();
            DontDestroyOnLoad(this.CachedGameObject);

            this.CachedTransform.SetParent(Hierarchy.Get("SRDebugger"), true);
            this.ShowErrorNotification = Settings.Instance.ErrorNotification;

            this.name = "Trigger";
        }

        private void OnError(IConsoleService console)
        {
            if (this._trigger != null)
            {
                this._trigger.ErrorNotifier.ShowErrorWarning();
            }
        }

        private void CreateTrigger()
        {
            var prefab = Resources.Load<TriggerRoot>(SRDebugPaths.TriggerPrefabPath);

            if (prefab == null)
            {
                Debug.LogError("[SRDebugger] Error loading trigger prefab");
                return;
            }

            this._trigger = SRInstantiate.Instantiate(prefab);
            this._trigger.CachedTransform.SetParent(this.CachedTransform, true);

            SetTriggerPosition(this._trigger.TriggerTransform, this._position);

            switch (Settings.Instance.TriggerBehaviour)
            {
                case Settings.TriggerBehaviours.TripleTap:
                    {
                        this._trigger.TripleTapButton.onClick.AddListener(this.OnTriggerButtonClick);
                        this._trigger.TapHoldButton.gameObject.SetActive(false);

                        break;
                    }

                case Settings.TriggerBehaviours.TapAndHold:
                    {
                        this._trigger.TapHoldButton.onLongPress.AddListener(this.OnTriggerButtonClick);
                        this._trigger.TripleTapButton.gameObject.SetActive(false);

                        break;
                    }

                case Settings.TriggerBehaviours.DoubleTap:
                    {
                        this._trigger.TripleTapButton.RequiredTapCount = 2;
                        this._trigger.TripleTapButton.onClick.AddListener(this.OnTriggerButtonClick);
                        this._trigger.TapHoldButton.gameObject.SetActive(false);

                        break;
                    }

                default:
                    throw new Exception("Unhandled TriggerBehaviour");
            }

            SRDebuggerUtil.EnsureEventSystemExists();

            UnityEngine.SceneManagement.SceneManager.activeSceneChanged += OnActiveSceneChanged;

            if (this._showErrorNotification)
            {
                this._consoleService = SRServiceManager.GetService<IConsoleService>();
                this._consoleService.Error += this.OnError;
            }
        }

        protected override void OnDestroy()
        {
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged -= OnActiveSceneChanged;

            if (this._consoleService != null)
            {
                this._consoleService.Error -= this.OnError;
            }

            base.OnDestroy();
        }

        private static void OnActiveSceneChanged(UnityEngine.SceneManagement.Scene s1, UnityEngine.SceneManagement.Scene s2)
        {
            SRDebuggerUtil.EnsureEventSystemExists();
        }

        private void OnTriggerButtonClick()
        {
            if (this._trigger.ErrorNotifier.IsVisible)
            {
                // Open into console if there is an error.
                SRDebug.Instance.ShowDebugPanel(DefaultTabs.Console);
            }
            else
            {
                SRDebug.Instance.ShowDebugPanel();
            }
        }

        private static void SetTriggerPosition(RectTransform t, PinAlignment position)
        {
            var pivotX = 0f;
            var pivotY = 0f;

            var posX = 0f;
            var posY = 0f;

            if (position == PinAlignment.TopLeft || position == PinAlignment.TopRight || position == PinAlignment.TopCenter)
            {
                pivotY = 1f;
                posY = 1f;
            }
            else if (position == PinAlignment.BottomLeft || position == PinAlignment.BottomRight || position == PinAlignment.BottomCenter)
            {
                pivotY = 0f;
                posY = 0f;
            }
            else if (position == PinAlignment.CenterLeft || position == PinAlignment.CenterRight)
            {
                pivotY = 0.5f;
                posY = 0.5f;
            }

            if (position == PinAlignment.TopLeft || position == PinAlignment.BottomLeft || position == PinAlignment.CenterLeft)
            {
                pivotX = 0f;
                posX = 0f;
            }
            else if (position == PinAlignment.TopRight || position == PinAlignment.BottomRight || position == PinAlignment.CenterRight)
            {
                pivotX = 1f;
                posX = 1f;
            }
            else if (position == PinAlignment.TopCenter || position == PinAlignment.BottomCenter)
            {
                pivotX = 0.5f;
                posX = 0.5f;
            }

            t.pivot = new Vector2(pivotX, pivotY);
            t.anchorMax = t.anchorMin = new Vector2(posX, posY);
        }
    }
}