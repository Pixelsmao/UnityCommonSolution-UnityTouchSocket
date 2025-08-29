using System;
using System.Text;
using TouchSocket.Core;
using UnityEngine;

namespace Assets.Scripts.SROptons
{
    /// <summary>
    /// Unity 日志插件
    /// </summary>
    public class UnityDebugLogger : LoggerBase
    {
        static UnityDebugLogger()
        {
            Default = new UnityDebugLogger();
        }

        private UnityDebugLogger()
        {

        }

        /// <summary>
        /// 默认的实例
        /// </summary>
        public static UnityDebugLogger Default { get; }

        /// <inheritdoc/>
        /// <param name="logLevel"></param>
        /// <param name="source"></param>
        /// <param name="message"></param>
        /// <param name="exception"></param>
        protected override void WriteLog(LogLevel logLevel, object source, string message, Exception exception)
        {
            lock (typeof(ConsoleLogger))
            {
                var logString = new StringBuilder();
                logString.Append(DateTime.Now.ToString(this.DateTimeFormat));
                logString.Append(" | ");

                logString.Append(logLevel.ToString());
                logString.Append(" | ");
                logString.Append(message);

                if (exception != null)
                {
                    logString.Append(" | ");
                    logString.Append($"[Exception Message]：{exception.Message}");
                    logString.Append($"[Stack Trace]：{exception.StackTrace}");
                }

                switch (logLevel)
                {
                    case LogLevel.Warning:
                        Debug.LogWarning(logString.ToString());
                        break;

                    case LogLevel.Error:
                        if (exception != null)
                        {
                            throw exception;
                            //Debug.LogError(exception);
                        }
                        else
                        {
                            Debug.LogError(logString.ToString());

                        }

                        break;

                    case LogLevel.Info:
                    default:
                        Debug.Log(logString.ToString());
                        break;
                }
            }
        }
    }
}
