using System.Threading.Tasks;
using TouchSocket.Core;
using TouchSocket.Dmtp;
using TouchSocket.Sockets;

internal class MyTcpRpcPlugin : PluginBase, IDmtpHandshakedPlugin, IDmtpClosedPlugin
{
    public MyTcpRpcPlugin(ILog logger)
    {
        this.Logger = logger;
    }

    public ILog Logger { get; }

    public async Task OnDmtpHandshaked(IDmtpActorObject client, DmtpVerifyEventArgs e)
    {
        this.Logger.Info($"Dmtp客户端：已连接");
        await e.InvokeNext();
    }

    public async Task OnDmtpClosed(IDmtpActorObject client, ClosedEventArgs e)
    {
        this.Logger.Info($"客户端：已断开连接");
        await e.InvokeNext();
    }
}