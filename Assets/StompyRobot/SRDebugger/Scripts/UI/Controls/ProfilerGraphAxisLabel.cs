namespace SRDebugger.UI.Controls
{
    using SRF;
    using UnityEngine;
    using UnityEngine.UI;

    [RequireComponent(typeof(RectTransform))]
    public class ProfilerGraphAxisLabel : SRMonoBehaviourEx
    {
        private float _prevFrameTime;
        private float? _queuedFrameTime;
        private float _yPosition;

        [RequiredField] public Text Text;

        protected override void Update()
        {
            base.Update();

            if (this._queuedFrameTime.HasValue)
            {
                this.SetValueInternal(this._queuedFrameTime.Value);
                this._queuedFrameTime = null;
            }
        }

        public void SetValue(float frameTime, float yPosition)
        {
            if (this._prevFrameTime == frameTime && this._yPosition == yPosition)
            {
                return;
            }

            this._queuedFrameTime = frameTime;
            this._yPosition = yPosition;
        }

        private void SetValueInternal(float frameTime)
        {
            this._prevFrameTime = frameTime;

            var ms = Mathf.FloorToInt(frameTime * 1000);
            var fps = Mathf.RoundToInt(1f / frameTime);

            this.Text.text = "{0}ms ({1}FPS)".Fmt(ms, fps);

            var r = (RectTransform)this.CachedTransform;
            r.anchoredPosition = new Vector2(r.rect.width * 0.5f + 10f, this._yPosition);
        }
    }
}
