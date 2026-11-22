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
        /// <summary>模块初始化：注册事件、加载配置等，在首帧或启动阶段调用一次。</summary>
        public abstract void DoInit();

        /// <summary>每帧逻辑更新（与 Unity <c>Update</c> 对齐）。</summary>
        public abstract void DoUpdate();

        /// <summary>在每帧 <c>Update</c> 之后调用，用于需要滞后一帧的逻辑。</summary>
        public abstract void DoLateUpdate();

        /// <summary>释放资源、取消订阅，在场景切换或应用退出时调用。</summary>
        public abstract void DoClose();
    }
}
