namespace SRF.UI
{
    using Internal;
    using UnityEngine;

    [ExecuteInEditMode]
    [AddComponentMenu(ComponentMenuPaths.StyleRoot)]
    public sealed class StyleRoot : SRMonoBehaviour
    {
        private StyleSheet _activeStyleSheet;
        public StyleSheet StyleSheet;

        public Style GetStyle(string key)
        {
            if (this.StyleSheet == null)
            {
                Debug.LogWarning("[StyleRoot] StyleSheet is not set.", this);
                return null;
            }

            return this.StyleSheet.GetStyle(key);
        }

        private void OnEnable()
        {
            this._activeStyleSheet = null;

            if (this.StyleSheet != null)
            {
                this.OnStyleSheetChanged();
            }
        }

        private void OnDisable()
        {
            this.OnStyleSheetChanged();
        }

        private void Update()
        {
            if (this._activeStyleSheet != this.StyleSheet)
            {
                this.OnStyleSheetChanged();
            }
        }

        private void OnStyleSheetChanged()
        {
            this._activeStyleSheet = this.StyleSheet;

            this.BroadcastMessage("SRStyleDirty", SendMessageOptions.DontRequireReceiver);
        }

        public void SetDirty()
        {
            this._activeStyleSheet = null;
        }
    }
}
