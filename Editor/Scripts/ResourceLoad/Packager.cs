using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine;
using UnityEditor;
namespace ST.Core
{
    /// <summary>
    /// AssetBundle 与 Lua 字节码打包编辑器工具：菜单位于 <c>ST/</c>，依赖业务侧通过 <see cref="RegisterConfig"/> 注入 <see cref="IResourceConfig"/>。
    /// </summary>
    public class Packager
    {
        /// <summary>预留的 Android 输出平台枚举。</summary>
        public enum EAndroidBuildPlatform { NoPlatform }

        static IResourceConfig s_Config;

        /// <summary>由业务在 <c>InitializeOnLoad</c> 等时机注入，供菜单与路径替换使用。</summary>
        public static void RegisterConfig(IResourceConfig config)
        {
            s_Config = config;
        }

        static string[] TexturePackageDir = { "ui/icon/", "ui/image/" };
        static string[] FontPackageDir = { "font/" };
        static string[] AudioMusicPackageDir = { "audio/music/" };
        static string[] AudioSoundPackageDir = { "audio/sound/" };
        static string[] PrefabPackageDir = { "prefabs/", "ui/uiprefab/" };
        static string[] SceneDir = { "scene/" };

        const string LuaCodePath = "/Lua/";
        const string LuaCExeRelativePath = "/tools/xlua_v2.1.14/build/luac/build64/Release/luac.exe";
        const string LuaCExeMacRelativePath = "/tools/xlua_v2.1.14/build/luac/build_unix/luac";
        const string LuaCSrcFileRelativePath = "/Assets/Lua/";
        const string LuaCGenFileRelativePath = "/Temp/";
        const string LuaPath = "config/lua/";
        const string LuaFileName = "luapackage";
        const string Lua_Suffix = ".asset";
        const string Bytes_Suffix = ".bytes";

        static List<AssetBundleBuild> m_BundleBuildList = new List<AssetBundleBuild>();

        /// <summary>切换 iOS 目标并构建资源到 StreamingAssets 应用子目录。</summary>
        [MenuItem("ST/Build iPhone Resource", false, 100)]
        public static void BuildiPhoneResource()
        {
            PlayerSettings.iOS.appleEnableAutomaticSigning = true;
            PlayerSettings.iOS.appleDeveloperTeamID = "24AZABCKN4";
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.iOS, BuildTarget.iOS);
            BuildAssetResource(BuildTarget.iOS, AppPlatform.GetStreamingAssetsPath(s_Config.appName));
        }

        /// <summary>切换 Android 目标并构建资源到 StreamingAssets 应用子目录。</summary>
        [MenuItem("ST/Build Android Resource", false, 101)]
        public static void BuildAndroidResource()
        {
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            BuildAssetResource(BuildTarget.Android, AppPlatform.GetStreamingAssetsPath(s_Config.appName));
        }

        /// <summary>切换 Standalone 目标并构建资源到 StreamingAssets 应用子目录。</summary>
        [MenuItem("ST/Build Windows Resource", false, 102)]
        public static void BuildWindowsResource()
        {
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64);
            BuildAssetResource(BuildTarget.StandaloneWindows, AppPlatform.GetStreamingAssetsPath(s_Config.appName));
        }

        /// <summary>清空输出目录、生成 Lua 资产、收集 <see cref="AssetBundleBuild"/> 并调用 <see cref="BuildPipeline.BuildAssetBundles"/>。</summary>
        static void BuildAssetResource(BuildTarget target, string resPath)
        {
            m_BundleBuildList.Clear();

            if (Directory.Exists(resPath))
                Directory.Delete(resPath, true);

            Directory.CreateDirectory(resPath);
            AssetDatabase.Refresh();

            GenerateLuaScriptableObject();

            string packagePathPrefix = "/Package/";
            AddAllAssetBundle(Application.dataPath + packagePathPrefix);

            BuildPipeline.BuildAssetBundles(resPath, m_BundleBuildList.ToArray(), BuildAssetBundleOptions.None, target);
            AssetDatabase.Refresh();
        }

        /// <summary>追加一条打包映射到内部列表。</summary>
        static void AddAssetBundleBuild(string assetBundleName, string[] assetNames, string assetBundleVariant = "unity3d")
        {
            AssetBundleBuild build = new AssetBundleBuild();
            build.assetBundleName = assetBundleName;
            build.assetBundleVariant = assetBundleVariant;
            build.assetNames = assetNames;
            m_BundleBuildList.Add(build);
        }

        /// <summary>扫描字体子目录中的 <c>.ttf</c> 并登记为独立 Bundle。</summary>
        static void PackageFont(string rootpath)
        {
            foreach (var subdir in FontPackageDir)
            {
                DirectoryInfo dirInfo = new DirectoryInfo(rootpath + subdir);
                foreach (FileInfo file in dirInfo.GetFiles("*.ttf", SearchOption.AllDirectories))
                {
                    string source = file.FullName.Replace("\\", "/");
                    string assetpath = "Assets" + source.Substring(Application.dataPath.Length);
                    var bundlePath = assetpath.Replace(s_Config.editorPathPrefix, "").Replace(".ttf", "");
                    AddAssetBundleBuild(bundlePath, new string[] { assetpath });
                }
            }
            AssetDatabase.Refresh();
        }

        /// <summary>扫描音乐 <c>.mp3</c> 与音效 <c>.ogg</c> 并登记 Bundle。</summary>
        static void PackageAudio(string rootpath)
        {
            foreach (var subdir in AudioMusicPackageDir)
            {
                DirectoryInfo dirInfo = new DirectoryInfo(rootpath + subdir);
                foreach (FileInfo file in dirInfo.GetFiles("*.mp3", SearchOption.AllDirectories))
                {
                    string source = file.FullName.Replace("\\", "/");
                    string assetpath = "Assets" + source.Substring(Application.dataPath.Length);
                    var bundlePath = assetpath.Replace(s_Config.editorPathPrefix, "").Replace(".mp3", "");
                    AddAssetBundleBuild(bundlePath, new string[] { assetpath });
                }
            }
            foreach (var subdir in AudioSoundPackageDir)
            {
                DirectoryInfo dirInfo = new DirectoryInfo(rootpath + subdir);
                foreach (FileInfo file in dirInfo.GetFiles("*.ogg", SearchOption.AllDirectories))
                {
                    string source = file.FullName.Replace("\\", "/");
                    string assetpath = "Assets" + source.Substring(Application.dataPath.Length);
                    var bundlePath = assetpath.Replace(s_Config.editorPathPrefix, "").Replace(".ogg", "");
                    AddAssetBundleBuild(bundlePath, new string[] { assetpath });
                }
            }
            AssetDatabase.Refresh();
        }

        /// <summary>扫描 UI 贴图目录下 <c>.png</c> 并登记 Bundle。</summary>
        static void PackageTexture(string rootpath)
        {
            foreach (var subdir in TexturePackageDir)
            {
                DirectoryInfo dirInfo = new DirectoryInfo(rootpath + subdir);
                foreach (FileInfo file in dirInfo.GetFiles("*.png", SearchOption.AllDirectories))
                {
                    string source = file.FullName.Replace("\\", "/");
                    string assetpath = "Assets" + source.Substring(Application.dataPath.Length);
                    var bundlePath = assetpath.Replace(s_Config.editorPathPrefix, "").Replace(".png", "");
                    AddAssetBundleBuild(bundlePath, new string[] { assetpath });
                }
            }
            AssetDatabase.Refresh();
        }

        /// <summary>扫描 Prefab 目录并登记 Bundle。</summary>
        static void PackagePrefab(string rootpath)
        {
            foreach (var subdir in PrefabPackageDir)
            {
                DirectoryInfo dirInfo = new DirectoryInfo(rootpath + subdir);
                foreach (FileInfo file in dirInfo.GetFiles("*.prefab", SearchOption.AllDirectories))
                {
                    string source = file.FullName.Replace("\\", "/");
                    string assetpath = "Assets" + source.Substring(Application.dataPath.Length);
                    var bundlePath = assetpath.Replace(s_Config.editorPathPrefix, "").Replace(".prefab", "");
                    AddAssetBundleBuild(bundlePath, new string[] { assetpath });
                }
            }
            AssetDatabase.Refresh();
        }

        /// <summary>扫描场景目录下 <c>.unity</c> 并登记 Bundle。</summary>
        static void PackageScene(string rootpath)
        {
            foreach (var subdir in SceneDir)
            {
                DirectoryInfo dirInfo = new DirectoryInfo(rootpath + subdir);
                foreach (FileInfo file in dirInfo.GetFiles("*.unity", SearchOption.AllDirectories))
                {
                    string source = file.FullName.Replace("\\", "/");
                    string assetpath = "Assets" + source.Substring(Application.dataPath.Length);
                    var bundlePath = assetpath.Replace(s_Config.editorPathPrefix, "").Replace(".unity", "");
                    AddAssetBundleBuild(bundlePath, new string[] { assetpath });
                }
            }
            AssetDatabase.Refresh();
        }

        /// <summary>将配置目录下 Lua 相关 <c>.asset</c> / <c>.bytes</c> 登记为 Bundle。</summary>
        static void PackageConfig_Lua(string rootpath)
        {
            DirectoryInfo dirInfo = new DirectoryInfo(rootpath + LuaPath);
            foreach (FileInfo file in dirInfo.GetFiles("*.asset", SearchOption.AllDirectories))
            {
                string source = file.FullName.Replace("\\", "/");
                string assetpath = "Assets" + source.Substring(Application.dataPath.Length);
                var bundlePath = assetpath.Replace(s_Config.editorPathPrefix, "").Replace(Lua_Suffix, "");
                AddAssetBundleBuild(bundlePath, new string[] { assetpath });
            }
            foreach (FileInfo file in dirInfo.GetFiles("*.bytes", SearchOption.AllDirectories))
            {
                string source = file.FullName.Replace("\\", "/");
                string assetpath = "Assets" + source.Substring(Application.dataPath.Length);
                var bundlePath = assetpath.Replace(s_Config.editorPathPrefix, "").Replace(Bytes_Suffix, "");
                AddAssetBundleBuild(bundlePath, new string[] { assetpath });
            }
            AssetDatabase.Refresh();
        }

        /// <summary>按类型依次调用各类 <c>Package*</c> 收集构建列表。</summary>
        static void AddAllAssetBundle(string rootpath)
        {
            PackageFont(rootpath);
            PackageTexture(rootpath);
            PackageAudio(rootpath);
            PackagePrefab(rootpath);
            PackageScene(rootpath);
            PackageConfig_Lua(rootpath);
        }

        /// <summary>调用外部 luac 编译 <c>Assets/Lua</c> 下脚本并写入 <see cref="LuaScriptableObject"/> 资产。</summary>
        public static void GenerateLuaScriptableObject()
        {
            string packagePathPrefix = "/Package/";
            var path = "Assets/" + packagePathPrefix + LuaPath + LuaFileName + Lua_Suffix;
            var obj = AssetDatabase.LoadAssetAtPath<LuaScriptableObject>(path);
            if (obj == null)
            {
                obj = UnityEngine.ScriptableObject.CreateInstance<LuaScriptableObject>();
                AssetDatabase.CreateAsset(obj, path);
            }
            else
            {
                obj.Clear();
            }

            var luacodepath = Application.dataPath + LuaCodePath;
            luacodepath = luacodepath.Replace("\\", "/");
            var luafilearray = Directory.GetFiles(luacodepath, "*.lua", SearchOption.AllDirectories);
            foreach (var luafile in luafilearray)
            {
                var luapath = luafile.Replace("\\", "/");
                var subluapath = luapath.Substring(luacodepath.Length);

                var destFile = Environment.CurrentDirectory + LuaCGenFileRelativePath + subluapath;
                var destfiledir = Path.GetDirectoryName(destFile);
                if (!Directory.Exists(destfiledir))
                    Directory.CreateDirectory(destfiledir);

                var srcFile = Environment.CurrentDirectory + LuaCSrcFileRelativePath + subluapath;
                var exepath = Environment.CurrentDirectory + LuaCExeRelativePath;
#if UNITY_IOS
                exepath = Environment.CurrentDirectory + LuaCExeMacRelativePath;
#endif
                var args = " -o " + destFile + " " + srcFile;
                if (!EditorUtils.ExecuteProcess(exepath, args))
                    continue;

                var luacode = System.IO.File.ReadAllBytes(destFile);
                System.IO.File.Delete(destFile);
                obj.AddEntry(subluapath, luacode);
            }

            EditorUtility.SetDirty(obj);
            AssetDatabase.SaveAssets();
        }

        /// <summary>预留：根据平台枚举返回相对 APK 输出路径。</summary>
        static string GetAndroidBuildPath(EAndroidBuildPlatform platform)
        {
            string prefix = "../../gameapp/";
            if (platform == EAndroidBuildPlatform.NoPlatform)
                return string.Format("{0}{1}.apk", prefix, s_Config.appName);
            return "";
        }

        /// <summary>返回 <see cref="EditorBuildSettings.scenes"/> 中已启用的场景路径列表。</summary>
        static List<string> GetAllScenes()
        {
            List<string> scenes = new List<string>();
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                if (!scene.enabled) continue;
                scenes.Add(scene.path);
            }
            return scenes;
        }
    }
}
