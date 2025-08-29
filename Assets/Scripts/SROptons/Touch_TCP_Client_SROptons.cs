using Assets.Scripts.SROptons;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using TouchSocket.Core;
using TouchSocket.Sockets;
using UnityEngine;
public partial class SROptions
{
    public const string Touch_TCP_Client_IPPort = "127.0.0.1:7789";

    /// <summary>
    /// WX SDK WebSocket
    /// </summary>
    public static TcpClient Touch_TCP_Client;
    [Category("Touch TCP Client"), DisplayName("Connect")]
    public async void Touch_TCP_ClientConnection()
    {
        try
        {
            Touch_TCP_Client.SafeDispose();
            Touch_TCP_Client = new TcpClient();
            //声明配置
            var config = new TouchSocketConfig();
            config.ConfigureContainer(a =>
            {
                a.AddLogger(UnityDebugLogger.Default);
            })
                .ConfigurePlugins(a =>
                {
                    a.Add<Touch_TCP_ClientPlugin>();
                })
                .SetRemoteIPHost(new IPHost(Touch_TCP_Client_IPPort))
                .SetTcpDataHandlingAdapter(() => new FixedHeaderPackageAdapter());

            //载入配置
            await Touch_TCP_Client.SetupAsync(config);

            await Touch_TCP_Client.ConnectAsync();

        }
        catch (Exception ex)
        {
            UnityDebugLogger.Default.Exception(ex);
        }
    }

    [Category("Touch TCP Client"), DisplayName("Send Text")]
    public async void Touch_TCP_ClientSendText()
    {
        if (Touch_TCP_Client.Online)
        {
            await Touch_TCP_Client.SendAsync("Hello Touch-TCP-Client!");
        }
        else
        {
            Debug.Log("Touch_TCP_Client 未在线");
        }
    }

    [Category("Touch TCP Client"), DisplayName("Send Text X100")]
    public async void Touch_TCP_ClientSendTextX100()
    {
        if (Touch_TCP_Client.Online)
        {
            for (var i = 0; i < 100; i++)
            {
                await Touch_TCP_Client.SendAsync("Hello Touch-TCP-Client!" + i);
            }
        }
        else
        {
            Debug.Log("Touch_TCP_Client 未在线");
        }
    }
    [Category("Touch TCP Client"), DisplayName("Close")]
    public async void Touch_TCP_ClientClose()
    {
        if (Touch_TCP_Client.Online)
        {
            await Touch_TCP_Client.CloseAsync();
        }
        else
        {
            Debug.Log("Touch_TCP_Client 未在线");
        }
    }

    private class Touch_TCP_ClientPlugin : PluginBase, ITcpConnectedPlugin, ITcpClosedPlugin, ITcpReceivedPlugin
    {
        public async Task OnTcpClosed(ITcpSession client, ClosedEventArgs e)
        {
            Touch_TCP_Client.Logger.Info("Touch_TCP_Client 会话断开");
            await e.InvokeNext();
        }

        public async Task OnTcpConnected(ITcpSession client, ConnectedEventArgs e)
        {
            Touch_TCP_Client.Logger.Info("Touch_TCP_Client 客户端连接成功");
            await e.InvokeNext();
        }

        public async Task OnTcpReceived(ITcpSession client, ReceivedDataEventArgs e)
        {
            Touch_TCP_Client.Logger.Info("Touch_TCP_Client 收到数据:" + e.ByteBlock.ToString());
            await e.InvokeNext();
        }
    }
}

