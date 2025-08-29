namespace SRDebugger.UI.Controls
{
    using SRF;
    using UnityEngine.UI;

    public abstract class OptionsControlBase : SRMonoBehaviourEx
    {
        private bool _selectionModeEnabled;

        [RequiredField] public Toggle SelectionModeToggle;

        public OptionDefinition Option;

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

                this.SelectionModeToggle.gameObject.SetActive(this._selectionModeEnabled);

                if (this.SelectionModeToggle.graphic != null)
                {
                    this.SelectionModeToggle.graphic.CrossFadeAlpha(this.IsSelected ? this._selectionModeEnabled ? 1.0f : 0.2f : 0f, 0,
                        true);
                }
            }
        }

        public bool IsSelected
        {
            get { return this.SelectionModeToggle.isOn; }
            set
            {
                this.SelectionModeToggle.isOn = value;

                if (this.SelectionModeToggle.graphic != null)
                {
                    this.SelectionModeToggle.graphic.CrossFadeAlpha(value ? this._selectionModeEnabled ? 1.0f : 0.2f : 0f, 0, true);
                }
            }
        }

        protected override void Awake()
        {
            base.Awake();

            this.IsSelected = false;
            this.SelectionModeToggle.gameObject.SetActive(false);
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            // Reapply selection indicator alpha (is reset when disabled / reenabled)
            if (this.SelectionModeToggle.graphic != null)
            {
                this.SelectionModeToggle.graphic.CrossFadeAlpha(this.IsSelected ? this._selectionModeEnabled ? 1.0f : 0.2f : 0f, 0,
                    true);
            }
        }

        public virtual void Refresh() { }
    }
}
