#if UNITY_2018_1_OR_NEWER

namespace SRDebugger.Profiler
{
    using System.Collections;
    using System.Diagnostics;
    using SRDebugger.Services;
    using SRF;
    using SRF.Service;
    using UnityEngine;
#if UNITY_2019_3_OR_NEWER
    using UnityEngine.Rendering;
#else
    using UnityEngine.Experimental.Rendering;
#endif

    public class SRPProfilerService : SRServiceBase<IProfilerService>, IProfilerService
    {
        public float AverageFrameTime { get; private set; }
        public float LastFrameTime { get; private set; }

        public CircularBuffer<ProfilerFrame> FrameBuffer
        {
            get { return this._frameBuffer; }
        }

        private const int FrameBufferSize = 400;
        private readonly CircularBuffer<ProfilerFrame> _frameBuffer = new CircularBuffer<ProfilerFrame>(FrameBufferSize);

        private ProfilerLateUpdateListener _lateUpdateListener;

        // Time between first Update() and last LateUpdate()
        private double _updateDuration;

        // Time that render pipeline starts
        private double _renderStartTime;

        // Time between scripted render pipeline starts + EndOfFrame
        private double _renderDuration;

        private readonly Stopwatch _stopwatch = new Stopwatch();

        protected override void Awake()
        {
            base.Awake();
            this._lateUpdateListener = this.gameObject.AddComponent<ProfilerLateUpdateListener>();
            this._lateUpdateListener.OnLateUpdate = this.OnLateUpdate;

            this.CachedGameObject.hideFlags = HideFlags.NotEditable;
            this.CachedTransform.SetParent(Hierarchy.Get("SRDebugger"), true);

#if UNITY_2019_3_OR_NEWER
            RenderPipelineManager.beginFrameRendering += this.RenderPipelineOnBeginFrameRendering;
#else
            RenderPipeline.beginFrameRendering += RenderPipelineOnBeginFrameRendering;
#endif

            this.StartCoroutine(this.EndOfFrameCoroutine());
        }

        protected override void Update()
        {
            base.Update();

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

        private IEnumerator EndOfFrameCoroutine()
        {
            var endOfFrame = new WaitForEndOfFrame();

            while (true)
            {
                yield return endOfFrame;
                this._renderDuration = this._stopwatch.Elapsed.TotalSeconds - this._renderStartTime;
            }
        }

        protected void PushFrame(double totalTime, double updateTime, double renderTime)
        {
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

#if UNITY_2019_3_OR_NEWER
        private void RenderPipelineOnBeginFrameRendering(ScriptableRenderContext context, Camera[] cameras)
#else
        private void RenderPipelineOnBeginFrameRendering(Camera[] obj)
#endif
        {
            this._renderStartTime = this._stopwatch.Elapsed.TotalSeconds;
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
#endif