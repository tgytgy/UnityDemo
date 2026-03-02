using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 系统管理器，负责所有子系统的注册、生命周期管理和访问
/// 子系统配置由 Excel 导出的 GdSystem 表驱动
/// </summary>
public class SystemManager : SingletonMono<SystemManager>
{
    private readonly Dictionary<Type, BaseSystem> _systems = new Dictionary<Type, BaseSystem>();

    /// <summary>
    /// 根据 GdSystem 表数据初始化所有标记为自动初始化的子系统
    /// </summary>
    public void InitFromGdSystem()
    {
        var data = GdSystem.Data;
        for (int i = 0; i < data.Count; i++)
        {
            var entry = data[i];
            if (entry.AutoInit)
                RegisterSystem(entry.SystemName, entry.Priority);
        }
    }

    /// <summary>
    /// 通过类型名注册子系统（配合 Excel 导出的表使用）
    /// 自动在所有已加载程序集中查找类型
    /// </summary>
    public void RegisterSystem(string typeName, int priority)
    {
        var type = FindType(typeName);
        if (type == null)
        {
            Debug.LogError($"[SystemManager] 找不到子系统类型: {typeName}");
            return;
        }

        RegisterByType(type, priority);
    }

    /// <summary>
    /// 注册一个子系统（泛型方式）
    /// </summary>
    public T Register<T>(int priority = 0) where T : BaseSystem, new()
    {
        var type = typeof(T);
        if (_systems.ContainsKey(type))
        {
            Debug.LogWarning($"[SystemManager] 子系统 {type.Name} 已注册，跳过重复注册");
            return _systems[type] as T;
        }

        var system = new T();
        _systems.Add(type, system);
        system.InternalInit();
        return system;
    }

    /// <summary>
    /// 注册一个已有实例的子系统
    /// </summary>
    public T Register<T>(T system, int priority = 0) where T : BaseSystem
    {
        var type = typeof(T);
        if (_systems.ContainsKey(type))
        {
            Debug.LogWarning($"[SystemManager] 子系统 {type.Name} 已注册，跳过重复注册");
            return _systems[type] as T;
        }

        _systems.Add(type, system);
        system.InternalInit();
        return system;
    }

    /// <summary>
    /// 注销一个子系统
    /// </summary>
    public void Unregister<T>() where T : BaseSystem
    {
        var type = typeof(T);
        if (!_systems.TryGetValue(type, out var system))
        {
            Debug.LogWarning($"[SystemManager] 子系统 {type.Name} 未注册，无法注销");
            return;
        }

        system.InternalDestroy();
        _systems.Remove(type);
    }

    /// <summary>
    /// 获取子系统实例
    /// </summary>
    public T Get<T>() where T : BaseSystem
    {
        _systems.TryGetValue(typeof(T), out var system);
        return system as T;
    }

    /// <summary>
    /// 判断子系统是否已注册
    /// </summary>
    public bool Has<T>() where T : BaseSystem
    {
        return _systems.ContainsKey(typeof(T));
    }

    /// <summary>
    /// 销毁所有子系统
    /// </summary>
    public void DestroyAllSystems()
    {
        foreach (var system in _systems.Values)
        {
            system.InternalDestroy();
        }

        _systems.Clear();
    }

    private void RegisterByType(Type type, int priority)
    {
        if (_systems.ContainsKey(type))
        {
            Debug.LogWarning($"[SystemManager] 子系统 {type.Name} 已注册，跳过重复注册");
            return;
        }

        var system = (BaseSystem)Activator.CreateInstance(type);
        _systems.Add(type, system);
        system.InternalInit();
    }

    private static Type FindType(string typeName)
    {
        var type = Type.GetType(typeName);
        if (type != null)
            return type;

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            type = assembly.GetType(typeName);
            if (type != null)
                return type;
        }

        return null;
    }

    private void OnDestroy()
    {
        DestroyAllSystems();
    }
}
