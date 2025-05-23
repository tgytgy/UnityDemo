using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HybridCLR;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameLaunch : MonoBehaviour
{
    #region Inner Class
        
    [Serializable]
    public class MethodExecutionInfo
    {
        public string assemblyName;

        public string typeName;
            
        public string methodName;
            
        public int sequence;
            
        public MethodExecutionInfo(string assemblyName, string typeName, string methodName, int sequence)
        {
            this.assemblyName = assemblyName;
            this.typeName = typeName;
            this.methodName = methodName;
            this.sequence = sequence;
        }

        public void Execute()
        {
            var assembly = FindObjectOfType<GameLaunch>().GetAssembly(assemblyName);
            if (assembly == null)
            {
                Debug.LogError($"cant find assembly,name:{assemblyName}");
                return;
            }

            var type = assembly.GetType(typeName);
            if (type == null)
            {
                Debug.LogError($"cant find type,name:{typeName}");
                return;
            }

            var method = type.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (method == null)
            {
                Debug.LogError($"cant find method,name:{methodName}");
                return;
            }

            method.Invoke(null, null);
        }
    }

    [Serializable]
    public class RuntimeInitializeOnLoadMethodCollection
    {
        public List<MethodExecutionInfo> methodExecutionInfos = new List<MethodExecutionInfo>();
    }
        
    #endregion
    
    //gameplay程序集
    const string GAMEPLAY_DLL_NAME = "GamePlay.dll";
    
    //开始场景
    const string START_SCENE_NAME = "StartScene.unity";
    
    //元数据信息文件
    public const string META_DATA_DLLS_TO_LOAD_PATH = "Assets/HotUpdateDlls/MetaDataDllToLoad.txt";
    
    //addressable中的元数据信息文件名字
    public const string META_DATA_DLLS_TO_LOAD = "MetaDataDllToLoad.txt";

    //保存RuntimeInitializeOnLoadMethod方法的文件
    public const string RUN_TIME_INITIALIZE_ON_LOAD_METHOD_COLLECTION_PATH = "Assets/HotUpdateDlls/RuntimeInitializeOnLoadMethodCollection.txt";
    
    //addressable中的保存RuntimeInitializeOnLoadMethod方法的文件名字
    public const string RUN_TIME_INITIALIZE_ON_LOAD_METHOD_COLLECTION = "RuntimeInitializeOnLoadMethodCollection.txt";

    //元数据信息文件分隔符
    public const string META_DATA_DLL_SEPARATOR = "!";
    
    private Coroutine _launchCoroutine;
    public bool enableHybridCLR = true;
    private AssetLoader _assetLoader;
    private Dictionary<string, Assembly> _allHotUpdateAssemblies = new();
    private byte[] _dllBytes;

    private TMP_Text _textInfo;
    private Button _btnLogin;
    private Button _btnUpd;
    private bool _loginBtnClicked;
    private bool _updBtnClicked;
    
    ///GamePlay程序集依赖的热更程序集，这些程序集要先于gameplay程序集加载，需要手动填写
    private readonly List<string> _gamePlayDependencyDlls = new List<string>()
    {
    };
    
    private void Start()
    {
        _assetLoader = new AssetLoader();
        if (enableHybridCLR)
        {
            HybridCLROptimizer.OptimizeHybridCLR();
        }
        _launchCoroutine = StartCoroutine(Launch());
        InitUI();
    }

    private void InitUI()
    {
        _textInfo = Utils.GetNode(transform, "Text_Download").GetComponent<TMP_Text>();
        _btnLogin = Utils.GetNode(transform, "Btn_Login").GetComponent<Button>();
        _btnUpd = Utils.GetNode(transform, "Btn_Update").GetComponent<Button>();
        _textInfo.text = "";
        _btnLogin.gameObject.SetActive(false);
        _btnUpd.gameObject.SetActive(false);
        _btnLogin.onClick.AddListener(LoginBtnClick);
        _btnUpd.onClick.AddListener(UpdGameBtnClick);
    }
    
    private void OnDestroy()
    {
        StopCoroutine(_launchCoroutine);
        _launchCoroutine = null;
    }
    
    private IEnumerator Launch()
    {
        Debug.Log($"Launch Game! enableHybridCLR:{enableHybridCLR}");
        
        yield return VersionCheck();
        yield return VersionUpdate();
        yield return LoadAssemblies();
        yield return EnterGame();
    }

    /// <summary>
    /// 版本检查
    /// </summary>
    /// <returns></returns>
    private IEnumerator VersionCheck()
    {
        Debug.Log($"VersionCheck start!");
        yield return CheckNewAPPVersion();
        yield return _assetLoader.CheckUpdate();
        Debug.Log($"VersionCheck finish,has Content to download:{_assetLoader.HasContentToDownload()}");
    }
    
    /// <summary>
    /// 版本资源更新
    /// </summary>
    /// <returns></returns>
    private IEnumerator VersionUpdate()
    {
        if (!_assetLoader.HasContentToDownload())
        {
            _btnLogin.gameObject.SetActive(true);
            yield return new WaitUntil(() => _loginBtnClicked);
            yield break;
        }
        Debug.Log($"VersionUpdate start!");
        yield return OpenVersionUpdateUI();
        yield return new WaitUntil(() => _updBtnClicked);
        yield return Download();
        Debug.Log($"VersionUpdate finish!");
    }
    
    /// <summary>
    /// 加载热更新程序集
    /// </summary>
    /// <returns></returns>
    private IEnumerator LoadAssemblies()
    {
        if (!enableHybridCLR)
            yield break;
        Debug.Log("LoadAssemblies start!");
        yield return LoadMetadataForAOTAssemblies();
        yield return LoadGamePlayDependencyAssemblies();
        yield return LoadGamePlayAssemblies();
        yield return _assetLoader.AfterAllDllLoaded();
        ExecuteRuntimeInitializeOnLoadMethodAttribute();
        Debug.Log("LoadAssemblies finish!");
        yield return null;
    }
    
    //补充元数据
    private IEnumerator LoadMetadataForAOTAssemblies()
    {
        var aotAssemblies = GetMetaDataDllToLoad();
        if (aotAssemblies == null)
        {
            yield break;
        }
            
        foreach (var aotDllName in aotAssemblies)
        {
            if(string.IsNullOrEmpty(aotDllName))
                continue;
            var path = $"{aotDllName}.bytes";
            ReadDllBytes(path);
            if (_dllBytes != null)
            {
                //HybridCLR补充元数据
                var err = RuntimeApi.LoadMetadataForAOTAssembly(_dllBytes, HomologousImageMode.SuperSet);
                Debug.Log($"LoadMetadataForAOTAssembly:{aotDllName}. ret:{err}");
            }
        }

        Debug.Log("LoadMetadataForAOTAssemblies finish!");
    }
    
    /// <summary>
    /// 找到需要补充的元数据的名字
    /// </summary>
    /// <returns></returns>
    private string[] GetMetaDataDllToLoad()
    {
        string[] result = null;
        var metaDataToLoad = _assetLoader.LoadAssetSync<TextAsset>(META_DATA_DLLS_TO_LOAD);
        if (metaDataToLoad == null)
        {
            Debug.LogError($"cant load metaDataText,path:{META_DATA_DLLS_TO_LOAD}");
        }
        else
        {
            var text = metaDataToLoad.text;
            result = text.Split(META_DATA_DLL_SEPARATOR);
            AssetLoader.UnloadAsset(metaDataToLoad);
        }

        return result;
    }
    
    //加载GamePlay依赖的第三方程序集
    private IEnumerator LoadGamePlayDependencyAssemblies()
    {
        foreach (var dllName in _gamePlayDependencyDlls)
        {
            yield return LoadSingleHotUpdateAssembly(dllName);
        }

        Debug.Log("LoadGamePlayDependencyAssemblies finish!");
    }
    
    //加载GamePlay程序集
    private IEnumerator LoadGamePlayAssemblies()
    {
        yield return LoadSingleHotUpdateAssembly(GAMEPLAY_DLL_NAME);
        Debug.Log("LoadGamePlayAssemblies finish!");
    }
    
    private IEnumerator LoadSingleHotUpdateAssembly(string dllName)
    {
        var path = $"{dllName}.bytes";
        ReadDllBytes(path);
        if (_dllBytes != null)
        {
            var assembly = Assembly.Load(_dllBytes);
            _allHotUpdateAssemblies.Add(assembly.FullName, assembly);
            Debug.Log($"Load Assembly success,assembly Name:{assembly.FullName}");
        }

        yield return null;
    }
    
    /// <summary>
    /// 加载dll
    /// </summary>
    /// <param name="path"></param>
    private void ReadDllBytes(string path)
    {
        var dllText = _assetLoader.LoadAssetSync<TextAsset>(path);

        if (dllText == null)
        {
            Debug.LogError($"cant load dllText,path:{path}");
            _dllBytes = null;
        }
        else
        {
            _dllBytes = dllText.bytes;
        }

        AssetLoader.UnloadAsset(dllText);
    }
    
    /// <summary>
    /// 反射执行被RuntimeInitializeOnLoadMethod attribute标注的函数，HybridCLR不支持该attribute
    /// </summary>
    private void ExecuteRuntimeInitializeOnLoadMethodAttribute()
    {
        var runtimeInitializeOnLoadMethodCollection = _assetLoader.LoadAssetSync<TextAsset>(RUN_TIME_INITIALIZE_ON_LOAD_METHOD_COLLECTION);
        var json = runtimeInitializeOnLoadMethodCollection.text;
        var collection = JsonUtility.FromJson<RuntimeInitializeOnLoadMethodCollection>(json);
        foreach (var methodInfo in collection.methodExecutionInfos)
        {
            methodInfo.Execute();
        }
            
        Debug.Log("execute RuntimeInitializeOnLoadMethod finish!");
    }
    
    private IEnumerator EnterGame()
    {
        yield return _assetLoader.ChangeScene(START_SCENE_NAME);
        Debug.Log("EnterGame finish!");
    }
    
    //下载资源
    private IEnumerator Download()
    {
        yield return _assetLoader.DownloadAssets();
    }
    
    //打开版本更新UI
    private IEnumerator OpenVersionUpdateUI()
    {
        _textInfo.text = "New Assets Need To Download";
        _btnUpd.gameObject.SetActive(true);
        return null;
    }
    
    /// <summary>
    /// todo 如果包体版本有更新则提示用户去app store重新下载
    /// </summary>
    /// <returns></returns>
    private IEnumerator CheckNewAPPVersion()
    {
        yield return null;
    }
    
    private Assembly GetAssembly(string assemblyName)
    {
        assemblyName = assemblyName.Replace(".dll", "");
        IEnumerable<Assembly> allAssemblies =
            enableHybridCLR ? _allHotUpdateAssemblies.Values : AppDomain.CurrentDomain.GetAssemblies();
        return allAssemblies.First(assembly => assembly.FullName.Contains(assemblyName));
    }

    private void LoginBtnClick()
    {
        _loginBtnClicked = true;
    }

    private void UpdGameBtnClick()
    {
        _updBtnClicked = true;
    }
}