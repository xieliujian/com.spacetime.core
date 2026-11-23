using ST.Core.Logging;

namespace ST.Core
{
    /// <summary>资源访问日志部分。</summary>
    public static partial class FileMgr
    {
        /// <summary>记录一次资源访问，未启用日志时直接返回。</summary>
        /// <param name="relativePath">被访问的相对路径。</param>
        static void PushFileLog(string relativePath)
        {
            if (!isAccessLogEnabled)
            {
                return;
            }

            bool isIgnoredPath = relativePath.Contains(FileDefine.k_IgnoreLogKeyword);
            if (isIgnoredPath)
            {
                return;
            }

            lock (s_FileLogLock)
            {
                bool isLoggerNull = s_FileLogger == null;
                if (isLoggerNull)
                {
                    string logFilePath = FilePath.persistentDataPath4Temp + FileDefine.k_AccessLogFileName;
                    s_FileLogger = new LogFileWriter(logFilePath, FileDefine.k_AccessLogFlushCount,
                        FileDefine.k_MaxLogFileSize, false);
                }

                s_FileLogger.Write(relativePath);
            }
        }
    }
}

