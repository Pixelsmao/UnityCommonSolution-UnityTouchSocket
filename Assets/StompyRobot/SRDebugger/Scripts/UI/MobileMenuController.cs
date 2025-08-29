namespace SRDebugger.UI
{
    using Other;
    using SRF;
    using UnityEngine;
    using UnityEngine.UI;

    public class MobileMenuController : SRMonoBehaviourEx
    {
        private UnityEngine.UI.Button _closeButton;

        [SerializeField] private float _maxMenuWidth = 185f;

        [SerializeField] private float _peekAmount = 45f;

        private float _targetXPosition;

        [RequiredField] public RectTransform Content;

        [RequiredField] public RectTransform Menu;

        [RequiredField] public UnityEngine.UI.Button OpenButton;

        [RequiredField] public SRTabController TabController;

        public float PeekAmount
        {
            get { return this._peekAmount; }
        }

        public float MaxMenuWidth
        {
            get { return this._maxMenuWidth; }
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            var parent = this.Menu.parent as RectTransform;

            var layoutElement = this.Menu.GetComponent<LayoutElement>();
            layoutElement.ignoreLayout = true;

            // Set up menu anchors so it stretches to full height and has the screen width
            this.Menu.pivot = new Vector2(1, 1);

            this.Menu.offsetMin = new Vector2(1f, 0f);
            this.Menu.offsetMax = new Vector2(1f, 1f);

            this.Menu.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,
                Mathf.Clamp(parent.rect.width - this.PeekAmount, 0, this.MaxMenuWidth));

            this.Menu.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, parent.rect.height);

            this.Menu.anchoredPosition = new Vector2(0, 0);

            if (this._closeButton == null)
            {
                this.CreateCloseButton();
            }

            this.OpenButton.gameObject.SetActive(true);

            this.TabController.ActiveTabChanged += this.TabControllerOnActiveTabChanged;
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            var layoutElement = this.Menu.GetComponent<LayoutElement>();
            layoutElement.ignoreLayout = false;

            // Reset content position in case it has been moved by opening the menu
            this.Content.anchoredPosition = new Vector2(0, 0);

            this._closeButton.gameObject.SetActive(false);
            this.OpenButton.gameObject.SetActive(false);

            this.TabController.ActiveTabChanged -= this.TabControllerOnActiveTabChanged;
        }

        private void CreateCloseButton()
        {
            var go = new GameObject("SR_CloseButtonCanvas", typeof(RectTransform));
            go.transform.SetParent(this.Content, false);
            var c = go.AddComponent<Canvas>();
            go.AddComponent<GraphicRaycaster>();
            var rect = go.GetComponentOrAdd<RectTransform>();

            c.overrideSorting = true;
            c.sortingOrder = 122;

            go.AddComponent<LayoutElement>().ignoreLayout = true;

            this.SetRectSize(rect);

            var cGo = new GameObject("SR_CloseButton", typeof(RectTransform));
            cGo.transform.SetParent(rect, false);
            var cRect = cGo.GetComponent<RectTransform>();

            this.SetRectSize(cRect);
            cGo.AddComponent<Image>().color = new Color(0, 0, 0, 0);

            this._closeButton = cGo.AddComponent<UnityEngine.UI.Button>();
            this._closeButton.transition = Selectable.Transition.None;
            this._closeButton.onClick.AddListener(this.CloseButtonClicked);
            this._closeButton.gameObject.SetActive(false);
        }

        private void SetRectSize(RectTransform rect)
        {
            rect.anchorMin = new Vector2(0, 0);
            rect.anchorMax = new Vector2(1, 1);
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, this.Content.rect.width);
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, this.Content.rect.height);
        }

        private void CloseButtonClicked()
        {
            this.Close();
        }

        protected override void Update()
        {
            base.Update();

            var from = this.Content.anchoredPosition.x;

            if (Mathf.Abs(this._targetXPosition - from) < 2.5f)
            {
                this.Content.anchoredPosition = new Vector2(this._targetXPosition, this.Content.anchoredPosition.y);
            }
            else
            {
                this.Content.anchoredPosition =
                    new Vector2(SRMath.SpringLerp(from, this._targetXPosition, 15f, Time.unscaledDeltaTime),
                        this.Content.anchoredPosition.y);
            }
        }

        private void TabControllerOnActiveTabChanged(SRTabController srTabController, SRTab srTab)
        {
            this.Close();
        }

        [ContextMenu("Open")]
        public void Open()
        {
            this._targetXPosition = this.Menu.rect.width;
            this._closeButton.gameObject.SetActive(true);
        }

        [ContextMenu("Close")]
        public void Close()
        {
            this._targetXPosition = 0;
            this._closeButton.gameObject.SetActive(false);
        }
    }
}
