using ST.Core.Logging;

namespace ST.Core
{
    /// <summary>文件模块状态定义部分，集中存放清单、日志与配置开关。</summary>
    public static partial class FileMgr
    {
        /// <summary>外存清单判定，为 null 时按目录扫描。</summary>
        static IFileResolvePolicy s_ResolvePolicy;

        /// <summary>资源访问日志实例，未启用时为 null。</summary>
        static LogFileWriter s_FileLogger;

        /// <summary>资源访问日志的锁对象。</summary>
        static readonly object s_FileLogLock = new object();

#if FORCE_RES_LOG
        /// <summary>是否记录资源访问日志，默认在强制资源日志宏下开启。</summary>
        public static bool isAccessLogEnabled = true;

#else
        /// <summary>是否记录资源访问日志。</summary>
        public static bool isAccessLogEnabled = false;

#endif
    }
}

