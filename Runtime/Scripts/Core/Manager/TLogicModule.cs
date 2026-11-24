namespace ST.Core
{
    /// <summary>
    /// 带静态单例字段的模块基类，派生类型可直接通过 <see cref="S"/> 访问自身实例。
    /// </summary>
    /// <typeparam name="T">派生模块类型，必须提供无参构造函数。</typeparam>
    public abstract class TLogicModule<T> : ILogicModule where T : new()
    {
        /// <summary>派生模块的全局唯一实例，首次访问该类型时创建。</summary>
        public static T S = new T();
    }
}
