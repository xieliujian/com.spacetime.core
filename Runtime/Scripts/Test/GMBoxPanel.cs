using ST.Core.UI;
using UnityEngine;
using UnityEngine.UI;

namespace ST.Core.Test
{
    /// <summary>
    /// GM 调试面板的 UIManager 适配层。
    /// <para>
    /// 挂载于 <c>ui_panel_gm_box.prefab</c> 根节点，使该 Prefab 满足
    /// <see cref="UIPanelActive"/> 对 <see cref="AbstractPanel"/> 的要求，
    /// 从而可通过 <c>UIManager.S.OpenPanel(TestUIID.GMBoxPanel)</c> 正常加载。
    /// </para>
    /// <para>
    /// 实际 GM 指令输入/输出由场景中挂载的 <see cref="TestGMBoxPanel"/>（IMGUI）负责；
    /// 本类仅在 UIManager 生命周期回调中控制该 IMGUI 面板的显示与隐藏。
    /// </para>
    /// </summary>
    public class GMBoxPanel : AbstractPanel
    {
        // ─── 序列化字段（Inspector） ──────────────────────────────────────

        /// <summary>可选：面板标题文字组件，OnOpen 时更新。</summary>
        public Text m_TitleText;

        /// <summary>可选：关闭按钮，OnCreate 时注册事件。</summary>
        public Button m_CloseButton;

        // ─── AbstractPanel 生命周期 ───────────────────────────────────────

        /// <summary>初始化：注册关闭按钮事件（若存在）。</summary>
        protected override void OnCreate()
        {
            Debug.Log("[GMBoxPanel] OnCreate");

            if (m_CloseButton != null)
                m_CloseButton.onClick.AddListener(OnClickClose);
        }

        /// <summary>
        /// 打开时：查找场景中的 <see cref="TestGMBoxPanel"/> 并将其显示。
        /// </summary>
        protected override void OnOpen(object[] args)
        {
            Debug.Log("[GMBoxPanel] OnOpen");

            if (m_TitleText != null)
                m_TitleText.text = "GM Box";

            // 查找场景中的 TestGMBoxPanel，令其可见
            var gmBox = FindObjectOfType<TestGMBoxPanel>();
            if (gmBox != null)
                gmBox.Show();
        }

        /// <summary>关闭时：隐藏 TestGMBoxPanel IMGUI 面板。</summary>
        protected override void OnClose()
        {
            Debug.Log("[GMBoxPanel] OnClose");

            var gmBox = FindObjectOfType<TestGMBoxPanel>();
            if (gmBox != null)
                gmBox.Hide();
        }

        /// <summary>销毁时：取消按钮监听。</summary>
        protected override void OnDispose()
        {
            Debug.Log("[GMBoxPanel] OnDispose");

            if (m_CloseButton != null)
                m_CloseButton.onClick.RemoveAllListeners();
        }

        /// <summary>可见性变化时同步更新 IMGUI 面板状态。</summary>
        protected override void OnVisibleChanged(bool isVisible)
        {
            var gmBox = FindObjectOfType<TestGMBoxPanel>();
            if (gmBox == null) return;

            if (isVisible)
                gmBox.Show();
            else
                gmBox.Hide();
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
