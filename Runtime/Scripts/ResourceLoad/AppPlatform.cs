using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ST.Core
{
    /// <summary>
    /// 应用数据根路径、StreamingAssets 子目录、以及编辑器下构建目标与打包输出路径等跨平台工具。
    /// </summary>
    public static class AppPlatform
    {
        /// <summary>数据根目录（Editor 下为工程目录，运行时因平台而异）。</summary>
        public static string dataPath
        {
            get
            {
#if UNITY_EDITOR
                return Application.dataPath + "/../";
#else
                if (Application.platform == RuntimePlatform.WindowsPlayer)
                    return Application.dataPath + "/";
                return Application.persistentDataPath + "/";
#endif
            }
        }

        /// <summary>StreamingAssets 下以应用名为子目录的完整路径（小写）。</summary>
        /// <param name="appName">应用/游戏名称，与 <see cref="IResourceConfig.appName"/> 一致。</param>
        public static string GetStreamingAssetsPath(string appName)
        {
            return Application.streamingAssetsPath + "/" + appName.ToLower() + "/";
        }

#if UNITY_EDITOR
        /// <summary>根据当前编辑器宏推断活动 <see cref="BuildTarget"/>（无匹配时为 <see cref="BuildTarget.NoTarget"/>）。</summary>
        public static BuildTarget GetCurBuildTarget()
        {
            var target = BuildTarget.NoTarget;
#if UNITY_ANDROID
            target = BuildTarget.Android;
#endif
#if UNITY_IOS
            target = BuildTarget.iOS;
#endif
#if UNITY_STANDALONE_WIN
            target = BuildTarget.StandaloneWindows;
#endif
            return target;
        }

        /// <summary>根据当前编辑器宏推断 <see cref="BuildTargetGroup"/>。</summary>
        public static BuildTargetGroup GetCurBuildTargetGroup()
        {
            var targetgroup = BuildTargetGroup.Standalone;
#if UNITY_ANDROID
            targetgroup = BuildTargetGroup.Android;
#endif
#if UNITY_IOS
            targetgroup = BuildTargetGroup.iOS;
#endif
#if UNITY_STANDALONE_WIN
            targetgroup = BuildTargetGroup.Standalone;
#endif
            return targetgroup;
        }

        /// <summary>编辑器下 AssetBundle 打包输出目录（工程外 <c>assetBundle/&lt;平台&gt;/&lt;应用名&gt;/</c>）。</summary>
        /// <param name="target">目标平台。</param>
        /// <param name="appName">应用名（小写子目录）。</param>
        public static string GetPackageResPath(BuildTarget target, string appName)
        {
            string platformpath = "";
            if (target == BuildTarget.StandaloneWindows)
                platformpath = RuntimePlatform.WindowsPlayer.ToString().ToLower();
            else if (target == BuildTarget.Android)
                platformpath = RuntimePlatform.Android.ToString().ToLower();
            else if (target == BuildTarget.iOS)
                platformpath = RuntimePlatform.IPhonePlayer.ToString().ToLower();

            string appname = appName.ToLower();
            return Application.dataPath + "/../assetBundle/" + platformpath + "/" + appname + "/";
        }
#endif
    }
}