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
        /// 获取 Bundle 根目录，统一返回 <see cref="ST.Core.FilePath.streamingAssetsPath"/>。
        /// Editor 下为工程根目录的 <c>BuildRes/</c>；
        /// Windows 独立包为 exe 上两级的 <c>BuildRes/</c>；
        /// 移动端为 <c>StreamingAssets/</c>。
        /// </summary>
        /// <returns>以 <c>/</c> 结尾的根目录路径。</returns>
        public string GetFilePath()
        {
            string rootPath = FilePath.streamingAssetsPath;
            return rootPath;
        }

        /// <summary>
        /// 拼接 Bundle 完整磁盘路径：<c>GetFilePath() + appName + "/" + respath</c>。
        /// </summary>
        /// <param name="respath">Bundle 在应用子目录内的相对路径（来自清单 key）。</param>
        /// <returns>可直接传入 <c>AssetBundle.LoadFromFile</c> 的完整路径。</returns>
        public string GetBundleFullPath(string respath)
        {
            string rootPath = GetFilePath();
            string bundlePath = string.Format("{0}{1}/{2}", rootPath, m_Config.appName, respath);
            return bundlePath;
        }
    }
}
