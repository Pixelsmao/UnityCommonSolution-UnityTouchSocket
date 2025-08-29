using TouchSocket.Core;
using UnityEngine;

namespace Assets.Scenes._2D.Script
{
    /// <summary>
    /// 网络服务
    /// </summary>
    public class NetworkService : MonoBehaviour
    {
        public NetworkService()
        {
            //对于单个的基础服务，将它注入到容器中
            ContainerService.container.RegisterSingleton<NetworkService>(this);

            ContainerService.container.RegisterSingleton<Touch_2DWebSocket_Client>();
            ContainerService.container.RegisterSingleton<Touch_JsonWebSocket_Client_2D>();

        }

        private void Awake()
        {

        }
        private void Start()
        {
            ContainerService.Resolve<Touch_JsonWebSocket_Client_2D>().Connection();
        }
        private void OnDestroy()
        {
            ContainerService.Resolve<Touch_2DWebSocket_Client>().CloseAsync("");
        }

    }
}
