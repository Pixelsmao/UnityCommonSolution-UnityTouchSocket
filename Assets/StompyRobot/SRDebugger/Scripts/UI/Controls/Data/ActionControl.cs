namespace SRDebugger.UI.Controls.Data
{
    using SRF;
    using System;
    using UnityEngine;
    using UnityEngine.UI;

    public class ActionControl : OptionsControlBase
    {
        private SRF.Helpers.MethodReference _method;

        [RequiredField] public UnityEngine.UI.Button Button;

        [RequiredField] public Text Title;

        public SRF.Helpers.MethodReference Method
        {
            get { return this._method; }
        }

        protected override void Start()
        {
            base.Start();
            this.Button.onClick.AddListener(this.ButtonOnClick);
        }

        private void ButtonOnClick()
        {
            if (this._method == null)
            {
                Debug.LogWarning("[SRDebugger.Options] No method set for action control", this);
                return;
            }

            try
            {
                this._method.Invoke(null);
            }
            catch (Exception e)
            {
                Debug.LogError("[SRDebugger] Exception thrown while executing action.");
                Debug.LogException(e);
            }
        }

        public void SetMethod(string methodName, SRF.Helpers.MethodReference method)
        {
            this._method = method;
            this.Title.text = methodName;
        }
    }
}
