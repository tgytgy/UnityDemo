using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;

public class GameManager : SingletonMono<GameManager>
{
    private Coroutine _sceneCoroutine;

    public void StartChangeScene(string sceneName)
    {
        _sceneCoroutine = StartCoroutine(ChangeScene(sceneName));
    }
    
    public IEnumerator ChangeScene(string sceneName)
    {
        var op = Addressables.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        yield return op;
        if (op.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogError($"load scene failed,exception:{op.OperationException.Message} \r\n {op.OperationException.StackTrace}");
        }

        PanelManager.Instance.InitLayers();
        ChangeSceneCallBack(sceneName);
        StopCoroutine(_sceneCoroutine);
    }

    private void ChangeSceneCallBack(string sceneName)
    {
        if (sceneName == GameLaunch.START_SCENE_NAME)
        {
            PanelManager.Instance.PanelOn("prefab_main_panel.prefab", "MainPanel", UILayer.LayerMiddle1);
        }
    }
}