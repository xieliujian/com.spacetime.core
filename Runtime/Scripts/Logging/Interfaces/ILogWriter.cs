namespace ST.Core.Logging
{
    /// <summary>
    /// 日志写入器接口，定义日志的持久化能力
    /// </summary>
    public interface ILogWriter
    {
        /// <summary>
        /// 写入一条格式化后的日志
        /// </summary>
        /// <param name="formattedLog">已完成格式化的日志字符串</param>
        void Write(string formattedLog);

        /// <summary>
        /// 将缓存中的日志立即刷新到持久化存储
        /// </summary>
        void Flush();

        /// <summary>
        /// 刷新缓存并关闭写入器，释放文件句柄等资源
        /// </summary>
        void Close();
    }
}
