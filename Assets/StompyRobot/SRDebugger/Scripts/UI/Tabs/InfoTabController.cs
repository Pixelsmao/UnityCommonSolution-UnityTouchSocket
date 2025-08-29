using SRF.UI;

namespace SRDebugger.UI.Tabs
{
    using Controls;
    using Services;
    using SRF;
    using SRF.Service;
    using System.Collections.Generic;
    using System.Text;
    using UnityEngine;

    public class InfoTabController : SRMonoBehaviourEx
    {
        public const char Tick = '\u2713';
        public const char Cross = '\u00D7';
        public const string NameColor = "#BCBCBC";
        private readonly Dictionary<string, InfoBlock> _infoBlocks = new Dictionary<string, InfoBlock>();

        [RequiredField] public InfoBlock InfoBlockPrefab;

        [RequiredField] public RectTransform LayoutContainer;

        [RequiredField] public FlashGraphic ToggleButton;

        private bool _updateEveryFrame;

        protected override void OnEnable()
        {
            base.OnEnable();
            this.InternalRefresh();

            if (this._updateEveryFrame)
            {
                this.ToggleButton.FlashAndHoldUntilNextPress();
            }
        }

        public void Refresh()
        {
            this.ToggleButton.Flash(); // flash to disable any "press and hold" that is going on
            this._updateEveryFrame = false;
            this.InternalRefresh();
        }

        protected override void Update()
        {
            if (this._updateEveryFrame)
            {
                this.InternalRefresh();
            }
        }

        public void ActivateRefreshEveryFrame()
        {
            this.ToggleButton.FlashAndHoldUntilNextPress();
            this._updateEveryFrame = true;
            this.InternalRefresh();
        }

        private void InternalRefresh()
        {
            var s = SRServiceManager.GetService<ISystemInformationService>();

            foreach (var category in s.GetCategories())
            {
                if (!this._infoBlocks.ContainsKey(category))
                {
                    var block = this.CreateBlock(category);
                    this._infoBlocks.Add(category, block);
                }
            }

            foreach (var kv in this._infoBlocks)
            {
                this.FillInfoBlock(kv.Value, s.GetInfo(kv.Key));
            }
        }

        private void FillInfoBlock(InfoBlock block, IList<InfoEntry> info)
        {
            var sb = new StringBuilder();

            var maxTitleLength = 0;

            foreach (var systemInfo in info)
            {
                if (systemInfo.Title.Length > maxTitleLength)
                {
                    maxTitleLength = systemInfo.Title.Length;
                }
            }

            maxTitleLength += 2;

            var first = true;
            foreach (var i in info)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    sb.AppendLine();
                }

                sb.Append("<color=");
                sb.Append(NameColor);
                sb.Append(">");

                sb.Append(i.Title);
                sb.Append(": ");

                sb.Append("</color>");

                for (var j = i.Title.Length; j <= maxTitleLength; ++j)
                {
                    sb.Append(' ');
                }

                if (i.Value is bool)
                {
                    sb.Append((bool)i.Value ? Tick : Cross);
                }
                else
                {
                    sb.Append(i.Value);
                }
            }

            block.Content.text = sb.ToString();
        }

        private InfoBlock CreateBlock(string title)
        {
            var block = SRInstantiate.Instantiate(this.InfoBlockPrefab);
            block.Title.text = title;

            block.CachedTransform.SetParent(this.LayoutContainer, false);

            return block;
        }
    }
}
