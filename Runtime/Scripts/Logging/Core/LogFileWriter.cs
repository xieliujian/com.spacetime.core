using System;
using System.Collections.Generic;
using System.IO;

namespace ST.Core.Logging
{
    /// <summary>
    /// 文件日志写入器，使用内存缓存批量写入，并在打开文件时按大小做轮转与备份。
    /// </summary>
    public class LogFileWriter
    {
        /// <summary>日志文件完整路径，构造后不再变化。</summary>
        readonly string m_FilePath;

        /// <summary>缓存达到该条数时自动刷新，数值越小越实时、内存占用越低。</summary>
        readonly int m_MaxFlushCount;

        /// <summary>单个文件字节上限；只在打开文件时检查，写入过程中不会中途轮转。</summary>
        readonly long m_MaxFileSize;

        /// <summary>超限时是否把旧文件重命名为 .bak；为 false 时直接删除旧文件。</summary>
        readonly bool m_EnableBackup;

        /// <summary>底层文件流；打开失败时为 null。</summary>
        FileStream m_FileStream;

        /// <summary>文件流上的文本写入器；打开失败时为 null，此时所有写入会被忽略。</summary>
        StreamWriter m_StreamWriter;

        /// <summary>尚未落盘的日志，按插入顺序批量写出。</summary>
        List<string> m_CacheList = new List<string>();

        /// <summary>日志文件的完整路径。</summary>
        public string filePath
        {
            get { return m_FilePath; }
        }

        /// <summary>
        /// 保存轮转参数，并以覆盖模式打开日志文件。
        /// </summary>
        /// <param name="filePath">日志文件完整路径。</param>
        /// <param name="maxFlushCount">缓存达到多少条时自动刷新。</param>
        /// <param name="maxFileSize">单个文件字节上限，超出时在打开阶段轮转。</param>
        /// <param name="enableBackup">超限时是否保留 .bak 备份。</param>
        public LogFileWriter(string filePath, int maxFlushCount, long maxFileSize, bool enableBackup)
        {
            m_FilePath = filePath;
            m_MaxFlushCount = maxFlushCount;
            m_MaxFileSize = maxFileSize;
            m_EnableBackup = enableBackup;

            OpenFile(false);
        }

        /// <summary>
        /// 把一行日志加入缓存，达到阈值时立即刷新；写入器未打开时直接丢弃。
        /// </summary>
        /// <param name="formattedLog">已格式化的日志文本。</param>
        public void Write(string formattedLog)
        {
            lock (m_CacheList)
            {
                if (m_StreamWriter == null)
                {
                    return;
                }

                m_CacheList.Add(formattedLog);

                if (m_CacheList.Count >= m_MaxFlushCount)
                {
                    Flush();
                }
            }
        }

        /// <summary>
        /// 把缓存中的日志一次写入磁盘并清空缓存。
        /// <para>缓存为空或写入器未打开时直接返回；写入失败时保留缓存，并把错误输出到 Unity 控制台。</para>
        /// </summary>
        public void Flush()
        {
            lock (m_CacheList)
            {
                bool isWriterNull = m_StreamWriter == null;
                bool isEmptyCache = m_CacheList.Count == 0;
                bool isSkipFlush = isWriterNull || isEmptyCache;
                if (isSkipFlush)
                {
                    return;
                }

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
                    UnityEngine.Debug.LogError($"[LogFileWriter] Flush failed: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 先刷新缓存，再关闭写入器和文件流并清空引用。
        /// <para>重复调用是安全的。</para>
        /// </summary>
        public void Close()
        {
            lock (m_CacheList)
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
        }

        /// <summary>
        /// 把当前日志导出为快照，导出后以追加模式重新打开，不影响后续写入。
        /// </summary>
        /// <param name="destinationPath">快照目标路径。</param>
        public void CopyTo(string destinationPath)
        {
            bool isEmptyPath = string.IsNullOrEmpty(destinationPath);
            if (isEmptyPath)
            {
                return;
            }

            lock (m_CacheList)
            {
                Flush();
                Close();

                try
                {
                    File.Copy(m_FilePath, destinationPath);
                }
                finally
                {
                    OpenFile(true);
                }
            }
        }

        /// <summary>
        /// 打开或创建日志文件，必要时先备份或删除超限的旧文件。
        /// <para>打开失败时写入器保持为空，后续写入全部丢弃。</para>
        /// </summary>
        /// <param name="append">true 表示追加写入，false 表示清空重写。</param>
        void OpenFile(bool append)
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
                UnityEngine.Debug.LogError($"[LogFileWriter] Open file failed: {m_FilePath}, Error: {ex.Message}");
            }
        }

        /// <summary>
        /// 把现有日志文件重命名为 .bak，覆盖同名旧备份。
        /// <para>失败时只记录错误，原文件保持不变。</para>
        /// </summary>
        void BackupFile()
        {
            try
            {
                string backupPath = m_FilePath + LogDefine.k_BackupSuffix;
                if (File.Exists(backupPath))
                {
                    File.Delete(backupPath);
                }

                File.Move(m_FilePath, backupPath);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[LogFileWriter] Backup file failed: {ex.Message}");
            }
        }
    }
}
