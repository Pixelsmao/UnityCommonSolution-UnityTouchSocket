namespace SRDebugger.UI.Other
{
    using Controls;
    using SRF;
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UI;

    public class SRTabController : SRMonoBehaviourEx
    {
        private readonly SRList<SRTab> _tabs = new SRList<SRTab>();
        private SRTab _activeTab;

        [RequiredField] public RectTransform TabButtonContainer;

        [RequiredField] public SRTabButton TabButtonPrefab;

        [RequiredField] public RectTransform TabContentsContainer;

        [RequiredField] public RectTransform TabHeaderContentContainer;

        [RequiredField] public Text TabHeaderText;

        public SRTab ActiveTab
        {
            get { return this._activeTab; }
            set { this.MakeActive(value); }
        }

        public IList<SRTab> Tabs
        {
            get { return this._tabs.AsReadOnly(); }
        }

        public event Action<SRTabController, SRTab> ActiveTabChanged;

        public void AddTab(SRTab tab, bool visibleInSidebar = true)
        {
            tab.CachedTransform.SetParent(this.TabContentsContainer, false);
            tab.CachedGameObject.SetActive(false);

            if (visibleInSidebar)
            {
                // Create a tab button for this tab
                var button = SRInstantiate.Instantiate(this.TabButtonPrefab);
                button.CachedTransform.SetParent(this.TabButtonContainer, false);
                button.TitleText.text = tab.Title.ToUpper();

                if (tab.IconExtraContent != null)
                {
                    var extraContent = SRInstantiate.Instantiate(tab.IconExtraContent);
                    extraContent.SetParent(button.ExtraContentContainer, false);
                }

                button.IconStyleComponent.StyleKey = tab.IconStyleKey;
                button.IsActive = false;

                button.Button.onClick.AddListener(() => this.MakeActive(tab));

                tab.TabButton = button;
            }

            this._tabs.Add(tab);
            this.SortTabs();

            if (this._tabs.Count == 1)
            {
                this.ActiveTab = tab;
            }
        }

        private void MakeActive(SRTab tab)
        {
            if (!this._tabs.Contains(tab))
            {
                throw new ArgumentException("tab is not a member of this tab controller", "tab");
            }

            if (this._activeTab != null)
            {
                this._activeTab.CachedGameObject.SetActive(false);

                if (this._activeTab.TabButton != null)
                {
                    this._activeTab.TabButton.IsActive = false;
                }

                if (this._activeTab.HeaderExtraContent != null)
                {
                    this._activeTab.HeaderExtraContent.gameObject.SetActive(false);
                }
            }

            this._activeTab = tab;

            if (this._activeTab != null)
            {
                this._activeTab.CachedGameObject.SetActive(true);
                this.TabHeaderText.text = this._activeTab.LongTitle;

                if (this._activeTab.TabButton != null)
                {
                    this._activeTab.TabButton.IsActive = true;
                }

                if (this._activeTab.HeaderExtraContent != null)
                {
                    this._activeTab.HeaderExtraContent.SetParent(this.TabHeaderContentContainer, false);
                    this._activeTab.HeaderExtraContent.gameObject.SetActive(true);
                }
            }

            if (ActiveTabChanged != null)
            {
                ActiveTabChanged(this, this._activeTab);
            }
        }

        private void SortTabs()
        {
            this._tabs.Sort((t1, t2) => t1.SortIndex.CompareTo(t2.SortIndex));

            for (var i = 0; i < this._tabs.Count; i++)
            {
                if (this._tabs[i].TabButton != null)
                {
                    this._tabs[i].TabButton.CachedTransform.SetSiblingIndex(i);
                }
            }
        }
    }
}
