namespace ST.Core
{
    /// <summary>
    /// 外存资源清单判定，由业务的更新模块实现，用于决定相对路径是否命中已下载到外存的资源。
    /// <para>未注入或判定为非权威时，文件层回退到外存目录扫描。</para>
    /// </summary>
    public interface IFileResolvePolicy
    {
        /// <summary>
        /// 清单是否可以作为唯一判定依据。
        /// </summary>
        /// <returns><c>true</c> 表示以清单为准，不再探测磁盘。</returns>
        bool IsManifestAuthoritative();

        /// <summary>
        /// 判断相对路径是否记录在外存清单中。
        /// </summary>
        /// <param name="relativePath">相对资源根目录的路径。</param>
        /// <returns><c>true</c> 表示清单中存在该路径。</returns>
        bool ExistInExternal(string relativePath);
    }
}

