namespace SRDebugger.Internal
{
    using SRF;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Text;
    using UnityEngine;
    using UnityEngine.Networking;

    internal class BugReportApi : MonoBehaviour
    {
        private string _apiKey;
        private BugReport _bugReport;
        private bool _isBusy;

        private UnityWebRequest _webRequest;
        private Action<BugReportSubmitResult> _onComplete;
        private IProgress<float> _progress;

        public static void Submit(BugReport report, string apiKey, Action<BugReportSubmitResult> onComplete, IProgress<float> progress)
        {
            var go = new GameObject("BugReportApi");
            go.transform.parent = Hierarchy.Get("SRDebugger");

            var bugReportApi = go.AddComponent<BugReportApi>();
            bugReportApi.Init(report, apiKey, onComplete, progress);
            bugReportApi.StartCoroutine(bugReportApi.Submit());
        }

        private void Init(BugReport report, string apiKey, Action<BugReportSubmitResult> onComplete, IProgress<float> progress)
        {
            this._bugReport = report;
            this._apiKey = apiKey;
            this._onComplete = onComplete;
            this._progress = progress;
        }

        private void Update()
        {
            if (this._isBusy && this._webRequest != null)
            {
                this._progress.Report(this._webRequest.uploadProgress);
            }
        }

        private IEnumerator Submit()
        {
            if (this._isBusy)
            {
                throw new InvalidOperationException("BugReportApi is already sending a bug report");
            }

            // Reset state
            this._isBusy = true;
            this._webRequest = null;

            string json;
            byte[] jsonBytes;

            try
            {
                json = BuildJsonRequest(this._bugReport);
                jsonBytes = Encoding.UTF8.GetBytes(json);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                this.SetCompletionState(BugReportSubmitResult.Error("Error building bug report."));
                yield break;
            }

            try
            {
                const string jsonContentType = "application/json";

                this._webRequest = new UnityWebRequest(SRDebugApi.BugReportEndPoint, UnityWebRequest.kHttpVerbPOST,
                    new DownloadHandlerBuffer(), new UploadHandlerRaw(jsonBytes)
                    {
                        contentType = jsonContentType
                    });
                this._webRequest.SetRequestHeader("Accept", jsonContentType);
                this._webRequest.SetRequestHeader("X-ApiKey", this._apiKey);
            }
            catch (Exception e)
            {
                Debug.LogError(e);

                if (this._webRequest != null)
                {
                    this._webRequest.Dispose();
                    this._webRequest = null;
                }
            }

            if (this._webRequest == null)
            {
                this.SetCompletionState(BugReportSubmitResult.Error("Error building bug report request."));
                yield break;
            }

            yield return this._webRequest.SendWebRequest();

#if UNITY_2018 || UNITY_2019
            bool isError = _webRequest.isNetworkError;
#else
            var isError = this._webRequest.result == UnityWebRequest.Result.ConnectionError || this._webRequest.result == UnityWebRequest.Result.DataProcessingError;
#endif

            if (isError)
            {
                this.SetCompletionState(BugReportSubmitResult.Error("Request Error: " + this._webRequest.error));
                this._webRequest.Dispose();
                yield break;
            }

            var responseCode = this._webRequest.responseCode;
            var responseJson = this._webRequest.downloadHandler.text;

            this._webRequest.Dispose();

            if (responseCode != 200)
            {
                this.SetCompletionState(BugReportSubmitResult.Error("Server: " + SRDebugApiUtil.ParseErrorResponse(responseJson, "Unknown response from server")));
                yield break;
            }

            this.SetCompletionState(BugReportSubmitResult.Success);
        }

        private void SetCompletionState(BugReportSubmitResult result)
        {
            this._bugReport.ScreenshotData = null; // Clear the heaviest data in case something holds onto it
            this._isBusy = false;

            if (!result.IsSuccessful)
            {
                Debug.LogError("Bug Reporter Error: " + result.ErrorMessage);
            }

            Destroy(this.gameObject);
            this._onComplete(result);
        }

        private static string BuildJsonRequest(BugReport report)
        {
            var ht = new Hashtable();

            ht.Add("userEmail", report.Email);
            ht.Add("userDescription", report.UserDescription);

            ht.Add("console", CreateConsoleDump());
            ht.Add("systemInformation", report.SystemInformation);
            ht.Add("applicationIdentifier", Application.identifier);

            if (report.ScreenshotData != null)
            {
                ht.Add("screenshot", Convert.ToBase64String(report.ScreenshotData));
            }
            var json = Json.Serialize(ht);

            return json;
        }

        private static List<List<string>> CreateConsoleDump()
        {
            var consoleLog = Service.Console.AllEntries;
            var list = new List<List<string>>(consoleLog.Count);

            foreach (var consoleEntry in consoleLog)
            {
                var entry = new List<string>(5);

                entry.Add(consoleEntry.LogType.ToString());
                entry.Add(consoleEntry.Message);
                entry.Add(consoleEntry.StackTrace);

                if (consoleEntry.Count > 1)
                {
                    entry.Add(consoleEntry.Count.ToString());
                }

                list.Add(entry);
            }

            return list;
        }
    }
}
