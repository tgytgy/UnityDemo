using UnityEngine;

/// <summary>
/// 非MonoBehaviour的单例基类
/// 适用于不需要挂载到GameObject上的工具类
/// </summary>
/// <typeparam name="T">单例类型</typeparam>
public abstract class Singleton<T> where T : class, new()
{
    private static T _instance;
    private static readonly object _lock = new object();
    
    public static T Instance
    {
        get
        {
            lock (_lock)
            {
                if (_instance == null)
                {
                    _instance = new T();
                    Debug.Log($"创建 {typeof(T)} 单例实例");
                }
                return _instance;
            }
        }
    }
    
    protected Singleton()
    {
        // 防止通过new创建实例
        if (_instance != null)
        {
            throw new System.Exception($"已存在 {typeof(T)} 的实例，请使用 Instance 属性获取");
        }
    }
    
}