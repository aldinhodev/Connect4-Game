using System;
using System.Collections.Generic;
using UnityEngine;

public class UnityMainThreadDispatcher : MonoBehaviour
{
    private static readonly Queue<Action> _executionQueue = new Queue<Action>();
    private static UnityMainThreadDispatcher _instance;

    public static UnityMainThreadDispatcher Instance()
    {
        if (_instance == null)
        {
            var go = new GameObject("UnityMainThreadDispatcher");
            _instance = go.AddComponent<UnityMainThreadDispatcher>();
            DontDestroyOnLoad(go);
        }
        return _instance;
    }

    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    public void Enqueue(Action action)
    {
        if (action == null) throw new ArgumentNullException(nameof(action));
        lock (_executionQueue) { _executionQueue.Enqueue(action); }
    }

    void Update()
    {
        // Execute all actions queued by background threads
        Action action = null;
        while (true)
        {
            lock (_executionQueue)
            {
                if (_executionQueue.Count > 0) action = _executionQueue.Dequeue();
                else action = null;
            }
            if (action == null) break;
            try { action.Invoke(); }
            catch (Exception ex) { Debug.LogError("Erro em ação enfileirada: " + ex); }
        }
    }
}
