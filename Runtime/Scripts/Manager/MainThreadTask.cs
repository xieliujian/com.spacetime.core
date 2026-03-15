using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace ST.Core
{
    public class MainThreadTask : IMainThreadTask
    {
        /// <summary>
        /// 
        /// </summary>
        Queue<GameEvent> m_TaskFunList = new Queue<GameEvent>(100);

        public override void DoClose()
        {
            
        }

        public override void DoInit()
        {
            
        }

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
                        Debugger.Debugger.LogError(e.ToString());
                    }
                }
            }
        }

        public override void DoLateUpdate()
        {
            
        }

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
