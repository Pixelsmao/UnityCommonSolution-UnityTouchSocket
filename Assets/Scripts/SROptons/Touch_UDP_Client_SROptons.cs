using Assets.Scripts.SROptons;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using TouchSocket.Core;
using TouchSocket.Sockets;
using UnityEngine;
public partial class SROptions
{
    public const string Touch_UDP_Client_IPPort = "127.0.0.1:7791";

    /// <summary>
    /// UDP Client
    /// </summary>
    public static UdpSession Touch_UDP_Client;
    [Category("Touch UDP Client"), DisplayName("Connect")]
    public async void Touch_UDP_ClientConnection()
    {
        try
        {
            Touch_UDP_Client.SafeDispose();
            Touch_UDP_Client = new UdpSession();
            //声明配置
            var config = new TouchSocketConfig();
            config.ConfigureContainer(a =>
            {
                a.AddLogger(UnityDebugLogger.Default);
            })
                .ConfigurePlugins(a =>
                {
                    a.Add<Touch_UDP_ClientPlugin>();
                })
                .SetRemoteIPHost(new IPHost(Touch_UDP_Client_IPPort))
                .SetUdpDataHandlingAdapter(() => new NormalUdpDataHandlingAdapter())
                .SetBindIPHost(new IPHost(0));//0端口即为随机端口;

            //载入配置
            await Touch_UDP_Client.SetupAsync(config);

            await Touch_UDP_Client.StartAsync();

        }
        catch (Exception ex)
        {
            UnityDebugLogger.Default.Exception(ex);
        }
    }

    [Category("Touch UDP Client"), DisplayName("Send Text")]
    public async void Touch_UDP_ClientSendText()
    {
        try
        {
            await Touch_UDP_Client.SendAsync("Hello Touch-UDP-Client!");
        }
        catch (Exception)
        {
            Debug.Log("Touch_UDP_Client 未在线");
        }
    }

    [Category("Touch UDP Client"), DisplayName("Send Text X100")]
    public async void Touch_UDP_ClientSendTextX100()
    {
        try
        {
            for (var i = 0; i < 100; i++)
            {
                await Touch_UDP_Client.SendAsync("Hello Touch-UDP-Client!" + i);
            }
        }
        catch (Exception )
        {
            Debug.Log("Touch_UDP_Client 未在线");
        }
    }
    [Category("Touch UDP Client"), DisplayName("Close")]
    public async void Touch_UDP_ClientClose()
    {
        try
        {
            await Touch_UDP_Client.StopAsync();
        }
        catch (Exception )
        {
            Debug.Log("Touch_UDP_Client 未在线");
        }
    }

    private class Touch_UDP_ClientPlugin : PluginBase, IUdpReceivingPlugin
    {
        public async Task OnUdpReceiving(IUdpSessionBase client, UdpReceiveingEventArgs e)
        {
            Touch_UDP_Client.Logger.Info("Touch_UDP_Client 收到数据:" + e.ByteBlock.ToString());
            await e.InvokeNext();
        }
    }
}

