using System;
using System.IO;
using System.Text;
using ST.Core.Logging;

namespace ST.Core
{
    /// <summary>文件写入部分，负责文本落盘与包内文件拷贝。</summary>
    public static partial class FileMgr
    {
        /// <summary>
        /// 把文本以 UTF-8 覆盖写入文件，目标目录需要已存在。
        /// </summary>
        /// <param name="content">写入内容。</param>
        /// <param name="filePath">目标文件绝对路径。</param>
        public static void WriteString2File(string content, string filePath)
        {
            try
            {
                string warpPath = FilePath.GetWindowsWarpPath(filePath);
                using (FileStream fileStream = new FileStream(warpPath, FileMode.Create))
                {
                    using (StreamWriter streamWriter = new StreamWriter(fileStream, Encoding.UTF8))
                    {
                        streamWriter.Write(content);
                    }
                }
            }
            catch (Exception e)
            {
                STLogger.LogError("WriteString2File Failed");
                STLogger.LogException(e);
            }
        }

        /// <summary>
        /// 把 Android 包内文件拷贝到外存目录。
        /// </summary>
        /// <param name="fileRelativePath">相对包内根目录的路径。</param>
        /// <param name="outAbsPath">目标文件绝对路径。</param>
        /// <param name="error">失败原因，成功时为空串。</param>
        /// <returns><c>true</c> 表示拷贝成功。</returns>
        public static bool CopyDataFromAndroidApk(string fileRelativePath, string outAbsPath, out string error)
        {
            error = string.Empty;
            bool isCopied = false;

#if UNITY_ANDROID && !UNITY_EDITOR
            STLogger.LogDebugF("FileMgr::CopyDataFromAndroidApk path={0} out={1}", fileRelativePath, outAbsPath);

            bool hasProvider = FilePath.platformProvider != null;
            if (hasProvider)
            {
                isCopied = FilePath.platformProvider.CopyFromPackage(fileRelativePath, outAbsPath);
            }
            else
            {
                error = "platform provider is null";
                STLogger.LogErrorF("[FileMgr] 拷贝包内文件缺少平台实现 {0}", fileRelativePath);
            }
#endif

            return isCopied;
        }
    }
}

