using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System;
using UnityEngine;

public class UnityMainThreadDispatcher : Singleton<UnityMainThreadDispatcher>
{
    private ConcurrentQueue<Action> _actionQueue = new();
    private Action _action;
    
    private void Update()
    {
        if (_actionQueue.Count == 0)
        {
            return;
        }
        
        while (!_actionQueue.TryDequeue(out _action))
        {
            continue;
        }

        _action?.Invoke();
    }

    public void Enqueue(Action action)
    {
        _actionQueue.Enqueue(action);
    }
}