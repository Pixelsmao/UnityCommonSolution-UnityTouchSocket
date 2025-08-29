namespace SRDebugger.UI.Other
{
    using Controls;
    using SRF;
    using System;
    using UnityEngine;
    using UnityEngine.Serialization;

    public class SRTab : SRMonoBehaviourEx
    {
        /// <summary>
        /// Content that will be added to the content area of the header
        /// </summary>
        public RectTransform HeaderExtraContent;

        [Obsolete][HideInInspector] public Sprite Icon;

        /// <summary>
        /// Content that will be added to the content area of the tab button
        /// </summary>
        public RectTransform IconExtraContent;

        public string IconStyleKey = "Icon_Stompy";
        public int SortIndex;

        [HideInInspector] public SRTabButton TabButton;

        public string Title
        {
            get { return this._title; }
        }

        public string LongTitle
        {
            get { return !string.IsNullOrEmpty(this._longTitle) ? this._longTitle : this._title; }
        }

        public string Key
        {
            get { return this._key; }
        }
#pragma warning disable 649

        [SerializeField][FormerlySerializedAs("Title")] private string _title;

        [SerializeField] private string _longTitle;

        [SerializeField] private string _key;

#pragma warning restore 649
    }
}
