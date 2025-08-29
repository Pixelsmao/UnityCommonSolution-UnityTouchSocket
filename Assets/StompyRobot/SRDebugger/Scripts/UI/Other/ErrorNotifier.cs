using UnityEngine;

namespace SRDebugger.UI.Other
{
    public class ErrorNotifier : MonoBehaviour
    {
        public bool IsVisible
        {
            get { return this._isShowing; }
        }

        private const float DisplayTime = 6;

        [SerializeField]
        private Animator _animator = null;

        private int _triggerHash;

        private float _hideTime;
        private bool _isShowing;

        private bool _queueWarning;

        private void Awake()
        {
            this._triggerHash = Animator.StringToHash("Display");
        }

        public void ShowErrorWarning()
        {
            this._queueWarning = true;
        }

        private void Update()
        {
            if (this._queueWarning)
            {
                this._hideTime = Time.realtimeSinceStartup + DisplayTime;

                if (!this._isShowing)
                {
                    this._isShowing = true;
                    this._animator.SetBool(this._triggerHash, true);
                }

                this._queueWarning = false;
            }

            if (this._isShowing && Time.realtimeSinceStartup > this._hideTime)
            {
                this._animator.SetBool(this._triggerHash, false);
                this._isShowing = false;
            }
        }
    }
}