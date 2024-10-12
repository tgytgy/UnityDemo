#if UNITY_EDITOR
    using UnityEditor;
    using UnityEditor.AddressableAssets.Build;
    using UnityEditor.AddressableAssets.Settings;
    using System;
    using System.IO;
    using UnityEngine;

    public static class BuildLauncher
    {
        private const string FormalBuildScript = "Assets/AddressableAssetsData/DataBuilders/BuildScriptPackedMode.asset";
        private const string SettingsAsset = "Assets/AddressableAssetsData/AddressableAssetSettings.asset";
        private const string BinRootPath = "Assets/AddressableAssetsData/";
        private const string FormalProfile = "Default";

        private static AddressableAssetSettings settings;

        
        
        //Play and Build Mode根目录
        //private static string _modeRootPath = "Assets/AddressableAssetsData/DataBuilders/";
        //Play and Build Mode Key
        public static string[] ModeAssetKey = { "Use Asset Database(fastest)", "Simulate Groups (advanced)", "Use Existing Build" };
        public static string[] ModeAsset = { "BuildScriptFastMode.asset", "BuildScriptVirtualMode.asset", "BuildScriptPackedPlayMode.asset" };
        
        //配置文件
        public static string[] ProfileKey = { "Default" };
        public static string TargetMode;
        public static string TargetProfile;
        
        /// <summary>
        /// 出新包
        /// </summary>
        /// <returns></returns>
        public static bool BuildAddressables() {
            GetSettingsObject(SettingsAsset);
            SetProfile(FormalProfile);
            var builderScript = AssetDatabase.LoadAssetAtPath<ScriptableObject>(FormalBuildScript) as IDataBuilder;
            if (builderScript == null) {
                Debug.LogError(FormalBuildScript + " couldn't be found or isn't a build script.");
                return false;
            }
            SetBuilder(builderScript);
            return BuildAddressableContent();
        }
        
        /// <summary>
        /// 更新
        /// </summary>
        /// <returns></returns>
        public static void UpdatePreviousBuild() {
            GetSettingsObject(SettingsAsset);
            SetProfile(FormalProfile);
            var builderScript = AssetDatabase.LoadAssetAtPath<ScriptableObject>(FormalBuildScript) as IDataBuilder;
            if (builderScript == null) {
                Debug.LogError(FormalBuildScript + " couldn't be found or isn't a build script.");
                return;
            }
            
            var buildPath = settings.profileSettings.GetValueByName(settings.activeProfileId, AddressableAssetSettings.kRemoteBuildPath);
            buildPath = buildPath.Replace("[BuildTarget]", EditorUserBuildSettings.activeBuildTarget.ToString());
            if (Directory.Exists(buildPath))
            {
                Debug.Log($"清理旧文件：{buildPath}");
                Directory.Delete(buildPath, true);
            }
            Directory.CreateDirectory(buildPath);
            SetBuilder(builderScript);
            var str = $"{BinRootPath}/{GetAddressableBuildTargetFolder(EditorUserBuildSettings.activeBuildTarget)}/addressables_content_state.bin";
            ContentUpdateScript.BuildContentUpdate(settings, str);
        }
        
        private static string GetAddressableBuildTargetFolder(BuildTarget target)
        {
            switch (target)
            {
                case BuildTarget.StandaloneOSX:
                    return "OSX";
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                    return "Windows";
                case BuildTarget.Android:
                    return "Android";
                case BuildTarget.iOS:
                    return "iOS";
                default:
                    return target.ToString();
            }
        }
        
        static void GetSettingsObject(string settingsAsset) {
            // This step is optional, you can also use the default settings:
            //settings = AddressableAssetSettingsDefaultObject.Settings;

            settings
                = AssetDatabase.LoadAssetAtPath<ScriptableObject>(settingsAsset)
                    as AddressableAssetSettings;

            if (settings == null)
                Debug.LogError($"{settingsAsset} couldn't be found or isn't " +
                               $"a settings object.");
        }

        static void SetProfile(string profile) {
            string profileId = settings.profileSettings.GetProfileId(profile);
            if (String.IsNullOrEmpty(profileId))
                Debug.LogWarning($"Couldn't find a profile named, {profile}, " +
                                 $"using current profile instead.");
            else
                settings.activeProfileId = profileId;
        }

        static void SetBuilder(IDataBuilder builder) {
            int index = settings.DataBuilders.IndexOf((ScriptableObject)builder);

            if (index > 0)
                settings.ActivePlayerDataBuilderIndex = index;
            else
                Debug.LogWarning($"{builder} must be added to the " +
                                 $"DataBuilders list before it can be made " +
                                 $"active. Using last run builder instead.");
        }

        static bool BuildAddressableContent() {
            AddressableAssetSettings
                .BuildPlayerContent(out AddressablesPlayerBuildResult result);
            bool success = string.IsNullOrEmpty(result.Error);

            if (!success) {
                Debug.LogError("Addressables build error encountered: " + result.Error);
            }
            return success;
        }

        public static bool UpdateAddressables() {
            GetSettingsObject(SettingsAsset);
            SetProfile(TargetProfile);
            IDataBuilder builderScript = AssetDatabase.LoadAssetAtPath<ScriptableObject>(TargetMode) as IDataBuilder;
            if (builderScript == null)
            {
                Debug.LogError(TargetMode + " couldn't be found or isn't a build script.");
                return false;
            }
            SetBuilder(builderScript);
            
            // 定义更新前一次构建的内容
            var contentStatePath = $"Assets/AddressableAssetsData/{EditorUserBuildSettings.activeBuildTarget}/addressables_content_state.bin";
            AddressablesPlayerBuildResult result = ContentUpdateScript.BuildContentUpdate(settings, contentStatePath);

            if (!string.IsNullOrEmpty(result.Error))
            {
                Debug.LogError("Update Addressables build error encountered: " + result.Error);
            }
            else
            {
                Debug.Log("Successfully updated Addressables content.");
            }


            return false;


            /*AddressableAssetSettings
                .BuildPlayerContent(out AddressablesPlayerBuildResult result);
            bool success = string.IsNullOrEmpty(result.Error);

            if (!success) {
                Debug.LogError("Addressables build error encountered: " + result.Error);
            }
            return success;*/
        }
        
        public static void BuildAddressablesAndPlayer() {
            bool contentBuildSucceeded = BuildAddressables();

            if (contentBuildSucceeded) {
                var options = new BuildPlayerOptions();
                BuildPlayerOptions playerSettings
                    = BuildPlayerWindow.DefaultBuildMethods.GetBuildPlayerOptions(options);

                BuildPipeline.BuildPlayer(playerSettings);
            }
        }
    }
#endif

