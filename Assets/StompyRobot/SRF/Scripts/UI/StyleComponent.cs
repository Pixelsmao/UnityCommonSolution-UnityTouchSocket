namespace SRF.UI
{
    using Internal;
    using UnityEngine;
    using UnityEngine.Serialization;
    using UnityEngine.UI;

    [ExecuteInEditMode]
    [AddComponentMenu(ComponentMenuPaths.StyleComponent)]
    public class StyleComponent : SRMonoBehaviour
    {
        private Style _activeStyle;
        private StyleRoot _cachedRoot;
        private Graphic _graphic;
        private bool _hasStarted;
        private Image _image;
        private Selectable _selectable;

        [SerializeField][FormerlySerializedAs("StyleKey")][HideInInspector] private string _styleKey;

        public bool IgnoreImage = false;

        public string StyleKey
        {
            get { return this._styleKey; }
            set
            {
                this._styleKey = value;
                this.Refresh(false);
            }
        }

        private void Start()
        {
            this.Refresh(true);
            this._hasStarted = true;
        }

        private void OnEnable()
        {
            if (this._hasStarted)
            {
                this.Refresh(false);
            }
        }

#if UNITY_EDITOR

        /// <summary>
        /// This method is not included in exported builds - don't worry about it showing up in the profiler.
        /// </summary>
        private void Update()
        {
            if (!Application.isPlaying)
            {
                this.ApplyStyle();
            }
        }

#endif

        public void Refresh(bool invalidateCache)
        {
            if (string.IsNullOrEmpty(this.StyleKey))
            {
                this._activeStyle = null;
                return;
            }

            if (!this.isActiveAndEnabled)
            {
                this._cachedRoot = null;
                return;
            }

            if (this._cachedRoot == null || invalidateCache)
            {
                this._cachedRoot = this.GetStyleRoot();
            }

            if (this._cachedRoot == null)
            {
                Debug.LogWarning("[StyleComponent] No active StyleRoot object found in parents.", this);
                this._activeStyle = null;
                return;
            }

            var s = this._cachedRoot.GetStyle(this.StyleKey);

            if (s == null)
            {
                Debug.LogWarning("[StyleComponent] Style not found ({0})".Fmt(this.StyleKey), this);
                this._activeStyle = null;
                return;
            }

            this._activeStyle = s;
            this.ApplyStyle();
        }

        /// <summary>
        /// Find the nearest enable style root component in parents
        /// </summary>
        /// <returns></returns>
        private StyleRoot GetStyleRoot()
        {
            var t = this.CachedTransform;
            StyleRoot root;

            var i = 0;

            do
            {
                root = t.GetComponentInParent<StyleRoot>();

                if (root != null)
                {
                    t = root.transform.parent;
                }

                ++i;

                if (i > 100)
                {
                    Debug.LogWarning("Breaking Loop");
                    break;
                }
            } while ((root != null && !root.enabled) && t != null);

            return root;
        }

        private void ApplyStyle()
        {
            if (this._activeStyle == null)
            {
                return;
            }

            if (this._graphic == null)
            {
                this._graphic = this.GetComponent<Graphic>();
            }

            if (this._selectable == null)
            {
                this._selectable = this.GetComponent<Selectable>();
            }

            if (this._image == null)
            {
                this._image = this.GetComponent<Image>();
            }

            if (!this.IgnoreImage && this._image != null)
            {
                this._image.sprite = this._activeStyle.Image;
            }

            if (this._selectable != null)
            {
                var colours = this._selectable.colors;
                colours.normalColor = this._activeStyle.NormalColor;
                colours.highlightedColor = this._activeStyle.HoverColor;
                colours.pressedColor = this._activeStyle.ActiveColor;
                colours.disabledColor = this._activeStyle.DisabledColor;
                colours.colorMultiplier = 1f;

                this._selectable.colors = colours;

                if (this._graphic != null)
                {
                    this._graphic.color = Color.white;
                }
            }
            else if (this._graphic != null)
            {
                this._graphic.color = this._activeStyle.NormalColor;
            }
        }

        private void SRStyleDirty()
        {
            // If inactive, invalidate the cached root and return. Next time it is enabled
            // a new root will be found
            if (!this.CachedGameObject.activeInHierarchy)
            {
                this._cachedRoot = null;
                return;
            }

            this.Refresh(true);
        }
    }
}
