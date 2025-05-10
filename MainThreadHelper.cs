using System;
using System.Collections.Generic;
using UnityEngine;

namespace JetIslandArchipelago;

public class MainThreadHelper : MonoBehaviour
{
    private static readonly Queue<Action> ActionQueue = new();

    public static void Enqueue(Action action)
    {
        if(action == null) return;
        lock(ActionQueue)
            ActionQueue.Enqueue(action);
    }

    private void Awake()
    {
        Debug.Log("MainThreadHelper Started");
    }

    private void Update()
    {
        while (true)
        {
            Action action;
            lock (ActionQueue)
            {
                if (ActionQueue.Count == 0)
                    break;
                action = ActionQueue.Dequeue();
            }
            action?.Invoke();
        }
    }

    private void OnDestroy()
    {
        lock (ActionQueue)
        {
            ActionQueue.Clear();
        }
    }
}