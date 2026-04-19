using UnityEngine;
using UnityEngine.UI;

namespace ST.Core.Test
{
    /// <summary>
    /// 背包格子单元，挂载在 <c>ItemTemplate</c> 节点上。
    /// <para>
    /// 由 <see cref="TestBagPanel"/> 通过 <see cref="Init"/> 初始化数据；
    /// 模板节点在 Prefab 中默认 <c>SetActive(false)</c>，运行时按需 Instantiate。
    /// </para>
    /// </summary>
    public class TestBagItem : MonoBehaviour
    {
        // ─── 序列化字段 ──────────────────────────────────────────────────

        /// <summary>图标 Image，由 Init 动态赋色以区分不同物品。</summary>
        public Image  m_IconImage;

        /// <summary>物品名称文字。</summary>
        public Text   m_NameText;

        /// <summary>数量角标文字（格式 "×N"）。</summary>
        public Text   m_CountText;

        /// <summary>整格点击按钮，点击时打印调试日志。</summary>
        public Button m_Button;

        // ─── 私有状态 ────────────────────────────────────────────────────

        /// <summary>格子在列表中的序号（1-based）。</summary>
        int m_Index;

        // ─── 公共接口 ────────────────────────────────────────────────────

        /// <summary>
        /// 初始化格子数据。
        /// </summary>
        /// <param name="index">格子序号（1-based），点击时输出到 Console。</param>
        /// <param name="itemName">物品名称。</param>
        /// <param name="count">物品数量。</param>
        /// <param name="iconColor">图标颜色，用随机色模拟不同道具。</param>
        public void Init(int index, string itemName, int count, Color iconColor)
        {
            m_Index = index;

            if (m_IconImage != null)
                m_IconImage.color = iconColor;

            if (m_NameText != null)
                m_NameText.text = itemName;

            if (m_CountText != null)
                m_CountText.text = string.Format("×{0}", count);

            if (m_Button != null)
            {
                m_Button.onClick.RemoveAllListeners();
                m_Button.onClick.AddListener(OnClick);
            }
        }

        // ─── 私有方法 ────────────────────────────────────────────────────

        /// <summary>格子点击回调，输出格子序号到 Console。</summary>
        void OnClick()
        {
            Debug.LogFormat("[TestBagItem] 点击了第 {0} 个物品：{1}", m_Index,
                m_NameText != null ? m_NameText.text : "?");
        }
    }
}
