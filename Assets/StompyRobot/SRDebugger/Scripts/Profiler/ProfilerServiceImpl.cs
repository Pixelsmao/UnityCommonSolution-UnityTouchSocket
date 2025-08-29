namespace SRDebugger.Profiler
{
    using Services;
    using SRF;
    using SRF.Service;
    using System.Diagnostics;
    using UnityEngine;

    public class ProfilerServiceImpl : SRServiceBase<IProfilerService>, IProfilerService
    {
        public float AverageFrameTime { get; private set; }
        public float LastFrameTime { get; private set; }

        public CircularBuffer<ProfilerFrame> FrameBuffer
        {
            get { return this._frameBuffer; }
        }

        private const int FrameBufferSize = 400;

        private readonly CircularBuffer<ProfilerFrame>
            _frameBuffer = new CircularBuffer<ProfilerFrame>(FrameBufferSize);

        private ProfilerLateUpdateListener _lateUpdateListener;

        private readonly Stopwatch _stopwatch = new Stopwatch();

        // Time between first Update() and last LateUpdate().
        private double _updateDuration;

        // Time that first camera rendered.
        private double _renderStartTime;

        // Time between first camera prerender and last camera postrender.
        private double _renderDuration;

        private int _camerasThisFrame;

        protected override void Awake()
        {
            base.Awake();
            this._lateUpdateListener = this.gameObject.AddComponent<ProfilerLateUpdateListener>();
            this._lateUpdateListener.OnLateUpdate = this.OnLateUpdate;

            this.CachedGameObject.hideFlags = HideFlags.NotEditable;
            this.CachedTransform.SetParent(Hierarchy.Get("SRDebugger"), true);

            Camera.onPreRender += this.OnCameraPreRender;
            Camera.onPostRender += this.OnCameraPostRender;
        }

        protected override void Update()
        {
            base.Update();

            this._camerasThisFrame = 0;

            this.EndFrame();

            // Set the frame time for the last frame
            if (this.FrameBuffer.Count > 0)
            {
                var frame = this.FrameBuffer.Back();
                frame.FrameTime = Time.unscaledDeltaTime;
                this.FrameBuffer[this.FrameBuffer.Count - 1] = frame;
            }

            this.LastFrameTime = Time.unscaledDeltaTime;

            var frameCount = Mathf.Min(20, this.FrameBuffer.Count);

            var f = 0d;
            for (var i = 0; i < frameCount; i++)
            {
                f += this.FrameBuffer[this.FrameBuffer.Count - 1 - i].FrameTime;
            }

            this.AverageFrameTime = (float)f / frameCount;

            this._stopwatch.Start();
        }

        protected void PushFrame(double totalTime, double updateTime, double renderTime)
        {
            //UnityEngine.Debug.Log("Frame: u: {0} r: {1}".Fmt(updateTime, renderTime));

            this._frameBuffer.PushBack(new ProfilerFrame
            {
                OtherTime = totalTime - updateTime - renderTime,
                UpdateTime = updateTime,
                RenderTime = renderTime
            });
        }

        private void OnLateUpdate()
        {
            this._updateDuration = this._stopwatch.Elapsed.TotalSeconds;
        }

        private void OnCameraPreRender(Camera cam)
        {
            if (this._camerasThisFrame == 0)
            {
                this._renderStartTime = this._stopwatch.Elapsed.TotalSeconds;
            }

            this._camerasThisFrame++;
        }

        private void OnCameraPostRender(Camera cam)
        {
            this._renderDuration = this._stopwatch.Elapsed.TotalSeconds - this._renderStartTime;
        }

        private void EndFrame()
        {
            if (this._stopwatch.IsRunning)
            {
                this.PushFrame(this._stopwatch.Elapsed.TotalSeconds, this._updateDuration, this._renderDuration);

                this._stopwatch.Reset();
                this._stopwatch.Start();
            }

            this._updateDuration = this._renderDuration = 0;
        }
    }
}
