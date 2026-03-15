using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace ST.Core
{
    /// <summary>
    /// 
    /// </summary>
    public class GameEvent : UnityEvent
    {
        public new void AddListener(UnityAction call)
        {
            base.AddListener(call);
        }

        public new void RemoveListener(UnityAction call)
        {
            base.RemoveListener(call);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T0"></typeparam>
    public class GameEvent<T0> : UnityEvent<T0>
    {
        public new void AddListener(UnityAction<T0> call)
        {
            base.AddListener(call);
        }

        public new void RemoveListener(UnityAction<T0> call)
        {
            base.RemoveListener(call);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T0"></typeparam>
    /// <typeparam name="T1"></typeparam>
    public class GameEvent<T0, T1> : UnityEvent<T0, T1>
    {
        public new void AddListener(UnityAction<T0, T1> call)
        {
            base.AddListener(call);
        }

        public new void RemoveListener(UnityAction<T0, T1> call)
        {
            base.RemoveListener(call);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T0"></typeparam>
    /// <typeparam name="T1"></typeparam>
    /// <typeparam name="T2"></typeparam>
    public class GameEvent<T0, T1, T2> : UnityEvent<T0, T1, T2>
    {
        public new void AddListener(UnityAction<T0, T1, T2> call)
        {
            base.AddListener(call);
        }

        public new void RemoveListener(UnityAction<T0, T1, T2> call)
        {
            base.RemoveListener(call);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T0"></typeparam>
    /// <typeparam name="T1"></typeparam>
    /// <typeparam name="T2"></typeparam>
    /// <typeparam name="T3"></typeparam>
    public class GameEvent<T0, T1, T2, T3> : UnityEvent<T0, T1, T2, T3>
    {
        public new void AddListener(UnityAction<T0, T1, T2, T3> call)
        {
            base.AddListener(call);
        }

        public new void RemoveListener(UnityAction<T0, T1, T2, T3> call)
        {
            base.RemoveListener(call);
        }
    }
}

