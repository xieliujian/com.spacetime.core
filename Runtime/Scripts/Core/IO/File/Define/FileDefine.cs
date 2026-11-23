namespace ST.Core
{
    /// <summary>
    /// 文件模块的目录名、文件名与阈值常量，集中存放避免散落在实现中。
    /// </summary>
    public static class FileDefine
    {
        // 目录定义
        public const string k_BuildResDirPath = "/BuildRes/"; // 包内根目录在工程或安装目录中的相对路径
        public const string k_ResDirName = "Res/"; // 外存资源目录名
        public const string k_BinDirName = "Bin/"; // 外存 Bin 目录名
        public const string k_BinResDirName = "BinRes/"; // 外存 Bin 资源目录名，Windows 当前目录模式使用
        public const string k_ChatMediaDirName = "ChatMedia/"; // 外存聊天媒体目录名
        public const string k_CameraMediaDirName = "CameraShot/"; // 外存拍照目录名
        public const string k_LogDirName = "Log/"; // 外存日志目录名
        public const string k_TempDirName = "Temp/"; // 外存临时目录名
        public const string k_NetTextureDirName = "NetTexture/"; // 外存网络贴图目录名

        // 文件名定义
        public const string k_MainAssetPackFileName = "split_MainAssetPack.apk"; // Android 分包主资源包文件名
        public const string k_AccessLogFileName = "FileLog.txt"; // 资源访问日志文件名

        // 日志定义
        public const int k_AccessLogFlushCount = 20; // 资源访问日志缓存达到该条数时刷新
        public const string k_IgnoreLogKeyword = "The referenced script on this Behaviour"; // 命中后丢弃的日志关键字
        public const long k_MaxLogFileSize = 20 * 1024 * 1024; // 资源访问日志单文件上限 20MB

        // 平台阈值定义
        public const int k_WindowsShortPathLength = 240; // Windows 短路径长度上限，超过后转扩展路径
    }
}

