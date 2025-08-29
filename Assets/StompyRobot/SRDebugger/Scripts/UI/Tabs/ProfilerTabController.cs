//#define SR_CONSOLE_DEBUG

namespace SRDebugger.UI.Tabs
{
    using SRF;
    using UnityEngine.UI;

    public class ProfilerTabController : SRMonoBehaviourEx
    {
        private bool _isDirty;

        [RequiredField] public Toggle PinToggle;

        protected override void Start()
        {
            base.Start();

            this.PinToggle.onValueChanged.AddListener(this.PinToggleValueChanged);
            this.Refresh();
        }

        private void PinToggleValueChanged(bool isOn)
        {
            SRDebug.Instance.IsProfilerDocked = isOn;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            this._isDirty = true;
        }

        protected override void Update()
        {
            base.Update();

            if (this._isDirty)
            {
                this.Refresh();
            }
        }

        private void Refresh()
        {
            this.PinToggle.isOn = SRDebug.Instance.IsProfilerDocked;
            this._isDirty = false;
        }
    }
}
