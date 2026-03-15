using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ST.Core
{
    /// <summary>
    /// 
    /// </summary>
    public delegate void MainThreadTaskFun();

    /// <summary>
    /// 
    /// </summary>
    public abstract class IMainThreadTask : IManager
    {
        public IMainThreadTask()
        {
            s_Instance = this;
        }

        protected static IMainThreadTask s_Instance = null;

        public static IMainThreadTask S
        {
            get { return s_Instance; }
        }

        public abstract void AddTask(GameEvent func);
    }
}
