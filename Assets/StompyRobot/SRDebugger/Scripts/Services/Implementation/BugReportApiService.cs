namespace SRDebugger.Services.Implementation
{
    using Internal;
    using SRF.Service;
    using System;
    using UnityEngine;

    [Service(typeof(IBugReportService))]
    internal class BugReportApiService : IBugReportService
    {
        private IBugReporterHandler _handler = new InternalBugReporterHandler();

        public bool IsUsable
        {
            get
            {
                return this._handler != null && this._handler.IsUsable;
            }
        }

        public void SetHandler(IBugReporterHandler handler)
        {
            Debug.LogFormat("[SRDebugger] Bug Report handler set to {0}", handler);
            this._handler = handler;
        }

        public void SendBugReport(BugReport report, BugReportCompleteCallback completeHandler,
            IProgress<float> progress = null)
        {
            if (this._handler == null)
            {
                throw new InvalidOperationException("No bug report handler has been configured.");
            }

            if (!this._handler.IsUsable)
            {
                throw new InvalidOperationException("Bug report handler is not usable.");
            }

            if (report == null)
            {
                throw new ArgumentNullException("report");
            }

            if (completeHandler == null)
            {
                throw new ArgumentNullException("completeHandler");
            }

            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                completeHandler(false, "No Internet Connection");
                return;
            }

            this._handler.Submit(report, result => completeHandler(result.IsSuccessful, result.ErrorMessage), progress);
        }
    }
}
