
#pragma warning disable 169
#pragma warning disable 649

namespace SRDebugger.UI.Controls
{
    using Internal;
    using Services;
    using SRF;
    using SRF.UI.Layout;
    using System;
    using System.Collections;
    using UnityEngine;
    using UnityEngine.UI;

    public class ConsoleLogControl : SRMonoBehaviourEx
    {
        [RequiredField][SerializeField] private VirtualVerticalLayoutGroup _consoleScrollLayoutGroup;

        [RequiredField][SerializeField] private ScrollRect _consoleScrollRect;

        private bool _isDirty;
        private Vector2? _scrollPosition;
        private bool _showErrors = true;
        private bool _showInfo = true;
        private bool _showWarnings = true;
        public Action<ConsoleEntry> SelectedItemChanged;
        private string _filter;

        public bool ShowErrors
        {
            get { return this._showErrors; }
            set
            {
                this._showErrors = value;
                this.SetIsDirty();
            }
        }

        public bool ShowWarnings
        {
            get { return this._showWarnings; }
            set
            {
                this._showWarnings = value;
                this.SetIsDirty();
            }
        }

        public bool ShowInfo
        {
            get { return this._showInfo; }
            set
            {
                this._showInfo = value;
                this.SetIsDirty();
            }
        }

        public bool EnableSelection
        {
            get { return this._consoleScrollLayoutGroup.EnableSelection; }
            set { this._consoleScrollLayoutGroup.EnableSelection = value; }
        }

        public string Filter
        {
            get { return this._filter; }
            set
            {
                if (this._filter != value)
                {
                    this._filter = value;
                    this._isDirty = true;
                }
            }
        }

        protected override void Awake()
        {
            base.Awake();

            this._consoleScrollLayoutGroup.SelectedItemChanged.AddListener(this.OnSelectedItemChanged);
            Service.Console.Updated += this.ConsoleOnUpdated;
        }

        protected override void Start()
        {
            base.Start();
            this.SetIsDirty();
            this.StartCoroutine(this.ScrollToBottom());
        }

        private IEnumerator ScrollToBottom()
        {
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();
            this._scrollPosition = new Vector2(0, 0);
        }

        protected override void OnDestroy()
        {
            if (Service.Console != null)
            {
                Service.Console.Updated -= this.ConsoleOnUpdated;
            }

            base.OnDestroy();
        }

        private void OnSelectedItemChanged(object arg0)
        {
            var entry = arg0 as ConsoleEntry;

            if (this.SelectedItemChanged != null)
            {
                this.SelectedItemChanged(entry);
            }
        }

        protected override void Update()
        {
            base.Update();

            if (this._scrollPosition.HasValue)
            {
                this._consoleScrollRect.normalizedPosition = this._scrollPosition.Value;
                this._scrollPosition = null;
            }

            if (this._isDirty)
            {
                this.Refresh();
            }
        }

        private void Refresh()
        {
            if (this._consoleScrollRect.normalizedPosition.y < 0.01f)
            {
                this._scrollPosition = this._consoleScrollRect.normalizedPosition;
            }

            this._consoleScrollLayoutGroup.ClearItems();

            var entries = Service.Console.Entries;

            for (var i = 0; i < entries.Count; i++)
            {
                var e = entries[i];

                if ((e.LogType == LogType.Error || e.LogType == LogType.Exception || e.LogType == LogType.Assert) &&
                    !this.ShowErrors)
                {
                    if (e == this._consoleScrollLayoutGroup.SelectedItem) this._consoleScrollLayoutGroup.SelectedItem = null;
                    continue;
                }

                if (e.LogType == LogType.Warning && !this.ShowWarnings)
                {
                    if (e == this._consoleScrollLayoutGroup.SelectedItem) this._consoleScrollLayoutGroup.SelectedItem = null;
                    continue;
                }

                if (e.LogType == LogType.Log && !this.ShowInfo)
                {
                    if (e == this._consoleScrollLayoutGroup.SelectedItem) this._consoleScrollLayoutGroup.SelectedItem = null;
                    continue;
                }

                if (!string.IsNullOrEmpty(this.Filter))
                {
                    if (e.Message.IndexOf(this.Filter, StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        if (e == this._consoleScrollLayoutGroup.SelectedItem) this._consoleScrollLayoutGroup.SelectedItem = null;
                        continue;
                    }
                }

                this._consoleScrollLayoutGroup.AddItem(e);
            }

            this._isDirty = false;
        }

        private void SetIsDirty()
        {
            this._isDirty = true;
        }

        private void ConsoleOnUpdated(IConsoleService console)
        {
            this.SetIsDirty();
        }
    }
}
