using System;
using System.Text;
using System.Threading.Tasks;
using TouchSocket.Core;
using TouchSocket.Sockets;
using UnityEngine;

public class TouchSocketTest : MonoBehaviour
{
    [Header("TCP Service")] public string tcpServiceIPAddress = "127.0.0.1";
    public int tcpServiceListenPort = 5001;
    [Header("TCP Client")] public string tcpClientConnectIPAddress = "127.0.0.1";
    public int tcpClientConnectPort = 5002;
    [Header("UDP Service")] public string udpServiceListenIP = "127.0.0.1";
    public int udpServiceListenPort = 5003;
    [Header("UDP Client")] public int udpClientBindPort;
    public string udpClientRemoteIP = "127.0.0.1";
    public int udpClientRemotePort = 5004;
    private UdpSession udpService;
    private UdpSession udpClient;
    private TcpService tcpService;
    private TcpClient tcpClient;

    private void Start()
    {
        LaunchUdpService();
        LaunchUdpClient();
        LaunchTcpService();
        LaunchTcpClient();
    }


    private async void OnDestroy()
    {
        if (udpService != null)
        {
            await udpService.StopAsync();
            udpService.Dispose();
        }

        if (udpClient != null)
        {
            await udpClient.StopAsync();
            udpClient.Dispose();
        }

        if (tcpService != null)
        {
            await tcpService.StopAsync();
            tcpService.Dispose();
        }

        tcpClient?.SafeDispose();
    }

    private async void LaunchTcpService()
    {
        tcpService = new TcpService();
        tcpService.Connected = (client, _) =>
        {
            Debug.Log($"客户端{client.GetIPPort()}接入服务器。");
            return Task.CompletedTask;
        };
        tcpService.Received = (client, args) =>
        {
            var command = args.ByteBlock.Span.ToString(Encoding.UTF8);
            Debug.Log($"收到来自TCP客户端{client.GetIPPort()}的远程控制命令:{command} ");
            tcpService.SendAsync(client.Id, Encoding.UTF8.GetBytes("Successful received"));
            return Task.CompletedTask;
        };

        var config = new TouchSocketConfig();
        config.SetListenIPHosts($"{tcpServiceIPAddress}:{tcpServiceListenPort}");
        config.ConfigureContainer(x => x.AddConsoleLogger());
        await tcpService.SetupAsync(config);
        await tcpService.StartAsync();
        Debug.Log($"TCP服务器已启动，监听端口为：{tcpServiceListenPort}");
    }

    private async void LaunchTcpClient()
    {
        tcpClient = new TcpClient();
        var config = new TouchSocketConfig();
        config.SetRemoteIPHost($"{tcpClientConnectIPAddress}:{tcpClientConnectPort}");
        config.ConfigureContainer(a => { a.AddConsoleLogger(); });
        config.ConfigurePlugins(manager => { manager.UseTcpReconnection().UsePolling(TimeSpan.FromSeconds(1)); });
        await tcpClient.SetupAsync(config);
        tcpClient.Received = (remote, args) =>
        {
            var command = args.ByteBlock.Span.ToString(Encoding.UTF8);
            Debug.Log($"收到来自TCP服务器{remote.GetIPPort()}的远程控制命令: {command}");
            tcpClient.SendAsync("Successful received".ToUtf8Bytes());
            return Task.CompletedTask;
        };
        await tcpClient.ConnectAsync();
        Debug.Log($"TCP客户端{tcpClient.GetIPPort()}已启动，目标主机为{tcpClient.RemoteIPHost}");
    }

    private async void LaunchUdpService()
    {
        udpService.SafeDispose();
        udpService = new UdpSession();
        var bindIPHost = new IPHost($"{udpServiceListenIP}:{udpServiceListenPort}");
        var config = new TouchSocketConfig();
        config.SetBindIPHost(bindIPHost);
        await udpService.SetupAsync(config);
        await udpService.StartAsync();
        udpService.Received = (remote, args) =>
        {
            var command = Encoding.UTF8.GetString(args.ByteBlock.Span);
            Debug.Log($"收到来自UDP客户端{args.EndPoint}的远程控制命令: {command}");
            remote.SendAsync("Successful received".ToUtf8Bytes());
            return Task.CompletedTask;
        };
        Debug.Log($"UDP服务已启动，监听地址为：{udpServiceListenIP}:{udpServiceListenPort}");
    }

    private async void LaunchUdpClient()
    {
        udpClient.SafeDispose();
        udpClient = new UdpSession();
        var remoteIPHost = new IPHost($"{udpClientRemoteIP}:{udpClientRemotePort}");
        var config = new TouchSocketConfig();
        config.SetUdpDataHandlingAdapter(() => new NormalUdpDataHandlingAdapter());
        config.SetBindIPHost(new IPHost(udpClientBindPort));
        config.SetRemoteIPHost(remoteIPHost);
        await udpClient.SetupAsync(config);
        await udpClient.StartAsync();
        udpClient.Received = (remote, args) =>
        {
            var command = args.ByteBlock.Span.ToString(Encoding.UTF8);
            Debug.Log($"收到来自UDP服务器{args.EndPoint}的远程控制命令: {command}");
            remote.SendAsync(args.EndPoint, Encoding.UTF8.GetBytes("Successful Received"));
            return Task.CompletedTask;
        };
        Debug.Log($"UDP客户端已启动，监听端口为{udpClient.IsClient}");
        await udpClient.SendAsync(Encoding.UTF8.GetBytes("UDP"));
    }
}