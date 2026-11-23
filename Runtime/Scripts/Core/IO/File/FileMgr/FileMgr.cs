using ST.Core.Logging;

namespace ST.Core
{
    /// <summary>
    /// 原始文件访问层，按外存优先、包内兜底的顺序解析相对路径并同步读取字节。
    /// <para>外存清单判定与 Android 包内读取通过 <see cref="IFileResolvePolicy"/> 与 <see cref="IPlatformFileProvider"/> 注入。</para>
    /// </summary>
    public static partial class FileMgr
    {
        /// <summary>资源访问日志实例，未启用时为 null。</summary>
        public static LogFileWriter fileLogger
        {
            get { return s_FileLogger; }
        }

        /// <summary>
        /// 注入外存清单判定。
        /// </summary>
        /// <param name="policy">清单判定，传 null 恢复目录扫描。</param>
        public static void SetResolvePolicy(IFileResolvePolicy policy)
        {
            s_ResolvePolicy = policy;
        }

        /// <summary>
        /// 解析资源路径，外存存在时返回外存绝对路径，否则返回包内绝对路径。
        /// </summary>
        /// <param name="relativePath">相对资源根目录的路径。</param>
        /// <returns>可直接交给外部库加载的绝对路径。</returns>
        public static string GetResPathInPersistentOrStream(string relativePath)
        {
            string resPersistentPath = FindFilePathInExteral(relativePath);
            bool hasPersistentPath = !string.IsNullOrEmpty(resPersistentPath);

            string result = resPersistentPath;
            if (!hasPersistentPath)
            {
                result = FilePath.streamingAssetsPath + relativePath;
            }

            return result;
        }

        /// <summary>
        /// 判断相对路径是否解析到外存。
        /// </summary>
        /// <param name="relativePath">相对资源根目录的路径。</param>
        /// <returns><c>true</c> 表示命中外存。</returns>
        public static bool IsInExternal(string relativePath)
        {
            string externalFilePath = FindFilePathInExteral(relativePath);
            bool isInExternal = !string.IsNullOrEmpty(externalFilePath);
            return isInExternal;
        }

        /// <summary>
        /// 判断相对路径对应的文件是否存在，先查外存再查包内。
        /// </summary>
        /// <param name="relativePath">相对资源根目录的路径。</param>
        /// <returns><c>true</c> 表示文件存在。</returns>
        public static bool IsFileExists(string relativePath)
        {
            string externalFilePath = FindFilePathInExteral(relativePath);
            bool isExternalExist = !string.IsNullOrEmpty(externalFilePath);
            if (isExternalExist)
            {
                return true;
            }

            bool isInternalExist = IsFileExistsInPackageOrInternal(relativePath);
            return isInternalExist;
        }

        /// <summary>
        /// 在外存目录中查找文件，清单权威时只查清单。
        /// </summary>
        /// <param name="file">相对资源根目录的路径。</param>
        /// <returns>外存绝对路径，未命中时返回空串。</returns>
        public static string FindFilePathInExteral(string file)
        {
            PushFileLog(file);

            bool isManifestAuthoritative = IsManifestAuthoritative();
            if (isManifestAuthoritative)
            {
                bool isExistInManifest = s_ResolvePolicy.ExistInExternal(file);

                string manifestPath = string.Empty;
                if (isExistInManifest)
                {
                    manifestPath = FilePath.persistentDataPath4Res + file;
                }

                return manifestPath;
            }

            string externalFilePath = FilePath.persistentDataPath4Res + file;
            bool isExist = FilePath.FileExists(externalFilePath);

            string result = string.Empty;
            if (isExist)
            {
                result = externalFilePath;
            }

            return result;
        }

        /// <summary>
        /// 在包内目录中查找文件。
        /// </summary>
        /// <param name="fileRelativePath">相对资源根目录的路径。</param>
        /// <returns>包内绝对路径，未命中时返回空串。</returns>
        static string FindFilePathInternal(string fileRelativePath)
        {
            PushFileLog(fileRelativePath);

            string absoluteFilePath = FilePath.streamingAssetsPath + fileRelativePath;
            bool isExist = FilePath.FileExists(absoluteFilePath);

            string result = string.Empty;
            if (isExist)
            {
                result = absoluteFilePath;
            }

            return result;
        }

        /// <summary>判断外存清单是否可以作为唯一判定依据。</summary>
        /// <returns><c>true</c> 表示以清单为准。</returns>
        static bool IsManifestAuthoritative()
        {
            bool isAuthoritative = false;
            bool hasPolicy = s_ResolvePolicy != null;
            if (hasPolicy)
            {
                isAuthoritative = s_ResolvePolicy.IsManifestAuthoritative();
            }

            return isAuthoritative;
        }

        /// <summary>
        /// 是否把包内文件当作存在依据，Android 走 APK，其它平台查磁盘。
        /// </summary>
        /// <param name="relativePath">相对资源根目录的路径。</param>
        /// <returns><c>true</c> 表示存在。</returns>
        static bool IsFileExistsInPackageOrInternal(string relativePath)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            bool isInPackage = IsFileInAndroidApk(relativePath);
            return isInPackage;
#else
            string internalFilePath = FindFilePathInternal(relativePath);
            bool isInternalExist = !string.IsNullOrEmpty(internalFilePath);
            return isInternalExist;
#endif
        }
    }
}
