using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HybridCLR.Editor;
using HybridCLR.Editor.Commands;
using HybridCLR.Editor.HotUpdate;
using HybridCLR.Editor.Installer;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using static GameLaunch;

public class BuildTool : MonoBehaviour
{
    //HybridCLR Generate/All存放的目录
    private static string HotUpdateDllPath =>
        $"{Application.dataPath}/../HybridCLRData/HotUpdateDlls/{EditorUserBuildSettings.activeBuildTarget}/";

    //把HybridCLR Generate/All出来的dll加.bytes放到这里
    private static string HotUpdateDestinationPath => $"{Application.dataPath}/HotUpdateDlls/HotUpdateDll/";

    //HybridCLR Generate/All生成的裁剪后的AOT dll路径,用于补充元数据
    private static string MetaDataDLLPath =>
        $"{Application.dataPath}/../HybridCLRData/AssembliesPostIl2CppStrip/{EditorUserBuildSettings.activeBuildTarget}/";

    //把HybridCLR Generate/All 裁剪后的AOT dll放到这里
    private static string MetaDataDestinationPath => $"{Application.dataPath}/HotUpdateDlls/MetaDataDll/";

    //生成的AOTGenericReferences.cs文件中包含了应该补充元数据的assembly列表
    private static string AOTGenericReferencesPath => $"{Application.dataPath}/HybridCLRGenerate/AOTGenericReferences.cs";

    //游戏启动场景
    private static string GameLauncherSceneName => "Assets/Scenes/GameLaunch.unity";
    private static string BuildDataPath => $"{Application.dataPath}/../BuildData/";
    private static string CurrPlatformBuildDataPath => $"{BuildDataPath}{EditorUserBuildSettings.activeBuildTarget}/";

    
    [MenuItem("BuildTool/Build New Player")]
    private static void BuildPlayer()
    {
        BuildDll(true);
        AddressableManager.BuildAddressablesAndPlayer();
    }
    
    [MenuItem("BuildTool/Hot Update Asset")]
    private static void UpdateAsset()
    {
        BuildDll(false);
        AddressableManager.UpdateAPreviousBuilder();
    }
    
    [MenuItem("BuildTool/Build&Copy Dll")]
    private static void ChangeDll()
    {
        BuildDll(false);
    }
    
    private static void BuildDll(bool isBuildPlayer)
    {
        //把热更程序集自动化放到setting中
        InitHotUpdateAssemblyDefinitions();

        //开启热更新
        EnableHotUpdate();

        //如果未安装，安装HybridCLR
        var controller = new InstallerController();
        if (!controller.HasInstalledHybridCLR())
            controller.InstallDefaultHybridCLR();

        //执行HybridCLR
        PrebuildCommand.GenerateAll();

        //如果是更新，则检查热更代码中是否引用了被裁减的AOT代码,如果引用了则发包失败,重新发端执行了Generate/All则不会出现代码被裁剪类型缺失的问题
        if (!isBuildPlayer)
            if (!CheckAccessMissingMetadata())
                return;

        //拷贝dll
        CopyHotUpdateDll();
        //拷贝元数据
        CopyMetaDataDll();

        //如果是发包，则拷贝AOT dll
        if (isBuildPlayer)
            CopyAotDllsForStripCheck();

        //收集RuntimeInitializeOnLoadMethod
        CollectRuntimeInitializeOnLoadMethod();
        AddressableManager.PackGroup();
    }
    
    private static void InitHotUpdateAssemblyDefinitions()
    {
        var assembliesList = new List<AssemblyDefinitionAsset>();
        FindAssemblyDefinitions("Assets/ExternalRes/Scripts", assembliesList);
        SettingsUtil.HybridCLRSettings.hotUpdateAssemblyDefinitions = assembliesList.ToArray();
    }
    
    private static void EnableHotUpdate()
    {
        var gameLauncherScene = EditorSceneManager.OpenScene(GameLauncherSceneName, OpenSceneMode.Additive);
        if (!gameLauncherScene.IsValid())
        {
            Debug.LogError($"can't open scene:{GameLauncherSceneName}");
            return;
        }

        var gameLauncher = FindFirstObjectByType<GameLaunch>();
        if (gameLauncher == null)
        {
            Debug.LogError("can't find GameLauncher");
            return;
        }

        gameLauncher.enableHybridCLR = true;
        EditorSceneManager.MarkSceneDirty(gameLauncherScene);
        EditorSceneManager.SaveScene(gameLauncherScene);
        EditorSceneManager.CloseScene(gameLauncherScene, false);
        SettingsUtil.Enable = true;
    }

    /// <summary>
    /// 热更代码中可能会调用到AOT中已经被裁剪的函数，需要检查一下
    /// https://hybridclr.doc.code-philosophy.com/docs/basic/codestriping#%E6%A3%80%E6%9F%A5%E7%83%AD%E6%9B%B4%E6%96%B0%E4%BB%A3%E7%A0%81%E4%B8%AD%E6%98%AF%E5%90%A6%E5%BC%95%E7%94%A8%E4%BA%86%E8%A2%AB%E8%A3%81%E5%89%AA%E7%9A%84%E7%B1%BB%E5%9E%8B%E6%88%96%E5%87%BD%E6%95%B0
    /// </summary>
    private static bool CheckAccessMissingMetadata()
    {
        BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
        // aotDir指向 构建主包时生成的裁剪aot dll目录，而不是最新的SettingsUtil.GetAssembliesPostIl2CppStripDir(target)目录。
        // 一般来说，发布热更新包时，由于中间可能调用过generate/all，SettingsUtil.GetAssembliesPostIl2CppStripDir(target)目录中包含了最新的aot dll，
        // 肯定无法检查出类型或者函数裁剪的问题。
        // 所以发完端后要把裁剪后的aot dll保存起来以供下次热更检查  在发端时调用这个方法保存CopyAotDllsForStripCheck(),保存到CurrPlatformBuildDataPath路径
        string aotDir = CurrPlatformBuildDataPath;
        Debug.Log($"aotDir: {aotDir}");
        var checker = new MissingMetadataChecker(aotDir, new List<string>());

        string hotUpdateDir = SettingsUtil.GetHotUpdateDllsOutputDirByTarget(target);
        foreach (var dll in SettingsUtil.HotUpdateAssemblyFilesExcludePreserved)
        {
            string dllPath = $"{hotUpdateDir}/{dll}";
            bool notAnyMissing = checker.Check(dllPath);
            if (!notAnyMissing)
            {
                Debug.LogError(
                    $"Update player failed!some hotUpdate dll:{dll} is using a stripped method or type in AOT dll!Please rebuild a player!");
                return false;
            }
        }

        return true;
    }
  
    private static void CopyHotUpdateDll()
    {
        var assemblies = SettingsUtil.HotUpdateAssemblyNamesExcludePreserved;
        var dir = new DirectoryInfo(HotUpdateDllPath);
        var files = dir.GetFiles();
        var destDir = HotUpdateDestinationPath;
        if (Directory.Exists(destDir))
            Directory.Delete(destDir, true);
        Directory.CreateDirectory(destDir);
        foreach (var file in files)
        {
            if (file.Extension == ".dll" && assemblies.Contains(file.Name.Substring(0, file.Name.Length - 4)))
            {
                var desPath = destDir + file.Name + ".bytes";
                file.CopyTo(desPath, true);
                File.SetLastWriteTimeUtc(desPath, DateTime.UtcNow);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("copy hot update dlls success!");
    }

    private static void CopyMetaDataDll()
    {
        List<string> assemblies = GetMetaDataDllList();
        var dir = new DirectoryInfo(MetaDataDLLPath);
        var files = dir.GetFiles();
        var destDir = MetaDataDestinationPath;
        if (Directory.Exists(destDir))
            Directory.Delete(destDir, true);
        Directory.CreateDirectory(destDir);
        foreach (var file in files)
        {
            if (file.Extension == ".dll" && assemblies.Contains(file.Name))
            {
                var desPath = destDir + file.Name + ".bytes";
                file.CopyTo(desPath, true);
            }
        }

        var metaDataDllListStr = string.Join(META_DATA_DLL_SEPARATOR, assemblies);
        if (!File.Exists(META_DATA_DLLS_TO_LOAD_PATH))
        {
            using (File.Create(META_DATA_DLLS_TO_LOAD_PATH))
            {
            }
        }

        //把需要补充的元数据保存到一个文件中
        File.WriteAllText(META_DATA_DLLS_TO_LOAD_PATH, metaDataDllListStr, Encoding.UTF8);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("copy meta data dll success!");
    }

    //之所以采用读取C#文件的方式是因为如果直接读取代码中的列表会出现打包时更改了AOTGenericReferences.cs但Unity编译未完成导致
    //AOTGenericReferences中PatchedAOTAssemblyList还是代码修改前的数据的问题，是因为Unity还没有reload domain
    //https://docs.unity.cn/2023.2/Documentation/Manual/DomainReloading.html
    private static List<string> GetMetaDataDllList()
    {
        var aotGenericRefPath = AOTGenericReferencesPath;
        List<string> result = new List<string>();
        using (StreamReader reader = new StreamReader(aotGenericRefPath))
        {
            var lineStr = "";
            while (!reader.ReadLine().Contains("new List<string>"))
            {
            }

            reader.ReadLine();
            while (true)
            {
                lineStr = reader.ReadLine().Replace("\t", "");
                if (lineStr.Contains("};"))
                    break;
                var dllName = lineStr.Substring(1, lineStr.Length - 3);
                result.Add(dllName);
            }
        }

        return result;
    }

    /// <summary>
    /// 如果是发包，需要拷贝Aot dll到BuildData文件夹下，为后续更新时的代码裁剪检查做准备
    /// </summary>
    private static void CopyAotDllsForStripCheck()
    {
        if (!Directory.Exists(BuildDataPath))
            Directory.CreateDirectory(BuildDataPath);
        var dir = new DirectoryInfo(MetaDataDLLPath);
        var files = dir.GetFiles();
        var destDir = CurrPlatformBuildDataPath;
        if (Directory.Exists(destDir))
            Directory.Delete(destDir, true);
        Directory.CreateDirectory(destDir);
        foreach (var file in files)
        {
            if (file.Extension == ".dll")
            {
                var desPath = destDir + file.Name;
                file.CopyTo(desPath, true);
            }
        }
    }

    private static void CollectRuntimeInitializeOnLoadMethod()
    {
        RuntimeInitializeOnLoadMethodCollection runtimeInitializeOnLoadMethodCollection = new();
        var hotUpdateAssemblies = SettingsUtil.HotUpdateAssemblyNamesExcludePreserved;
        var runtimeInitializedAttributeType = typeof(RuntimeInitializeOnLoadMethodAttribute);
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            var assemblyName = assembly.GetName().Name;
            if (!hotUpdateAssemblies.Contains(assemblyName))
            {
                continue;
            }

            foreach (var type in assembly.GetTypes())
            {
                foreach (var method in type.GetMethods(BindingFlags.Static | BindingFlags.Public |
                                                       BindingFlags.NonPublic))
                {
                    if (!method.IsStatic)
                        continue;
                    var attribute =
                        method.GetCustomAttribute(runtimeInitializedAttributeType) as
                            RuntimeInitializeOnLoadMethodAttribute;
                    if (attribute == null)
                        continue;
                    var sequence = (int)attribute.loadType;
                    var methodInfo = new MethodExecutionInfo(assemblyName, type.FullName, method.Name, sequence);
                    runtimeInitializeOnLoadMethodCollection.methodExecutionInfos.Add(methodInfo);
                }
            }
        }

        runtimeInitializeOnLoadMethodCollection.methodExecutionInfos.Sort(
            (a, b) => b.sequence.CompareTo(a.sequence));
        var json = JsonUtility.ToJson(runtimeInitializeOnLoadMethodCollection, true);
        if (!File.Exists(RUN_TIME_INITIALIZE_ON_LOAD_METHOD_COLLECTION_PATH))
        {
            using (File.Create(RUN_TIME_INITIALIZE_ON_LOAD_METHOD_COLLECTION_PATH))
            {
            }
        }

        File.WriteAllText(RUN_TIME_INITIALIZE_ON_LOAD_METHOD_COLLECTION_PATH, json, Encoding.UTF8);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    public static void FindAssemblyDefinitions(string directoryPath, List<AssemblyDefinitionAsset> asmdefList)
    {
        // 检查目录是否存在
        if (!Directory.Exists(directoryPath))
        {
            Debug.LogError("指定的目录不存在：" + directoryPath);
            return;
        }

        // 获取目录下的所有 .asmdef 文件
        string[] asmdefFiles = Directory.GetFiles(directoryPath, "*.asmdef", SearchOption.AllDirectories);

        // 遍历 .asmdef 文件并加载 AssemblyDefinitionAsset
        foreach (string asmdefFile in asmdefFiles)
        {
            try
            {
                // 加载 AssemblyDefinitionAsset
                AssemblyDefinitionAsset asmdefAsset =
                    AssetDatabase.LoadAssetAtPath<AssemblyDefinitionAsset>(asmdefFile);

                if (asmdefAsset != null)
                {
                    // 将 AssemblyDefinitionAsset 添加到列表中
                    asmdefList.Add(asmdefAsset);
                    Debug.Log("加载程序集定义成功：" + asmdefAsset.name);
                }
                else
                {
                    Debug.LogError("无法加载程序集定义文件：" + asmdefFile);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("加载程序集定义失败：" + asmdefFile + "\n错误：" + ex.Message);
            }
        }
    }
}