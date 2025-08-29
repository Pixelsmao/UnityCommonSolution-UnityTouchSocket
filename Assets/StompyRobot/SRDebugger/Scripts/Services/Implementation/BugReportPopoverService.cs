namespace SRDebugger.Services.Implementation
{
    using Internal;
    using SRF;
    using SRF.Service;
    using System;
    using System.Collections;
    using UI.Other;
    using UnityEngine;

    [Service(typeof(BugReportPopoverService))]
    public class BugReportPopoverService : SRServiceBase<BugReportPopoverService>
    {
        private BugReportCompleteCallback _callback;
        private bool _isVisible;
        private BugReportPopoverRoot _popover;
        private BugReportSheetController _sheet;

        public bool IsShowingPopover
        {
            get { return this._isVisible; }
        }

        public void ShowBugReporter(BugReportCompleteCallback callback, bool takeScreenshotFirst = true,
            string descriptionText = null)
        {
            if (this._isVisible)
            {
                throw new InvalidOperationException("Bug report popover is already visible.");
            }

            if (this._popover == null)
            {
                this.Load();
            }

            if (this._popover == null)
            {
                Debug.LogWarning("[SRDebugger] Bug report popover failed loading, executing callback with fail result");
                callback(false, "Resource load failed");
                return;
            }

            this._callback = callback;

            this._isVisible = true;
            SRDebuggerUtil.EnsureEventSystemExists();

            this.StartCoroutine(this.OpenCo(takeScreenshotFirst, descriptionText));
        }

        private IEnumerator OpenCo(bool takeScreenshot, string descriptionText)
        {
            if (takeScreenshot)
            {
                // Wait for screenshot to be captured
                yield return this.StartCoroutine(BugReportScreenshotUtil.ScreenshotCaptureCo());
            }
            this._popover.CachedGameObject.SetActive(true);

            yield return new WaitForEndOfFrame();

            if (!string.IsNullOrEmpty(descriptionText))
            {
                this._sheet.DescriptionField.text = descriptionText;
            }
        }

        private void SubmitComplete(bool didSucceed, string errorMessage)
        {
            this.OnComplete(didSucceed, errorMessage, false);
        }

        private void CancelPressed()
        {
            this.OnComplete(false, "User Cancelled", true);
        }

        private void OnComplete(bool success, string errorMessage, bool close)
        {
            if (!this._isVisible)
            {
                Debug.LogWarning("[SRDebugger] Received callback at unexpected time. ???");
                return;
            }

            if (!success && !close)
            {
                return;
            }

            this._isVisible = false;

            // Destroy it all so it doesn't linger in the scene using memory
            this._popover.gameObject.SetActive(false);
            Destroy(this._popover.gameObject);

            this._popover = null;
            this._sheet = null;

            BugReportScreenshotUtil.ScreenshotData = null;

            this._callback(success, errorMessage);
        }

        private void TakingScreenshot()
        {
            if (!this.IsShowingPopover)
            {
                Debug.LogWarning("[SRDebugger] Received callback at unexpected time. ???");
                return;
            }

            this._popover.CanvasGroup.alpha = 0f;
        }

        private void ScreenshotComplete()
        {
            if (!this.IsShowingPopover)
            {
                Debug.LogWarning("[SRDebugger] Received callback at unexpected time. ???");
                return;
            }

            this._popover.CanvasGroup.alpha = 1f;
        }

        protected override void Awake()
        {
            base.Awake();

            this.CachedTransform.SetParent(Hierarchy.Get("SRDebugger"));
        }

        private void Load()
        {
            var popoverPrefab = Resources.Load<BugReportPopoverRoot>(SRDebugPaths.BugReportPopoverPath);
            var sheetPrefab = Resources.Load<BugReportSheetController>(SRDebugPaths.BugReportSheetPath);

            if (popoverPrefab == null)
            {
                Debug.LogError("[SRDebugger] Unable to load bug report popover prefab");
                return;
            }

            if (sheetPrefab == null)
            {
                Debug.LogError("[SRDebugger] Unable to load bug report sheet prefab");
                return;
            }

            this._popover = SRInstantiate.Instantiate(popoverPrefab);
            this._popover.CachedTransform.SetParent(this.CachedTransform, false);

            this._sheet = SRInstantiate.Instantiate(sheetPrefab);
            this._sheet.CachedTransform.SetParent(this._popover.Container, false);

            this._sheet.SubmitComplete = this.SubmitComplete;
            this._sheet.CancelPressed = this.CancelPressed;

            this._sheet.TakingScreenshot = this.TakingScreenshot;
            this._sheet.ScreenshotComplete = this.ScreenshotComplete;

            this._popover.CachedGameObject.SetActive(false);
        }
    }
}
