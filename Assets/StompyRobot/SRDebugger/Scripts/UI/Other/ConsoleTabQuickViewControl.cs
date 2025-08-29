namespace SRDebugger.UI.Other
{
    using Services;
    using SRF;
    using UnityEngine;
    using UnityEngine.UI;

    public class ConsoleTabQuickViewControl : SRMonoBehaviourEx
    {
        private const int Max = 1000;
        private static readonly string MaxString = (Max - 1) + "+";
        private int _prevErrorCount = -1;
        private int _prevInfoCount = -1;
        private int _prevWarningCount = -1;

        [Import] public IConsoleService ConsoleService;

        [RequiredField] public Text ErrorCountText;

        [RequiredField] public Text InfoCountText;

        [RequiredField] public Text WarningCountText;

        protected override void Awake()
        {
            base.Awake();

            this.ErrorCountText.text = "0";
            this.WarningCountText.text = "0";
            this.InfoCountText.text = "0";
        }

        protected override void Update()
        {
            base.Update();

            if (this.ConsoleService == null)
            {
                return;
            }

            if (HasChanged(this.ConsoleService.ErrorCount, ref this._prevErrorCount, Max))
            {
                this.ErrorCountText.text = Internal.SRDebuggerUtil.GetNumberString(this.ConsoleService.ErrorCount, Max, MaxString);
            }

            if (HasChanged(this.ConsoleService.WarningCount, ref this._prevWarningCount, Max))
            {
                this.WarningCountText.text = Internal.SRDebuggerUtil.GetNumberString(this.ConsoleService.WarningCount, Max,
                    MaxString);
            }

            if (HasChanged(this.ConsoleService.InfoCount, ref this._prevInfoCount, Max))
            {
                this.InfoCountText.text = Internal.SRDebuggerUtil.GetNumberString(this.ConsoleService.InfoCount, Max, MaxString);
            }
        }

        private static bool HasChanged(int newCount, ref int oldCount, int max)
        {
            var newCountClamped = Mathf.Clamp(newCount, 0, max);
            var oldCountClamped = Mathf.Clamp(oldCount, 0, max);

            var hasChanged = newCountClamped != oldCountClamped;

            oldCount = newCount;

            return hasChanged;
        }
    }
}
