using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ST.Core.Test
{
    /// <summary>
    /// GM 调试指令面板测试组件。
    /// 提供可拖拽的 OnGUI 窗口，支持指令注册、执行、历史记录（PlayerPrefs 持久化）与快捷指令列表。
    /// <para>挂载到场景 GameObject 后，按 <see cref="toggleKey"/>（默认 F1）开关面板。</para>
    /// <para>参考：LingRen 项目 GMBoxPanel.cs 的核心交互逻辑，去除业务依赖后移植到框架 Test 层。</para>
    /// </summary>
    public class TestGMBoxPanel : MonoBehaviour
    {
        // ─── 常量 ────────────────────────────────────────────────────────

        /// <summary>历史记录存储到 PlayerPrefs 的键名。</summary>
        const string k_HistoryKey     = "ST_GM_History";
        /// <summary>历史记录最大保存条数。</summary>
        const int    k_MaxHistory     = 40;
        /// <summary>输出区最大字符数，超出时截断旧内容。</summary>
        const int    k_MaxOutputChars = 4000;

        // ─── Inspector 字段 ──────────────────────────────────────────────

        /// <summary>开关面板的快捷键，可在 Inspector 中修改。</summary>
        public KeyCode toggleKey = KeyCode.F1;
        /// <summary>全屏面板四周的内边距（像素）。</summary>
        public float   padding   = 10f;
        /// <summary>左侧指令列表列宽（像素）。</summary>
        public float   cmdListWidth = 200f;

        // ─── 私有状态 ────────────────────────────────────────────────────

        /// <summary>面板当前是否可见。</summary>
        bool m_IsVisible;

        /// <summary>输入框当前文本内容。</summary>
        string m_InputText = string.Empty;
        /// <summary>输出区文本内容，每次执行指令后追加。</summary>
        string m_OutputText = string.Empty;

        /// <summary>历史记录列表，最新条目在末尾。</summary>
        List<string> m_History = new List<string>(CommonDefine.s_ListConst_64);
        /// <summary>当前历史导航指针，等于 Count 时表示未选中任何历史。</summary>
        int m_HistoryIndex;

        /// <summary>已注册的指令表：小写指令名 → <see cref="CommandEntry"/>。</summary>
        Dictionary<string, CommandEntry> m_Commands =
            new Dictionary<string, CommandEntry>(CommonDefine.s_ListConst_32);

        /// <summary>指令列表区域的滚动位置。</summary>
        Vector2 m_CmdScrollPos;
        /// <summary>输出区域的滚动位置。</summary>
        Vector2 m_OutputScrollPos;

        /// <summary>是否已完成 GUIStyle 初始化（首次 OnGUI 时延迟执行）。</summary>
        bool m_StyleInited;
        /// <summary>按钮样式。</summary>
        GUIStyle m_BtnStyle;
        /// <summary>输入框样式。</summary>
        GUIStyle m_InputStyle;
        /// <summary>标签/输出文本样式（开启自动换行）。</summary>
        GUIStyle m_LabelStyle;
        /// <summary>标题标签样式（粗体）。</summary>
        GUIStyle m_TitleStyle;

        /// <summary>内部复用 StringBuilder，避免频繁 GC。</summary>
        StringBuilder m_SB = new StringBuilder();

        // ─── Unity 生命周期 ──────────────────────────────────────────────

        /// <summary>恢复历史记录并注册内置指令。</summary>
        void Start()
        {
            LoadHistory();
            RegisterBuiltinCommands();

            AppendOutput(string.Format("GM Box 已就绪。已注册 {0} 条内置指令，按 {1} 开关面板。",
                m_Commands.Count, toggleKey));
        }

        /// <summary>每帧检测快捷键与键盘历史导航（↑↓）及回车发送。</summary>
        void Update()
        {
            if (Input.GetKeyDown(toggleKey))
                m_IsVisible = !m_IsVisible;

            if (!m_IsVisible)
                return;

            if (Input.GetKeyDown(KeyCode.UpArrow))
                NavigateHistory(-1);
            else if (Input.GetKeyDown(KeyCode.DownArrow))
                NavigateHistory(1);
            else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                ExecuteInput();
        }

        /// <summary>渲染全屏 GM 调试面板（仅可见时绘制）。</summary>
        void OnGUI()
        {
            if (!m_IsVisible)
                return;

            EnsureStyles();

            // 全屏半透明背景
            var bgColor = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.82f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = bgColor;

            // 全屏内容区域
            var area = new Rect(padding, padding,
                Screen.width  - padding * 2f,
                Screen.height - padding * 2f);

            GUILayout.BeginArea(area);
            DrawPanelContent();
            GUILayout.EndArea();
        }

        // ─── 公开 API ────────────────────────────────────────────────────

        /// <summary>
        /// 注册一条 GM 指令。重复注册同名指令时覆盖旧处理函数。
        /// </summary>
        /// <param name="cmd">指令名（不含空格；注册后自动转小写）。</param>
        /// <param name="handler">
        /// 处理函数，参数为按空格解析后的字符串数组（<c>args[0]</c> 为指令名本身）。
        /// </param>
        /// <param name="desc">指令描述，显示在面板左侧快捷列表中。</param>
        public void RegisterCommand(string cmd, Action<string[]> handler, string desc = "")
        {
            if (string.IsNullOrEmpty(cmd))
                return;

            if (handler == null)
                return;

            m_Commands[cmd.ToLower()] = new CommandEntry(handler, desc);
        }

        /// <summary>
        /// 向输出区追加一行文本，可用于指令处理函数输出结果。
        /// 超出 <see cref="k_MaxOutputChars"/> 时自动截断旧内容。
        /// </summary>
        /// <param name="text">要追加的文本行。</param>
        public void AppendOutput(string text)
        {
            if (m_OutputText.Length > k_MaxOutputChars)
                m_OutputText = m_OutputText.Substring(m_OutputText.Length - (k_MaxOutputChars / 2));

            m_OutputText += text + "\n";
            m_OutputScrollPos.y = float.MaxValue;
        }

        // ─── 窗口绘制 ────────────────────────────────────────────────────

        /// <summary>绘制面板全部控件：标题行、输入行、主体区域（指令列表 + 输出区）、底部历史导航行。</summary>
        void DrawPanelContent()
        {
            GUILayout.BeginVertical();

            DrawTitleRow();
            GUILayout.Space(6f);
            DrawInputRow();
            GUILayout.Space(4f);
            DrawMainArea();
            GUILayout.Space(4f);
            DrawHistoryRow();

            GUILayout.EndVertical();
        }

        /// <summary>绘制顶部标题行（标题文字 + 右侧关闭按钮）。</summary>
        void DrawTitleRow()
        {
            GUILayout.BeginHorizontal();

            GUILayout.Label(
                string.Format("★ GM Box  —  按 {0} 关闭", toggleKey),
                m_TitleStyle,
                GUILayout.ExpandWidth(true));

            if (GUILayout.Button("× 关闭", m_BtnStyle, GUILayout.Width(90f)))
                m_IsVisible = false;

            GUILayout.EndHorizontal();
        }

        /// <summary>绘制顶部指令输入行（输入框 + 执行按钮 + 清空输出按钮）。</summary>
        void DrawInputRow()
        {
            GUILayout.BeginHorizontal();

            GUILayout.Label("指令:", m_LabelStyle, GUILayout.Width(46f));
            m_InputText = GUILayout.TextField(m_InputText, m_InputStyle, GUILayout.ExpandWidth(true));

            if (GUILayout.Button("执行", m_BtnStyle, GUILayout.Width(72f)))
                ExecuteInput();

            if (GUILayout.Button("清空", m_BtnStyle, GUILayout.Width(72f)))
                m_OutputText = string.Empty;

            GUILayout.EndHorizontal();
        }

        /// <summary>绘制主体区域：左侧指令快捷列表 + 右侧输出滚动区。</summary>
        void DrawMainArea()
        {
            GUILayout.BeginHorizontal();

            DrawCommandList();
            GUILayout.Space(4f);
            DrawOutputArea();

            GUILayout.EndHorizontal();
        }

        /// <summary>绘制左侧已注册指令快捷列表；点击按钮将指令名填入输入框并显示描述。</summary>
        void DrawCommandList()
        {
            GUILayout.BeginVertical(GUILayout.Width(cmdListWidth));
            GUILayout.Label("── 指令列表 ──", m_TitleStyle);

            m_CmdScrollPos = GUILayout.BeginScrollView(m_CmdScrollPos, GUILayout.ExpandHeight(true));

            foreach (var kv in m_Commands)
            {
                if (GUILayout.Button(kv.Key, m_BtnStyle))
                {
                    m_InputText = kv.Key;
                    AppendOutput(string.Format("$ {0,-20} // {1}", kv.Key, kv.Value.desc));
                }
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        /// <summary>绘制右侧输出滚动区，显示 <see cref="m_OutputText"/> 的全部内容。</summary>
        void DrawOutputArea()
        {
            GUILayout.BeginVertical();
            GUILayout.Label("── 输出 ──", m_TitleStyle);

            m_OutputScrollPos = GUILayout.BeginScrollView(
                m_OutputScrollPos, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

            GUILayout.Label(m_OutputText, m_LabelStyle);

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        /// <summary>绘制底部历史导航行（历史数量显示 + ↑↓ 按钮 + 清空历史按钮）。</summary>
        void DrawHistoryRow()
        {
            GUILayout.BeginHorizontal();

            GUILayout.Label(
                string.Format("历史 {0}/{1}", m_HistoryIndex, m_History.Count),
                m_LabelStyle,
                GUILayout.Width(120f));

            if (GUILayout.Button("↑ 前一条", m_BtnStyle, GUILayout.Width(100f)))
                NavigateHistory(-1);

            if (GUILayout.Button("↓ 后一条", m_BtnStyle, GUILayout.Width(100f)))
                NavigateHistory(1);

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("清除历史", m_BtnStyle, GUILayout.Width(100f)))
                ClearHistory();

            GUILayout.EndHorizontal();
        }

        // ─── 指令执行 ────────────────────────────────────────────────────

        /// <summary>
        /// 读取 <see cref="m_InputText"/>，记录到历史，解析并分发给对应的处理函数；
        /// 未找到指令时输出错误提示，执行异常时捕获并输出异常信息。
        /// </summary>
        void ExecuteInput()
        {
            string raw = m_InputText.Trim();
            if (string.IsNullOrEmpty(raw))
                return;

            AddHistory(raw);
            m_InputText = string.Empty;

            string[] parts = raw.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            string   key   = parts[0].ToLower();

            AppendOutput(string.Format("> {0}", raw));

            if (!m_Commands.TryGetValue(key, out CommandEntry entry))
            {
                AppendOutput(string.Format("[错误] 未知指令 \"{0}\"，输入 help 查看所有可用指令。", parts[0]));
                return;
            }

            try
            {
                entry.handler(parts);
            }
            catch (Exception ex)
            {
                AppendOutput(string.Format("[异常] {0}", ex.Message));
                Debug.LogException(ex);
            }
        }

        // ─── 历史记录 ────────────────────────────────────────────────────

        /// <summary>
        /// 将指令加入历史列表末尾（已存在则先移除）。
        /// 超出 <see cref="k_MaxHistory"/> 时从头部截断，然后持久化。
        /// </summary>
        /// <param name="cmd">要记录的原始指令字符串。</param>
        void AddHistory(string cmd)
        {
            int idx = m_History.IndexOf(cmd);
            if (idx != -1)
                m_History.RemoveAt(idx);

            m_History.Add(cmd);

            if (m_History.Count > k_MaxHistory)
                m_History.RemoveRange(0, m_History.Count - k_MaxHistory);

            m_HistoryIndex = m_History.Count;
            SaveHistory();
        }

        /// <summary>
        /// 按方向在历史记录中移动指针并将对应条目填入输入框。
        /// </summary>
        /// <param name="direction">-1 向前（更早），+1 向后（更近）。</param>
        void NavigateHistory(int direction)
        {
            if (m_History.Count == 0)
                return;

            m_HistoryIndex = Mathf.Clamp(m_HistoryIndex + direction, 0, m_History.Count - 1);
            m_InputText    = m_History[m_HistoryIndex];
        }

        /// <summary>清空内存与 PlayerPrefs 中的历史记录，并输出确认信息。</summary>
        void ClearHistory()
        {
            m_History.Clear();
            m_HistoryIndex = 0;
            PlayerPrefs.DeleteKey(k_HistoryKey);
            AppendOutput("[历史] 已全部清空。");
        }

        /// <summary>将历史列表序列化为换行分隔字符串并写入 PlayerPrefs。</summary>
        void SaveHistory()
        {
            if (m_History.Count == 0)
                return;

            m_SB.Clear();
            for (int i = 0; i < m_History.Count; ++i)
            {
                if (i > 0)
                    m_SB.Append('\n');

                m_SB.Append(m_History[i]);
            }

            PlayerPrefs.SetString(k_HistoryKey, m_SB.ToString());
        }

        /// <summary>从 PlayerPrefs 反序列化历史列表，并将指针定位到末尾。</summary>
        void LoadHistory()
        {
            string raw = PlayerPrefs.GetString(k_HistoryKey, string.Empty);
            m_History.Clear();

            if (string.IsNullOrEmpty(raw))
                return;

            string[] items = raw.Split('\n');
            for (int i = 0; i < items.Length; ++i)
            {
                if (!string.IsNullOrEmpty(items[i]))
                    m_History.Add(items[i]);
            }

            m_HistoryIndex = m_History.Count;
        }

        // ─── 内置指令 ────────────────────────────────────────────────────

        /// <summary>注册三条内置指令：help / clear / echo。</summary>
        void RegisterBuiltinCommands()
        {
            RegisterCommand("help",  CmdHelp,  "列出所有已注册指令及其描述");
            RegisterCommand("clear", CmdClear, "清空输出区域");
            RegisterCommand("echo",  CmdEcho,  "回显参数内容：echo <内容>");
        }

        /// <summary>列出所有已注册指令的名称与描述，输出到面板。</summary>
        void CmdHelp(string[] args)
        {
            m_SB.Clear();
            m_SB.AppendLine(string.Format("── 共 {0} 条指令 ──", m_Commands.Count));

            foreach (var kv in m_Commands)
                m_SB.AppendLine(string.Format("  {0,-22} {1}", kv.Key, kv.Value.desc));

            AppendOutput(m_SB.ToString());
        }

        /// <summary>清空输出区域文本。</summary>
        void CmdClear(string[] args)
        {
            m_OutputText = string.Empty;
        }

        /// <summary>将 <c>args[1..]</c> 以空格拼接后回显到输出区。</summary>
        void CmdEcho(string[] args)
        {
            if (args.Length < 2)
            {
                AppendOutput("[echo] 用法：echo <内容>");
                return;
            }

            m_SB.Clear();
            for (int i = 1; i < args.Length; ++i)
            {
                if (i > 1)
                    m_SB.Append(' ');

                m_SB.Append(args[i]);
            }

            AppendOutput(m_SB.ToString());
        }

        // ─── GUIStyle 初始化 ─────────────────────────────────────────────

        /// <summary>
        /// 首次渲染时延迟初始化 GUIStyle，避免在非 OnGUI 上下文访问 <c>GUI.skin</c>。
        /// </summary>
        void EnsureStyles()
        {
            if (m_StyleInited)
                return;

            m_StyleInited = true;

            m_BtnStyle            = new GUIStyle(GUI.skin.button);
            m_BtnStyle.fontSize   = 20;

            m_InputStyle          = new GUIStyle(GUI.skin.textField);
            m_InputStyle.fontSize = 22;

            m_LabelStyle           = new GUIStyle(GUI.skin.label);
            m_LabelStyle.fontSize  = 20;
            m_LabelStyle.wordWrap  = true;

            m_TitleStyle           = new GUIStyle(GUI.skin.label);
            m_TitleStyle.fontSize  = 20;
            m_TitleStyle.fontStyle = FontStyle.Bold;
        }

        // ══════════════════════════════════════════
        // 内部数据结构
        // ══════════════════════════════════════════

        /// <summary>单条注册指令的处理函数与说明描述的组合结构。</summary>
        struct CommandEntry
        {
            /// <summary>指令执行回调，<c>args[0]</c> 为指令名，后续为参数列表。</summary>
            public Action<string[]> handler;
            /// <summary>指令描述文本，显示在面板左侧快捷列表中。</summary>
            public string desc;

            /// <summary>以指定的处理函数与描述构造 <see cref="CommandEntry"/>。</summary>
            public CommandEntry(Action<string[]> h, string d)
            {
                handler = h;
                desc    = d;
            }
        }
    }
}
