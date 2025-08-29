using Assets.Scripts.SROptons;
using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using TouchSocket.Core;
using TouchSocket.Http;
using TouchSocket.Http.WebSockets;
using TouchSocket.JsonRpc;
using TouchSocket.Rpc;
using TouchSocket.Sockets;

namespace Assets.Scenes._2D.Script
{
    internal class Touch_JsonWebSocket_Client_2D
    {
        public const string Touch_JsonWebSocket_Client_IPPort = "ws://127.0.0.1:7794/ws";
        //public const string Touch_JsonWebSocket_Client_IPPort = "ws://47.96.146.3:7794/ws";

        /// <summary>
        /// Touch JsonWebSocket
        /// </summary>
        private readonly Touch_2DWebSocket_Client Client;

        public Touch_JsonWebSocket_Client_2D(Touch_2DWebSocket_Client Client)
        {
            this.Client = Client;
        }

        public void Connection()
        {

            try
            {

                //声明配置
                var config = new TouchSocketConfig();
                config.ConfigureContainer(a =>
                {
                    a.AddLogger(UnityDebugLogger.Default);
                    a.AddRpcStore(store =>
                    {
                        store.RegisterServer<Reverse2DSquareRpcServer>();
#if DEBUG
                        var code = store.GetProxyCodes("UnityRpcProxy", typeof(JsonRpcAttribute));
                        var dirPath = "./RPCStore";
                        if (!Directory.Exists(dirPath))
                        {
                            Directory.CreateDirectory(dirPath);
                        }
                        File.WriteAllText(Path.Combine(dirPath, "Reverse2DSquareRpcServer.cs"), code);
#endif
                    });
                })
                .ConfigurePlugins(a =>
                {
                    a.Add<Touch_JsonWebSocket_ClientPlugin>();
                })

                .SetRemoteIPHost(new IPHost(Touch_JsonWebSocket_Client_IPPort));

                //载入配置
                this.Client.Setup(config);

                this.Client.ConnectAsync();

            }
            catch (Exception ex)
            {
                UnityDebugLogger.Default.Exception(ex);
            }
        }
        /// <summary>
        /// 状态打印插件
        /// </summary>
        private class Touch_JsonWebSocket_ClientPlugin : PluginBase, IWebSocketClosedPlugin, IWebSocketHandshakedPlugin, IWebSocketReceivedPlugin
        {
            private readonly ILog m_logger;
            public Touch_JsonWebSocket_ClientPlugin(ILog logger)
            {

                this.m_logger = logger;
            }
            public async Task OnWebSocketClosed(IWebSocket webSocket, ClosedEventArgs e)
            {
                this.m_logger.Info("Touch_JsonWebSocket_Client 会话断开");
                await e.InvokeNext();
            }

            public async Task OnWebSocketHandshaked(IWebSocket webSocket, HttpContextEventArgs e)
            {
                this.m_logger.Info("Touch_JsonWebSocket_Client 客户端连接成功");
                await e.InvokeNext();
            }

            public async Task OnWebSocketReceived(IWebSocket webSocket, WSDataFrameEventArgs e)
            {
                this.m_logger.Info("Touch_JsonWebSocket_Client 收到数据:" + e.DataFrame.ToText());
                await e.InvokeNext();
            }
        }
    }

    /// <summary>
    /// 客户端服务
    /// </summary>
    public class Reverse2DSquareRpcServer : SingletonRpcServer
    {
        private readonly ILog m_logger;
        public Reverse2DSquareRpcServer(ILog logger)
        {
            this.m_logger = logger;
        }

        [Description("更新位置")]
        [JsonRpc(MethodInvoke = true)]
        public void UpdatePosition(ICallContext callContext, int id, System.Numerics.Vector3 vector3, long time)
        {
            if (callContext.Caller is Touch_2DWebSocket_Client client)
            {
                if (time > client.Server_Timer)
                {
                    client.Server_Timer = time;
                }
                else
                {
                    //可能是过期的包
                    return;
                }
                var npcPool = ContainerService.Resolve<NPCPool>();
                if (npcPool != null && npcPool.TryGetValue(id, out var npc))
                {
                    npc.UpdatePosition(new UnityEngine.Vector3(vector3.X, vector3.Y, vector3.Z));
                }
            }

        }

        [Description("创建新的NPC")]
        [JsonRpc(MethodInvoke = true)]
        public void NewNPC(ICallContext callContext, int id, System.Numerics.Vector3 vector3)
        {
            var npcPool = ContainerService.Resolve<NPCPool>();
            if (npcPool != null)
            {
                npcPool.CreateCharacter(id, new UnityEngine.Vector3(vector3.X, vector3.Y, vector3.Z));
            }
        }
        [Description("玩家离线")]
        [JsonRpc(MethodInvoke = true)]
        public void Offline(ICallContext callContext, int id)
        {
            var npcPool = ContainerService.Resolve<NPCPool>();
            if (npcPool != null)
            {

                npcPool.DestroyNPC(id);
            }
        }

        [Description("玩家登陆")]
        [JsonRpc(MethodInvoke = true)]
        public void PlayerLogin(ICallContext callContext, int id)
        {
            if (callContext.Caller is Touch_2DWebSocket_Client client)
            {
                var gamePlayer = ContainerService.Resolve<GamePlayer>();
                var npcPool = ContainerService.Resolve<NPCPool>();
                if (gamePlayer != null && npcPool != null && npcPool.TryGetValue(id, out var playUnit))
                {
                    gamePlayer.ID = id;
                    gamePlayer.GamePlayerUnit = playUnit;
                }
            }

        }

    }
    public class Touch_2DWebSocket_Client : UnityWebSocketJsonRpcClient
    {
        /// <summary>
        /// 服务器时间
        /// </summary>
        public long Server_Timer { get; set; }



        public Touch_2DWebSocket_Client() : base()
        {


        }

    }
}
