namespace SRF.UI
{
    using Internal;
    using UnityEngine;
    using UnityEngine.EventSystems;

    /// <summary>
    /// Do not allow an object to become select (automatically unfocus when receiving selection callback)
    /// </summary>
    [AddComponentMenu(ComponentMenuPaths.Unselectable)]
    public sealed class Unselectable : SRMonoBehaviour, ISelectHandler
    {
        private bool _suspectedSelected;

        public void OnSelect(BaseEventData eventData)
        {
            this._suspectedSelected = true;
        }

        private void Update()
        {
            if (!this._suspectedSelected)
            {
                return;
            }

            if (EventSystem.current.currentSelectedGameObject == this.CachedGameObject)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }

            this._suspectedSelected = false;
        }
    }
}
