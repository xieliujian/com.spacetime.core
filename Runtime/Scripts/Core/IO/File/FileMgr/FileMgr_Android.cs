using ST.Core.Logging;

namespace ST.Core
{
    /// <summary>Android 包内读取部分，仅在 Android 真机编译。</summary>
    public static partial class FileMgr
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        /// <summary>从 APK 读取文件到调用方缓冲区。</summary>
        /// <param name="fileRelativePath">相对包内根目录的路径。</param>
        /// <param name="result">接收数据的缓冲区。</param>
        /// <returns>实际读取字节数，失败时返回 -1。</returns>
        static int ReadDataInAndroidApk(string fileRelativePath, byte[] result)
        {
            if (result == null)
            {
                return -1;
            }

            bool hasProvider = FilePath.platformProvider != null;
            if (!hasProvider)
            {
                STLogger.LogErrorF("[FileMgr] 包内读取缺少平台实现 {0}", fileRelativePath);
                return -1;
            }

            int readLength = FilePath.platformProvider.ReadFromPackage(fileRelativePath, result);
            return readLength;
        }

        /// <summary>从 APK 读取文件全部字节。</summary>
        /// <param name="fileRelativePath">相对包内根目录的路径。</param>
        /// <returns>文件字节，失败时返回 null。</returns>
        static byte[] ReadDataInAndroidApk(string fileRelativePath)
        {
            bool hasProvider = FilePath.platformProvider != null;
            if (!hasProvider)
            {
                STLogger.LogErrorF("[FileMgr] 包内读取缺少平台实现 {0}", fileRelativePath);
                return null;
            }

            byte[] data = FilePath.platformProvider.ReadFromPackage(fileRelativePath);
            return data;
        }

        /// <summary>判断文件是否在 APK 内。</summary>
        /// <param name="fileRelativePath">相对包内根目录的路径。</param>
        /// <returns><c>true</c> 表示包内存在该文件。</returns>
        static bool IsFileInAndroidApk(string fileRelativePath)
        {
            bool hasProvider = FilePath.platformProvider != null;
            if (!hasProvider)
            {
                return false;
            }

            bool isInPackage = FilePath.platformProvider.IsFileInPackage(fileRelativePath);
            return isInPackage;
        }
#endif
    }
}

