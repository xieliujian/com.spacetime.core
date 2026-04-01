using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ST.Core
{
    /// <summary>
    /// 管理器基类，约定统一的初始化、帧更新与关闭生命周期。
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
        /// 在每帧 <c>Update</c> 之后调用，用于需要滞后一帧的逻辑。
        /// </summary>
        public abstract void DoLateUpdate();

        /// <summary>
        /// 关闭
        /// </summary>
        public abstract void DoClose();
    }
}
