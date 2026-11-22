using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace ST.Core
{
    /// <summary>
    /// 无参数 Unity 持久化事件封装，显式 <c>new</c> 隐藏基类监听器方法以保持类型一致。
    /// </summary>
    public class GameEvent : UnityEvent
    {
        /// <summary>注册无参回调。</summary>
        public new void AddListener(UnityAction call)
        {
            base.AddListener(call);
        }

        /// <summary>移除无参回调。</summary>
        public new void RemoveListener(UnityAction call)
        {
            base.RemoveListener(call);
        }
    }

    /// <summary>
    /// 单参数 <see cref="UnityEvent{T0}"/> 封装，用于主线程任务等带一个载荷的事件。
    /// </summary>
    /// <typeparam name="T0">第一个事件参数类型</typeparam>
    public class GameEvent<T0> : UnityEvent<T0>
    {
        /// <summary>注册单参回调。</summary>
        public new void AddListener(UnityAction<T0> call)
        {
            base.AddListener(call);
        }

        /// <summary>移除单参回调。</summary>
        public new void RemoveListener(UnityAction<T0> call)
        {
            base.RemoveListener(call);
        }
    }

    /// <summary>
    /// 双参数持久化事件封装。
    /// </summary>
    /// <typeparam name="T0">第一个参数类型</typeparam>
    /// <typeparam name="T1">第二个参数类型</typeparam>
    public class GameEvent<T0, T1> : UnityEvent<T0, T1>
    {
        /// <summary>注册双参回调。</summary>
        public new void AddListener(UnityAction<T0, T1> call)
        {
            base.AddListener(call);
        }

        /// <summary>移除双参回调。</summary>
        public new void RemoveListener(UnityAction<T0, T1> call)
        {
            base.RemoveListener(call);
        }
    }

    /// <summary>
    /// 三参数持久化事件封装。
    /// </summary>
    /// <typeparam name="T0">第一个参数类型</typeparam>
    /// <typeparam name="T1">第二个参数类型</typeparam>
    /// <typeparam name="T2">第三个参数类型</typeparam>
    public class GameEvent<T0, T1, T2> : UnityEvent<T0, T1, T2>
    {
        /// <summary>注册三参回调。</summary>
        public new void AddListener(UnityAction<T0, T1, T2> call)
        {
            base.AddListener(call);
        }

        /// <summary>移除三参回调。</summary>
        public new void RemoveListener(UnityAction<T0, T1, T2> call)
        {
            base.RemoveListener(call);
        }
    }

    /// <summary>
    /// 四参数持久化事件封装。
    /// </summary>
    /// <typeparam name="T0">第一个参数类型</typeparam>
    /// <typeparam name="T1">第二个参数类型</typeparam>
    /// <typeparam name="T2">第三个参数类型</typeparam>
    /// <typeparam name="T3">第四个参数类型</typeparam>
    public class GameEvent<T0, T1, T2, T3> : UnityEvent<T0, T1, T2, T3>
    {
        /// <summary>注册四参回调。</summary>
        public new void AddListener(UnityAction<T0, T1, T2, T3> call)
        {
            base.AddListener(call);
        }

        /// <summary>移除四参回调。</summary>
        public new void RemoveListener(UnityAction<T0, T1, T2, T3> call)
        {
            base.RemoveListener(call);
        }
    }
}

