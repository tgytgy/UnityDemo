using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;

public class SceneManager : SingletonMono<SceneManager>
{
    private Coroutine _sceneCoroutine;

    public void StartChangeScene(string sceneName, Action<string> onSceneLoaded = null)
    {
        _sceneCoroutine = StartCoroutine(ChangeScene(sceneName, onSceneLoaded));
    }
    
    public IEnumerator ChangeScene(string sceneName, Action<string> onSceneLoaded = null)
    {
        var op = Addressables.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        yield return op;
        if (op.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogError($"load scene failed,exception:{op.OperationException.Message} \r\n {op.OperationException.StackTrace}");
        }

        onSceneLoaded?.Invoke(sceneName);
        StopCoroutine(_sceneCoroutine);
    }
}