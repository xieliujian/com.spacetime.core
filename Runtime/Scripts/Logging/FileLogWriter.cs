using System;
using System.Collections.Generic;
using System.IO;

namespace ST.Core.Logging
{
    /// <summary>
    /// 文件日志写入器，实现 <see cref="ILogWriter"/>
    /// 采用内存缓存 + 批量刷新策略，支持文件轮转与备份
    /// </summary>
    public class FileLogWriter : ILogWriter
    {
        /// <summary>日志文件完整路径</summary>
        private readonly string m_FilePath;

        /// <summary>缓存中积累多少条日志后触发自动刷新</summary>
        private readonly int m_MaxFlushCount;

        /// <summary>单个日志文件最大允许大小（字节）</summary>
        private readonly long m_MaxFileSize;

        /// <summary>文件超出大小限制时是否保留 .bak 备份</summary>
        private readonly bool m_EnableBackup;

        /// <summary>底层文件流，用于持久化日志数据</summary>
        private FileStream m_FileStream;

        /// <summary>在 <see cref="m_FileStream"/> 之上的文本写入器</summary>
        private StreamWriter m_StreamWriter;

        /// <summary>待写入文件的日志缓存列表</summary>
        private List<string> m_CacheList = new List<string>();

        /// <summary>
        /// 构造函数，创建写入器并以覆盖模式打开日志文件
        /// </summary>
        /// <param name="filePath">日志文件路径</param>
        /// <param name="maxFlushCount">自动刷新阈值（缓存条目数）</param>
        /// <param name="maxFileSize">文件最大大小（字节）</param>
        /// <param name="enableBackup">超限时是否保留备份</param>
        public FileLogWriter(string filePath, int maxFlushCount, long maxFileSize, bool enableBackup)
        {
            m_FilePath = filePath;
            m_MaxFlushCount = maxFlushCount;
            m_MaxFileSize = maxFileSize;
            m_EnableBackup = enableBackup;

            OpenFile(false);
        }

        /// <summary>
        /// 将格式化后的日志追加到缓存，达到阈值时自动触发 <see cref="Flush"/>
        /// </summary>
        /// <param name="formattedLog">已格式化的日志字符串</param>
        public void Write(string formattedLog)
        {
            if (m_StreamWriter == null) return;

            m_CacheList.Add(formattedLog);

            if (m_CacheList.Count >= m_MaxFlushCount)
            {
                Flush();
            }
        }

        /// <summary>
        /// 将缓存中所有日志一次性写入文件并清空缓存
        /// 缓存为空时直接返回，写入失败时记录错误到 Unity 控制台
        /// </summary>
        public void Flush()
        {
            if (m_StreamWriter == null || m_CacheList.Count == 0) return;

            try
            {
                foreach (string log in m_CacheList)
                {
                    m_StreamWriter.WriteLine(log);
                }
                m_CacheList.Clear();
                m_StreamWriter.Flush();
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[FileLogWriter] Flush failed: {ex.Message}");
            }
        }

        /// <summary>
        /// 刷新缓存后关闭 <see cref="StreamWriter"/> 与 <see cref="FileStream"/>，释放文件句柄
        /// </summary>
        public void Close()
        {
            Flush();

            if (m_StreamWriter != null)
            {
                m_StreamWriter.Close();
                m_StreamWriter = null;
            }

            if (m_FileStream != null)
            {
                m_FileStream.Close();
                m_FileStream = null;
            }
        }

        /// <summary>
        /// 打开（或创建）日志文件
        /// 若文件超出大小限制，先执行备份或删除再重新创建
        /// </summary>
        /// <param name="append">true 表示追加写入，false 表示清空重写</param>
        private void OpenFile(bool append)
        {
            try
            {
                if (File.Exists(m_FilePath))
                {
                    FileInfo fileInfo = new FileInfo(m_FilePath);
                    if (fileInfo.Length > m_MaxFileSize)
                    {
                        if (m_EnableBackup)
                        {
                            BackupFile();
                        }
                        else
                        {
                            File.Delete(m_FilePath);
                        }
                    }
                }

                string directory = Path.GetDirectoryName(m_FilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

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
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[FileLogWriter] Open file failed: {m_FilePath}, Error: {ex.Message}");
            }
        }

        /// <summary>
        /// 将当前日志文件重命名为 .bak 备份文件
        /// 若已存在旧备份文件，先将其删除再执行重命名
        /// </summary>
        private void BackupFile()
        {
            try
            {
                string backupPath = m_FilePath + ".bak";
                if (File.Exists(backupPath))
                {
                    File.Delete(backupPath);
                }
                File.Move(m_FilePath, backupPath);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[FileLogWriter] Backup file failed: {ex.Message}");
            }
        }
    }
}
