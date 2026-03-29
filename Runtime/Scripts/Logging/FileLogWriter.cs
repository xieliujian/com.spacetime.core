using System;
using System.Collections.Generic;
using System.IO;

namespace ST.Core.Logging
{
    /// <summary>
    /// 文件日志写入器
    /// 支持批量刷新、文件轮转、备份机制
    /// </summary>
    public class FileLogWriter : ILogWriter
    {
        private readonly string m_FilePath;
        private readonly int m_MaxFlushCount;
        private readonly long m_MaxFileSize;
        private readonly bool m_EnableBackup;

        private FileStream m_FileStream;
        private StreamWriter m_StreamWriter;
        private List<string> m_CacheList = new List<string>();

        public FileLogWriter(string filePath, int maxFlushCount, long maxFileSize, bool enableBackup)
        {
            m_FilePath = filePath;
            m_MaxFlushCount = maxFlushCount;
            m_MaxFileSize = maxFileSize;
            m_EnableBackup = enableBackup;

            OpenFile(false);
        }

        public void Write(string formattedLog)
        {
            if (m_StreamWriter == null) return;

            m_CacheList.Add(formattedLog);

            if (m_CacheList.Count >= m_MaxFlushCount)
            {
                Flush();
            }
        }

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

        private void OpenFile(bool append)
        {
            try
            {
                // 检查文件大小，超过限制则清空或备份
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

                // 创建目录
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
