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

            lock (s_FileLogLock)
            {
                bool isLoggerNull = s_FileLogger == null;
                if (isLoggerNull)
                {
                    string logFilePath = FilePath.persistentDataPath4Temp + "FileLog.txt";
                    s_FileLogger = new FileLogger(logFilePath);
                }

                s_FileLogger.PushLog(relativePath);
            }
        }
    }
}

