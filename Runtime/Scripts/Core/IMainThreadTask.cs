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
            _instance = this;
        }

        protected static IMainThreadTask _instance = null;

        public static IMainThreadTask instance
        {
            get { return _instance; }
        }

        public abstract void AddTask(GameEvent func);
    }
}
