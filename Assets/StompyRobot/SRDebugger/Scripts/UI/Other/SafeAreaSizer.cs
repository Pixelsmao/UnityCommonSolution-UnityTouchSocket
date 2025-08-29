using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace SRDebugger.UI.Other
{
    /// <summary>
    /// 
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [ExecuteInEditMode]
    public class SafeAreaSizer : UIBehaviour, ILayoutElement
    {
        public RectTransform.Edge Edge
        {
            get { return this._edge; }
            set
            {
                if (this._edge != value)
                {
                    this._edge = value;
                    LayoutRebuilder.MarkLayoutForRebuild(this.transform as RectTransform);
                }
            }
        }

        [SerializeField, FormerlySerializedAs("Edge")]
        private RectTransform.Edge _edge;
        public float Scale = 1f;

        private float _height;
        private float _width;


        public float preferredWidth
        {
            get
            {
                return this._width;
            }
        }

        public float preferredHeight
        {
            get
            {
                return this._height;
            }
        }

        public float minWidth
        {
            get
            {
                return this._width;
            }
        }

        public float minHeight
        {
            get
            {
                return this._height;
            }
        }

        public int layoutPriority
        {
            get { return 2; }
        }

        public float flexibleHeight
        {
            get { return -1; }
        }

        public float flexibleWidth
        {
            get { return -1; }
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            if (Application.isPlaying)
            {
                this.Refresh();
            }
        }

        private void Update()
        {
            this._width = this._height = 0;
        }
#endif

        private void Refresh()
        {
            // Determine the distance in local coords
            var safeArea = Screen.safeArea;
            var myCanvas = this.GetComponentInParent<Canvas>();
            if (myCanvas == null)
            {
                return;
            }

            var canvasRect = myCanvas.GetComponent<RectTransform>();

            // RectTransformUtility.PixelAdjustRect()
            this._width = this._height = 0;

            switch (this._edge)
            {
                case RectTransform.Edge.Left:
                    this._width = (safeArea.x / myCanvas.pixelRect.width) * canvasRect.rect.width;
                    break;
                case RectTransform.Edge.Right:
                    this._width = (Screen.width - safeArea.width - safeArea.x) / myCanvas.pixelRect.width * canvasRect.rect.width;
                    break;
                case RectTransform.Edge.Top:
                    this._height = (Screen.height - safeArea.height - safeArea.y) / myCanvas.pixelRect.height * canvasRect.rect.height;
                    break;
                case RectTransform.Edge.Bottom:
                    this._height = (safeArea.y / myCanvas.pixelRect.height) * canvasRect.rect.height;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            this._width *= this.Scale;
            this._height *= this.Scale;
        }

        public void CalculateLayoutInputHorizontal()
        {
            if (Application.isPlaying)
            {
                this.Refresh();
            }
        }

        public void CalculateLayoutInputVertical()
        {
            if (Application.isPlaying)
            {
                this.Refresh();
            }
        }
    }
}
