using ST.Core.UI;
using UnityEngine;
using UnityEngine.UI;

namespace ST.Core.Test
{
    /// <summary>
    /// 模态测试面板。
    /// <para>
    /// 继承 <see cref="AbstractPanel"/>，覆盖 <see cref="hideMask"/> 为
    /// <see cref="PanelHideMask.HideAndUnInteractive"/>，用于验证 UIManager
    /// HideMask 机制：打开本面板后，下层所有面板将被隐藏且不可交互。
    /// </para>
    /// <para>
    /// 挂载于 <c>ui_panel_test_modal.prefab</c> 根节点。
    /// 在 GM 面板输入 <c>openui 3</c> 打开，<c>closeui 3</c> 关闭；
    /// 关闭后可验证下层面板恢复可见与交互。
    /// </para>
    /// </summary>
    public class TestModalPanel : AbstractPanel
    {
        // ─── AbstractPanel 重写 ───────────────────────────────────────────

        /// <summary>遮蔽下层：下层面板不可见且不可交互。</summary>
        public override PanelHideMask hideMask => PanelHideMask.HideAndUnInteractive;

        // ─── 序列化字段（Inspector） ──────────────────────────────────────

        /// <summary>可选：标题文字，OnOpen 时更新内容。</summary>
        public Text m_TitleText;

        /// <summary>可选：信息文字，显示遮蔽效果说明。</summary>
        public Text m_InfoText;

        /// <summary>可选：关闭按钮。</summary>
        public Button m_CloseButton;

        // ─── 私有状态 ─────────────────────────────────────────────────────

        /// <summary>面板被打开的累计次数。</summary>
        int m_OpenCount;

        /// <summary>面板被打开的累计次数（只读），供 <see cref="UIFlowTest"/> 断言使用。</summary>
        public int openCount { get { return m_OpenCount; } }

        // ─── 生命周期 ─────────────────────────────────────────────────────

        /// <summary>初始化：注册关闭按钮事件。</summary>
        protected override void OnCreate()
        {
            Debug.Log("[TestModalPanel] OnCreate  hideMask=" + hideMask);

            if (m_CloseButton != null)
                m_CloseButton.onClick.AddListener(OnClickClose);
        }

        /// <summary>每次打开时：更新标题与信息文字，打印日志。</summary>
        protected override void OnOpen(object[] args)
        {
            m_OpenCount++;
            Debug.LogFormat("[TestModalPanel] OnOpen  第 {0} 次  hideMask={1}", m_OpenCount, hideMask);

            if (m_TitleText != null)
                m_TitleText.text = string.Format("模态面板  #{0}", m_OpenCount);

            if (m_InfoText != null)
                m_InfoText.text = "HideMask = HideAndUnInteractive\n下层面板已隐藏且不可交互\n点击关闭按钮恢复";
        }

        /// <summary>关闭时：打印日志（下层面板将由 UIManager 自动恢复可见与交互）。</summary>
        protected override void OnClose()
        {
            Debug.Log("[TestModalPanel] OnClose  下层面板即将恢复可见与交互");
        }

        /// <summary>销毁时：释放按钮监听。</summary>
        protected override void OnDispose()
        {
            Debug.Log("[TestModalPanel] OnDispose");

            if (m_CloseButton != null)
                m_CloseButton.onClick.RemoveAllListeners();
        }

        /// <summary>可见性变化时：打印日志，验证 HideMask 链式传播是否正确。</summary>
        protected override void OnVisibleChanged(bool isVisible)
        {
            Debug.LogFormat("[TestModalPanel] OnVisibleChanged  isVisible={0}", isVisible);
        }

        // ─── 私有方法 ─────────────────────────────────────────────────────

        /// <summary>关闭按钮点击回调。</summary>
        void OnClickClose()
        {
            if (UIManager.S != null)
                UIManager.S.ClosePanel(uiID);
        }
    }
}
