//#define ENABLE_LOGGING

namespace SRF.Service
{
    using System;
    using System.Collections;
    using System.Diagnostics;
    using UnityEngine;
    using Debug = UnityEngine.Debug;
    using Object = UnityEngine.Object;

    /// <summary>
    /// A service which has async-loading dependencies
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class SRDependencyServiceBase<T> : SRServiceBase<T>, IAsyncService where T : class
    {
        private bool _isLoaded;
        protected abstract Type[] Dependencies { get; }

        public bool IsLoaded
        {
            get { return this._isLoaded; }
        }

        [Conditional("ENABLE_LOGGING")]
        private void Log(string msg, Object target)
        {
            //#if ENABLE_LOGGING
            Debug.Log(msg, target);
            //#endif
        }

        protected override void Start()
        {
            base.Start();

            this.StartCoroutine(this.LoadDependencies());
        }

        /// <summary>
        /// Invoked once all dependencies are loaded
        /// </summary>
        protected virtual void OnLoaded() { }

        private IEnumerator LoadDependencies()
        {
            SRServiceManager.LoadingCount++;

            this.Log("[Service] Loading service ({0})".Fmt(this.GetType().Name), this);

            foreach (var d in this.Dependencies)
            {
                var hasService = SRServiceManager.HasService(d);

                this.Log("[Service] Resolving Service ({0}) HasService: {1}".Fmt(d.Name, hasService), this);

                if (hasService)
                {
                    continue;
                }

                var service = SRServiceManager.GetService(d);

                if (service == null)
                {
                    Debug.LogError("[Service] Could not resolve dependency ({0})".Fmt(d.Name));
                    this.enabled = false;
                    yield break;
                }

                var a = service as IAsyncService;

                if (a != null)
                {
                    while (!a.IsLoaded)
                    {
                        yield return new WaitForEndOfFrame();
                    }
                }
            }

            this.Log("[Service] Loading service ({0}) complete.".Fmt(this.GetType().Name), this);

            this._isLoaded = true;
            SRServiceManager.LoadingCount--;

            this.OnLoaded();
        }
    }
}
