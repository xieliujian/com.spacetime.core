using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace ST.Core
{
    public class MainThreadTask : IMainThreadTask
    {
        #region 变量

        private Queue<GameEvent> m_taskfunlist = new Queue<GameEvent>(100);

        #endregion

        #region 继承函数

        public override void DoClose()
        {
            
        }

        public override void DoInit()
        {
            
        }

        public override void DoUpdate()
        {
            lock(m_taskfunlist)
            {
                while(m_taskfunlist.Count > 0)
                {
                    var fun = m_taskfunlist.Dequeue();

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

            lock(m_taskfunlist)
            {
                m_taskfunlist.Enqueue(func);
            }
        }

        #endregion
    }
}
