namespace ST.Core
{
    /// <summary>
    /// 平台相关文件能力，承载 Android 的外存根目录、分包 dataPath 与 APK 包内读取。
    /// <para>未注入时文件层按 Unity 默认目录与 <c>System.IO</c> 处理。</para>
    /// </summary>
    public interface IPlatformFileProvider
    {
        /// <summary>
        /// 获取外存根目录。
        /// </summary>
        /// <returns>目录路径，返回空串或 null 表示使用 Unity 默认目录。</returns>
        string GetPersistentDataPath();

        /// <summary>
        /// 获取 application dataPath。
        /// </summary>
        /// <returns>路径，返回空串或 null 表示使用 <c>Application.dataPath</c>。</returns>
        string GetDataPath();

        /// <summary>
        /// 判断当前是否是 Android App Bundle 安装。
        /// </summary>
        /// <returns><c>true</c> 表示分包安装。</returns>
        bool IsAppBundle();

        /// <summary>
        /// 判断包内是否存在指定相对路径。
        /// </summary>
        /// <param name="relativePath">相对包内根目录的路径。</param>
        /// <returns><c>true</c> 表示包内存在该文件。</returns>
        bool IsFileInPackage(string relativePath);

        /// <summary>
        /// 读取包内文件的全部字节。
        /// </summary>
        /// <param name="relativePath">相对包内根目录的路径。</param>
        /// <returns>文件字节，失败时返回 null。</returns>
        byte[] ReadFromPackage(string relativePath);

        /// <summary>
        /// 把包内文件读入调用方缓冲区。
        /// </summary>
        /// <param name="relativePath">相对包内根目录的路径。</param>
        /// <param name="result">接收数据的缓冲区。</param>
        /// <returns>实际读取字节数，失败时返回 -1。</returns>
        int ReadFromPackage(string relativePath, byte[] result);

        /// <summary>
        /// 把包内文件拷贝到外存绝对路径。
        /// </summary>
        /// <param name="relativePath">相对包内根目录的路径。</param>
        /// <param name="outAbsPath">目标文件绝对路径。</param>
        /// <returns><c>true</c> 表示拷贝成功。</returns>
        bool CopyFromPackage(string relativePath, string outAbsPath);
    }
}

