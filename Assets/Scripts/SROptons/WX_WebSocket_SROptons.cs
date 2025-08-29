using System.ComponentModel;
using UnityEngine;
using UnityWebSocket;
/// <summary>
/// 微信Web Socket通信方案
/// </summary>
public partial class SROptions
{
    public const string WX_WebSocket_IPPort = "ws://127.0.0.1:7792/ws";

    /// <summary>
    /// WX SDK WebSocket
    /// </summary>
    public WebSocket WX_WebSocket;
    [Category("WX WebSocket"), DisplayName("Connect")]
    public void WX_WebSocketConnection()
    {
        this.WX_WebSocket = new WebSocket(WX_WebSocket_IPPort);
        this.WX_WebSocket.OnOpen += this.WebSocketOnOPenHandle;
        this.WX_WebSocket.OnClose += this.WebSocketOnCloseHandle;
        this.WX_WebSocket.OnError += this.WebSocketOnErrorHandle;
        this.WX_WebSocket.OnMessage += this.WebSocketOnMessageHandle;

        this.WX_WebSocket.ConnectAsync();
    }

    [Category("WX WebSocket"), DisplayName("Send Text")]
    public void WX_WebSocketSendText()
    {
        if (this.WX_WebSocket?.ReadyState == WebSocketState.Open)
        {
            this.WX_WebSocket.SendAsync("Hello Wx_WebSocket!");
        }
        else
        {
            Debug.Log("WX WebSocket 连接未打开");
        }
    }

    [Category("WX WebSocket"), DisplayName("Send Text X100")]
    public void WX_WebSocketSendTextX100()
    {
        if (this.WX_WebSocket?.ReadyState == WebSocketState.Open)
        {
            for (var i = 0; i < 100; i++)
            {
                this.WX_WebSocket.SendAsync("Hello Wx_WebSocket!" + i);
            }
        }
        else
        {
            Debug.Log("WX WebSocket 连接未打开");
        }
    }

    [Category("WX WebSocket"), DisplayName("Close")]
    public void WX_WebSocketClose()
    {
        if (this.WX_WebSocket?.ReadyState == WebSocketState.Open)
        {
            this.WX_WebSocket.CloseAsync();
        }
        else
        {
            Debug.Log("WX WebSocket 连接未打开");
        }
    }

    private void WebSocketOnOPenHandle(object sender, OpenEventArgs e)
    {
        Debug.Log("WX WebSocket 连接成功");
    }
    private void WebSocketOnCloseHandle(object sender, CloseEventArgs e)
    {
        Debug.Log("WX WebSocket 连接关闭");
    }
    private void WebSocketOnErrorHandle(object sender, ErrorEventArgs e)
    {
        Debug.Log("WX WebSocket 连接异常" + e.Message);
    }
    private void WebSocketOnMessageHandle(object sender, MessageEventArgs e)
    {
        Debug.Log("WX WebSocket 接收信息，" + $"{e.IsText},{e.Data}");

    }
}