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
        /// <summary>
        /// 
        /// </summary>
        protected static IMainThreadTask s_Instance = null;

        /// <summary>
        /// 
        /// </summary>
        public static IMainThreadTask S
        {
            get { return s_Instance; }
        }

        /// <summary>
        /// 
        /// </summary>
        public IMainThreadTask()
        {
            s_Instance = this;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="func"></param>
        public abstract void AddTask(GameEvent func);
    }
}
