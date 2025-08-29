namespace SRDebugger.Services.Implementation
{
    using Internal;
    using SRF;
    using SRF.Service;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UI.Controls;
    using UnityEngine;

    [Service(typeof(IPinEntryService))]
    public class PinEntryServiceImpl : SRServiceBase<IPinEntryService>, IPinEntryService
    {
        private PinEntryCompleteCallback _callback;
        private bool _isVisible;
        private PinEntryControl _pinControl;
        private readonly List<int> _requiredPin = new List<int>(4);

        public bool IsShowingKeypad
        {
            get { return this._isVisible; }
        }

        public void ShowPinEntry(IReadOnlyList<int> requiredPin, string message, PinEntryCompleteCallback callback,
            bool allowCancel = true)
        {
            if (this._isVisible)
            {
                throw new InvalidOperationException("Pin entry is already in progress");
            }

            this.VerifyPin(requiredPin);

            if (this._pinControl == null)
            {
                this.Load();
            }

            if (this._pinControl == null)
            {
                Debug.LogWarning("[PinEntry] Pin entry failed loading, executing callback with fail result");
                callback(false);
                return;
            }

            this._pinControl.Clear();
            this._pinControl.PromptText.text = message;

            this._pinControl.CanCancel = allowCancel;

            this._callback = callback;

            this._requiredPin.Clear();
            this._requiredPin.AddRange(requiredPin);

            this._pinControl.Show();

            this._isVisible = true;

            SRDebuggerUtil.EnsureEventSystemExists();
        }

        protected override void Awake()
        {
            base.Awake();

            this.CachedTransform.SetParent(Hierarchy.Get("SRDebugger"));
        }

        private void Load()
        {
            var prefab = Resources.Load<PinEntryControl>(SRDebugPaths.PinEntryPrefabPath);

            if (prefab == null)
            {
                Debug.LogError("[PinEntry] Unable to load pin entry prefab");
                return;
            }

            this._pinControl = SRInstantiate.Instantiate(prefab);
            this._pinControl.CachedTransform.SetParent(this.CachedTransform, false);

            this._pinControl.Hide();

            this._pinControl.Complete += this.PinControlOnComplete;
        }

        private void PinControlOnComplete(IList<int> result, bool didCancel)
        {
            var isValid = this._requiredPin.SequenceEqual(result);

            if (!didCancel && !isValid)
            {
                this._pinControl.Clear();
                this._pinControl.PlayInvalidCodeAnimation();

                return;
            }

            this._isVisible = false;
            this._pinControl.Hide();

            if (didCancel)
            {
                this._callback(false);
                return;
            }

            this._callback(isValid);
        }

        private void VerifyPin(IReadOnlyList<int> pin)
        {
            if (pin.Count != 4)
            {
                throw new ArgumentException("Pin list must have 4 elements");
            }

            for (var i = 0; i < pin.Count; i++)
            {
                if (pin[i] < 0 || pin[i] > 9)
                {
                    throw new ArgumentException("Pin numbers must be >= 0 && <= 9");
                }
            }
        }
    }
}
