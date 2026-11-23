using System;
using System.IO;
using System.Text;

namespace ST.Core
{
    /// <summary>Windows 超长路径转换与低层文件操作部分。</summary>
    public static partial class FilePath
    {
        /// <summary>
        /// 把 Windows 超长路径转换为扩展路径，其它平台原样返回。
        /// </summary>
        /// <param name="path">原始路径。</param>
        /// <returns>可交给 <c>System.IO</c> 使用的路径。</returns>
        public static string GetWindowsWarpPath(string path)
        {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            bool isEmptyPath = string.IsNullOrEmpty(path);
            if (isEmptyPath)
            {
                return path;
            }

            bool hasExtendedPrefix = path.StartsWith(@"\\?\", StringComparison.Ordinal);
            if (hasExtendedPrefix)
            {
                return path;
            }

            bool hasDevicePrefix = path.StartsWith(@"\\.\", StringComparison.Ordinal);
            if (hasDevicePrefix)
            {
                return path;
            }

            bool isShortPath = path.Length <= 240;
            if (isShortPath)
            {
                return path;
            }

            string fullPath = Path.GetFullPath(path);
            bool isUncPath = fullPath.StartsWith(@"\\", StringComparison.Ordinal);
            if (isUncPath)
            {
                string uncPath = @"\\?\UNC\" + fullPath.Substring(2);
                return uncPath;
            }

            string extendedPath = @"\\?\" + fullPath;
            return extendedPath;
#else
            return path;
#endif
        }

        /// <summary>判断文件是否存在，Windows 下自动处理超长路径。</summary>
        /// <param name="path">文件路径。</param>
        /// <returns><c>true</c> 表示文件存在。</returns>
        public static bool FileExists(string path)
        {
            string warpPath = GetWindowsWarpPath(path);
            bool isExist = File.Exists(warpPath);
            return isExist;
        }

        /// <summary>删除文件，Windows 下自动处理超长路径。</summary>
        /// <param name="path">文件路径。</param>
        public static void FileDelete(string path)
        {
            string warpPath = GetWindowsWarpPath(path);
            File.Delete(warpPath);
        }

        /// <summary>拷贝文件，Windows 下自动处理超长路径。</summary>
        /// <param name="srcPath">源文件路径。</param>
        /// <param name="dstPath">目标文件路径。</param>
        public static void FileCopy(string srcPath, string dstPath)
        {
            string warpSrcPath = GetWindowsWarpPath(srcPath);
            string warpDstPath = GetWindowsWarpPath(dstPath);
            File.Copy(warpSrcPath, warpDstPath);
        }

        /// <summary>移动文件，Windows 下自动处理超长路径。</summary>
        /// <param name="srcPath">源文件路径。</param>
        /// <param name="dstPath">目标文件路径。</param>
        public static void FileMove(string srcPath, string dstPath)
        {
            string warpSrcPath = GetWindowsWarpPath(srcPath);
            string warpDstPath = GetWindowsWarpPath(dstPath);
            File.Move(warpSrcPath, warpDstPath);
        }

        /// <summary>以只读方式打开文件，允许其它进程同时读取。</summary>
        /// <param name="path">文件路径。</param>
        /// <returns>只读文件流。</returns>
        public static FileStream FileOpenRead(string path)
        {
            string warpPath = GetWindowsWarpPath(path);
            FileStream fileStream = new FileStream(warpPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            return fileStream;
        }

        /// <summary>按指定编码读取全部文本。</summary>
        /// <param name="path">文件路径。</param>
        /// <param name="encoding">文本编码。</param>
        /// <returns>文件全部文本。</returns>
        public static string FileReadAllText(string path, Encoding encoding)
        {
            string warpPath = GetWindowsWarpPath(path);
            string text = File.ReadAllText(warpPath, encoding);
            return text;
        }

        /// <summary>读取全部字节。</summary>
        /// <param name="path">文件路径。</param>
        /// <returns>文件全部字节。</returns>
        public static byte[] FileReadAllBytes(string path)
        {
            string warpPath = GetWindowsWarpPath(path);
            byte[] data = File.ReadAllBytes(warpPath);
            return data;
        }
    }
}

