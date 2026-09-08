using UnityEngine;

namespace ST.Core.Launch
{
    /// <summary>
    /// 启动场景组件，将 Unity 生命周期转发给游戏运行入口。
    /// </summary>
    public sealed class GameLauncher : MonoBehaviour
    {
        /// <summary>游戏运行入口。</summary>
        GameApplication m_GameApplication;

        /// <summary>创建并初始化游戏运行入口。</summary>
        void Awake()
        {
            m_GameApplication = new GameApplication();
            m_GameApplication.DoInit();
        }

        /// <summary>转发 Unity 逐帧更新。</summary>
        void Update()
        {
            if (m_GameApplication == null)
            {
                return;
            }

            m_GameApplication.DoUpdate();
        }

        /// <summary>转发 Unity 滞后帧更新。</summary>
        void LateUpdate()
        {
            if (m_GameApplication == null)
            {
                return;
            }

            m_GameApplication.DoLateUpdate();
        }

        /// <summary>销毁时关闭游戏运行入口。</summary>
        void OnDestroy()
        {
            if (m_GameApplication == null)
            {
                return;
            }

            m_GameApplication.DoClose();
            m_GameApplication = null;
        }
    }
}
