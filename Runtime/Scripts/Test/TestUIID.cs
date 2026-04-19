namespace ST.Core.Test
{
    /// <summary>
    /// 测试场景专用 UI 整型 ID 常量表。
    /// <para>
    /// 上层正式工程应改用 <c>enum UIID : int</c> 定义面板 ID；
    /// 此处用 <c>static class</c> 保持框架层零污染，便于测试场景独立运行。
    /// </para>
    /// </summary>
    public static class TestUIID
    {
        /// <summary>GM 调试面板 ID，对应 <c>ui_panel_gm_box.prefab</c>（<see cref="GMBoxPanel"/>）。</summary>
        public const int GMBoxPanel   = 1;

        /// <summary>通用测试面板 ID，对应 <c>ui_panel_test.prefab</c>（<see cref="TestPanel"/>）。</summary>
        public const int TestPanel    = 2;

        /// <summary>模态测试面板 ID，对应 <c>ui_panel_test_modal.prefab</c>（<see cref="TestModalPanel"/>）。</summary>
        public const int TestModal    = 3;
    }
}
