using Assets.Scripts.SROptons;
using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using TouchSocket.Core;
using TouchSocket.Dmtp;
using TouchSocket.Dmtp.Rpc;
using TouchSocket.Rpc;
using TouchSocket.Sockets;
using UnityEngine;
using UnityRpcProxy_TCPDmtp;
public partial class SROptions
{
    public const string Touch_HttpDmtp_Client_IPPort = "127.0.0.1:7790";

    /// <summary>
    /// Touch HttpDmtp
    /// </summary>
    public static HttpDmtpClient Touch_HttpDmtp_Client;
    [Category("Touch HttpDmtp Client"), DisplayName("Connect")]
    public async void Touch_HttpDmtp_ClientConnection()
    {
        try
        {
            Touch_HttpDmtp_Client.SafeDispose();
            Touch_HttpDmtp_Client = new HttpDmtpClient();
            //声明配置
            var config = new TouchSocketConfig();
            config.ConfigureContainer(a =>
            {
                //注册rpc服务
                a.AddRpcStore(store =>
                {
                    store.RegisterServer<Touch_HttpDmtp_Client_UnityRpcStore>();
#if DEBUG
                    var code = store.GetProxyCodes("UnityRpcProxy", typeof(DmtpRpcAttribute));
                    var dirPath = "./RPCStore";
                    if (!Directory.Exists(dirPath))
                    {
                        Directory.CreateDirectory(dirPath);
                    }
                    File.WriteAllText("./RPCStore/UnityRpcProxy_Client_HttpDmtp.cs", code);
#endif
                });
                a.AddLogger(UnityDebugLogger.Default);
            })
            .ConfigurePlugins(a =>
            {
                a.UseDmtpRpc();
                a.Add<Touch_HttpDmtp_ClientPlugin>();
            })
             .SetDmtpOption(new DmtpOption()
             {
                 VerifyToken = "Dmtp"
             })
            .SetRemoteIPHost(new IPHost(Touch_HttpDmtp_Client_IPPort));

            //载入配置
            await Touch_HttpDmtp_Client.SetupAsync(config);

            await Touch_HttpDmtp_Client.ConnectAsync();

        }
        catch (Exception ex)
        {
            UnityDebugLogger.Default.Exception(ex);
        }
    }

    [Category("Touch HttpDmtp Client"), DisplayName("Login RPC")]
    public async void Touch_HttpDmtp_ClientLogin()
    {
        if (Touch_HttpDmtp_Client.Online)
        {
            var result = await Touch_HttpDmtp_Client.GetDmtpRpcActor().DmtpRpc_LoginAsync(new LoginModel() { Account = "123", Password = "123" });
            Touch_HttpDmtp_Client.Logger.Info(result.Message);
        }
        else
        {
            Debug.Log("Touch_HttpDmtp_Client 未在线");
        }
    }
    [Category("Touch HttpDmtp Client"), DisplayName("Dmtp Channel")]
    public async void Touch_HttpDmtp_ClientSendText()
    {
        if (Touch_HttpDmtp_Client.Online)
        {

            using (var channel = Touch_HttpDmtp_Client.CreateChannel())
            {
                var count = 1024 * 1;//测试1Gb数据
                //设置限速
                //channel.MaxSpeed = 1024 * 1024;

                Touch_HttpDmtp_Client.Logger.Info($"通道创建成功，即将写入{count}Mb数据");
                var bytes = new byte[1024 * 1024];
                for (var i = 0; i < count; i++)
                {
                    //2.持续写入数据
                    await channel.WriteAsync(bytes);
                }

                //3.在写入完成后调用终止指令。例如：Complete、Cancel、HoldOn、Dispose等
                await channel.CompleteAsync("我完成了");
                Touch_HttpDmtp_Client.Logger.Info("通道写入结束");

            }

            await Touch_HttpDmtp_Client.DmtpActor.SendStringAsync(DmtpActor.P1_Handshake_Request, "Hello Touch-HttpDmtp-Client!");
        }
        else
        {
            Debug.Log("Touch_HttpDmtp_Client 未在线");
        }
    }

    [Category("Touch HttpDmtp Client"), DisplayName("Login RPC X100")]
    public async void Touch_HttpDmtp_ClientLoginX100()
    {
        if (Touch_HttpDmtp_Client.Online)
        {
            for (var i = 0; i < 100; i++)
            {
                await Touch_HttpDmtp_Client.GetDmtpRpcActor().DmtpRpc_LoginAsync(new LoginModel() { Account = "123", Password = "123" });
            }
        }
        else
        {
            Debug.Log("Touch_HttpDmtp_Client 未在线");
        }
    }
    [Category("Touch HttpDmtp Client"), DisplayName("Close")]
    public async void Touch_HttpDmtp_ClientClose()
    {
        if (Touch_HttpDmtp_Client.Online)
        {
            await Touch_HttpDmtp_Client.CloseAsync();
        }
        else
        {
            Debug.Log("Touch_HttpDmtp_Client 未在线");
        }
    }

    private class Touch_HttpDmtp_ClientPlugin : PluginBase, IDmtpReceivedPlugin, IDmtpClosedPlugin, IDmtpHandshakedPlugin
    {
        public async Task OnDmtpClosed(IDmtpActorObject client, ClosedEventArgs e)
        {
            Touch_HttpDmtp_Client.Logger.Info("Touch_HttpDmtp_Client 会话断开");
            await e.InvokeNext();
        }

        public async Task OnDmtpHandshaked(IDmtpActorObject client, DmtpVerifyEventArgs e)
        {
            Touch_HttpDmtp_Client.Logger.Info("Touch_HttpDmtp_Client 客户端连接成功");
            await e.InvokeNext();
        }

        public async Task OnDmtpReceived(IDmtpActorObject client, DmtpMessageEventArgs e)
        {
            Touch_HttpDmtp_Client.Logger.Info("Touch_HttpDmtp_Client 收到数据:");
            await e.InvokeNext();
        }
    }

    public class Touch_HttpDmtp_Client_UnityRpcStore : SingletonRpcServer
    {
        [DmtpRpc(MethodInvoke = true)]
        public int RandomNumber(ICallContext callContext, int a, int b)
        {
            //每一秒随机从服务器发送数据到客户端演示反向调用
            if (callContext.Caller is HttpDmtpClient clientSession)
            {
                clientSession.Logger.Info("服务端发来RPC数据");
            }
            return a + b;
        }
    }

}
