using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;

public class AssetLoader
{
    [Serializable]
    private class DownloadContent
    {
        public List<string> catalogs = new();
    }

    //记录在playerPres里的需要下载的catalogs的ID
    const string DOWNLOAD_CATALOGS_ID = "DownloadCatalogs";

    //待下载信息
    private readonly List<object> _keysNeedToDownload = new();
    private readonly DownloadContent _downloadContent = new();
    private AsyncOperationHandle _downloadOp;

    /// <summary>
    /// 检查是否需要新资源
    /// </summary>
    /// <returns></returns>
    public IEnumerator CheckUpdate()
    {
        //检查服务器上是否有新版本的资源目录(catalog)
        var checkUpdateOP = Addressables.CheckForCatalogUpdates(false);
        yield return checkUpdateOP;
        if (checkUpdateOP.Status == AsyncOperationStatus.Succeeded)
        {
            _downloadContent.catalogs = checkUpdateOP.Result;
            if (HasContentToDownload())
            {
                Debug.Log("new version on server");
                //说明服务器上有新的资源，记录要下载的catalog值在playerprefs中,如果下载的过程中被打断，下次打开游戏使用该值还能继续下载
                var jsonStr = JsonUtility.ToJson(_downloadContent);
                PlayerPrefs.SetString(DOWNLOAD_CATALOGS_ID, jsonStr);
                PlayerPrefs.Save();
            }
            else
            {
                if (PlayerPrefs.HasKey(DOWNLOAD_CATALOGS_ID))
                {
                    //上一次的更新还没下载完
                    Debug.Log("there are some contents remains from last downloading");
                    var jsonStr = PlayerPrefs.GetString(DOWNLOAD_CATALOGS_ID);
                    JsonUtility.FromJsonOverwrite(jsonStr, _downloadContent);
                }
                else
                {
                    //没有需要下载的内容
                    Debug.Log("there are no content to be downloaded");
                }
            }

            if (HasContentToDownload())
            {
                //下载并更新指定的资源目录
                var updateCatalogOp = Addressables.UpdateCatalogs(_downloadContent.catalogs, false);
                yield return updateCatalogOp;
                if (updateCatalogOp.Status == AsyncOperationStatus.Succeeded)
                {
                    _keysNeedToDownload.Clear();
                    //保存待下载信息
                    foreach (var resourceLocator in updateCatalogOp.Result)
                    {
                        _keysNeedToDownload.AddRange(resourceLocator.Keys);
                    }
                }
                else
                {
                    Debug.LogError($"Update catalog failed!exception:{updateCatalogOp.OperationException.Message}");
                }

                Addressables.Release(updateCatalogOp);
            }
        }
        else
        {
            Debug.LogError($"CheckUpdate failed!exception:{checkUpdateOP.OperationException.Message}");
        }

        Addressables.Release(checkUpdateOP);
        //更新完catalog后重新加载一下Addressable的Catalog
        yield return ReloadAddressableCatalog();
    }

    /// <summary>
    /// 下载热更新资源
    /// </summary>
    /// <returns></returns>
    public IEnumerator DownloadAssets(TMP_Text text = null)
    {
        var downloadSizeOp = Addressables.GetDownloadSizeAsync((IEnumerable)_keysNeedToDownload);
        yield return downloadSizeOp;
        Debug.Log($"download size:{downloadSizeOp.Result / (1024f * 1024f)}MB");
        if (text != null) text.text = $"download size:{downloadSizeOp.Result / (1024f * 1024f)}MB";
        if (downloadSizeOp.Result > 0)
        {
            Addressables.Release(downloadSizeOp);

            _downloadOp =
                Addressables.DownloadDependenciesAsync((IEnumerable)_keysNeedToDownload,
                    Addressables.MergeMode.Union, false);

            yield return _downloadOp;

            if (_downloadOp.Status == AsyncOperationStatus.Succeeded)
                Debug.Log($"download finish!");
            else
                Debug.LogError(
                    $"Download Failed! exception:{_downloadOp.OperationException.Message} \r\n {_downloadOp.OperationException.StackTrace}");

            Addressables.Release(_downloadOp);
        }

        //清除需要下载的内容
        Debug.Log($"delete key:{DOWNLOAD_CATALOGS_ID}");
        PlayerPrefs.DeleteKey(DOWNLOAD_CATALOGS_ID);
    }

    public static void UnloadAsset(UnityEngine.Object asset)
    {
        if (asset != null)
            Addressables.Release(asset);
    }

    public IEnumerator AfterAllDllLoaded()
    {
        yield return ReloadAddressableCatalog();
    }

    /// <summary>
    /// 重新加载catalog
    /// Addressable初始化时热更新代码所对应的ScriptableObject的类型会被识别为System.Object，需要在热更新dll加载完后重新加载一下Addressable的Catalog
    /// https://hybridclr.doc.code-philosophy.com/docs/help/commonerrors#%E4%BD%BF%E7%94%A8addressable%E8%BF%9B%E8%A1%8C%E7%83%AD%E6%9B%B4%E6%96%B0%E6%97%B6%E5%8A%A0%E8%BD%BD%E8%B5%84%E6%BA%90%E5%87%BA%E7%8E%B0-unityengineaddressableassetsinvlidkeyexception-exception-of-type-unityengineaddressableassetsinvalidkeyexception-was-thrown-no-asset-found-with-for-key-xxxx-%E5%BC%82%E5%B8%B8
    /// </summary>
    /// <returns></returns>
    private IEnumerator ReloadAddressableCatalog()
    {
        var op = Addressables.LoadContentCatalogAsync($"{Addressables.RuntimePath}/catalog.json");
        yield return op;
        if (op.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogError(
                $"load content catalog failed, exception:{op.OperationException.Message} \r\n {op.OperationException.StackTrace}");
        }
    }

    

    public bool HasContentToDownload()
    {
        return _downloadContent != null && _downloadContent.catalogs != null &&
               _downloadContent.catalogs.Count > 0;
        ;
    }
}