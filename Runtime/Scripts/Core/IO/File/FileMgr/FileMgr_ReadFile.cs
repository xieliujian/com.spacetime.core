using System;
using System.IO;
using ST.Core.Logging;

namespace ST.Core
{
    /// <summary>物理文件读取部分，负责读满与缓冲区校验。</summary>
    public static partial class FileMgr
    {
        /// <summary>
        /// 读取文件全部字节。
        /// </summary>
        /// <param name="path">文件绝对路径。</param>
        /// <returns>文件字节，读取不完整或失败时返回 null。</returns>
        static byte[] ReadFile(string path)
        {
            byte[] result = null;

            try
            {
                string warpPath = FilePath.GetWindowsWarpPath(path);
                using (FileStream fileStream = new FileStream(warpPath, FileMode.Open, FileAccess.Read))
                {
                    long fileLength = fileStream.Length;
                    bool isTooLarge = fileLength > int.MaxValue;
                    if (isTooLarge)
                    {
                        STLogger.LogErrorF("[FileMgr] 文件超过 2GB 无法一次读取 {0}", path);
                    }
                    else
                    {
                        byte[] byteData = new byte[(int)fileLength];
                        int readLength = ReadFully(fileStream, byteData);
                        bool isComplete = readLength == byteData.Length;
                        if (isComplete)
                        {
                            result = byteData;
                        }
                        else
                        {
                            STLogger.LogErrorF("[FileMgr] 读取不完整 期望 {0} 实际 {1} 路径 {2}",
                                byteData.Length, readLength, path);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                STLogger.LogException(e);
            }

            return result;
        }

        /// <summary>
        /// 读取文件到调用方缓冲区。
        /// </summary>
        /// <param name="path">文件绝对路径。</param>
        /// <param name="result">接收数据的缓冲区。</param>
        /// <returns>实际读取字节数，缓冲区不足或读取失败时返回 -1。</returns>
        static int ReadFile(string path, byte[] result)
        {
            if (result == null)
            {
                return -1;
            }

            int readLength = -1;

            try
            {
                string warpPath = FilePath.GetWindowsWarpPath(path);
                using (FileStream fileStream = new FileStream(warpPath, FileMode.Open, FileAccess.Read))
                {
                    long fileLength = fileStream.Length;
                    bool isBufferTooSmall = fileLength > result.Length;
                    if (isBufferTooSmall)
                    {
                        STLogger.LogErrorF("[FileMgr] 缓冲区不足 文件 {0} 缓冲区 {1} 路径 {2}", fileLength, result.Length, path);
                    }
                    else
                    {
                        readLength = ReadFully(fileStream, result);
                    }
                }
            }
            catch (Exception e)
            {
                STLogger.LogException(e);
            }

            return readLength;
        }

        /// <summary>
        /// 把流读到缓冲区，直到读满或流结束。
        /// </summary>
        /// <param name="fileStream">已打开的只读文件流。</param>
        /// <param name="buffer">接收数据的缓冲区。</param>
        /// <returns>实际读取字节数。</returns>
        static int ReadFully(FileStream fileStream, byte[] buffer)
        {
            int totalRead = 0;

            while (totalRead < buffer.Length)
            {
                int readLength = fileStream.Read(buffer, totalRead, buffer.Length - totalRead);
                bool isEndOfStream = readLength <= 0;
                if (isEndOfStream)
                {
                    break;
                }

                totalRead += readLength;
            }

            return totalRead;
        }
    }
}

