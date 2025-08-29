namespace SRDebugger.Services.Implementation
{
    using SRF;
    using SRF.Service;
    using UnityEngine;

    [Service(typeof(IDebugCameraService))]
    public class DebugCameraServiceImpl : IDebugCameraService
    {
        private readonly Camera _debugCamera;

        public DebugCameraServiceImpl()
        {
            if (Settings.Instance.UseDebugCamera)
            {
                this._debugCamera = new GameObject("SRDebugCamera").AddComponent<Camera>();

                this._debugCamera.cullingMask = 1 << Settings.Instance.DebugLayer;
                this._debugCamera.depth = Settings.Instance.DebugCameraDepth;

                this._debugCamera.clearFlags = CameraClearFlags.Depth;

                this._debugCamera.transform.SetParent(Hierarchy.Get("SRDebugger"));
            }
        }

        public Camera Camera
        {
            get { return this._debugCamera; }
        }
    }
}
