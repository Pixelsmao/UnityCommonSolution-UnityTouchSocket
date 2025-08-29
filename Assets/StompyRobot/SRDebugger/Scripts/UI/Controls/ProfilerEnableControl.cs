namespace SRDebugger.UI.Controls
{
    using Internal;
    using SRF;
    using UnityEngine;
    using UnityEngine.UI;
#if UNITY_5_5_OR_NEWER
    using UnityEngine.Profiling;
#endif

    public class ProfilerEnableControl : SRMonoBehaviourEx
    {
        private bool _previousState;
        [RequiredField] public Text ButtonText;
        [RequiredField] public UnityEngine.UI.Button EnableButton;
        [RequiredField] public Text Text;

        protected override void Start()
        {
            base.Start();

            if (!Profiler.supported)
            {
                this.Text.text = SRDebugStrings.Current.Profiler_NotSupported;
                this.EnableButton.gameObject.SetActive(false);
                this.enabled = false;
                return;
            }

            if (!Application.HasProLicense())
            {
                this.Text.text = SRDebugStrings.Current.Profiler_NoProInfo;
                this.EnableButton.gameObject.SetActive(false);
                this.enabled = false;
                return;
            }

            this.UpdateLabels();
        }

        protected void UpdateLabels()
        {
            if (!Profiler.enabled)
            {
                this.Text.text = SRDebugStrings.Current.Profiler_EnableProfilerInfo;
                this.ButtonText.text = "Enable";
            }
            else
            {
                this.Text.text = SRDebugStrings.Current.Profiler_DisableProfilerInfo;
                this.ButtonText.text = "Disable";
            }

            this._previousState = Profiler.enabled;
        }

        protected override void Update()
        {
            base.Update();

            if (Profiler.enabled != this._previousState)
            {
                this.UpdateLabels();
            }
        }

        public void ToggleProfiler()
        {
            Debug.Log("Toggle Profiler");
            Profiler.enabled = !Profiler.enabled;
        }
    }
}
