namespace SRF.UI
{
    using Internal;
    using UnityEngine;
    using UnityEngine.UI;

    [ExecuteInEditMode]
    [RequireComponent(typeof(RectTransform))]
    [AddComponentMenu(ComponentMenuPaths.ScrollToBottom)]
    public class ScrollToBottomBehaviour : MonoBehaviour
    {
#pragma warning disable 649
        [SerializeField]
        private ScrollRect _scrollRect;

        [SerializeField]
        private CanvasGroup _canvasGroup;

        [SerializeField]
        private bool _scrollToTop;
#pragma warning restore 649


        public void Start()
        {
            if (this._scrollRect == null)
            {
                Debug.LogError("[ScrollToBottomBehaviour] ScrollRect not set");
                return;
            }

            if (this._canvasGroup == null)
            {
                Debug.LogError("[ScrollToBottomBehaviour] CanvasGroup not set");
                return;
            }

            this._scrollRect.onValueChanged.AddListener(this.OnScrollRectValueChanged);
            this.Refresh();
        }

        private void OnEnable()
        {
            this.Refresh();
        }

        public void Trigger()
        {
            if (this._scrollToTop)
            {
                this._scrollRect.normalizedPosition = new Vector2(0, 1);
            }
            else
            {
                this._scrollRect.normalizedPosition = new Vector2(0, 0);
            }
        }

        private void OnScrollRectValueChanged(Vector2 position)
        {
            this.Refresh();
        }

        private void Refresh()
        {
            if (this._scrollRect == null)
                return;

            var position = this._scrollRect.normalizedPosition;

            if (position.y < 0.001f || (this._scrollToTop && position.y >= 0.999f))
            {
                this.SetVisible(false);
            }
            else
            {
                this.SetVisible(true);
            }
        }

        private void SetVisible(bool truth)
        {
            if (truth)
            {
                this._canvasGroup.alpha = 1f;
                this._canvasGroup.interactable = true;
                this._canvasGroup.blocksRaycasts = true;
            }
            else
            {
                this._canvasGroup.alpha = 0f;
                this._canvasGroup.interactable = false;
                this._canvasGroup.blocksRaycasts = false;
            }
        }
    }
}
