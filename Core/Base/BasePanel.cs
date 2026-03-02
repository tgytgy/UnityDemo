using System;
using System.Collections.Generic;
using UnityEngine;

public class BasePanel : MonoBehaviour
{
    private readonly List<(string msg, Delegate callback)> _listeners = new List<(string, Delegate)>();

    public void InitByArgs(Dictionary<string, object> args)
    {
    }

    public void CloseSelf()
    {
        Destroy(gameObject);
    }

    protected virtual void OnDestroy()
    {
        for (int i = 0; i < _listeners.Count; i++)
        {
            var (msg, callback) = _listeners[i];
            MessageManger.Instance.Unregister(msg, callback);
        }
        _listeners.Clear();
    }

    #region MessageManger 便捷注册（Panel 销毁时自动移除）

    protected void Listen(string msg, Action callback)
    {
        MessageManger.Instance.Register(msg, callback);
        _listeners.Add((msg, callback));
    }

    protected void Listen<T>(string msg, Action<T> callback)
    {
        MessageManger.Instance.Register(msg, callback);
        _listeners.Add((msg, callback));
    }

    protected void Listen<T1, T2>(string msg, Action<T1, T2> callback)
    {
        MessageManger.Instance.Register(msg, callback);
        _listeners.Add((msg, callback));
    }

    protected void Listen<T1, T2, T3>(string msg, Action<T1, T2, T3> callback)
    {
        MessageManger.Instance.Register(msg, callback);
        _listeners.Add((msg, callback));
    }

    #endregion
}
