using System.Collections.Generic;

namespace ST.Core
{
    /// <summary>无参数回调事件，支持重复注册去重与异常隔离调用。</summary>
    public class RunEvent
    {
        /// <summary>已注册的回调列表；首次注册时才创建，未注册时保持 null。</summary>
        List<Run> m_CallBackList;

        /// <summary>注册回调，重复注册同一回调会被忽略。</summary>
        /// <param name="run">要注册的回调，为 null 时不去重判断但会加入列表。</param>
        public void AddListener(Run run)
        {
            if (m_CallBackList == null)
            {
                m_CallBackList = new List<Run>();
            }

            if (!m_CallBackList.Contains(run))
            {
                m_CallBackList.Add(run);
            }
        }

        /// <summary>移除已注册的回调，未注册时忽略。</summary>
        /// <param name="run">要移除的回调。</param>
        public void RemoveListener(Run run)
        {
            if (m_CallBackList == null)
            {
                return;
            }

            m_CallBackList.Remove(run);
        }

        /// <summary>通过加号运算符注册回调。</summary>
        /// <param name="e">目标事件，可为 null。</param>
        /// <param name="run">要注册的回调。</param>
        /// <returns>传入的事件实例；事件为 null 时返回 null。</returns>
        public static RunEvent operator +(RunEvent e, Run run)
        {
            if (e == null)
            {
                return null;
            }

            e.AddListener(run);
            return e;
        }

        /// <summary>通过减号运算符移除回调。</summary>
        /// <param name="e">目标事件，可为 null。</param>
        /// <param name="run">要移除的回调。</param>
        /// <returns>传入的事件实例；事件为 null 时返回 null。</returns>
        public static RunEvent operator -(RunEvent e, Run run)
        {
            if (e == null)
            {
                return null;
            }

            e.RemoveListener(run);
            return e;
        }

        /// <summary>依次触发所有回调，单个回调异常不会中断后续回调。</summary>
        public void Invoke()
        {
            if (m_CallBackList == null)
            {
                return;
            }

            for (int i = 0; i < m_CallBackList.Count; ++i)
            {
                ERunner.Run(m_CallBackList[i]);
            }
        }

        /// <summary>清空所有已注册的回调，保留列表容量。</summary>
        public void RemoveAllListeners()
        {
            if (m_CallBackList != null)
            {
                m_CallBackList.Clear();
            }
        }
    }

    /// <summary>单参数回调事件，支持重复注册去重与异常隔离调用。</summary>
    /// <typeparam name="T">回调参数类型。</typeparam>
    public class RunEvent<T>
    {
        /// <summary>已注册的回调列表；首次注册时才创建，未注册时保持 null。</summary>
        List<Run<T>> m_CallBackList;

        /// <summary>注册回调，重复注册同一回调会被忽略。</summary>
        /// <param name="run">要注册的回调。</param>
        public void AddListener(Run<T> run)
        {
            if (m_CallBackList == null)
            {
                m_CallBackList = new List<Run<T>>();
            }

            if (!m_CallBackList.Contains(run))
            {
                m_CallBackList.Add(run);
            }
        }

        /// <summary>移除已注册的回调，未注册时忽略。</summary>
        /// <param name="run">要移除的回调。</param>
        public void RemoveListener(Run<T> run)
        {
            if (m_CallBackList == null)
            {
                return;
            }

            m_CallBackList.Remove(run);
        }

        /// <summary>通过加号运算符注册回调。</summary>
        /// <param name="e">目标事件，可为 null。</param>
        /// <param name="run">要注册的回调。</param>
        /// <returns>传入的事件实例；事件为 null 时返回 null。</returns>
        public static RunEvent<T> operator +(RunEvent<T> e, Run<T> run)
        {
            if (e == null)
            {
                return null;
            }

            e.AddListener(run);
            return e;
        }

        /// <summary>通过减号运算符移除回调。</summary>
        /// <param name="e">目标事件，可为 null。</param>
        /// <param name="run">要移除的回调。</param>
        /// <returns>传入的事件实例；事件为 null 时返回 null。</returns>
        public static RunEvent<T> operator -(RunEvent<T> e, Run<T> run)
        {
            if (e == null)
            {
                return null;
            }

            e.RemoveListener(run);
            return e;
        }

        /// <summary>依次触发所有回调，单个回调异常不会中断后续回调。</summary>
        /// <param name="t">回调参数。</param>
        public void Invoke(T t)
        {
            if (m_CallBackList == null)
            {
                return;
            }

            for (int i = 0; i < m_CallBackList.Count; ++i)
            {
                ERunner.Run(m_CallBackList[i], t);
            }
        }

        /// <summary>清空所有已注册的回调，保留列表容量。</summary>
        public void RemoveAllListeners()
        {
            if (m_CallBackList != null)
            {
                m_CallBackList.Clear();
            }
        }
    }

    /// <summary>双参数回调事件，支持重复注册去重与异常隔离调用。</summary>
    /// <typeparam name="T0">第一个回调参数类型。</typeparam>
    /// <typeparam name="T1">第二个回调参数类型。</typeparam>
    public class RunEvent<T0, T1>
    {
        /// <summary>已注册的回调列表；首次注册时才创建，未注册时保持 null。</summary>
        List<Run<T0, T1>> m_CallBackList;

        /// <summary>注册回调，重复注册同一回调会被忽略。</summary>
        /// <param name="run">要注册的回调。</param>
        public void AddListener(Run<T0, T1> run)
        {
            if (m_CallBackList == null)
            {
                m_CallBackList = new List<Run<T0, T1>>();
            }

            if (!m_CallBackList.Contains(run))
            {
                m_CallBackList.Add(run);
            }
        }

        /// <summary>移除已注册的回调，未注册时忽略。</summary>
        /// <param name="run">要移除的回调。</param>
        public void RemoveListener(Run<T0, T1> run)
        {
            if (m_CallBackList == null)
            {
                return;
            }

            m_CallBackList.Remove(run);
        }

        /// <summary>通过加号运算符注册回调。</summary>
        /// <param name="e">目标事件，可为 null。</param>
        /// <param name="run">要注册的回调。</param>
        /// <returns>传入的事件实例；事件为 null 时返回 null。</returns>
        public static RunEvent<T0, T1> operator +(RunEvent<T0, T1> e, Run<T0, T1> run)
        {
            if (e == null)
            {
                return null;
            }

            e.AddListener(run);
            return e;
        }

        /// <summary>通过减号运算符移除回调。</summary>
        /// <param name="e">目标事件，可为 null。</param>
        /// <param name="run">要移除的回调。</param>
        /// <returns>传入的事件实例；事件为 null 时返回 null。</returns>
        public static RunEvent<T0, T1> operator -(RunEvent<T0, T1> e, Run<T0, T1> run)
        {
            if (e == null)
            {
                return null;
            }

            e.RemoveListener(run);
            return e;
        }

        /// <summary>依次触发所有回调，单个回调异常不会中断后续回调。</summary>
        /// <param name="t0">第一个回调参数。</param>
        /// <param name="t1">第二个回调参数。</param>
        public void Invoke(T0 t0, T1 t1)
        {
            if (m_CallBackList == null)
            {
                return;
            }

            for (int i = 0; i < m_CallBackList.Count; ++i)
            {
                ERunner.Run(m_CallBackList[i], t0, t1);
            }
        }

        /// <summary>清空所有已注册的回调，保留列表容量。</summary>
        public void RemoveAllListeners()
        {
            if (m_CallBackList != null)
            {
                m_CallBackList.Clear();
            }
        }
    }

    /// <summary>三参数回调事件，支持重复注册去重与异常隔离调用。</summary>
    /// <typeparam name="T0">第一个回调参数类型。</typeparam>
    /// <typeparam name="T1">第二个回调参数类型。</typeparam>
    /// <typeparam name="T2">第三个回调参数类型。</typeparam>
    public class RunEvent<T0, T1, T2>
    {
        /// <summary>已注册的回调列表；首次注册时才创建，未注册时保持 null。</summary>
        List<Run<T0, T1, T2>> m_CallBackList;

        /// <summary>注册回调，重复注册同一回调会被忽略。</summary>
        /// <param name="run">要注册的回调。</param>
        public void AddListener(Run<T0, T1, T2> run)
        {
            if (m_CallBackList == null)
            {
                m_CallBackList = new List<Run<T0, T1, T2>>();
            }

            if (!m_CallBackList.Contains(run))
            {
                m_CallBackList.Add(run);
            }
        }

        /// <summary>移除已注册的回调，未注册时忽略。</summary>
        /// <param name="run">要移除的回调。</param>
        public void RemoveListener(Run<T0, T1, T2> run)
        {
            if (m_CallBackList == null)
            {
                return;
            }

            m_CallBackList.Remove(run);
        }

        /// <summary>通过加号运算符注册回调。</summary>
        /// <param name="e">目标事件，可为 null。</param>
        /// <param name="run">要注册的回调。</param>
        /// <returns>传入的事件实例；事件为 null 时返回 null。</returns>
        public static RunEvent<T0, T1, T2> operator +(RunEvent<T0, T1, T2> e, Run<T0, T1, T2> run)
        {
            if (e == null)
            {
                return null;
            }

            e.AddListener(run);
            return e;
        }

        /// <summary>通过减号运算符移除回调。</summary>
        /// <param name="e">目标事件，可为 null。</param>
        /// <param name="run">要移除的回调。</param>
        /// <returns>传入的事件实例；事件为 null 时返回 null。</returns>
        public static RunEvent<T0, T1, T2> operator -(RunEvent<T0, T1, T2> e, Run<T0, T1, T2> run)
        {
            if (e == null)
            {
                return null;
            }

            e.RemoveListener(run);
            return e;
        }

        /// <summary>依次触发所有回调，单个回调异常不会中断后续回调。</summary>
        /// <param name="t0">第一个回调参数。</param>
        /// <param name="t1">第二个回调参数。</param>
        /// <param name="t2">第三个回调参数。</param>
        public void Invoke(T0 t0, T1 t1, T2 t2)
        {
            if (m_CallBackList == null)
            {
                return;
            }

            for (int i = 0; i < m_CallBackList.Count; ++i)
            {
                ERunner.Run(m_CallBackList[i], t0, t1, t2);
            }
        }

        /// <summary>清空所有已注册的回调，保留列表容量。</summary>
        public void RemoveAllListeners()
        {
            if (m_CallBackList != null)
            {
                m_CallBackList.Clear();
            }
        }
    }

    /// <summary>四参数回调事件，支持重复注册去重与异常隔离调用。</summary>
    /// <typeparam name="T0">第一个回调参数类型。</typeparam>
    /// <typeparam name="T1">第二个回调参数类型。</typeparam>
    /// <typeparam name="T2">第三个回调参数类型。</typeparam>
    /// <typeparam name="T3">第四个回调参数类型。</typeparam>
    public class RunEvent<T0, T1, T2, T3>
    {
        /// <summary>已注册的回调列表；首次注册时才创建，未注册时保持 null。</summary>
        List<Run<T0, T1, T2, T3>> m_CallBackList;

        /// <summary>注册回调，重复注册同一回调会被忽略。</summary>
        /// <param name="run">要注册的回调。</param>
        public void AddListener(Run<T0, T1, T2, T3> run)
        {
            if (m_CallBackList == null)
            {
                m_CallBackList = new List<Run<T0, T1, T2, T3>>();
            }

            if (!m_CallBackList.Contains(run))
            {
                m_CallBackList.Add(run);
            }
        }

        /// <summary>移除已注册的回调，未注册时忽略。</summary>
        /// <param name="run">要移除的回调。</param>
        public void RemoveListener(Run<T0, T1, T2, T3> run)
        {
            if (m_CallBackList == null)
            {
                return;
            }

            m_CallBackList.Remove(run);
        }

        /// <summary>通过加号运算符注册回调。</summary>
        /// <param name="e">目标事件，可为 null。</param>
        /// <param name="run">要注册的回调。</param>
        /// <returns>传入的事件实例；事件为 null 时返回 null。</returns>
        public static RunEvent<T0, T1, T2, T3> operator +(RunEvent<T0, T1, T2, T3> e, Run<T0, T1, T2, T3> run)
        {
            if (e == null)
            {
                return null;
            }

            e.AddListener(run);
            return e;
        }

        /// <summary>通过减号运算符移除回调。</summary>
        /// <param name="e">目标事件，可为 null。</param>
        /// <param name="run">要移除的回调。</param>
        /// <returns>传入的事件实例；事件为 null 时返回 null。</returns>
        public static RunEvent<T0, T1, T2, T3> operator -(RunEvent<T0, T1, T2, T3> e, Run<T0, T1, T2, T3> run)
        {
            if (e == null)
            {
                return null;
            }

            e.RemoveListener(run);
            return e;
        }

        /// <summary>依次触发所有回调，单个回调异常不会中断后续回调。</summary>
        /// <param name="t0">第一个回调参数。</param>
        /// <param name="t1">第二个回调参数。</param>
        /// <param name="t2">第三个回调参数。</param>
        /// <param name="t3">第四个回调参数。</param>
        public void Invoke(T0 t0, T1 t1, T2 t2, T3 t3)
        {
            if (m_CallBackList == null)
            {
                return;
            }

            for (int i = 0; i < m_CallBackList.Count; ++i)
            {
                ERunner.Run(m_CallBackList[i], t0, t1, t2, t3);
            }
        }

        /// <summary>清空所有已注册的回调，保留列表容量。</summary>
        public void RemoveAllListeners()
        {
            if (m_CallBackList != null)
            {
                m_CallBackList.Clear();
            }
        }
    }
}