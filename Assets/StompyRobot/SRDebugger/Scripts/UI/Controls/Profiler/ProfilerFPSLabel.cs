namespace SRDebugger.UI
{
    using Services;
    using SRF;
    using UnityEngine;
    using UnityEngine.UI;

    public class ProfilerFPSLabel : SRMonoBehaviourEx
    {
        private float _nextUpdate;

        protected override void Update()
        {
            base.Update();

            if (Time.realtimeSinceStartup > this._nextUpdate)
            {
                this.Refresh();
            }
        }

        private void Refresh()
        {
            this._text.text = "FPS: {0:0.00}".Fmt(1f / this._profilerService.AverageFrameTime);

            this._nextUpdate = Time.realtimeSinceStartup + this.UpdateFrequency;
        }
#pragma warning disable 649

        [Import] private readonly IProfilerService _profilerService;

        public float UpdateFrequency = 1f;

        [RequiredField][SerializeField] private Text _text;

#pragma warning restore 649
    }
}
