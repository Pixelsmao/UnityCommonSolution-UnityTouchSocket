using System.Collections.Generic;

namespace SRDebugger.Services
{
    using UnityEngine;

    public delegate void ConsoleUpdatedEventHandler(IConsoleService console);

    public interface IConsoleService
    {
        int ErrorCount { get; }
        int WarningCount { get; }
        int InfoCount { get; }

        /// <summary>
        /// List of ConsoleEntry objects since the last clear.
        /// </summary>
        IReadOnlyList<ConsoleEntry> Entries { get; }

        /// <summary>
        /// List of all ConsoleEntry objects, regardless of clear.
        /// </summary>
        IReadOnlyList<ConsoleEntry> AllEntries { get; }

        event ConsoleUpdatedEventHandler Updated;

        event ConsoleUpdatedEventHandler Error;

        bool LoggingEnabled { get; set; }

        bool LogHandlerIsOverriden { get; }

        void Clear();
    }

    public class ConsoleEntry
    {
        private const int MessagePreviewLength = 180;
        private const int StackTracePreviewLength = 120;
        private string _messagePreview;
        private string _stackTracePreview;

        /// <summary>
        /// Number of times this log entry has occured (if collapsing is enabled)
        /// </summary>
        public int Count = 1;

        public LogType LogType;
        public string Message;
        public string StackTrace;
        public ConsoleEntry() { }

        public ConsoleEntry(ConsoleEntry other)
        {
            this.Message = other.Message;
            this.StackTrace = other.StackTrace;
            this.LogType = other.LogType;
            this.Count = other.Count;
        }

        public string MessagePreview
        {
            get
            {
                if (this._messagePreview != null)
                {
                    return this._messagePreview;
                }
                if (string.IsNullOrEmpty(this.Message))
                {
                    return "";
                }

                this._messagePreview = this.Message.Split('\n')[0];
                this._messagePreview = this._messagePreview.Substring(0, Mathf.Min(this._messagePreview.Length, MessagePreviewLength));

                return this._messagePreview;
            }
        }

        public string StackTracePreview
        {
            get
            {
                if (this._stackTracePreview != null)
                {
                    return this._stackTracePreview;
                }
                if (string.IsNullOrEmpty(this.StackTrace))
                {
                    return "";
                }

                this._stackTracePreview = this.StackTrace.Split('\n')[0];
                this._stackTracePreview = this._stackTracePreview.Substring(0,
                    Mathf.Min(this._stackTracePreview.Length, StackTracePreviewLength));

                return this._stackTracePreview;
            }
        }

        public bool Matches(ConsoleEntry other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }
            if (ReferenceEquals(this, other))
            {
                return true;
            }
            return string.Equals(this.Message, other.Message) && string.Equals(this.StackTrace, other.StackTrace) &&
                   this.LogType == other.LogType;
        }
    }
}
