namespace SRDebugger.Scripts
{
    using SRF;
    using System;
    using System.Linq;
    using UI.Other;
    using UnityEngine;

    public class DebuggerTabController : SRMonoBehaviourEx
    {
        private SRTab _aboutTabInstance;
        private DefaultTabs? _activeTab;
        private bool _hasStarted;
        public SRTab AboutTab;

        [RequiredField] public SRTabController TabController;

        public DefaultTabs? ActiveTab
        {
            get
            {
                var key = this.TabController.ActiveTab.Key;

                if (string.IsNullOrEmpty(key))
                {
                    return null;
                }

                var t = Enum.Parse(typeof(DefaultTabs), key);

                if (!Enum.IsDefined(typeof(DefaultTabs), t))
                {
                    return null;
                }

                return (DefaultTabs)t;
            }
        }

        protected override void Start()
        {
            base.Start();

            this._hasStarted = true;

            // Loads all available tabs from resources
            var tabs = Resources.LoadAll<SRTab>("SRDebugger/UI/Prefabs/Tabs");
            var defaultTabs = Enum.GetNames(typeof(DefaultTabs));

            foreach (var srTab in tabs)
            {
                var enabler = srTab.GetComponent(typeof(IEnableTab)) as IEnableTab;

                if (enabler != null && !enabler.IsEnabled)
                {
                    continue;
                }

                if (defaultTabs.Contains(srTab.Key))
                {
                    var tabValue = Enum.Parse(typeof(DefaultTabs), srTab.Key);

                    if (Enum.IsDefined(typeof(DefaultTabs), tabValue) &&
                        Settings.Instance.DisabledTabs.Contains((DefaultTabs)tabValue))
                    {
                        continue;
                    }
                }

                this.TabController.AddTab(SRInstantiate.Instantiate(srTab));
            }

            // Add about tab (has no button, accessed via "Stompy" logo
            if (this.AboutTab != null)
            {
                this._aboutTabInstance = SRInstantiate.Instantiate(this.AboutTab);
                this.TabController.AddTab(this._aboutTabInstance, false);
            }

            // Open active tab (set before panel loaded), or default tab from settings
            var defaultTab = this._activeTab ?? Settings.Instance.DefaultTab;

            if (!this.OpenTab(defaultTab))
            {
                this.TabController.ActiveTab = this.TabController.Tabs.FirstOrDefault();
            }
        }

        public bool OpenTab(DefaultTabs tab)
        {
            if (!this._hasStarted)
            {
                this._activeTab = tab;
                return true;
            }

            var tabName = tab.ToString();

            foreach (var t in this.TabController.Tabs)
            {
                if (t.Key == tabName)
                {
                    this.TabController.ActiveTab = t;
                    return true;
                }
            }

            return false;
        }

        public void ShowAboutTab()
        {
            if (this._aboutTabInstance != null)
            {
                this.TabController.ActiveTab = this._aboutTabInstance;
            }
        }
    }
}
