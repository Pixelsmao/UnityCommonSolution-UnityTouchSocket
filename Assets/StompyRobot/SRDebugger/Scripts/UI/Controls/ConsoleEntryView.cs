namespace SRDebugger.UI.Controls
{
    using Services;
    using SRF;
    using SRF.UI;
    using SRF.UI.Layout;
    using System;
    using UnityEngine;
    using UnityEngine.UI;

    [RequireComponent(typeof(RectTransform))]
    public class ConsoleEntryView : SRMonoBehaviourEx, IVirtualView
    {
        public const string ConsoleBlobInfo = "Console_Info_Blob";
        public const string ConsoleBlobWarning = "Console_Warning_Blob";
        public const string ConsoleBlobError = "Console_Error_Blob";
        private int _count;
        private bool _hasCount;
        private ConsoleEntry _prevData;
        private RectTransform _rectTransform;

        [RequiredField] public Text Count;

        [RequiredField] public CanvasGroup CountContainer;

        [RequiredField] public StyleComponent ImageStyle;

        [RequiredField] public Text Message;

        [RequiredField] public Text StackTrace;

        public void SetDataContext(object data)
        {
            var msg = data as ConsoleEntry;

            if (msg == null)
            {
                throw new Exception("Data should be a ConsoleEntry");
            }

            // Always check for updates on "Count", as it can change
            if (msg.Count > 1)
            {
                if (!this._hasCount)
                {
                    this.CountContainer.alpha = 1f;
                    this._hasCount = true;
                }

                if (msg.Count != this._count)
                {
                    this.Count.text = Internal.SRDebuggerUtil.GetNumberString(msg.Count, 999, "999+");
                    this._count = msg.Count;
                }
            }
            else if (this._hasCount)
            {
                this.CountContainer.alpha = 0f;
                this._hasCount = false;
            }

            // Only update everything else if data context has changed, not just for an update
            if (msg == this._prevData)
            {
                return;
            }

            this._prevData = msg;

            this.Message.text = msg.MessagePreview;
            this.StackTrace.text = msg.StackTracePreview;

            if (string.IsNullOrEmpty(this.StackTrace.text))
            {
                this.Message.rectTransform.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Bottom, 2,
                    this._rectTransform.rect.height - 4);
            }
            else
            {
                this.Message.rectTransform.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Bottom, 12,
                    this._rectTransform.rect.height - 14);
            }

            switch (msg.LogType)
            {
                case LogType.Log:
                    this.ImageStyle.StyleKey = ConsoleBlobInfo;
                    break;

                case LogType.Warning:
                    this.ImageStyle.StyleKey = ConsoleBlobWarning;
                    break;

                case LogType.Exception:
                case LogType.Assert:
                case LogType.Error:
                    this.ImageStyle.StyleKey = ConsoleBlobError;
                    break;
            }
        }

        protected override void Awake()
        {
            base.Awake();

            this._rectTransform = this.CachedTransform as RectTransform;
            this.CountContainer.alpha = 0f;

            this.Message.supportRichText = Settings.Instance.RichTextInConsole;
        }
    }
}
