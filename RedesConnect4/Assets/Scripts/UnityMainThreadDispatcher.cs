using System;
using System.Collections.Generic;
using UnityEngine;

public class UnityMainThreadDispatcher : MonoBehaviour
{
    private static readonly Queue<Action> _executionQueue = new Queue<Action>();
    private static UnityMainThreadDispatcher _instance = null;

    public static UnityMainThreadDispatcher Instance()
{
    if (_instance == null)
    {
        _instance = FindFirstObjectByType<UnityMainThreadDispatcher>(); // novo método
        if (_instance == null)
        {
            Debug.LogError("UnityMainThreadDispatcher não foi inicializado. Por favor, adicione o script a um GameObject na cena.");
            return null;
        }
    }
    return _instance;
}


    void Awake()
    {
        if (_instance == null) _instance = this;
    }

    void Update()
    {
        lock (_executionQueue)
        {
            while (_executionQueue.Count > 0)
            {
                var action = _executionQueue.Dequeue();
                try { action?.Invoke(); }
                catch (Exception ex) { Debug.LogError("Erro em ação enfileirada: " + ex); }
            }
        }
    }

    public void Enqueue(Action action)
    {
        if (action == null) return;
        lock (_executionQueue)
        {
            _executionQueue.Enqueue(action);
        }
    }
}

