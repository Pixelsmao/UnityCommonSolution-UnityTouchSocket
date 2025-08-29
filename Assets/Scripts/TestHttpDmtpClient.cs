using System;
using System.Threading;
using System.Threading.Tasks;
using TouchSocket.Core;
using TouchSocket.Dmtp;
using TouchSocket.Dmtp.Rpc;
using TouchSocket.Sockets;
using UnityEngine;
using UnityEngine.UI;
using UnityRpcProxy;

public class TestTcpDmtpClient : MonoBehaviour
{
    // Start is called before the first frame update
    private void Start()
    {
    }

    public InputField inputField_Iphost;
    public InputField inputField_Account;
    public InputField inputField_Password;

    private HttpDmtpClient m_client;

    public async void Connect()
    {
        try
        {
            this.m_client.SafeDispose();
            this.m_client = new HttpDmtpClient();
            await this.m_client.SetupAsync(new TouchSocketConfig()
                 .SetRemoteIPHost(this.inputField_Iphost.text)
                 .SetDmtpOption(new DmtpOption()
                 {
                     VerifyToken = "Dmtp"
                 })
                 .ConfigureContainer(a =>
                 {
                     a.AddLogger(UnityLog.Logger);
                 })
                 .ConfigurePlugins(a =>
                 {
                     a.UseDmtpRpc();
                     a.Add<MyTcpRpcPlugin>();
                 }));
            await this.m_client.ConnectAsync();

            UnityLog.Logger.Info("success");
        }
        catch (Exception ex)
        {
            UnityLog.Logger.Exception(ex);
        }
    }

    public async void Login()
    {
        try
        {
            //直接调用时，第一个参数为调用键，服务类全名+方法名（必须全小写）
            //第二个参数为调用配置参数，可设置调用超时时间，取消调用等功能。
            //后续参数为调用参数。
            //bool result = client.Invoke<bool>("Login", InvokeOption.WaitInvoke, "123", "abc");

            var result = await this.m_client.GetDmtpRpcActor().DmtpRpc_LoginAsync(new MyLoginModel() { Account = this.inputField_Account.text, Password = this.inputField_Password.text });
            UnityLog.Logger.Info($"结果：{result.ResultCode}，消息：{result.Message}");
        }
        catch (Exception ex)
        {
            UnityLog.Logger.Exception(ex);
        }
    }

    public void Performance_1()
    {
        Task.Run(() =>
        {
            var count = 0;
            var timespan = TimeMeasurer.Run(async () =>
              {
                  for (var i = 0; i < 10000; i++)
                  {
                      try
                      {
                          if (this.m_client.Online)
                          {
                              var result = await this.m_client.GetDmtpRpcActor().DmtpRpc_PerformanceAsync(i);
                              if (result != i + 1)
                              {
                                  UnityLog.Logger.Info($"调用结果不一致，应为：{i + 1}，实际：{result}");
                              }
                              else
                              {
                                  count++;
                              }
                          }
                      }
                      catch (Exception ex)
                      {
                          UnityLog.Logger.Exception(ex);
                      }
                  }
              });

            UnityLog.Logger.Info($"调用结束，成功调用{count}次，耗时：{timespan}");
        });
    }

    private readonly CancellationTokenSource tokenSource;

    public async void SendStream()
    {
        await Task.Run(() =>
        {
            //Timer timer = default;
            //MemoryStream memoryStream = new MemoryStream(1024 * 1024 * 100);
            //memoryStream.SetLength(1024 * 1024 * 100);
            //memoryStream.Seek(0, SeekOrigin.Begin);
            //try
            //{
            //    var timespan = TimeMeasurer.Run(() =>
            //    {
            //        tokenSource = new CancellationTokenSource();
            //        StreamOperator streamOperator = new StreamOperator();
            //        streamOperator.Token = tokenSource.Token;
            //        timer = new Timer((o) =>
            //        {
            //            if (streamOperator.Result.ResultCode == TouchSocket.Core.ResultCode.Default)
            //            {
            //                UnityLog.Logger.Info($"流传输中，进度={streamOperator.Progress}，速度={streamOperator.Speed()}");
            //            }
            //        }, null, 0, 1000);
            //        var result = this.m_client.SendStream(memoryStream, streamOperator);
            //        UnityLog.Logger.Info($"流传输结束，结果={result}");
            //        timer.Dispose();
            //    });

            //    UnityLog.Logger.Info($"发送结束，耗时：{timespan}");
            //}
            //catch (Exception ex)
            //{
            //    UnityLog.Logger.Exception(ex);
            //}
            //finally
            //{
            //    memoryStream.SafeDispose();
            //    timer.SafeDispose();
            //}
        });
    }

    public void CancelStream()
    {
        this.tokenSource?.Cancel();
    }

    // Update is called once per frame
    private void Update()
    {
    }
}