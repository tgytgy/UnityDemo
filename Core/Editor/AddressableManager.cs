using UnityEditor;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public static class AddressableManager
{
    private const string BuildScript = "Assets/AddressableAssetsData/DataBuilders/BuildScriptPackedMode.asset";
    private const string SettingsAsset = "Assets/AddressableAssetsData/AddressableAssetSettings.asset";
    private const string ProfileName = "Default";
    private const string GroupName = "RemoteGroup";
    private static readonly string[] TargetFiles = new []{"Assets/HotUpdateDlls", "Assets/ExternalRes/GameAssets"};
    private static readonly HashSet<string> AllowedExtensions = new HashSet<string>
    {
        ".png", ".jpg", // 图片
        ".prefab", // 预制体
        ".unity", // 场景
        ".mat", // 材质
        ".fbx", ".obj", ".blend", // 模型
        ".wav", ".mp3", ".ogg", // 音频
        ".anim", ".controller", // 动画
        ".asset", // ScriptableObject
        ".shader", // 着色器
        ".bytes",    //程序集
        ".txt"    //文本
    };
    
    private static AddressableAssetSettings _settings;
    
    private static void GetSettingsObject(string settingsAsset)
    {
        // This step is optional, you can also use the default settings:
        //settings = AddressableAssetSettingsDefaultObject.Settings;
        //var builderInput = new AddressablesDataBuilderInput(context.Settings);
        _settings
            = AssetDatabase.LoadAssetAtPath<ScriptableObject>(settingsAsset)
                as AddressableAssetSettings;

        if (_settings == null)
            Debug.LogError($"{settingsAsset} couldn't be found or isn't " +
                           $"a settings object.");
    }


    private static void SetProfile(string profile)
    {
        string profileId = _settings.profileSettings.GetProfileId(profile);
        if (String.IsNullOrEmpty(profileId))
            Debug.LogWarning($"Couldn't find a profile named, {profile}, " +
                             $"using current profile instead.");
        else
            _settings.activeProfileId = profileId;
    }


    private static void SetBuilder(IDataBuilder builder)
    {
        int index = _settings.DataBuilders.IndexOf((ScriptableObject)builder);

        if (index > 0)
            _settings.ActivePlayerDataBuilderIndex = index;
        else
            Debug.LogWarning($"{builder} must be added to the " +
                             $"DataBuilders list before it can be made " +
                             $"active. Using last run builder instead.");
    }


    private static bool BuildAddressableContent()
    {
        AddressableAssetSettings
            .BuildPlayerContent(out AddressablesPlayerBuildResult result);
        bool success = string.IsNullOrEmpty(result.Error);

        if (!success)
        {
            Debug.LogError("Addressables build error encountered: " + result.Error);
        }

        return success;
    }

    /// <summary>
    /// addressable打出新包
    /// </summary>
    /// <returns></returns>
    public static bool BuildAddressables()
    {
        GetSettingsObject(SettingsAsset);
        SetProfile(ProfileName);
        var builderScript
            = AssetDatabase.LoadAssetAtPath<ScriptableObject>(BuildScript) as IDataBuilder;

        if (builderScript == null)
        {
            Debug.LogError(BuildScript + " couldn't be found or isn't a build script.");
            return false;
        }

        SetBuilder(builderScript);
        return BuildAddressableContent();
    }

    /// <summary>
    /// build addressable并发端
    /// </summary>
    public static void BuildAddressablesAndPlayer()
    {
        var contentBuildSucceeded = BuildAddressables();
        if (!contentBuildSucceeded) return;
        var options = new BuildPlayerOptions();
        var playerSettings = BuildPlayerWindow.DefaultBuildMethods.GetBuildPlayerOptions(options);
        BuildPipeline.BuildPlayer(playerSettings);
    }

    public static void UpdateAPreviousBuilder()
    {
        GetSettingsObject(SettingsAsset);
        var builderInput = new AddressablesDataBuilderInput(_settings);
        var addressablesBuildMenuUpdateAPreviousBuild = new AddressablesBuildMenuUpdateAPreviousBuild();
        addressablesBuildMenuUpdateAPreviousBuild.OnPrebuild(builderInput);
    }
    
    /// <summary>
    /// 把资源添加到remote group中
    /// </summary>
    [MenuItem("BuildTool/Add Assets To Remote Group")]
    public static void PackGroup()
    {
        GetSettingsObject(SettingsAsset);
        foreach (var pathStr in TargetFiles)
        {
            var (topFolderName, subFolders) = GetAssetFolderInfo(pathStr);
            var rootPath = pathStr;
            var labels = new List<string> { topFolderName };
            var topFiles = FindTopLevelFiles(rootPath);
            foreach (var topFile in topFiles)
            {
                AddFileToAddressableGroup(topFile, labels);   
            }
            foreach (var str in subFolders)
            {
                var subPath = rootPath + "/" + str;
                labels.Add(str);
                var allFiles = FindAllFiles(subPath);
                foreach (var file in allFiles)
                {
                    AddFileToAddressableGroup(file, labels);   
                }
            }
        }
    }
    
    public static (string folderName, List<string> subFolders) GetAssetFolderInfo(string assetPath)
    {
        // 确保路径以 Assets/ 开头
        if (!assetPath.StartsWith("Assets/") && !assetPath.StartsWith("Assets\\"))
        {
            assetPath = "Assets/" + assetPath;
        }

        // 获取文件夹名称
        string folderName = Path.GetFileName(assetPath.TrimEnd('/'));

        // 获取子文件夹
        List<string> subFolders = new List<string>();
        string[] subFolderGUIDs = AssetDatabase.GetSubFolders(assetPath);
        
        foreach (string guid in subFolderGUIDs)
        {
            string subPath = Path.GetFileName(guid.TrimEnd('/'));
            subFolders.Add(subPath);
        }

        return (folderName, subFolders);
    }
    
    /// <summary>
    /// 递归查找路径下的所有文件
    /// </summary>
    /// <param name="path">要搜索的路径</param>
    /// <param name="searchPattern">搜索模式（如 "*.png"），默认为所有文件</param>
    /// <returns>所有文件的完整路径列表</returns>
    public static IEnumerable<string> FindAllFiles(string path, string searchPattern = "*")
    {
        List<string> fileList = new List<string>();
        
        if (!Directory.Exists(path))
        {
            Debug.LogError($"路径不存在: {path}");
            return fileList;
        }

        try
        {
            // 添加当前目录下的文件
            fileList.AddRange(Directory.GetFiles(path, searchPattern));
            
            // 递归处理子目录
            foreach (string directory in Directory.GetDirectories(path))
            {
                fileList.AddRange(FindAllFiles(directory, searchPattern));
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"查找文件时出错: {e.Message}");
        }

        return FilterPath(fileList);
    }
    
    /// <summary>
    /// 查找路径下第一层的文件（不包含子文件夹中的文件）
    /// </summary>
    /// <param name="path">要搜索的路径</param>
    /// <param name="searchPattern">搜索模式（如 "*.prefab"），默认为所有文件</param>
    /// <returns>第一层文件的完整路径列表</returns>
    public static IEnumerable<string> FindTopLevelFiles(string path, string searchPattern = "*")
    {
        List<string> fileList = new List<string>();
    
        if (!Directory.Exists(path))
        {
            Debug.LogError($"路径不存在: {path}");
            return fileList;
        }

        try
        {
            // 只获取当前目录下的文件，不递归
            fileList.AddRange(Directory.GetFiles(path, searchPattern));
        }
        catch (System.Exception e)
        {
            Debug.LogError($"查找文件时出错: {e.Message}");
        }

        return FilterPath(fileList);
    }
    
    /// <summary>
    /// 将文件添加到Addressable系统
    /// </summary>
    /// <param name="filePath">文件完整路径</param>
    /// <param name="labels">标签</param>
    /// <returns>是否成功</returns>
    public static bool AddFileToAddressableGroup(string filePath, List<string> labels)
    {
        // 获取Addressable设置
        AddressableAssetSettings settings = _settings;
        if (settings == null)
        {
            Debug.LogError("Addressable设置未找到，请确保已初始化Addressable系统");
            return false;
        }
        string extension = Path.GetExtension(filePath).ToLower();
        if (!AllowedExtensions.Contains(extension))
        {
            if (extension != ".meta")
            {
                Debug.LogError($"不支持的文件类型: {filePath}");
            }
            return false;
        }
        try
        {
            // 查找或创建组
            AddressableAssetGroup group = settings.FindGroup(GroupName);
            if (group == null)
            {
                group = settings.CreateGroup(GroupName, false, false, true, 
                    new List<AddressableAssetGroupSchema> { settings.DefaultGroup.Schemas[0] });
                // 获取BundledAssetGroupSchema
                BundledAssetGroupSchema schema = group.GetSchema<BundledAssetGroupSchema>();

                if (schema != null)
                {
                    // 1. 设置Build & Load Path为Remote
                    schema.BuildPath.SetVariableByName(settings, "Remote.BuildPath");
                    schema.LoadPath.SetVariableByName(settings, "Remote.LoadPath");
    
                    // 2. 设置Bundle Mode为Pack Together By Label
                    schema.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackTogetherByLabel;
    
                    // 保存修改
                    EditorUtility.SetDirty(settings);
                    AssetDatabase.SaveAssets();
                }
                else
                {
                    Debug.LogError("Failed to get BundledAssetGroupSchema from the group.");
                }

                Debug.Log($"创建新组: {GroupName}");
            }
            // 获取文件GUID
            string guid = AssetDatabase.AssetPathToGUID(filePath);
            if (string.IsNullOrEmpty(guid))
            {
                Debug.LogError($"无法获取文件GUID: {filePath}");
                return false;
            }

            // 创建Addressable条目
            AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, group);
            if (entry == null)
            {
                Debug.LogError($"无法创建Addressable条目: {filePath}");
                return false;
            }

            // 设置Addressable名称（带后缀的文件名）
            string fileNameWithExtension = Path.GetFileName(filePath);
            entry.address = fileNameWithExtension;

            foreach (var label in labels)
            {
                if (!settings.GetLabels().Contains(label))
                {
                    settings.AddLabel(label);
                }
            
                if (!entry.labels.Contains(label))
                {
                    entry.labels.Add(label);
                }
            }

            // 保存修改
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            Debug.Log($"成功添加文件到Addressable: {fileNameWithExtension} (组: {GroupName})");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"添加文件到Addressable时出错: {e.Message}");
            return false;
        }
    }

    public static IEnumerable<string> FilterPath(IEnumerable<string> fileList)
    {
        var filteredPaths = fileList
            .Select(p => p.Replace(Application.dataPath, "").Replace('\\', '/'))
            .Where(p => FilterAssetPaths(new[] { p }).Any());
        return filteredPaths;
    }
    
    /// <summary>
    /// 过滤资源路径列表，移除隐藏文件和.meta文件
    /// </summary>
    /// <param name="paths">原始路径列表</param>
    /// <returns>过滤后的路径列表</returns>
    public static IEnumerable<string> FilterAssetPaths(IEnumerable<string> paths)
    {
        return paths.Where(path => 
            !IsHiddenFile(path) && 
            !IsMetaFile(path));
    }
    
    /// <summary>
    /// 检查是否是隐藏文件
    /// </summary>
    private static bool IsHiddenFile(string path)
    {
        // 过滤Unix-like系统的隐藏文件（以.开头）
        if (Path.GetFileName(path).StartsWith("."))
            return true;

        // 过滤Windows系统的隐藏文件
        if (File.Exists(path) && (File.GetAttributes(path) & FileAttributes.Hidden) == FileAttributes.Hidden)
            return true;

        return false;
    }

    /// <summary>
    /// 检查是否是.meta文件
    /// </summary>
    private static bool IsMetaFile(string path)
    {
        return path.EndsWith(".meta");
    }
    
    /// <summary>
    /// 删除指定的Addressable Group
    /// </summary>
    public static void DeleteAddressableGroup()
    {
        if (_settings == null)
        {
            Debug.LogError("Addressable Settings not found. Please initialize Addressables first.");
            return;
        }

        // 查找Group
        var group = _settings.FindGroup(GroupName);
        if (group == null)
        {
            return;
        }

        try
        {
           _settings.RemoveGroup(group);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error deleting group '{GroupName}': {e.Message}");
        }
    }
}