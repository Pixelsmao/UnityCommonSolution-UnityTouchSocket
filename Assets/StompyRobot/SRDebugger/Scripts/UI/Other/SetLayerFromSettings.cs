namespace SRDebugger.UI.Other
{
    using SRF;

    public class SetLayerFromSettings : SRMonoBehaviour
    {
        private void Start()
        {
            this.gameObject.SetLayerRecursive(Settings.Instance.DebugLayer);
        }
    }
}
