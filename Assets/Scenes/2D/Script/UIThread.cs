using System;
using System.Threading;
using TouchSocket.Core;
using UnityEngine;

namespace Assets.Scenes._2D.Script
{
    public class UIThread : MonoBehaviour
    {
        public UIThread()
        {
            //对于单个的基础服务，将它注入到容器中
            ContainerService.container.RegisterSingleton<UIThread>(this);
        }

        private SynchronizationContext UIContext;
        private void Awake()
        {
            Application.targetFrameRate = 60;
            this.UIContext = SynchronizationContext.Current;
            Application.runInBackground = true;
        }
        public void UIInvoke(Action action)
        {
            this.UIContext.Post(x => action(), null);

        }

    }
}
