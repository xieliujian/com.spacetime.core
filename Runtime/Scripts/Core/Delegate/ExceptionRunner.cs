using System;
using ST.Core.Logging;

namespace ST.Core
{
    /// <summary>无返回值回调的统一执行入口，隔离单个回调抛出的异常。</summary>
    public sealed partial class ERunner
    {
        /// <summary>执行无参数回调。</summary>
        /// <param name="run">待执行的回调，可为 null。</param>
        /// <returns>回调为空或抛出异常时返回 false，正常执行结束返回 true。</returns>
        public static bool Run(Run run)
        {
            if (run == null)
            {
                return false;
            }

            try
            {
                run();
            }
            catch (Exception exception)
            {
                Logger.LogError(exception.ToString());
                return false;
            }

            return true;
        }

        /// <summary>执行单参数回调。</summary>
        /// <typeparam name="T">回调参数类型。</typeparam>
        /// <param name="run">待执行的回调，可为 null。</param>
        /// <param name="p">回调参数。</param>
        /// <returns>回调为空或抛出异常时返回 false，正常执行结束返回 true。</returns>
        public static bool Run<T>(Run<T> run, T p)
        {
            if (run == null)
            {
                return false;
            }

            try
            {
                run(p);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception.ToString());
                return false;
            }

            return true;
        }

        /// <summary>执行双参数回调。</summary>
        /// <typeparam name="T1">第一个回调参数类型。</typeparam>
        /// <typeparam name="T2">第二个回调参数类型。</typeparam>
        /// <param name="run">待执行的回调，可为 null。</param>
        /// <param name="p1">第一个回调参数。</param>
        /// <param name="p2">第二个回调参数。</param>
        /// <returns>回调为空或抛出异常时返回 false，正常执行结束返回 true。</returns>
        public static bool Run<T1, T2>(Run<T1, T2> run, T1 p1, T2 p2)
        {
            if (run == null)
            {
                return false;
            }

            try
            {
                run(p1, p2);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception.ToString());
                return false;
            }

            return true;
        }

        /// <summary>执行三参数回调。</summary>
        /// <typeparam name="T1">第一个回调参数类型。</typeparam>
        /// <typeparam name="T2">第二个回调参数类型。</typeparam>
        /// <typeparam name="T3">第三个回调参数类型。</typeparam>
        /// <param name="run">待执行的回调，可为 null。</param>
        /// <param name="p1">第一个回调参数。</param>
        /// <param name="p2">第二个回调参数。</param>
        /// <param name="p3">第三个回调参数。</param>
        /// <returns>回调为空或抛出异常时返回 false，正常执行结束返回 true。</returns>
        public static bool Run<T1, T2, T3>(Run<T1, T2, T3> run, T1 p1, T2 p2, T3 p3)
        {
            if (run == null)
            {
                return false;
            }

            try
            {
                run(p1, p2, p3);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception.ToString());
                return false;
            }

            return true;
        }

        /// <summary>执行四参数回调。</summary>
        /// <typeparam name="T1">第一个回调参数类型。</typeparam>
        /// <typeparam name="T2">第二个回调参数类型。</typeparam>
        /// <typeparam name="T3">第三个回调参数类型。</typeparam>
        /// <typeparam name="T4">第四个回调参数类型。</typeparam>
        /// <param name="run">待执行的回调，可为 null。</param>
        /// <param name="p1">第一个回调参数。</param>
        /// <param name="p2">第二个回调参数。</param>
        /// <param name="p3">第三个回调参数。</param>
        /// <param name="p4">第四个回调参数。</param>
        /// <returns>回调为空或抛出异常时返回 false，正常执行结束返回 true。</returns>
        public static bool Run<T1, T2, T3, T4>(Run<T1, T2, T3, T4> run, T1 p1, T2 p2, T3 p3, T4 p4)
        {
            if (run == null)
            {
                return false;
            }

            try
            {
                run(p1, p2, p3, p4);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception.ToString());
                return false;
            }

            return true;
        }

        /// <summary>执行五参数回调。</summary>
        /// <typeparam name="T1">第一个回调参数类型。</typeparam>
        /// <typeparam name="T2">第二个回调参数类型。</typeparam>
        /// <typeparam name="T3">第三个回调参数类型。</typeparam>
        /// <typeparam name="T4">第四个回调参数类型。</typeparam>
        /// <typeparam name="T5">第五个回调参数类型。</typeparam>
        /// <param name="run">待执行的回调，可为 null。</param>
        /// <param name="p1">第一个回调参数。</param>
        /// <param name="p2">第二个回调参数。</param>
        /// <param name="p3">第三个回调参数。</param>
        /// <param name="p4">第四个回调参数。</param>
        /// <param name="p5">第五个回调参数。</param>
        /// <returns>回调为空或抛出异常时返回 false，正常执行结束返回 true。</returns>
        public static bool Run<T1, T2, T3, T4, T5>(Run<T1, T2, T3, T4, T5> run, T1 p1, T2 p2, T3 p3, T4 p4, T5 p5)
        {
            if (run == null)
            {
                return false;
            }

            try
            {
                run(p1, p2, p3, p4, p5);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception.ToString());
                return false;
            }

            return true;
        }

        /// <summary>禁止外部创建执行器实例，仅提供静态调用入口。</summary>
        ERunner() { }
    }

    /// <summary>带返回值回调的统一执行入口，隔离单个回调抛出的异常。</summary>
    public sealed partial class EFuncer
    {
        /// <summary>执行无参数回调。</summary>
        /// <typeparam name="TResult">回调返回值类型。</typeparam>
        /// <param name="func">待执行的回调，可为 null。</param>
        /// <returns>回调的返回值；回调为空或抛出异常时返回默认值。</returns>
        public static TResult Run<TResult>(RFunc<TResult> func)
        {
            if (func == null)
            {
                return default(TResult);
            }

            try
            {
                return func();
            }
            catch (Exception exception)
            {
                Logger.LogError(exception.ToString());
                return default(TResult);
            }
        }

        /// <summary>执行单参数回调。</summary>
        /// <typeparam name="T">回调参数类型。</typeparam>
        /// <typeparam name="TResult">回调返回值类型。</typeparam>
        /// <param name="func">待执行的回调，可为 null。</param>
        /// <param name="t">回调参数。</param>
        /// <returns>回调的返回值；回调为空或抛出异常时返回默认值。</returns>
        public static TResult Run<T, TResult>(RFunc<T, TResult> func, T t)
        {
            if (func == null)
            {
                return default(TResult);
            }

            try
            {
                return func(t);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception.ToString());
                return default(TResult);
            }
        }

        /// <summary>执行双参数回调。</summary>
        /// <typeparam name="T1">第一个回调参数类型。</typeparam>
        /// <typeparam name="T2">第二个回调参数类型。</typeparam>
        /// <typeparam name="TResult">回调返回值类型。</typeparam>
        /// <param name="func">待执行的回调，可为 null。</param>
        /// <param name="t1">第一个回调参数。</param>
        /// <param name="t2">第二个回调参数。</param>
        /// <returns>回调的返回值；回调为空或抛出异常时返回默认值。</returns>
        public static TResult Run<T1, T2, TResult>(RFunc<T1, T2, TResult> func, T1 t1, T2 t2)
        {
            if (func == null)
            {
                return default(TResult);
            }

            try
            {
                return func(t1, t2);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception.ToString());
                return default(TResult);
            }
        }

        /// <summary>执行三参数回调。</summary>
        /// <typeparam name="T1">第一个回调参数类型。</typeparam>
        /// <typeparam name="T2">第二个回调参数类型。</typeparam>
        /// <typeparam name="T3">第三个回调参数类型。</typeparam>
        /// <typeparam name="TResult">回调返回值类型。</typeparam>
        /// <param name="func">待执行的回调，可为 null。</param>
        /// <param name="t1">第一个回调参数。</param>
        /// <param name="t2">第二个回调参数。</param>
        /// <param name="t3">第三个回调参数。</param>
        /// <returns>回调的返回值；回调为空或抛出异常时返回默认值。</returns>
        public static TResult Run<T1, T2, T3, TResult>(RFunc<T1, T2, T3, TResult> func, T1 t1, T2 t2, T3 t3)
        {
            if (func == null)
            {
                return default(TResult);
            }

            try
            {
                return func(t1, t2, t3);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception.ToString());
                return default(TResult);
            }
        }

        /// <summary>执行四参数回调。</summary>
        /// <typeparam name="T1">第一个回调参数类型。</typeparam>
        /// <typeparam name="T2">第二个回调参数类型。</typeparam>
        /// <typeparam name="T3">第三个回调参数类型。</typeparam>
        /// <typeparam name="T4">第四个回调参数类型。</typeparam>
        /// <typeparam name="TResult">回调返回值类型。</typeparam>
        /// <param name="func">待执行的回调，可为 null。</param>
        /// <param name="t1">第一个回调参数。</param>
        /// <param name="t2">第二个回调参数。</param>
        /// <param name="t3">第三个回调参数。</param>
        /// <param name="t4">第四个回调参数。</param>
        /// <returns>回调的返回值；回调为空或抛出异常时返回默认值。</returns>
        public static TResult Run<T1, T2, T3, T4, TResult>(
            RFunc<T1, T2, T3, T4, TResult> func, T1 t1, T2 t2, T3 t3, T4 t4)
        {
            if (func == null)
            {
                return default(TResult);
            }

            try
            {
                return func(t1, t2, t3, t4);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception.ToString());
                return default(TResult);
            }
        }

        /// <summary>执行五参数回调。</summary>
        /// <typeparam name="T1">第一个回调参数类型。</typeparam>
        /// <typeparam name="T2">第二个回调参数类型。</typeparam>
        /// <typeparam name="T3">第三个回调参数类型。</typeparam>
        /// <typeparam name="T4">第四个回调参数类型。</typeparam>
        /// <typeparam name="T5">第五个回调参数类型。</typeparam>
        /// <typeparam name="TResult">回调返回值类型。</typeparam>
        /// <param name="func">待执行的回调，可为 null。</param>
        /// <param name="t1">第一个回调参数。</param>
        /// <param name="t2">第二个回调参数。</param>
        /// <param name="t3">第三个回调参数。</param>
        /// <param name="t4">第四个回调参数。</param>
        /// <param name="t5">第五个回调参数。</param>
        /// <returns>回调的返回值；回调为空或抛出异常时返回默认值。</returns>
        public static TResult Run<T1, T2, T3, T4, T5, TResult>(
            RFunc<T1, T2, T3, T4, T5, TResult> func, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5)
        {
            if (func == null)
            {
                return default(TResult);
            }

            try
            {
                return func(t1, t2, t3, t4, t5);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception.ToString());
                return default(TResult);
            }
        }

        /// <summary>禁止外部创建执行器实例，仅提供静态调用入口。</summary>
        EFuncer() { }
    }
}