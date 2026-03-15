using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ST.Core
{
    /// <summary>
    /// 
    /// </summary>
    public abstract class IManager
    {
        /// <summary>
        /// 初始化
        /// </summary>
        public abstract void DoInit();

        /// <summary>
        /// 刷新
        /// </summary>
        public abstract void DoUpdate();

        /// <summary>
        /// 
        /// </summary>
        public abstract void DoLateUpdate();

        /// <summary>
        /// 关闭
        /// </summary>
        public abstract void DoClose();
    }
}
