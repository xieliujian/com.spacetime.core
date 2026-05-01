using System.Data;
using System.IO;
using ExcelDataReader;

namespace ST.Core
{
    /// <summary>
    /// Excel 读取工具类：基于 ExcelDataReader 插件，支持按 Sheet 索引或名称读取为 DataTable。
    /// 支持 .xlsx（Excel 2007+）与 .xls（Excel 2003）格式。
    /// </summary>
    public static class ExcelUtils
    {
        /// <summary>
        /// 读取 Excel 文件中指定索引的 Sheet，返回 DataTable。
        /// </summary>
        /// <param name="filePath">文件绝对路径（.xlsx 或 .xls）。</param>
        /// <param name="sheetIndex">Sheet 索引，从 0 开始，默认 0。</param>
        /// <param name="headerRowIndex">列名所在行的行索引（0-based），默认 0。</param>
        /// <param name="startRowIndex">数据起始行的行索引（0-based），默认 1。</param>
        /// <returns>读取成功返回 DataTable，文件不存在或读取失败返回 null。</returns>
        public static DataTable ReadExcel(string filePath, int sheetIndex = 0, int headerRowIndex = 0, int startRowIndex = 1)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return null;

            using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = ExcelReaderFactory.CreateReader(stream))
            {
                for (int i = 0; i < sheetIndex; i++)
                {
                    if (!reader.NextResult())
                        return null;
                }

                return ReadSheet(reader, headerRowIndex, startRowIndex);
            }
        }

        /// <summary>
        /// 读取 Excel 文件中指定名称的 Sheet，返回 DataTable。
        /// </summary>
        /// <param name="filePath">文件绝对路径（.xlsx 或 .xls）。</param>
        /// <param name="sheetName">Sheet 名称。</param>
        /// <param name="headerRowIndex">列名所在行的行索引（0-based），默认 0。</param>
        /// <param name="startRowIndex">数据起始行的行索引（0-based），默认 1。</param>
        /// <returns>找到指定 Sheet 返回 DataTable，否则返回 null。</returns>
        public static DataTable ReadExcel(string filePath, string sheetName, int headerRowIndex = 0, int startRowIndex = 1)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return null;

            using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = ExcelReaderFactory.CreateReader(stream))
            {
                do
                {
                    if (reader.Name == sheetName)
                        return ReadSheet(reader, headerRowIndex, startRowIndex);
                }
                while (reader.NextResult());

                return null;
            }
        }

        /// <summary>
        /// 获取 Excel 文件所有 Sheet 的名称列表。
        /// </summary>
        /// <param name="filePath">文件绝对路径。</param>
        /// <returns>Sheet 名称数组，读取失败返回空数组。</returns>
        public static string[] GetSheetNames(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return new string[0];

            var names = new System.Collections.Generic.List<string>();
            using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = ExcelReaderFactory.CreateReader(stream))
            {
                do
                {
                    names.Add(reader.Name);
                }
                while (reader.NextResult());
            }

            return names.ToArray();
        }

        static DataTable ReadSheet(IExcelDataReader reader, int headerRowIndex, int startRowIndex)
        {
            var dataTable = new DataTable();
            int currentRow = 0;

            while (reader.Read())
            {
                if (currentRow == headerRowIndex)
                {
                    for (int col = 0; col < reader.FieldCount; col++)
                    {
                        string colName = reader.GetValue(col)?.ToString() ?? string.Empty;
                        if (!dataTable.Columns.Contains(colName))
                            dataTable.Columns.Add(new DataColumn(colName));
                        else
                            dataTable.Columns.Add(new DataColumn(colName + "_" + col));
                    }
                }
                else if (currentRow >= startRowIndex)
                {
                    var dataRow = dataTable.NewRow();
                    int colCount = System.Math.Min(dataTable.Columns.Count, reader.FieldCount);
                    for (int col = 0; col < colCount; col++)
                        dataRow[col] = reader.GetValue(col)?.ToString() ?? string.Empty;
                    dataTable.Rows.Add(dataRow);
                }

                currentRow++;
            }

            return dataTable;
        }
    }
}
