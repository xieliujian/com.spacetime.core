using UnityEngine;

namespace ST.Core
{
    /// <summary>
    /// 运行时文件路径帮助类，依赖 <see cref="IResourceConfig"/> 提供应用名称。
    /// </summary>
    public class FilePathHelper
    {
        /// <summary>资源配置，提供 <see cref="IResourceConfig.appName"/> 等参数。</summary>
        readonly IResourceConfig m_Config;

        /// <summary>使用资源配置构造路径帮助类。</summary>
        /// <param name="config">非空资源配置，用于解析应用子目录名等。</param>
        public FilePathHelper(IResourceConfig config)
        {
            m_Config = config;
        }

        /// <summary>
        /// 获取 Bundle 根目录：
        /// Editor 下返回工程根目录的 <c>BuildRes/</c>（避免放入 Assets 触发 import 卡顿）；
        /// Windows 独立包返回 exe 上两级的 <c>BuildRes/</c>；
        /// 移动端返回 <c>StreamingAssets/</c>。
        /// </summary>
        /// <returns>以 <c>/</c> 结尾的根目录路径。</returns>
        public string GetFilePath()
        {
#if UNITY_EDITOR
            return GetParentDir(Application.dataPath, 1) + "/BuildRes/";
#else
            if (Application.platform == RuntimePlatform.WindowsPlayer)
                return GetParentDir(Application.dataPath, 2) + "/BuildRes/";
            return Application.streamingAssetsPath + "/";
#endif
        }

        /// <summary>获取指定层级的上级目录。</summary>
        static string GetParentDir(string dir, int floor = 1)
        {
            string subDir = dir;
            for (int i = 0; i < floor; ++i)
            {
                int last = subDir.LastIndexOf('/');
                subDir = subDir.Substring(0, last);
            }
            return subDir;
        }

        /// <summary>
        /// 拼接 Bundle 完整磁盘路径：<c>GetFilePath() + appName + "/" + respath</c>。
        /// </summary>
        /// <param name="respath">Bundle 在应用子目录内的相对路径（来自清单 key）。</param>
        /// <returns>可直接传入 <c>AssetBundle.LoadFromFile</c> 的完整路径。</returns>
        public string GetBundleFullPath(string respath)
        {
            return GetFilePath() + m_Config.appName + "/" + respath;
        }
    }
}