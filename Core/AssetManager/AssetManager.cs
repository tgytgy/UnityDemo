using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public static class AssetManager
{
    public static T LoadAssetSync<T>(string path)
    {
        var op = Addressables.LoadAssetAsync<T>(path);
        if (!op.IsValid())
            return default;
        op.WaitForCompletion();
        return op.Result;
    }

    public static GameObject LoadPrefab(string path, Transform parent)
    {
        var go = LoadAssetSync<GameObject>(path);
        if (go) return go;
        Debug.LogError($"Failed to load prefab {path}");
        return null;
    }
    
    public static void LoadAssetAsync<T>(string path, Action<T> cb)
    {
        Addressables.LoadAssetAsync<T>(path).Completed += (handle) =>
        {
            switch (handle.Status)
            {
                case AsyncOperationStatus.Succeeded:
                    cb(handle.Result);
                    break;
                case AsyncOperationStatus.Failed:
                    Debug.LogError($"Failed to load asset: {path}");
                    break;
                case AsyncOperationStatus.None:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        };
    }
}
