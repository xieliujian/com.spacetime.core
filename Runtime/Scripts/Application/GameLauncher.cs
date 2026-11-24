using System.Collections.Generic;
using UnityEngine;

namespace ST.Core.Launch
{
    /// <summary>
    /// 启动场景组件基类，负责 Unity 生命周期转发。
    /// <para>
    /// 基础模块创建与业务扩展点位于 GameLauncher_Module；业务项目必须继承本类。
    /// </para>
    /// </summary>
    public abstract partial class GameLauncher : MonoBehaviour
    {
        /// <summary>游戏模块内核。</summary>
        GameWorld m_GameWorld;

        /// <summary>游戏模块内核，派生类可在模块初始化后访问启动模块等状态。</summary>
        protected GameWorld gameWorld { get { return m_GameWorld; } }

        /// <summary>创建基础模块与业务模块并初始化游戏模块内核；派生类重写时必须调用 base.Awake()。</summary>
        protected virtual void Awake()
        {
            List<ILogicModule> modules = CreateModules();
            m_GameWorld = new GameWorld();
            m_GameWorld.DoInit(modules);
        }

        /// <summary>转发 Unity 逐帧更新；派生类重写时必须调用 base.Update()。</summary>
        protected virtual void Update()
        {
            if (m_GameWorld == null)
                return;

            m_GameWorld.DoUpdate();
        }

        /// <summary>转发 Unity 滞后帧更新；派生类重写时必须调用 base.LateUpdate()。</summary>
        protected virtual void LateUpdate()
        {
            if (m_GameWorld == null)
                return;

            m_GameWorld.DoLateUpdate();
        }

        /// <summary>销毁时关闭游戏模块内核；派生类重写时必须调用 base.OnDestroy()。</summary>
        protected virtual void OnDestroy()
        {
            if (m_GameWorld == null)
                return;

            m_GameWorld.DoClose();
            m_GameWorld = null;
        }
    }
}
