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
            // TODO: Implement in Task 8
        }

        public void Flush()
        {
            // TODO: Implement in Task 8
        }

        public void Close()
        {
            // TODO: Implement in Task 8
        }

        private void OpenFile()
        {
            // TODO: Implement in Task 7
        }

        private void BackupFile()
        {
            // TODO: Implement in Task 7
        }
    }
}
