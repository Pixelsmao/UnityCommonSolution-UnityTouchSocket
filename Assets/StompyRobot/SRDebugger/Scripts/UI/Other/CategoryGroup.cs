namespace SRDebugger.UI.Other
{
    using SRF;
    using UnityEngine;
    using UnityEngine.UI;

    public class CategoryGroup : SRMonoBehaviourEx
    {
        [RequiredField] public RectTransform Container;
        [RequiredField] public Text Header;
        [RequiredField] public GameObject Background;
        [RequiredField] public Toggle SelectionToggle;

        public GameObject[] EnabledDuringSelectionMode = new GameObject[0];

        private bool _selectionModeEnabled = true;

        public bool IsSelected
        {
            get
            {
                return this.SelectionToggle.isOn;
            }
            set
            {
                this.SelectionToggle.isOn = value;

                if (this.SelectionToggle.graphic != null)
                {
                    this.SelectionToggle.graphic.CrossFadeAlpha(value ? this._selectionModeEnabled ? 1.0f : 0.2f : 0f, 0, true);
                }
            }
        }

        public bool SelectionModeEnabled
        {
            get { return this._selectionModeEnabled; }

            set
            {
                if (value == this._selectionModeEnabled)
                {
                    return;
                }

                this._selectionModeEnabled = value;

                for (var i = 0; i < this.EnabledDuringSelectionMode.Length; i++)
                {
                    this.EnabledDuringSelectionMode[i].SetActive(this._selectionModeEnabled);
                }
            }
        }

    }
}
