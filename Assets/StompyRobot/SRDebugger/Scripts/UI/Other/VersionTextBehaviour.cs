namespace SRDebugger.UI.Other
{
    using SRF;
    using UnityEngine.UI;

    public class VersionTextBehaviour : SRMonoBehaviourEx
    {
        public string Format = "SRDebugger {0}";

        [RequiredField] public Text Text;

        protected override void Start()
        {
            base.Start();

            this.Text.text = string.Format(this.Format, SRDebug.Version);
        }
    }
}
