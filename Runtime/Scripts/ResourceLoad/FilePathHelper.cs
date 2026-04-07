using UnityEngine;

namespace ST.Core
{
    /// <summary>
    /// 运行时文件路径帮助类，依赖 <see cref="IResourceConfig"/> 提供应用名称。
    /// </summary>
    public class FilePathHelper
    {
        readonly IResourceConfig m_Config;

        public FilePathHelper(IResourceConfig config)
        {
            m_Config = config;
        }

        /// <summary>获取 Bundle 根目录（StreamingAssets 下）。</summary>
        public string GetFilePath()
        {
#if UNITY_EDITOR
            return Application.streamingAssetsPath + "/";
#else
            if (Application.platform == RuntimePlatform.WindowsPlayer)
                return Application.dataPath + "/StreamingAssets/";
            return Application.streamingAssetsPath + "/";
#endif
        }

        /// <summary>获取指定相对路径的 Bundle 完整路径。</summary>
        public string GetBundleFullPath(string respath)
        {
            return GetFilePath() + m_Config.appName + "/" + respath;
        }
    }
}