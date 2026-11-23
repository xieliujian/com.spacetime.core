namespace ST.Core
{
    /// <summary>文件读取对外接口与包内数据分发部分。</summary>
    public static partial class FileMgr
    {
        /// <summary>
        /// 同步读取文件全部字节，非线程安全。
        /// </summary>
        /// <param name="fileRelativePath">相对资源根目录的路径。</param>
        /// <returns>文件字节，失败时返回 null。</returns>
        public static byte[] ReadSync(string fileRelativePath)
        {
            string absoluteFilePath = FindFilePathInExteral(fileRelativePath);
            bool isInExternal = !string.IsNullOrEmpty(absoluteFilePath);

            byte[] result = null;
            if (isInExternal)
            {
                result = ReadFile(absoluteFilePath);
            }
            else
            {
                result = ReadSyncInternal(fileRelativePath);
            }

            return result;
        }

        /// <summary>
        /// 线程安全的读取，调用方先解析好绝对路径与来源。
        /// </summary>
        /// <param name="fileRelativePath">相对资源根目录的路径。</param>
        /// <param name="absoluteFilePath">已经解析好的绝对路径。</param>
        /// <param name="isInExternal">为 <c>true</c> 时按绝对路径读取外存文件。</param>
        /// <returns>文件字节，失败时返回 null。</returns>
        public static byte[] ReadSyncInThread(string fileRelativePath, string absoluteFilePath, bool isInExternal)
        {
            byte[] result = null;
            if (isInExternal)
            {
                result = ReadFile(absoluteFilePath);
            }
            else
            {
                result = ReadInternalInThread(fileRelativePath, absoluteFilePath);
            }

            return result;
        }

        /// <summary>
        /// 同步读取文件到调用方缓冲区，非线程安全。
        /// </summary>
        /// <param name="fileRelativePath">相对资源根目录的路径。</param>
        /// <param name="result">接收数据的缓冲区。</param>
        /// <returns>实际读取字节数，失败时返回 -1。</returns>
        public static int ReadSync(string fileRelativePath, byte[] result)
        {
            string absoluteFilePath = FindFilePathInExteral(fileRelativePath);
            bool isInExternal = !string.IsNullOrEmpty(absoluteFilePath);

            int readLength = -1;
            if (isInExternal)
            {
                readLength = ReadFile(absoluteFilePath, result);
            }
            else
            {
                readLength = ReadSyncInternal(fileRelativePath, result);
            }

            return readLength;
        }

        /// <summary>
        /// 线程安全的读取，调用方先解析好绝对路径与来源。
        /// </summary>
        /// <param name="fileRelativePath">相对资源根目录的路径。</param>
        /// <param name="absoluteFilePath">已经解析好的绝对路径。</param>
        /// <param name="result">接收数据的缓冲区。</param>
        /// <param name="isInExternal">为 <c>true</c> 时按绝对路径读取外存文件。</param>
        /// <returns>实际读取字节数，失败时返回 -1。</returns>
        public static int ReadSyncInThread(string fileRelativePath, string absoluteFilePath,
                                           byte[] result, bool isInExternal)
        {
            int readLength = -1;
            if (isInExternal)
            {
                readLength = ReadFile(absoluteFilePath, result);
            }
            else
            {
                readLength = ReadInternalInThread(fileRelativePath, absoluteFilePath, result);
            }

            return readLength;
        }

        /// <summary>
        /// 从包内读取文件全部字节。
        /// </summary>
        /// <param name="fileRelativePath">相对资源根目录的路径。</param>
        /// <returns>文件字节，失败时返回 null。</returns>
        public static byte[] ReadSyncInternal(string fileRelativePath)
        {
            byte[] data = ReadInternalData(fileRelativePath);
            return data;
        }

        /// <summary>
        /// 从包内读取文件到调用方缓冲区。
        /// </summary>
        /// <param name="fileRelativePath">相对资源根目录的路径。</param>
        /// <param name="result">接收数据的缓冲区。</param>
        /// <returns>实际读取字节数，失败时返回 -1。</returns>
        public static int ReadSyncInternal(string fileRelativePath, byte[] result)
        {
            int readLength = ReadInternalData(fileRelativePath, result);
            return readLength;
        }

        /// <summary>
        /// 从外存读取文件全部字节。
        /// </summary>
        /// <param name="fileRelativePath">相对资源根目录的路径。</param>
        /// <returns>文件字节，失败时返回 null。</returns>
        public static byte[] ReadSyncExtenal(string fileRelativePath)
        {
            string absoluteFilePath = FindFilePathInExteral(fileRelativePath);
            bool hasFilePath = !string.IsNullOrEmpty(absoluteFilePath);

            byte[] result = null;
            if (hasFilePath)
            {
                result = ReadFile(absoluteFilePath);
            }

            return result;
        }

        /// <summary>
        /// 从外存读取文件到调用方缓冲区。
        /// </summary>
        /// <param name="fileRelativePath">相对资源根目录的路径。</param>
        /// <param name="result">接收数据的缓冲区。</param>
        /// <returns>实际读取字节数，失败时返回 -1。</returns>
        public static int ReadSyncExtenal(string fileRelativePath, byte[] result)
        {
            string absoluteFilePath = FindFilePathInExteral(fileRelativePath);
            bool hasFilePath = !string.IsNullOrEmpty(absoluteFilePath);

            int readLength = -1;
            if (hasFilePath)
            {
                readLength = ReadFile(absoluteFilePath, result);
            }

            return readLength;
        }

        /// <summary>
        /// 按绝对路径读取文件全部字节。
        /// </summary>
        /// <param name="absoluteFilePath">文件绝对路径。</param>
        /// <returns>文件字节，文件不存在或读取失败时返回 null。</returns>
        public static byte[] ReadSyncByAbsoluteFilePath(string absoluteFilePath)
        {
            bool isExist = FilePath.FileExists(absoluteFilePath);

            byte[] result = null;
            if (isExist)
            {
                result = ReadFile(absoluteFilePath);
            }

            return result;
        }

        /// <summary>
        /// 从包内读取文件全部字节。
        /// </summary>
        /// <param name="fileRelativePath">相对资源根目录的路径。</param>
        /// <returns>文件字节，失败时返回 null。</returns>
        static byte[] ReadInternalData(string fileRelativePath)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            byte[] packageData = ReadDataInAndroidApk(fileRelativePath);
            return packageData;
#else
            string absoluteFilePath = FindFilePathInternal(fileRelativePath);
            bool hasFilePath = !string.IsNullOrEmpty(absoluteFilePath);

            byte[] result = null;
            if (hasFilePath)
            {
                result = ReadFile(absoluteFilePath);
            }

            return result;
#endif
        }

        /// <summary>
        /// 从包内读取文件到调用方缓冲区。
        /// </summary>
        /// <param name="fileRelativePath">相对资源根目录的路径。</param>
        /// <param name="result">接收数据的缓冲区。</param>
        /// <returns>实际读取字节数，失败时返回 -1。</returns>
        static int ReadInternalData(string fileRelativePath, byte[] result)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            int packageLength = ReadDataInAndroidApk(fileRelativePath, result);
            return packageLength;
#else
            string absoluteFilePath = FindFilePathInternal(fileRelativePath);
            bool hasFilePath = !string.IsNullOrEmpty(absoluteFilePath);

            int readLength = -1;
            if (hasFilePath)
            {
                readLength = ReadFile(absoluteFilePath, result);
            }

            return readLength;
#endif
        }

        /// <summary>
        /// 线程读取时从包内取数据。
        /// </summary>
        /// <param name="fileRelativePath">相对资源根目录的路径。</param>
        /// <param name="absoluteFilePath">已经解析好的绝对路径。</param>
        /// <returns>文件字节，失败时返回 null。</returns>
        static byte[] ReadInternalInThread(string fileRelativePath, string absoluteFilePath)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            byte[] packageData = ReadDataInAndroidApk(fileRelativePath);
            return packageData;
#else
            byte[] fileData = ReadFile(absoluteFilePath);
            return fileData;
#endif
        }

        /// <summary>
        /// 线程读取时从包内取数据。
        /// </summary>
        /// <param name="fileRelativePath">相对资源根目录的路径。</param>
        /// <param name="absoluteFilePath">已经解析好的绝对路径。</param>
        /// <param name="result">接收数据的缓冲区。</param>
        /// <returns>实际读取字节数，失败时返回 -1。</returns>
        static int ReadInternalInThread(string fileRelativePath, string absoluteFilePath, byte[] result)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            int packageLength = ReadDataInAndroidApk(fileRelativePath, result);
            return packageLength;
#else
            int readLength = ReadFile(absoluteFilePath, result);
            return readLength;
#endif
        }
    }
}

