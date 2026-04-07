using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ST.Core
{
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

        /// <summary>StreamingAssets 下的应用子目录路径。</summary>
        public static string GetStreamingAssetsPath(string appName)
        {
            return Application.streamingAssetsPath + "/" + appName.ToLower() + "/";
        }

#if UNITY_EDITOR
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