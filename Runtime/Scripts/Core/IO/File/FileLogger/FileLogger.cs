using System;
using System.Collections.Generic;
using System.IO;
using ST.Core.Logging;

namespace ST.Core
{
    /// <summary>
    /// 资源访问日志，按批次写入文件，支持导出快照后继续追加。
    /// <para>与 <see cref="LogFileWriter"/> 的区别是提供 <see cref="CopyTo"/> 导出快照能力，供调试工具使用。</para>
    /// </summary>
    public partial class FileLogger
    {
        /// <summary>缓存达到该条数时自动刷新，数值越小越实时。</summary>
        public int maxFlushCount
        {
            get { return m_MaxFlushCount; }
            set { m_MaxFlushCount = value; }
        }

        /// <summary>日志文件的完整路径。</summary>
        public string filePath
        {
            get { return m_FilePath; }
        }

        /// <summary>
        /// 打开日志文件，旧文件超过 20MB 时先删除。
        /// </summary>
        /// <param name="logFilePath">日志文件完整路径。</param>
        /// <param name="append">为 <c>true</c> 时追加写入，否则清空重写。</param>
        public FileLogger(string logFilePath, bool append = false)
        {
            m_FilePath = logFilePath;

            bool isEmptyPath = string.IsNullOrEmpty(logFilePath);
            if (isEmptyPath)
            {
                STLogger.LogError("[FileLogger] 日志路径为空");
                return;
            }

            DeleteOversizeFile(logFilePath);

            try
            {
                m_FileStream = File.Open(m_FilePath, FileMode.OpenOrCreate);

                if (append)
                {
                    m_FileStream.Seek(0, SeekOrigin.End);
                }
                else
                {
                    m_FileStream.SetLength(0);
                }

                m_StreamWriter = new StreamWriter(m_FileStream);
            }
            catch (Exception e)
            {
                STLogger.LogErrorF("[FileLogger] 打开日志失败 {0}", logFilePath);
                STLogger.LogException(e);
            }
        }
    }
}
