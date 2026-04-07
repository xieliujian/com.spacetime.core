using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine;
using UnityEditor;
using ST.Core;

namespace ST.Core.Editor
{
    public class Packager
    {
        public enum EAndroidBuildPlatform { NoPlatform }

        static IResourceConfig s_Config;

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

        [MenuItem("ST/Build iPhone Resource", false, 100)]
        public static void BuildiPhoneResource()
        {
            PlayerSettings.iOS.appleEnableAutomaticSigning = true;
            PlayerSettings.iOS.appleDeveloperTeamID = "24AZABCKN4";
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.iOS, BuildTarget.iOS);
            BuildAssetResource(BuildTarget.iOS, AppPlatform.GetStreamingAssetsPath(s_Config.appName));
        }

        [MenuItem("ST/Build Android Resource", false, 101)]
        public static void BuildAndroidResource()
        {
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            BuildAssetResource(BuildTarget.Android, AppPlatform.GetStreamingAssetsPath(s_Config.appName));
        }

        [MenuItem("ST/Build Windows Resource", false, 102)]
        public static void BuildWindowsResource()
        {
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64);
            BuildAssetResource(BuildTarget.StandaloneWindows, AppPlatform.GetStreamingAssetsPath(s_Config.appName));
        }

        [MenuItem("ST/Package All Resource", false, 103)]
        public static void PackageAllResource()
        {
            BuildAssetResource(BuildTarget.StandaloneWindows, AppPlatform.GetPackageResPath(BuildTarget.StandaloneWindows, s_Config.appName));
            BuildAssetResource(BuildTarget.Android, AppPlatform.GetPackageResPath(BuildTarget.Android, s_Config.appName));
            BuildAssetResource(BuildTarget.iOS, AppPlatform.GetPackageResPath(BuildTarget.iOS, s_Config.appName));

            BuildTargetGroup curtargetgroup = AppPlatform.GetCurBuildTargetGroup();
            BuildTarget curtarget = AppPlatform.GetCurBuildTarget();
            EditorUserBuildSettings.SwitchActiveBuildTarget(curtargetgroup, curtarget);
        }

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

        static void AddAssetBundleBuild(string assetBundleName, string[] assetNames, string assetBundleVariant = "unity3d")
        {
            AssetBundleBuild build = new AssetBundleBuild();
            build.assetBundleName = assetBundleName;
            build.assetBundleVariant = assetBundleVariant;
            build.assetNames = assetNames;
            m_BundleBuildList.Add(build);
        }

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

        static void AddAllAssetBundle(string rootpath)
        {
            PackageFont(rootpath);
            PackageTexture(rootpath);
            PackageAudio(rootpath);
            PackagePrefab(rootpath);
            PackageScene(rootpath);
            PackageConfig_Lua(rootpath);
        }

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
                if (!EditorUtil.ExecuteProcess(exepath, args))
                    continue;

                var luacode = System.IO.File.ReadAllBytes(destFile);
                System.IO.File.Delete(destFile);
                obj.AddEntry(subluapath, luacode);
            }

            EditorUtility.SetDirty(obj);
            AssetDatabase.SaveAssets();
        }

        static string GetAndroidBuildPath(EAndroidBuildPlatform platform)
        {
            string prefix = "../../gameapp/";
            if (platform == EAndroidBuildPlatform.NoPlatform)
                return string.Format("{0}{1}.apk", prefix, s_Config.appName);
            return "";
        }

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
