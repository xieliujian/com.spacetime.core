using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using ST.Core.Logging;
using Logger = ST.Core.Logging.Logger;

namespace ST.Core
{
    /// <summary>
    /// 默认主线程任务实现：在 <see cref="DoUpdate"/> 中依次出队并 <see cref="GameEvent.Invoke"/>，供网络回调等切回主线程使用。
    /// </summary>
    public class MainThreadTask : IMainThreadTask
    {
        /// <summary>待执行的主线程事件队列。</summary>
        Queue<GameEvent> m_TaskFunList = new Queue<GameEvent>(100);

        /// <summary>当前无关闭清理逻辑。</summary>
        public override void DoClose()
        {
            
        }

        /// <summary>当前无初始化逻辑。</summary>
        public override void DoInit()
        {
            
        }

        /// <summary>在锁内清空队列并执行每个 <see cref="GameEvent"/>，捕获异常并记录。</summary>
        public override void DoUpdate()
        {
            lock(m_TaskFunList)
            {
                while(m_TaskFunList.Count > 0)
                {
                    var fun = m_TaskFunList.Dequeue();

                    try
                    {
                        fun.Invoke();
                    }
                    catch(Exception e)
                    {
                        Logger.LogError(e.ToString());
                    }
                }
            }
        }

        /// <summary>当前无 LateUpdate 逻辑。</summary>
        public override void DoLateUpdate()
        {
            
        }

        /// <summary>
        /// 将非空事件入队；与 <see cref="DoUpdate"/> 配对在主线程消费。
        /// </summary>
        /// <param name="func">封装了要在主线程执行的回调的 UnityEvent</param>
        public override void AddTask(GameEvent func)
        {
            if (func == null)
                return;

            lock(m_TaskFunList)
            {
                m_TaskFunList.Enqueue(func);
            }
        }
    }
}
