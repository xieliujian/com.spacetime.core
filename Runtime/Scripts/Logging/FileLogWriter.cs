using System;
using System.IO;
using System.Text;

namespace ST.Core.Logging
{
    /// <summary>
    /// 文件日志写入器
    /// </summary>
    public class FileLogWriter : ILogWriter
    {
        private readonly string logDirectory;
        private readonly string logFileName;
        private readonly long maxFileSize;
        private readonly Encoding encoding;

        private StreamWriter writer;
        private string currentFilePath;
        private long currentFileSize;

        public FileLogWriter(string logDirectory, string logFileName, long maxFileSize = 10 * 1024 * 1024)
        {
            this.logDirectory = logDirectory;
            this.logFileName = logFileName;
            this.maxFileSize = maxFileSize;
            this.encoding = Encoding.UTF8;

            OpenFile();
        }

        public void Write(string message)
        {
            if (writer == null)
            {
                return;
            }

            try
            {
                byte[] messageBytes = encoding.GetBytes(message + Environment.NewLine);
                long messageSize = messageBytes.Length;

                if (currentFileSize + messageSize > maxFileSize)
                {
                    BackupFile();
                    OpenFile();
                }

                writer.WriteLine(message);
                currentFileSize += messageSize;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"FileLogWriter Write failed: {ex.Message}");
            }
        }

        public void Flush()
        {
            if (writer != null)
            {
                try
                {
                    writer.Flush();
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"FileLogWriter Flush failed: {ex.Message}");
                }
            }
        }

        public void Close()
        {
            if (writer != null)
            {
                try
                {
                    writer.Flush();
                    writer.Close();
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"FileLogWriter Close failed: {ex.Message}");
                }
                finally
                {
                    writer = null;
                }
            }
        }

        private void OpenFile()
        {
            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }

            currentFilePath = Path.Combine(logDirectory, logFileName);

            if (File.Exists(currentFilePath))
            {
                FileInfo fileInfo = new FileInfo(currentFilePath);
                currentFileSize = fileInfo.Length;

                if (currentFileSize >= maxFileSize)
                {
                    BackupFile();
                    currentFileSize = 0;
                }
            }
            else
            {
                currentFileSize = 0;
            }

            writer = new StreamWriter(currentFilePath, true, encoding);
            writer.AutoFlush = false;
        }

        private void BackupFile()
        {
            if (writer != null)
            {
                writer.Close();
                writer = null;
            }

            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string backupFileName = Path.GetFileNameWithoutExtension(logFileName) + "_" + timestamp + Path.GetExtension(logFileName);
            string backupFilePath = Path.Combine(logDirectory, backupFileName);

            if (File.Exists(currentFilePath))
            {
                File.Move(currentFilePath, backupFilePath);
            }
        }
    }
}
