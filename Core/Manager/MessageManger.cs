using System;
using System.Collections.Generic;

/// <summary>
/// 消息中心，支持无参和任意类型参数的消息注册与发送
/// 超过3个参数时使用 ValueTuple 或自定义结构体作为泛型参数
/// </summary>
public class MessageManger : Singleton<MessageManger>
{
    private readonly Dictionary<string, Delegate> _events = new Dictionary<string, Delegate>();

    #region Register

    public void Register(string msg, Action callback)
    {
        AddListener(msg, callback);
    }

    public void Register<T>(string msg, Action<T> callback)
    {
        AddListener(msg, callback);
    }

    public void Register<T1, T2>(string msg, Action<T1, T2> callback)
    {
        AddListener(msg, callback);
    }

    public void Register<T1, T2, T3>(string msg, Action<T1, T2, T3> callback)
    {
        AddListener(msg, callback);
    }

    #endregion

    #region Unregister

    public void Unregister(string msg, Action callback)
    {
        RemoveListener(msg, callback);
    }

    public void Unregister<T>(string msg, Action<T> callback)
    {
        RemoveListener(msg, callback);
    }

    public void Unregister<T1, T2>(string msg, Action<T1, T2> callback)
    {
        RemoveListener(msg, callback);
    }

    public void Unregister<T1, T2, T3>(string msg, Action<T1, T2, T3> callback)
    {
        RemoveListener(msg, callback);
    }

    #endregion

    #region Send

    public void Send(string msg)
    {
        if (_events.TryGetValue(msg, out var d))
            (d as Action)?.Invoke();
    }

    public void Send<T>(string msg, T arg)
    {
        if (_events.TryGetValue(msg, out var d))
            (d as Action<T>)?.Invoke(arg);
    }

    public void Send<T1, T2>(string msg, T1 arg1, T2 arg2)
    {
        if (_events.TryGetValue(msg, out var d))
            (d as Action<T1, T2>)?.Invoke(arg1, arg2);
    }

    public void Send<T1, T2, T3>(string msg, T1 arg1, T2 arg2, T3 arg3)
    {
        if (_events.TryGetValue(msg, out var d))
            (d as Action<T1, T2, T3>)?.Invoke(arg1, arg2, arg3);
    }

    #endregion

    /// <summary>
    /// 通过 Delegate 移除监听（供内部自动清理使用）
    /// </summary>
    public void Unregister(string msg, Delegate callback)
    {
        RemoveListener(msg, callback);
    }

    /// <summary>
    /// 移除某条消息的所有监听
    /// </summary>
    public void UnregisterAll(string msg)
    {
        _events.Remove(msg);
    }

    /// <summary>
    /// 清空所有消息监听
    /// </summary>
    public void Clear()
    {
        _events.Clear();
    }

    private void AddListener(string msg, Delegate callback)
    {
        if (_events.TryGetValue(msg, out var existing))
            _events[msg] = Delegate.Combine(existing, callback);
        else
            _events[msg] = callback;
    }

    private void RemoveListener(string msg, Delegate callback)
    {
        if (!_events.TryGetValue(msg, out var existing))
            return;

        var result = Delegate.Remove(existing, callback);
        if (result == null)
            _events.Remove(msg);
        else
            _events[msg] = result;
    }
}
