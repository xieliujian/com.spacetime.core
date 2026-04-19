using ST.Core.UI;
using UnityEngine;
using UnityEngine.UI;

namespace ST.Core.Test
{
    /// <summary>
    /// 通用测试面板，挂载到 <c>ui_panel_test.prefab</c> 根节点（需同时挂载 <c>Canvas</c> 组件）。
    /// <para>
    /// 在 <see cref="OnOpen"/> 中打印传入参数，便于验证 UIManager 打开/关闭流程。
    /// 按场景需要在此类中添加临时调试逻辑。
    /// </para>
    /// </summary>
    public class TestPanel : AbstractPanel
    {
        // ─── 序列化字段（Inspector） ─────────────────────────────────────

        /// <summary>可选：面板上的标题文字组件，有则在 OnOpen 时更新文本。</summary>
        public Text m_TitleText;

        /// <summary>可选：关闭按钮，有则在 OnCreate 时注册点击关闭事件。</summary>
        public Button m_CloseButton;

        // ─── 私有状态 ────────────────────────────────────────────────────

        /// <summary>面板被打开的累计次数，用于验证生命周期调用是否正确。</summary>
        int m_OpenCount;

        /// <summary>面板被打开的累计次数（只读），供 <see cref="UIFlowTest"/> 断言使用。</summary>
        public int openCount { get { return m_OpenCount; } }

        // ─── 生命周期 ────────────────────────────────────────────────────

        /// <summary>初始化：注册关闭按钮事件（若存在）。</summary>
        protected override void OnCreate()
        {
            Debug.Log("[TestPanel] OnCreate");

            if (m_CloseButton != null)
                m_CloseButton.onClick.AddListener(CloseSelf);
        }

        /// <summary>每次打开时：累加计数、更新标题、打印传入参数。</summary>
        protected override void OnOpen(object[] args)
        {
            m_OpenCount++;
            Debug.LogFormat("[TestPanel] OnOpen  第 {0} 次  args={1}", m_OpenCount, ArgsToString(args));

            if (m_TitleText != null)
                m_TitleText.text = string.Format("TestPanel  #{0}", m_OpenCount);
        }

        /// <summary>关闭时打印日志。</summary>
        protected override void OnClose()
        {
            Debug.Log("[TestPanel] OnClose");
        }

        /// <summary>销毁时打印日志并取消按钮监听。</summary>
        protected override void OnDispose()
        {
            Debug.Log("[TestPanel] OnDispose");

            if (m_CloseButton != null)
                m_CloseButton.onClick.RemoveAllListeners();
        }

        /// <summary>可见性变化时打印日志，便于验证 HideMask 效果。</summary>
        protected override void OnVisibleChanged(bool isVisible)
        {
            Debug.LogFormat("[TestPanel] OnVisibleChanged  isVisible={0}", isVisible);
        }

        // ─── 私有方法 ────────────────────────────────────────────────────

        /// <summary>关闭按钮点击回调，通过 UIManager 关闭自身。</summary>
        void CloseSelf()
        {
            if (UIManager.S == null)
                return;

            UIManager.S.ClosePanel(uiID);
        }

        /// <summary>将 args 数组格式化为可读字符串，null 或空时返回 "(none)"。</summary>
        /// <param name="args">参数数组。</param>
        /// <returns>格式化后的字符串。</returns>
        string ArgsToString(object[] args)
        {
            if (args == null || args.Length == 0)
                return "(none)";

            var sb = new System.Text.StringBuilder();
            sb.Append('[');
            for (int i = 0; i < args.Length; ++i)
            {
                if (i > 0)
                    sb.Append(", ");

                sb.Append(args[i] != null ? args[i].ToString() : "null");
            }
            sb.Append(']');
            return sb.ToString();
        }
    }
}
