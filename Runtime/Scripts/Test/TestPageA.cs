using ST.Core.UI;
using UnityEngine;
using UnityEngine.UI;

namespace ST.Core.Test
{
    /// <summary>
    /// 测试子页面 A，用于验证 <see cref="AbstractPanel"/> 的子页面（<see cref="AbstractPage"/>）动态挂载/卸载流程。
    /// <para>
    /// 挂载到 <c>ui_page_test_a.prefab</c> 根节点（不需要 Canvas 组件，由父面板的 Canvas 渲染）。
    /// 通过 <see cref="UIPanelActive.AttachPage"/> / <see cref="UIPanelActive.DettachPage"/> 管理其生命周期。
    /// </para>
    /// </summary>
    public class TestPageA : AbstractPage
    {
        // ─── 序列化字段（Inspector） ─────────────────────────────────────

        /// <summary>可选：页面标题文字组件。</summary>
        public Text m_TitleText;

        /// <summary>可选：信息文字组件，用于显示打开次数等调试信息。</summary>
        public Text m_InfoText;

        /// <summary>可选：关闭按钮，点击后调用 DettachPage 卸载本页面。</summary>
        public Button m_CloseButton;

        // ─── 私有状态 ────────────────────────────────────────────────────

        /// <summary>本页面被打开的累计次数，用于验证生命周期是否正确调用。</summary>
        int m_OpenCount;

        /// <summary>本页面被打开的累计次数（只读），供测试断言使用。</summary>
        public int openCount { get { return m_OpenCount; } }

        // ─── 生命周期 ────────────────────────────────────────────────────

        /// <summary>初始化：注册关闭按钮事件（若存在）。</summary>
        protected override void OnCreate()
        {
            Debug.Log("[TestPageA] OnCreate");

            if (m_CloseButton != null)
                m_CloseButton.onClick.AddListener(CloseSelf);
        }

        /// <summary>每次打开时：累加计数、更新标题与信息文字、打印传入参数。</summary>
        protected override void OnOpen(object[] args)
        {
            m_OpenCount++;
            string argsStr = ArgsToString(args);
            Debug.LogFormat("[TestPageA] OnOpen  第 {0} 次  args={1}", m_OpenCount, argsStr);

            if (m_TitleText != null)
                m_TitleText.text = string.Format("TestPageA  #{0}", m_OpenCount);

            if (m_InfoText != null)
                m_InfoText.text = string.Format(
                    "子页面 A 已打开（第 {0} 次）\nargs = {1}\n\nuiID = {2}",
                    m_OpenCount, argsStr, uiID);
        }

        /// <summary>关闭时打印日志。</summary>
        protected override void OnClose()
        {
            Debug.Log("[TestPageA] OnClose");
        }

        /// <summary>销毁时打印日志并取消按钮监听。</summary>
        protected override void OnDispose()
        {
            Debug.Log("[TestPageA] OnDispose");

            if (m_CloseButton != null)
                m_CloseButton.onClick.RemoveAllListeners();
        }

        /// <summary>可见性变化时打印日志（受父面板 HideMask 影响）。</summary>
        protected override void OnVisibleChanged(bool isVisible)
        {
            Debug.LogFormat("[TestPageA] OnVisibleChanged  isVisible={0}", isVisible);
        }

        // ─── 私有方法 ────────────────────────────────────────────────────

        /// <summary>关闭按钮回调：通过 panelActive 卸载本页面。</summary>
        void CloseSelf()
        {
            panelActive?.DettachPage(uiID);
        }

        /// <summary>将 args 数组格式化为可读字符串，null 或空时返回 "(none)"。</summary>
        string ArgsToString(object[] args)
        {
            if (args == null || args.Length == 0)
                return "(none)";

            var sb = new System.Text.StringBuilder();
            sb.Append('[');
            for (int i = 0; i < args.Length; ++i)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(args[i] != null ? args[i].ToString() : "null");
            }
            sb.Append(']');
            return sb.ToString();
        }
    }
}
