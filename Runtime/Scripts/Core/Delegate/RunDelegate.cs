namespace ST.Core
{
    /// <summary>无参数、无返回值的回调委托，用于替代裸 Action。</summary>
    public delegate void Run();

    /// <summary>单参数、无返回值的回调委托，用于替代裸 Action。</summary>
    /// <typeparam name="T">回调参数类型。</typeparam>
    /// <param name="p">回调参数。</param>
    public delegate void Run<T>(T p);

    /// <summary>双参数、无返回值的回调委托，用于替代裸 Action。</summary>
    /// <typeparam name="T1">第一个回调参数类型。</typeparam>
    /// <typeparam name="T2">第二个回调参数类型。</typeparam>
    /// <param name="p1">第一个回调参数。</param>
    /// <param name="p2">第二个回调参数。</param>
    public delegate void Run<T1, T2>(T1 p1, T2 p2);

    /// <summary>三参数、无返回值的回调委托，用于替代裸 Action。</summary>
    /// <typeparam name="T1">第一个回调参数类型。</typeparam>
    /// <typeparam name="T2">第二个回调参数类型。</typeparam>
    /// <typeparam name="T3">第三个回调参数类型。</typeparam>
    /// <param name="p1">第一个回调参数。</param>
    /// <param name="p2">第二个回调参数。</param>
    /// <param name="p3">第三个回调参数。</param>
    public delegate void Run<T1, T2, T3>(T1 p1, T2 p2, T3 p3);

    /// <summary>四参数、无返回值的回调委托，用于替代裸 Action。</summary>
    /// <typeparam name="T1">第一个回调参数类型。</typeparam>
    /// <typeparam name="T2">第二个回调参数类型。</typeparam>
    /// <typeparam name="T3">第三个回调参数类型。</typeparam>
    /// <typeparam name="T4">第四个回调参数类型。</typeparam>
    /// <param name="p1">第一个回调参数。</param>
    /// <param name="p2">第二个回调参数。</param>
    /// <param name="p3">第三个回调参数。</param>
    /// <param name="p4">第四个回调参数。</param>
    public delegate void Run<T1, T2, T3, T4>(T1 p1, T2 p2, T3 p3, T4 p4);

    /// <summary>五参数、无返回值的回调委托，用于替代裸 Action。</summary>
    /// <typeparam name="T1">第一个回调参数类型。</typeparam>
    /// <typeparam name="T2">第二个回调参数类型。</typeparam>
    /// <typeparam name="T3">第三个回调参数类型。</typeparam>
    /// <typeparam name="T4">第四个回调参数类型。</typeparam>
    /// <typeparam name="T5">第五个回调参数类型。</typeparam>
    /// <param name="p1">第一个回调参数。</param>
    /// <param name="p2">第二个回调参数。</param>
    /// <param name="p3">第三个回调参数。</param>
    /// <param name="p4">第四个回调参数。</param>
    /// <param name="p5">第五个回调参数。</param>
    public delegate void Run<T1, T2, T3, T4, T5>(T1 p1, T2 p2, T3 p3, T4 p4, T5 p5);

    /// <summary>无参数、带返回值的回调委托，用于替代裸 Func。</summary>
    /// <typeparam name="TResult">回调返回值类型。</typeparam>
    public delegate TResult RFunc<out TResult>();

    /// <summary>单参数、带返回值的回调委托，用于替代裸 Func。</summary>
    /// <typeparam name="T">回调参数类型。</typeparam>
    /// <typeparam name="TResult">回调返回值类型。</typeparam>
    /// <param name="p">回调参数。</param>
    public delegate TResult RFunc<in T, out TResult>(T p);

    /// <summary>双参数、带返回值的回调委托，用于替代裸 Func。</summary>
    /// <typeparam name="T1">第一个回调参数类型。</typeparam>
    /// <typeparam name="T2">第二个回调参数类型。</typeparam>
    /// <typeparam name="TResult">回调返回值类型。</typeparam>
    /// <param name="p1">第一个回调参数。</param>
    /// <param name="p2">第二个回调参数。</param>
    public delegate TResult RFunc<in T1, in T2, out TResult>(T1 p1, T2 p2);

    /// <summary>三参数、带返回值的回调委托，用于替代裸 Func。</summary>
    /// <typeparam name="T1">第一个回调参数类型。</typeparam>
    /// <typeparam name="T2">第二个回调参数类型。</typeparam>
    /// <typeparam name="T3">第三个回调参数类型。</typeparam>
    /// <typeparam name="TResult">回调返回值类型。</typeparam>
    /// <param name="p1">第一个回调参数。</param>
    /// <param name="p2">第二个回调参数。</param>
    /// <param name="p3">第三个回调参数。</param>
    public delegate TResult RFunc<in T1, in T2, in T3, out TResult>(T1 p1, T2 p2, T3 p3);

    /// <summary>四参数、带返回值的回调委托，用于替代裸 Func。</summary>
    /// <typeparam name="T1">第一个回调参数类型。</typeparam>
    /// <typeparam name="T2">第二个回调参数类型。</typeparam>
    /// <typeparam name="T3">第三个回调参数类型。</typeparam>
    /// <typeparam name="T4">第四个回调参数类型。</typeparam>
    /// <typeparam name="TResult">回调返回值类型。</typeparam>
    /// <param name="p1">第一个回调参数。</param>
    /// <param name="p2">第二个回调参数。</param>
    /// <param name="p3">第三个回调参数。</param>
    /// <param name="p4">第四个回调参数。</param>
    public delegate TResult RFunc<in T1, in T2, in T3, in T4, out TResult>(T1 p1, T2 p2, T3 p3, T4 p4);

    /// <summary>五参数、带返回值的回调委托，用于替代裸 Func。</summary>
    /// <typeparam name="T1">第一个回调参数类型。</typeparam>
    /// <typeparam name="T2">第二个回调参数类型。</typeparam>
    /// <typeparam name="T3">第三个回调参数类型。</typeparam>
    /// <typeparam name="T4">第四个回调参数类型。</typeparam>
    /// <typeparam name="T5">第五个回调参数类型。</typeparam>
    /// <typeparam name="TResult">回调返回值类型。</typeparam>
    /// <param name="p1">第一个回调参数。</param>
    /// <param name="p2">第二个回调参数。</param>
    /// <param name="p3">第三个回调参数。</param>
    /// <param name="p4">第四个回调参数。</param>
    /// <param name="p5">第五个回调参数。</param>
    public delegate TResult RFunc<in T1, in T2, in T3, in T4, in T5, out TResult>(T1 p1, T2 p2, T3 p3, T4 p4, T5 p5);
}