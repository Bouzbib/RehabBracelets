// ============================================================
//  UnityMainThreadDispatcher.cs
//  Lets background threads safely call Debug.Log and other
//  Unity APIs by queuing actions onto the main thread.
//  Attach to any persistent GameObject (same one as Haptics).
// ============================================================

using System;
using System.Collections.Generic;
using UnityEngine;

public class UnityMainThreadDispatcher : MonoBehaviour
{
    private static readonly Queue<Action> _queue = new Queue<Action>();
    private static UnityMainThreadDispatcher _instance;

    public static void Enqueue(Action action)
    {
        lock (_queue) { _queue.Enqueue(action); }
    }

    void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        lock (_queue)
        {
            while (_queue.Count > 0)
                _queue.Dequeue().Invoke();
        }
    }
}
