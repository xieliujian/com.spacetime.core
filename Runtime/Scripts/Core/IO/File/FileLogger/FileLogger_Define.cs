using System.Collections.Generic;
using System.IO;

namespace ST.Core
{
    /// <summary>日志状态定义部分，集中存放写入参数与文件句柄。</summary>
    public partial class FileLogger
    {
        /// <summary>命中该关键字的日志会被丢弃。</summary>
        const string k_IgnoreLogKeyword = "The referenced script on this Behaviour";

        /// <summary>旧文件超过该大小时直接删除，避免日志无限增长。</summary>
        const long k_MaxFileSize = 20 * 1024 * 1024;

        /// <summary>尚未落盘的日志，按插入顺序批量写出。</summary>
        List<string> m_CacheLogList = new List<string>();

        /// <summary>日志文件完整路径。</summary>
        readonly string m_FilePath;

        /// <summary>底层文件流，打开失败时为 null。</summary>
        FileStream m_FileStream;

        /// <summary>文件流上的文本写入器，打开失败时为 null，此时所有写入被丢弃。</summary>
        StreamWriter m_StreamWriter;

        /// <summary>缓存达到该条数时自动刷新。</summary>
        int m_MaxFlushCount = 20;
    }
}

