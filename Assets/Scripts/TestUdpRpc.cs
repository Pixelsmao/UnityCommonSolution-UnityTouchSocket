//using System;
//using System.Threading.Tasks;
//using TcpRpcProxy;
//using TouchSocket.Sockets;
//using UnityEngine;
//using UnityEngine.UI;

//public class TestUdpRpc : MonoBehaviour
//{
//    // Start is called before the first frame update
//    private void Start()
//    {
//    }

//    // Update is called once per frame
//    private void Update()
//    {
//    }

//    public InputField inputField_IpHost;
//    public InputField inputField_Account;
//    public InputField inputField_Password;

//    public void InitUdp()
//    {
//        try
//        {
//            m_client.SafeDispose();
//            m_client = new UdpTouchRpc();
//            m_client.Setup(new TouchSocketConfig()
//                .SetBindIPHost(0)
//                .SetRemoteIPHost(inputField_IpHost.text));//设置目标地址。
//            m_client.Start();
//            UnityLog.Logger.Info($"UdpRpc初始化完成");
//        }
//        catch (Exception ex)
//        {
//            UnityLog.Logger.Exception(ex);
//        }
//    }

//    private UdpTouchRpc m_client;

//    public void Login()
//    {
//        try
//        {
//            var result = m_client.Login(new MyLoginModel() { Account = inputField_Account.text, Password = inputField_Password.text });
//            UnityLog.Logger.Info($"结果：{result.Status}，消息：{result.Message}");
//        }
//        catch (Exception ex)
//        {
//            UnityLog.Logger.Exception(ex);
//        }
//    }

//    public void Performance_1()
//    {
//        Task.Run(() =>
//        {
//            int count = 0;
//            var timespan = TimeMeasurer.Run(() =>
//            {
//                for (int i = 0; i < 10000; i++)
//                {
//                    try
//                    {
//                        if (m_client != null)
//                        {
//                            var result = m_client.Performance(i);
//                            if (result != i + 1)
//                            {
//                                UnityLog.Logger.Info($"调用结果不一致，应为：{i + 1}，实际：{result}");
//                            }
//                            else
//                            {
//                                count++;
//                            }
//                        }
//                    }
//                    catch (Exception ex)
//                    {
//                        UnityLog.Logger.Exception(ex);
//                    }
//                }
//            });

//            UnityLog.Logger.Info($"调用结束，成功调用{count}次，耗时：{timespan}");
//        });
//    }
//}