using SRF.Service;

namespace SRDebugger.UI.Tabs
{
    using Other;
    using Services;
    using SRF;
    using UnityEngine;

    public class BugReportTabController : SRMonoBehaviourEx, IEnableTab
    {
        [RequiredField] public BugReportSheetController BugReportSheetPrefab;

        [RequiredField] public RectTransform Container;

        public bool IsEnabled
        {
            get { return SRServiceManager.GetService<IBugReportService>().IsUsable; }
        }

        protected override void Start()
        {
            base.Start();

            var sheet = SRInstantiate.Instantiate(this.BugReportSheetPrefab);
            sheet.IsCancelButtonEnabled = false;

            // Callbacks when taking screenshot will hide the debug panel so it is not present in the image
            sheet.TakingScreenshot = this.TakingScreenshot;
            sheet.ScreenshotComplete = this.ScreenshotComplete;

            sheet.CachedTransform.SetParent(this.Container, false);
        }

        private void TakingScreenshot()
        {
            SRDebug.Instance.HideDebugPanel();
        }

        private void ScreenshotComplete()
        {
            SRDebug.Instance.ShowDebugPanel(false);
        }
    }
}
