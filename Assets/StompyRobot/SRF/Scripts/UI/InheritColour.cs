namespace SRF.UI
{
    using Internal;
    using UnityEngine;
    using UnityEngine.UI;

    [RequireComponent(typeof(Graphic))]
    [ExecuteInEditMode]
    [AddComponentMenu(ComponentMenuPaths.InheritColour)]
    public class InheritColour : SRMonoBehaviour
    {
        private Graphic _graphic;
        public Graphic From;

        private Graphic Graphic
        {
            get
            {
                if (this._graphic == null)
                {
                    this._graphic = this.GetComponent<Graphic>();
                }

                return this._graphic;
            }
        }

        private void Refresh()
        {
            if (this.From == null)
            {
                return;
            }

            this.Graphic.color = this.From.canvasRenderer.GetColor();
        }

        private void Update()
        {
            this.Refresh();
        }

        private void Start()
        {
            this.Refresh();
        }
    }
}
