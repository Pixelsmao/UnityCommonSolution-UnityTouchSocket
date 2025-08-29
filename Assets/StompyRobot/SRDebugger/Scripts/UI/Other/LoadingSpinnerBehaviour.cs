namespace SRDebugger.UI.Other
{
    using SRF;
    using UnityEngine;

    public class LoadingSpinnerBehaviour : SRMonoBehaviour
    {
        private float _dt;
        public int FrameCount = 12;
        public float SpinDuration = 0.8f;

        private void Update()
        {
            this._dt += Time.unscaledDeltaTime;

            var localRotation = this.CachedTransform.localRotation.eulerAngles;
            var r = localRotation.z;

            var fTime = this.SpinDuration / this.FrameCount;
            var hasChanged = false;

            while (this._dt > fTime)
            {
                r -= 360f / this.FrameCount;
                this._dt -= fTime;
                hasChanged = true;
            }

            if (hasChanged)
            {
                this.CachedTransform.localRotation = Quaternion.Euler(localRotation.x, localRotation.y, r);
            }
        }
    }
}
