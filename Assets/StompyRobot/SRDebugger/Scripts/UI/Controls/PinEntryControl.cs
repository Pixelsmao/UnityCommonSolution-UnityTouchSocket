#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace SRDebugger.UI.Controls
{
    using SRF;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using UnityEngine;
    using UnityEngine.UI;

    public delegate void PinEntryControlCallback(IList<int> result, bool didCancel);

    public class PinEntryControl : SRMonoBehaviourEx
    {
        private bool _isVisible = true;
        private readonly List<int> _numbers = new List<int>(4);

        [RequiredField] public Image Background;

        public bool CanCancel = true;

        [RequiredField] public UnityEngine.UI.Button CancelButton;

        [RequiredField] public Text CancelButtonText;

        [RequiredField] public CanvasGroup CanvasGroup;

        [RequiredField] public Animator DotAnimator;

        public UnityEngine.UI.Button[] NumberButtons;
        public Toggle[] NumberDots;

        [RequiredField] public Text PromptText;

        public event PinEntryControlCallback Complete;

        protected override void Awake()
        {
            base.Awake();

            for (var i = 0; i < this.NumberButtons.Length; i++)
            {
                var number = i;

                this.NumberButtons[i].onClick.AddListener(() => { this.PushNumber(number); });
            }

            this.CancelButton.onClick.AddListener(this.CancelButtonPressed);

            this.RefreshState();


        }

        protected override void OnEnable()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard.current.onTextInput += HandleCharacter;
#endif
        }

        protected override void OnDisable()
        {
#if ENABLE_INPUT_SYSTEM 
            Keyboard.current.onTextInput -= HandleCharacter;
#endif
        }

        protected override void Update()
        {
            base.Update();

            if (!this._isVisible)
            {
                return;
            }

#if ENABLE_INPUT_SYSTEM
            bool delete = Keyboard.current.deleteKey.wasPressedThisFrame || Keyboard.current.backspaceKey.wasPressedThisFrame;
#else
            var delete = (Input.GetKeyDown(KeyCode.Backspace) || Input.GetKeyDown(KeyCode.Delete));
#endif

            if (this._numbers.Count > 0 && delete)
            {
                this._numbers.PopLast();
                this.RefreshState();
            }

#if !ENABLE_INPUT_SYSTEM
            var input = Input.inputString;

            for (var i = 0; i < input.Length; i++)
            {
                this.HandleCharacter(input[i]);
            }
#endif
        }

        private void HandleCharacter(char i)
        {
            if (!this._isVisible)
            {
                return;
            }

            if (!char.IsNumber(i))
            {
                return;
            }

            var num = (int)char.GetNumericValue(i);

            if (num > 9 || num < 0)
            {
                return;
            }

            this.PushNumber(num);
        }

        public void Show()
        {
            this.CanvasGroup.alpha = 1f;
            this.CanvasGroup.blocksRaycasts = this.CanvasGroup.interactable = true;
            this._isVisible = true;
        }

        public void Hide()
        {
            this.CanvasGroup.alpha = 0f;
            this.CanvasGroup.blocksRaycasts = this.CanvasGroup.interactable = false;
            this._isVisible = false;
        }

        public void Clear()
        {
            this._numbers.Clear();
            this.RefreshState();
        }

        public void PlayInvalidCodeAnimation()
        {
            this.DotAnimator.SetTrigger("Invalid");
        }

        protected void OnComplete()
        {
            if (Complete != null)
            {
                Complete(new ReadOnlyCollection<int>(this._numbers), false);
            }
        }

        protected void OnCancel()
        {
            if (Complete != null)
            {
                Complete(new int[] { }, true);
            }
        }

        private void CancelButtonPressed()
        {
            if (this._numbers.Count > 0)
            {
                this._numbers.PopLast();
            }
            else
            {
                this.OnCancel();
            }

            this.RefreshState();
        }

        public void PushNumber(int number)
        {
            if (this._numbers.Count >= 4)
            {
                Debug.LogWarning("[PinEntry] Expected 4 numbers");
                return;
            }

            this._numbers.Add(number);

            if (this._numbers.Count >= 4)
            {
                this.OnComplete();
            }

            this.RefreshState();
        }

        private void RefreshState()
        {
            for (var i = 0; i < this.NumberDots.Length; i++)
            {
                this.NumberDots[i].isOn = i < this._numbers.Count;
            }

            if (this._numbers.Count > 0)
            {
                this.CancelButtonText.text = "Delete";
            }
            else
            {
                this.CancelButtonText.text = this.CanCancel ? "Cancel" : "";
            }
        }
    }
}
