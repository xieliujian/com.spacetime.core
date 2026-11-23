using System;
using System.IO;
using ST.Core.Logging;

namespace ST.Core
{
    /// <summary>写入、刷新与快照导出部分。</summary>
    public partial class FileLogger
    {
        /// <summary>
        /// 把一行日志加入缓存，达到阈值时立即刷新。
        /// </summary>
        /// <param name="log">日志文本。</param>
        /// <param name="flush">为 <c>true</c> 时本次写入后立即落盘。</param>
        public void PushLog(string log, bool flush = false)
        {
            bool isWriterNull = m_StreamWriter == null;
            if (isWriterNull)
            {
                return;
            }

            bool isEmptyLog = string.IsNullOrEmpty(log);
            bool isIgnoredLog = false;
            if (!isEmptyLog)
            {
                isIgnoredLog = log.Contains(k_IgnoreLogKeyword);
            }

            bool isInvalidLog = isEmptyLog || isIgnoredLog;
            if (isInvalidLog)
            {
                return;
            }

            lock (m_CacheLogList)
            {
                m_CacheLogList.Add(log);

                bool isReachLimit = m_CacheLogList.Count >= m_MaxFlushCount;
                bool isNeedFlush = flush || isReachLimit;
                if (isNeedFlush)
                {
                    Flush();
                }
            }
        }

        /// <summary>
        /// 把缓存中的日志一次写入磁盘并清空缓存。
        /// </summary>
        public void Flush()
        {
            lock (m_CacheLogList)
            {
                try
                {
                    bool isWriterNull = m_StreamWriter == null;
                    bool isEmptyCache = m_CacheLogList.Count == 0;
                    bool isSkipFlush = isWriterNull || isEmptyCache;
                    if (isSkipFlush)
                    {
                        return;
                    }

                    for (int i = 0; i < m_CacheLogList.Count; ++i)
                    {
                        m_StreamWriter.WriteLine(m_CacheLogList[i]);
                    }

                    m_CacheLogList.Clear();
                    m_StreamWriter.Flush();
                }
                catch (Exception e)
                {
                    STLogger.LogException(e);
                }
            }
        }

        /// <summary>
        /// 先刷新缓存，再关闭写入器和文件流。重复调用是安全的。
        /// </summary>
        public void Close()
        {
            lock (m_CacheLogList)
            {
                Flush();

                bool hasWriter = m_StreamWriter != null;
                if (hasWriter)
                {
                    m_StreamWriter.Close();
                    m_StreamWriter = null;
                }

                bool hasStream = m_FileStream != null;
                if (hasStream)
                {
                    m_FileStream.Close();
                    m_FileStream = null;
                }
            }
        }

        /// <summary>
        /// 把当前日志导出为快照，导出后重新以追加模式打开，不影响后续收集。
        /// </summary>
        /// <param name="destinationPath">快照目标路径。</param>
        public void CopyTo(string destinationPath)
        {
            bool isEmptyPath = string.IsNullOrEmpty(destinationPath);
            if (isEmptyPath)
            {
                return;
            }

            lock (m_CacheLogList)
            {
                Flush();
                Close();

                try
                {
                    File.Copy(m_FilePath, destinationPath);
                }
                finally
                {
                    ReopenAppend();
                }
            }
        }

        /// <summary>旧文件超过上限时删除，避免日志无限增长。</summary>
        /// <param name="logFilePath">日志文件完整路径。</param>
        static void DeleteOversizeFile(string logFilePath)
        {
            bool isExist = File.Exists(logFilePath);
            if (!isExist)
            {
                return;
            }

            FileInfo fileInfo = new FileInfo(logFilePath);
            bool isOversize = fileInfo.Length > k_MaxFileSize;
            if (!isOversize)
            {
                return;
            }

            File.Delete(logFilePath);
        }

        /// <summary>以追加模式重新打开日志文件，打开失败时记录日志。</summary>
        void ReopenAppend()
        {
            try
            {
                m_FileStream = File.Open(m_FilePath, FileMode.Append);
                m_StreamWriter = new StreamWriter(m_FileStream);
            }
            catch (Exception e)
            {
                STLogger.LogErrorF("[FileLogger] 重新打开日志失败 {0}", m_FilePath);
                STLogger.LogException(e);
            }
        }
    }
}

