using System;
using System.IO;
using UnityEngine;

namespace ST.Core
{
    /// <summary>平台根目录解析与目录创建部分。</summary>
    public static partial class FilePath
    {
        /// <summary>
        /// 获取指定层级的上级目录。
        /// </summary>
        /// <param name="dir">起始目录，使用 <c>/</c> 分隔。</param>
        /// <param name="floor">向上回溯的层数。</param>
        /// <returns>上级目录，层数超出时返回已经到达的根。</returns>
        public static string GetParentDir(string dir, int floor = 1)
        {
            string subDir = dir;

            for (int i = 0; i < floor; ++i)
            {
                int last = subDir.LastIndexOf('/');
                bool isValidIndex = last >= 0;
                if (!isValidIndex)
                {
                    break;
                }

                subDir = subDir.Substring(0, last);
            }

            return subDir;
        }

        /// <summary>解析外存根目录，Android 优先使用注入的平台实现。</summary>
        /// <returns>以 <c>/</c> 结尾的目录路径。</returns>
        static string GetPersistentDataPath()
        {
            string result = string.Empty;

#if UNITY_ANDROID && !UNITY_EDITOR
            bool hasProvider = s_PlatformProvider != null;
            if (hasProvider)
            {
                result = s_PlatformProvider.GetPersistentDataPath();
            }
#endif

            bool isEmptyResult = string.IsNullOrEmpty(result);
            if (isEmptyResult)
            {
                string defaultPath = Application.persistentDataPath;
                result = defaultPath;
            }

            string slashPath = EnsureTrailingSlash(result);
            return slashPath;
        }

        /// <summary>解析包内根目录，Editor 与 Windows 播放器使用工程或安装目录旁的 BuildRes。</summary>
        /// <returns>以 <c>/</c> 结尾的目录路径。</returns>
        static string GetStreamingAssetsPath()
        {
            string result = string.Empty;

#if UNITY_EDITOR
            string editorPath = GetParentDir(Application.dataPath, 1) + "/BuildRes/";
            result = editorPath;
#else
            bool isWindowsPlayer = Application.platform == RuntimePlatform.WindowsPlayer;
            if (isWindowsPlayer)
            {
                string playerPath = GetParentDir(Application.dataPath, 2) + "/BuildRes/";
                result = playerPath;
            }
            else
            {
                string defaultPath = Application.streamingAssetsPath;
                result = EnsureTrailingSlash(defaultPath);
            }
#endif

            return result;
        }

        /// <summary>解析 application dataPath，Android 分包时指向主资源包。</summary>
        /// <returns>dataPath。</returns>
        static string GetDataPath()
        {
            string result = string.Empty;

#if UNITY_ANDROID && !UNITY_EDITOR
            bool hasProvider = s_PlatformProvider != null;
            if (hasProvider)
            {
                result = s_PlatformProvider.GetDataPath();
            }
#endif

            bool hasProviderPath = !string.IsNullOrEmpty(result);
            if (hasProviderPath)
            {
                return result;
            }

            string defaultPath = Application.dataPath;
            result = defaultPath;

#if UNITY_ANDROID && !UNITY_EDITOR
            bool isBundle = isAppBundle;
            if (isBundle)
            {
                string parentDir = GetParentDir(result);
                result = parentDir + "/split_MainAssetPack.apk";
            }
#endif

            return result;
        }

        /// <summary>解析外存资源目录。</summary>
        /// <returns>以 <c>/</c> 结尾的目录路径。</returns>
        static string GetPersistentResPath()
        {
            string result = persistentDataPath + "Res/";

#if UNITY_STANDALONE_WIN && EXTERNAL_USE_CUR_DIR && !UNITY_EDITOR
            string externalPath = GetParentDir(Application.dataPath, 2) + "/Res/";
            result = externalPath;
#endif

            return result;
        }

        /// <summary>解析外存 Bin 目录。</summary>
        /// <returns>以 <c>/</c> 结尾的目录路径。</returns>
        static string GetPersistentBinPath()
        {
            string result = persistentDataPath + "Bin/";

#if UNITY_STANDALONE_WIN && EXTERNAL_USE_CUR_DIR && !UNITY_EDITOR
            string externalPath = GetParentDir(Application.dataPath, 2) + "/BinRes/";
            result = externalPath;
#endif

            return result;
        }

        /// <summary>确保目录存在，iOS 上同时设置不参与备份。</summary>
        /// <param name="directoryPath">目录路径。</param>
        static void EnsureDirectoryExists(string directoryPath)
        {
            bool isExist = Directory.Exists(directoryPath);
            if (isExist)
            {
                return;
            }

            string warpPath = GetWindowsWarpPath(directoryPath);
            Directory.CreateDirectory(warpPath);

#if UNITY_IPHONE && !UNITY_EDITOR
            UnityEngine.iOS.Device.SetNoBackupFlag(directoryPath);
#endif
        }

        /// <summary>确保路径以 <c>/</c> 结尾。</summary>
        /// <param name="path">原始路径。</param>
        /// <returns>补全后的路径，输入为空时返回空串。</returns>
        static string EnsureTrailingSlash(string path)
        {
            bool isEmptyPath = string.IsNullOrEmpty(path);
            if (isEmptyPath)
            {
                return string.Empty;
            }

            bool hasTrailingSlash = path.EndsWith("/", StringComparison.Ordinal);
            if (hasTrailingSlash)
            {
                return path;
            }

            string slashPath = path + "/";
            return slashPath;
        }
    }
}

