namespace ST.Core.Logging
{
    /// <summary>
    /// 日志写入器接口
    /// </summary>
    public interface ILogWriter
    {
        /// <summary>
        /// 写入格式化后的日志
        /// </summary>
        void Write(string formattedLog);

        /// <summary>
        /// 刷新缓存到文件
        /// </summary>
        void Flush();

        /// <summary>
        /// 关闭写入器
        /// </summary>
        void Close();
    }
}
