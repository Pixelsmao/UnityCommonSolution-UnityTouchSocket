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
using UnityEngine;
using UnityRpcProxy_Json_HttpDmtp;
public partial class SROptions
{
    public const string Touch_JsonWebSocket_Client_IPPort = "ws://127.0.0.1:7793/ws";

    /// <summary>
    /// Touch JsonWebSocket
    /// </summary>
    public static UnityWebSocketJsonRpcClient Touch_JsonWebSocket_Client;
    [Category("Touch JsonWebSocket Client"), DisplayName("Connect")]
    public async void Touch_JsonWebSocket_ClientConnection()
    {
        try
        {
            Touch_JsonWebSocket_Client.SafeDispose();
            Touch_JsonWebSocket_Client = new UnityWebSocketJsonRpcClient();
            //声明配置
            var config = new TouchSocketConfig();
            config.ConfigureContainer(a =>
            {
                a.AddLogger(UnityDebugLogger.Default);
                a.AddRpcStore(store =>
                {
                    store.RegisterServer<ReverseJsonRpcServer>();

#if DEBUG
                    var code = store.GetProxyCodes("UnityRpcProxy", typeof(JsonRpcAttribute));
                    var dirPath = "./RPCStore";
                    if (!Directory.Exists(dirPath))
                    {
                        Directory.CreateDirectory(dirPath);
                    }
                    File.WriteAllText("./RPCStore/UnityRpcProxy_Client_JsonRPCDmtp.cs", code);
#endif
                });
            })
            .ConfigurePlugins(a =>
            {
                a.Add<Touch_JsonWebSocket_ClientPlugin>();
            })

            .SetRemoteIPHost(new IPHost(Touch_JsonWebSocket_Client_IPPort));

            //载入配置
            await Touch_JsonWebSocket_Client.SetupAsync(config);

            await Touch_JsonWebSocket_Client.ConnectAsync();

        }
        catch (Exception ex)
        {
            UnityDebugLogger.Default.Exception(ex);
        }
    }

    [Category("Touch JsonWebSocket Client"), DisplayName("Login RPC")]
    public async void Touch_JsonWebSocket_ClientLogin()
    {
        if (Touch_JsonWebSocket_Client.Online)
        {
            var result = await Touch_JsonWebSocket_Client.JsonRpc_LoginAsync(new LoginModel() { Account = "123", Password = "123" });
            Touch_JsonWebSocket_Client.Logger.Info(result.Message);
        }
        else
        {
            Debug.Log("Touch_JsonWebSocket_Client 未在线");
        }
    }

    [Category("Touch JsonWebSocket Client"), DisplayName("Login RPC X100")]
    public async void Touch_JsonWebSocket_ClientLoginX100()
    {
        if (Touch_JsonWebSocket_Client.Online)
        {
            for (var i = 0; i < 100; i++)
            {
                await Touch_JsonWebSocket_Client.JsonRpc_LoginAsync(new LoginModel() { Account = "123", Password = "123" });
            }
        }
        else
        {
            Debug.Log("Touch_JsonWebSocket_Client 未在线");
        }
    }
    [Category("Touch JsonWebSocket Client"), DisplayName("Close")]
    public async void Touch_JsonWebSocket_ClientClose()
    {
        if (Touch_JsonWebSocket_Client.Online)
        {
            await Touch_JsonWebSocket_Client.CloseAsync();
        }
        else
        {
            Debug.Log("Touch_JsonWebSocket_Client 未在线");
        }
    }

    private class Touch_JsonWebSocket_ClientPlugin : PluginBase, IWebSocketReceivedPlugin, IWebSocketClosedPlugin, IWebSocketHandshakedPlugin
    {
        public async Task OnWebSocketClosed(IWebSocket webSocket, ClosedEventArgs e)
        {
            Touch_JsonWebSocket_Client.Logger.Info("Touch_JsonWebSocket_Client 会话断开");
            await e.InvokeNext();
        }

        public async Task OnWebSocketHandshaked(IWebSocket webSocket, HttpContextEventArgs e)
        {
            Touch_JsonWebSocket_Client.Logger.Info("Touch_JsonWebSocket_Client 客户端连接成功");
            await e.InvokeNext();
        }

        public async Task OnWebSocketReceived(IWebSocket webSocket, WSDataFrameEventArgs e)
        {
            Touch_JsonWebSocket_Client.Logger.Info("Touch_JsonWebSocket_Client 收到数据:" + e.DataFrame.ToText());
            await e.InvokeNext();
        }
    }

    public class ReverseJsonRpcServer : SingletonRpcServer
    {
        [JsonRpc(MethodInvoke = true)]
        public int Add(int a, int b)
        {
            Debug.Log($"{a}+{b}");
            return a + b;
        }
    }
}

