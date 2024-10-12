using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class AssetManager
{
    public static T LoadRes<T>(string nameStr)
    {
        var handle = Addressables.LoadAssetAsync<T>(nameStr);
        handle.WaitForCompletion();
        return handle.Result;
    }
}
