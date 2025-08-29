
#if NETFX_CORE
using UnityEngine.Windows;
#endif

namespace SRDebugger.UI.Other
{
    using Internal;
    using Services;
    using SRF;
    using SRF.Service;
    using System;
    using System.Collections;
    using System.Linq;
    using UnityEngine;
    using UnityEngine.EventSystems;
    using UnityEngine.UI;

    public class BugReportSheetController : SRMonoBehaviourEx
    {
        [RequiredField] public GameObject ButtonContainer;

        [RequiredField] public Text ButtonText;

        [RequiredField] public UnityEngine.UI.Button CancelButton;

        public Action CancelPressed;

        [RequiredField] public InputField DescriptionField;

        [RequiredField] public InputField EmailField;

        [RequiredField] public Slider ProgressBar;

        [RequiredField] public Text ResultMessageText;

        public Action ScreenshotComplete;

        [RequiredField] public UnityEngine.UI.Button SubmitButton;

        public Action<bool, string> SubmitComplete;
        public Action TakingScreenshot;

        public bool IsCancelButtonEnabled
        {
            get { return this.CancelButton.gameObject.activeSelf; }
            set { this.CancelButton.gameObject.SetActive(value); }
        }

        protected override void Start()
        {
            base.Start();

            this.SetLoadingSpinnerVisible(false);
            this.ClearErrorMessage();
            this.ClearForm();
        }

        public void Submit()
        {
            EventSystem.current.SetSelectedGameObject(null);

            this.ProgressBar.value = 0;
            this.ClearErrorMessage();
            this.SetLoadingSpinnerVisible(true);
            this.SetFormEnabled(false);

            if (!string.IsNullOrEmpty(this.EmailField.text))
            {
                this.SetDefaultEmailFieldContents(this.EmailField.text);
            }

            this.StartCoroutine(this.SubmitCo());
        }

        public void Cancel()
        {
            if (this.CancelPressed != null)
            {
                this.CancelPressed();
            }
        }

        private IEnumerator SubmitCo()
        {
            if (BugReportScreenshotUtil.ScreenshotData == null && Settings.Instance.EnableBugReportScreenshot)
            {
                if (this.TakingScreenshot != null)
                {
                    this.TakingScreenshot();
                }

                yield return new WaitForEndOfFrame();

                yield return this.StartCoroutine(BugReportScreenshotUtil.ScreenshotCaptureCo());

                if (this.ScreenshotComplete != null)
                {
                    this.ScreenshotComplete();
                }
            }

            var s = SRServiceManager.GetService<IBugReportService>();

            var r = new BugReport();

            r.Email = this.EmailField.text;
            r.UserDescription = this.DescriptionField.text;

            r.ConsoleLog = Service.Console.AllEntries.ToList();
            r.SystemInformation = SRServiceManager.GetService<ISystemInformationService>().CreateReport();
            r.ScreenshotData = BugReportScreenshotUtil.ScreenshotData;

            BugReportScreenshotUtil.ScreenshotData = null;

            s.SendBugReport(r, this.OnBugReportComplete, new Progress<float>(this.OnBugReportProgress));
        }

        private void OnBugReportProgress(float progress)
        {
            this.ProgressBar.value = progress;
        }

        private void OnBugReportComplete(bool didSucceed, string errorMessage)
        {
            if (!didSucceed)
            {
                this.ShowErrorMessage("Error sending bug report", errorMessage);
            }
            else
            {
                this.ClearForm();
                this.ShowErrorMessage("Bug report submitted successfully");
            }

            this.SetLoadingSpinnerVisible(false);
            this.SetFormEnabled(true);

            if (this.SubmitComplete != null)
            {
                this.SubmitComplete(didSucceed, errorMessage);
            }
        }

        protected void SetLoadingSpinnerVisible(bool visible)
        {
            this.ProgressBar.gameObject.SetActive(visible);
            this.ButtonContainer.SetActive(!visible);
        }

        protected void ClearForm()
        {
            this.EmailField.text = this.GetDefaultEmailFieldContents();
            this.DescriptionField.text = "";
        }

        protected void ShowErrorMessage(string userMessage, string serverMessage = null)
        {
            var txt = userMessage;

            if (!string.IsNullOrEmpty(serverMessage))
            {
                txt += " (<b>{0}</b>)".Fmt(serverMessage);
            }

            this.ResultMessageText.text = txt;
            this.ResultMessageText.gameObject.SetActive(true);
        }

        protected void ClearErrorMessage()
        {
            this.ResultMessageText.text = "";
            this.ResultMessageText.gameObject.SetActive(false);
        }

        protected void SetFormEnabled(bool e)
        {
            this.SubmitButton.interactable = e;
            this.CancelButton.interactable = e;
            this.EmailField.interactable = e;
            this.DescriptionField.interactable = e;
        }

        private string GetDefaultEmailFieldContents()
        {
            return PlayerPrefs.GetString("SRDEBUGGER_BUG_REPORT_LAST_EMAIL", "");
        }

        private void SetDefaultEmailFieldContents(string value)
        {
            PlayerPrefs.SetString("SRDEBUGGER_BUG_REPORT_LAST_EMAIL", value);
            PlayerPrefs.Save();
        }
    }
}
