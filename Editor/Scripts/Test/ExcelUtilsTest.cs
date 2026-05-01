using System;
using System.Data;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace ST.Core.Editor.Test
{
    /// <summary>
    /// ExcelUtils 编辑器测试窗口：选择 Excel 文件，按 Sheet 索引或名称读取并在 Console 打印结果。
    /// <para>菜单：<b>ST.Core / Test / Excel Utils Test</b></para>
    /// </summary>
    public class ExcelUtilsTest : EditorWindow
    {
        string m_FilePath      = string.Empty;
        int    m_SheetIndex    = 0;
        string m_SheetName     = string.Empty;
        int    m_HeaderRowIndex = 0;
        int    m_StartRowIndex  = 1;
        int    m_MaxPrintRows   = 10;

        Vector2 m_Scroll;

        // ─── 菜单入口 ────────────────────────────────────────────────────

        /// <summary>打开 ExcelUtils 测试窗口。</summary>
        [MenuItem("ST.Core/Test/Excel Utils Test")]
        static void Open()
        {
            GetWindow<ExcelUtilsTest>("Excel Utils Test").Show();
        }

        // ─── GUI ─────────────────────────────────────────────────────────

        void OnGUI()
        {
            m_Scroll = EditorGUILayout.BeginScrollView(m_Scroll);

            GUILayout.Label("ExcelUtils 读取测试", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            DrawFileField();
            EditorGUILayout.Space(4);

            m_SheetIndex     = EditorGUILayout.IntField("Sheet 索引（0-based）", m_SheetIndex);
            m_SheetName      = EditorGUILayout.TextField("Sheet 名称（按名称读取时用）", m_SheetName);
            m_HeaderRowIndex = EditorGUILayout.IntField("列名行索引（0-based）", m_HeaderRowIndex);
            m_StartRowIndex  = EditorGUILayout.IntField("数据起始行索引（0-based）", m_StartRowIndex);
            m_MaxPrintRows   = EditorGUILayout.IntField("Console 最多打印行数", m_MaxPrintRows);

            EditorGUILayout.Space(8);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("按索引读取 Sheet"))
                OnReadByIndex();

            if (GUILayout.Button("按名称读取 Sheet"))
                OnReadByName();

            if (GUILayout.Button("打印所有 Sheet 名"))
                OnPrintSheetNames();

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndScrollView();
        }

        // ─── 操作 ────────────────────────────────────────────────────────

        void OnReadByIndex()
        {
            var table = ExcelUtils.ReadExcel(m_FilePath, m_SheetIndex, m_HeaderRowIndex, m_StartRowIndex);
            PrintTable(table, $"Sheet[{m_SheetIndex}]", m_FilePath);
        }

        void OnReadByName()
        {
            if (string.IsNullOrEmpty(m_SheetName))
            {
                Debug.LogWarning("[ExcelUtilsTest] Sheet 名称为空，请在「Sheet 名称」字段中填写。");
                return;
            }

            var table = ExcelUtils.ReadExcel(m_FilePath, m_SheetName, m_HeaderRowIndex, m_StartRowIndex);
            PrintTable(table, $"Sheet \"{m_SheetName}\"", m_FilePath);
        }

        void OnPrintSheetNames()
        {
            var names = ExcelUtils.GetSheetNames(m_FilePath);
            if (names == null || names.Length == 0)
            {
                Debug.LogWarning($"[ExcelUtilsTest] 未读取到任何 Sheet，请检查文件路径：{m_FilePath}");
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine($"[ExcelUtilsTest] 文件共 {names.Length} 个 Sheet：");
            for (int i = 0; i < names.Length; i++)
                sb.AppendLine($"  [{i}] {names[i]}");

            Debug.Log(sb.ToString());
        }

        // ─── 打印工具 ────────────────────────────────────────────────────

        void PrintTable(DataTable table, string label, string filePath)
        {
            if (table == null)
            {
                Debug.LogError($"[ExcelUtilsTest] 读取失败：{label}  文件={filePath}");
                return;
            }

            int printRows  = Math.Min(table.Rows.Count, m_MaxPrintRows);
            int colCount   = table.Columns.Count;

            var sb = new StringBuilder();
            sb.AppendLine($"[ExcelUtilsTest] {label}  列数={colCount}  总行数={table.Rows.Count}  文件={filePath}");
            sb.AppendLine(new string('-', 60));

            // 列名
            for (int c = 0; c < colCount; c++)
                sb.Append($"{table.Columns[c].ColumnName,-20}");
            sb.AppendLine();
            sb.AppendLine(new string('-', 60));

            // 数据行
            for (int r = 0; r < printRows; r++)
            {
                for (int c = 0; c < colCount; c++)
                    sb.Append($"{table.Rows[r][c],-20}");
                sb.AppendLine();
            }

            if (table.Rows.Count > printRows)
                sb.AppendLine($"... 还有 {table.Rows.Count - printRows} 行未显示（调大「最多打印行数」可查看）");

            Debug.Log(sb.ToString());
        }

        // ─── 辅助 UI ─────────────────────────────────────────────────────

        void DrawFileField()
        {
            EditorGUILayout.BeginHorizontal();
            m_FilePath = EditorGUILayout.TextField("Excel 文件路径", m_FilePath);
            if (GUILayout.Button("浏览…", GUILayout.Width(64)))
            {
                string path = EditorUtility.OpenFilePanel("选择 Excel 文件", "", "xlsx,xls");
                if (!string.IsNullOrEmpty(path))
                {
                    m_FilePath = path;
                    GUI.FocusControl(null);
                    Repaint();
                }
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}
