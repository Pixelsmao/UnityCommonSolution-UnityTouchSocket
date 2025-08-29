using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;

public class Loom : MonoBehaviour
{
    public static int maxThreads = 8;
    private static int numThreads;

    private static Loom _current;

    //private int _count;
    public static Loom Current
    {
        get
        {
            Initialize();
            return _current;
        }
    }

    private void Awake()
    {
        _current = this;
        initialized = true;
    }

    private static bool initialized;

    public static void Initialize()
    {
        if (!initialized)
        {
            if (!Application.isPlaying)
                return;
            initialized = true;
            var g = new GameObject("Loom");
            _current = g.AddComponent<Loom>();
#if !ARTIST_BUILD
            UnityEngine.Object.DontDestroyOnLoad(g);
#endif
        }
    }

    public struct NoDelayedQueueItem
    {
        public Action<object> action;
        public object param;
    }

    private readonly List<NoDelayedQueueItem> _actions = new List<NoDelayedQueueItem>();

    public struct DelayedQueueItem
    {
        public float time;
        public Action<object> action;
        public object param;
    }

    private readonly List<DelayedQueueItem> _delayed = new List<DelayedQueueItem>();

    private readonly List<DelayedQueueItem> _currentDelayed = new List<DelayedQueueItem>();

    public static void QueueOnMainThread(Action<object> taction, object tparam)
    {
        QueueOnMainThread(taction, tparam, 0f);
    }

    public static void QueueOnMainThread(Action taction)
    {
        QueueOnMainThread(obj =>
        {
            taction?.Invoke();
        }, null, 0f);
    }

    public static void QueueOnMainThread(Action<object> taction, object tparam, float time)
    {
        if (time != 0)
        {
            lock (Current._delayed)
            {
                Current._delayed.Add(new DelayedQueueItem { time = Time.time + time, action = taction, param = tparam });
            }
        }
        else
        {
            lock (Current._actions)
            {
                Current._actions.Add(new NoDelayedQueueItem { action = taction, param = tparam });
            }
        }
    }

    public static Thread RunAsync(Action a)
    {
        Initialize();
        while (numThreads >= maxThreads)
        {
            Thread.Sleep(100);
        }
        Interlocked.Increment(ref numThreads);
        ThreadPool.QueueUserWorkItem(RunAction, a);
        return null;
    }

    private static void RunAction(object action)
    {
        try
        {
            ((Action)action)();
        }
        catch
        {
        }
        finally
        {
            Interlocked.Decrement(ref numThreads);
        }
    }

    private void OnDisable()
    {
        if (_current == this)
        {
            _current = null;
        }
    }

    // Use this for initialization
    private void Start()
    {
    }

    private readonly List<NoDelayedQueueItem> _currentActions = new List<NoDelayedQueueItem>();

    // Update is called once per frame
    private void Update()
    {
        if (this._actions.Count > 0)
        {
            lock (this._actions)
            {
                this._currentActions.Clear();
                this._currentActions.AddRange(this._actions);
                this._actions.Clear();
            }
            for (var i = 0; i < this._currentActions.Count; i++)
            {
                this._currentActions[i].action(this._currentActions[i].param);
            }
        }

        if (this._delayed.Count > 0)
        {
            lock (this._delayed)
            {
                this._currentDelayed.Clear();
                this._currentDelayed.AddRange(this._delayed.Where(d => d.time <= Time.time));
                for (var i = 0; i < this._currentDelayed.Count; i++)
                {
                    this._delayed.Remove(this._currentDelayed[i]);
                }
            }

            for (var i = 0; i < this._currentDelayed.Count; i++)
            {
                this._currentDelayed[i].action(this._currentDelayed[i].param);
            }
        }
    }
}