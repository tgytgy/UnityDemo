using UnityEngine;

/// <summary>
/// 子系统基类，所有子系统需继承此类
/// </summary>
public abstract class BaseSystem
{
    public bool IsInitialized { get; private set; }
    public bool IsEnabled { get; private set; }

    internal void InternalInit()
    {
        if (IsInitialized)
            return;

        OnInit();
        IsInitialized = true;
        IsEnabled = true;
        Debug.Log($"[SystemManager] 子系统 {GetType().Name} 初始化完成");
    }

    internal void InternalDestroy()
    {
        if (!IsInitialized)
            return;

        IsEnabled = false;
        OnDestroy();
        IsInitialized = false;
        Debug.Log($"[SystemManager] 子系统 {GetType().Name} 已销毁");
    }

    public void SetEnabled(bool enabled)
    {
        if (!IsInitialized)
            return;

        if (IsEnabled == enabled)
            return;

        IsEnabled = enabled;
        if (enabled)
            OnEnable();
        else
            OnDisable();
    }

    /// <summary>
    /// 初始化时调用，子类必须实现
    /// </summary>
    protected abstract void OnInit();

    /// <summary>
    /// 销毁时调用，子类必须实现
    /// </summary>
    protected abstract void OnDestroy();

    /// <summary>
    /// 启用时调用，子类可选重写
    /// </summary>
    protected virtual void OnEnable() { }

    /// <summary>
    /// 禁用时调用，子类可选重写
    /// </summary>
    protected virtual void OnDisable() { }
}
