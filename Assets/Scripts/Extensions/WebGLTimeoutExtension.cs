using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;

internal static class WebGLTimeoutExtension
{
    private static MonoBehaviour s_monoBehaviour;

    public static void Init(MonoBehaviour monoBehaviour)
    {
        s_monoBehaviour = monoBehaviour;
    }

    public static async Task<TResult> WaitTimeOnWebGLAsync<TResult>(this Task<TResult> task, TimeSpan timeout)
    {
        var tcs = new TaskCompletionSource<TResult>();

        // 启动一个协程来处理超时
        StartCoroutine(TimeoutCoroutine(tcs, timeout));

        // 等待任务完成或超时
        var completedTask = await Task.WhenAny(task, tcs.Task);

        if (completedTask == task)
        {
            // 如果任务先完成，取消超时任务
            tcs.TrySetCanceled();
            return await task;
        }
        else
        {
            // 如果超时任务先完成，抛出超时异常
            throw new TimeoutException();
        }
    }

    public static async Task WaitTimeOnWebGLAsync(this Task task, TimeSpan timeout)
    {
        var tcs = new TaskCompletionSource<bool>();

        // 启动一个协程来处理超时
        StartCoroutine(TimeoutCoroutine(tcs, timeout));

        // 等待任务完成或超时
        var completedTask = await Task.WhenAny(task, tcs.Task);

        if (completedTask == task)
        {
            // 如果任务先完成，取消超时任务
            tcs.TrySetCanceled();
            await task;
        }
        else
        {
            // 如果超时任务先完成，抛出超时异常
            throw new TimeoutException();
        }
    }

    private static void StartCoroutine(IEnumerator enumerator)
    {
        if (s_monoBehaviour is null)
        {
            throw new Exception($"{typeof(WebGLTimeoutExtension)} not Init");
        }
        s_monoBehaviour.StartCoroutine(enumerator);
    }

    private static System.Collections.IEnumerator TimeoutCoroutine<TResult>(TaskCompletionSource<TResult> tcs, TimeSpan timeout)
    {
        yield return new WaitForSeconds((float)timeout.TotalSeconds);
        tcs.TrySetException(new TimeoutException());
    }
}