using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ST.Core
{
    /// <summary>
    /// 主线程任务委托类型（无参数无返回值），用于排队到 Unity 主线程执行。
    /// </summary>
    public delegate void MainThreadTaskFun();

    /// <summary>
    /// 主线程任务调度抽象：在子线程或异步回调中把逻辑封装为 <see cref="GameEvent"/> 入队，由主线程在 <see cref="DoUpdate"/> 中统一执行。
    /// </summary>
    public abstract class IMainThreadTask : IManager
    {
        /// <summary>当前全局主线程任务实现实例，由派生类构造时赋值。</summary>
        protected static IMainThreadTask s_Instance = null;

        /// <summary>全局主线程任务调度器单例引用。</summary>
        public static IMainThreadTask S
        {
            get { return s_Instance; }
        }

        /// <summary>
        /// 构造时将 <see cref="s_Instance"/> 设为当前实例，便于通过 <see cref="S"/> 访问。
        /// </summary>
        public IMainThreadTask()
        {
            s_Instance = this;
        }

        /// <summary>
        /// 将封装了主线程回调的 <see cref="GameEvent"/> 加入队列，在后续 <see cref="IManager.DoUpdate"/> 中执行。
        /// </summary>
        /// <param name="func">待在主线程调用的 Unity 事件（通常 <c>Invoke</c> 内为业务回调）</param>
        public abstract void AddTask(GameEvent func);
    }
}
