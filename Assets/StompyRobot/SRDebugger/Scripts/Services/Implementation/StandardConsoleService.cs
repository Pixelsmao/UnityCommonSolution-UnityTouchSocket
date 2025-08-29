using System;
using System.Collections.Generic;

namespace SRDebugger.Services.Implementation
{
    using SRF.Service;
    using UnityEngine;

    [Service(typeof(IConsoleService))]
    public class StandardConsoleService : IConsoleService, IDisposable
    {
        private readonly bool _collapseEnabled;
        private bool _hasCleared;

        private readonly CircularBuffer<ConsoleEntry> _allConsoleEntries;
        private CircularBuffer<ConsoleEntry> _consoleEntries;
        private readonly object _threadLock = new object();

        private readonly ILogHandler _expectedLogHandler;

        public StandardConsoleService()
        {
            Application.logMessageReceivedThreaded += this.UnityLogCallback;
            this._expectedLogHandler = Debug.unityLogger.logHandler;

            SRServiceManager.RegisterService<IConsoleService>(this);
            this._collapseEnabled = Settings.Instance.CollapseDuplicateLogEntries;
            this._allConsoleEntries = new CircularBuffer<ConsoleEntry>(Settings.Instance.MaximumConsoleEntries);
        }

        public void Dispose()
        {
            Application.logMessageReceivedThreaded -= this.UnityLogCallback;
            if (this._consoleEntries != null)
            {
                this._consoleEntries.Clear();
            }

            this._allConsoleEntries.Clear();
        }

        public int ErrorCount { get; private set; }
        public int WarningCount { get; private set; }
        public int InfoCount { get; private set; }

        public event ConsoleUpdatedEventHandler Updated;
        public event ConsoleUpdatedEventHandler Error;

        public bool LoggingEnabled
        {
            get { return Debug.unityLogger.logEnabled; }
            set { Debug.unityLogger.logEnabled = value; }
        }

        public bool LogHandlerIsOverriden
        {
            get
            {
                return Debug.unityLogger.logHandler != this._expectedLogHandler;
            }
        }

        public IReadOnlyList<ConsoleEntry> Entries
        {
            get
            {
                if (!this._hasCleared)
                {
                    return this._allConsoleEntries;
                }

                return this._consoleEntries;
            }
        }

        public IReadOnlyList<ConsoleEntry> AllEntries
        {
            get { return this._allConsoleEntries; }
        }

        public void Clear()
        {
            lock (this._threadLock)
            {
                this._hasCleared = true;

                if (this._consoleEntries == null)
                {
                    this._consoleEntries = new CircularBuffer<ConsoleEntry>(Settings.Instance.MaximumConsoleEntries);
                }
                else
                {
                    this._consoleEntries.Clear();
                }

                this.ErrorCount = this.WarningCount = this.InfoCount = 0;
            }

            this.OnUpdated();
        }

        protected void OnEntryAdded(ConsoleEntry entry)
        {
            if (this._hasCleared)
            {
                // Decrement counters if adding this entry will push another
                // entry from the buffer.
                if (this._consoleEntries.IsFull)
                {
                    this.AdjustCounter(this._consoleEntries.Front().LogType, -1);
                    this._consoleEntries.PopFront();
                }

                this._consoleEntries.PushBack(entry);
            }
            else
            {
                if (this._allConsoleEntries.IsFull)
                {
                    this.AdjustCounter(this._allConsoleEntries.Front().LogType, -1);
                    this._allConsoleEntries.PopFront();
                }
            }

            this._allConsoleEntries.PushBack(entry);
            this.OnUpdated();
        }

        protected void OnEntryDuplicated(ConsoleEntry entry)
        {
            entry.Count++;
            this.OnUpdated();

            // If has cleared, add this entry again for the current list
            if (this._hasCleared && this._consoleEntries.Count == 0)
            {
                this.OnEntryAdded(new ConsoleEntry(entry) { Count = 1 });
            }
        }

        private void OnUpdated()
        {
            if (Updated != null)
            {
                try
                {
                    Updated(this);
                }
                catch { }
            }
        }

        private void UnityLogCallback(string condition, string stackTrace, LogType type)
        {
            //if (condition.StartsWith("[SRConsole]"))
            //    return;

            lock (this._threadLock)
            {
                var prevMessage = this._collapseEnabled && this._allConsoleEntries.Count > 0
                    ? this._allConsoleEntries[this._allConsoleEntries.Count - 1]
                    : null;

                this.AdjustCounter(type, 1);

                if (prevMessage != null && prevMessage.LogType == type && prevMessage.Message == condition &&
                    prevMessage.StackTrace == stackTrace)
                {
                    this.OnEntryDuplicated(prevMessage);
                }
                else
                {
                    var newEntry = new ConsoleEntry
                    {
                        LogType = type,
                        StackTrace = stackTrace,
                        Message = condition
                    };

                    this.OnEntryAdded(newEntry);
                }
            }
        }

        private void AdjustCounter(LogType type, int amount)
        {
            switch (type)
            {
                case LogType.Assert:
                case LogType.Error:
                case LogType.Exception:
                    this.ErrorCount += amount;

                    if (Error != null)
                    {
                        Error.Invoke(this);
                    }
                    break;

                case LogType.Warning:
                    this.WarningCount += amount;
                    break;

                case LogType.Log:
                    this.InfoCount += amount;
                    break;
            }
        }
    }
}
