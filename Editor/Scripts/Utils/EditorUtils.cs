using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using UnityEngine;
using UnityEditor;

namespace ST.Core
{
    /// <summary>
    /// 编辑器通用工具集：Assets 路径互转、路径格式化、MD5、外部进程启动与同步执行。
    /// </summary>
    public class EditorUtils
    {
        // ── 路径工具 ──────────────────────────────────────────────

        /// <summary>
        /// 将项目内 <c>Assets/...</c> 相对路径转为操作系统绝对路径。
        /// </summary>
        /// <param name="assetsPath">以 <c>Assets</c> 开头的路径。</param>
        /// <returns>完整绝对路径。</returns>
        public static string AssetsPath2ABSPath(string assetsPath)
        {
            string assetRootPath = System.IO.Path.GetFullPath(Application.dataPath);
            return assetRootPath.Substring(0, assetRootPath.Length - 6) + assetsPath;
        }

        /// <summary>
        /// 将磁盘绝对路径转为 Unity <c>Assets/...</c> 形式（正斜杠）。
        /// </summary>
        /// <param name="absPath">绝对路径。</param>
        /// <returns>Assets 相对路径。</returns>
        public static string ABSPath2AssetsPath(string absPath)
        {
            string assetRootPath = System.IO.Path.GetFullPath(Application.dataPath);
            return "Assets" + System.IO.Path.GetFullPath(absPath).Substring(assetRootPath.Length).Replace("\\", "/");
        }

        /// <summary>将路径规范为当前编辑器平台常用分隔符（Windows 用 <c>\</c>，macOS 用 <c>/</c>）。</summary>
        public static string FormatPath(string path)
        {
            path = path.Replace("/", "\\");
            if (Application.platform == RuntimePlatform.OSXEditor)
                path = path.Replace("\\", "/");
            return path;
        }

        // ── 文件工具 ──────────────────────────────────────────────

        /// <summary>计算文件内容的 MD5 十六进制小写字符串。</summary>
        /// <param name="file">文件完整路径。</param>
        public static string Md5file(string file)
        {
            try
            {
                FileStream fs = new FileStream(file, FileMode.Open);
                System.Security.Cryptography.MD5 md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
                byte[] retVal = md5.ComputeHash(fs);
                fs.Close();

                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < retVal.Length; i++)
                    sb.Append(retVal[i].ToString("x2"));
                return sb.ToString();
            }
            catch (Exception ex)
            {
                throw new Exception("Md5file() fail, error:" + ex.Message);
            }
        }

        // ── 进程工具 ──────────────────────────────────────────────

        /// <summary>
        /// 启动外部进程：macOS 下经 <c>/bin/sh</c>，Windows 下直接启动可执行文件。
        /// </summary>
        /// <param name="cmd">可执行文件路径（Windows）或 shell 参数（macOS）。</param>
        /// <param name="args">命令行参数。</param>
        /// <param name="workingDir">工作目录，为空时不指定。</param>
        public static Process CreateShellExProcess(string cmd, string args, string workingDir = "")
        {
#if UNITY_EDITOR_OSX
            var pStartInfo = new ProcessStartInfo();
            pStartInfo.FileName = "/bin/sh";
            pStartInfo.UseShellExecute = false;
            pStartInfo.RedirectStandardOutput = true;
            pStartInfo.Arguments = cmd;
            if (!string.IsNullOrEmpty(workingDir))
                pStartInfo.WorkingDirectory = workingDir;
            var p = Process.Start(pStartInfo);
            string strOutput = p.StandardOutput.ReadToEnd();
            p.WaitForExit();
            return p;
#else
            var pStartInfo = new ProcessStartInfo(cmd);
            pStartInfo.Arguments = args;
            pStartInfo.CreateNoWindow = false;
            pStartInfo.UseShellExecute = true;
            pStartInfo.RedirectStandardError = false;
            pStartInfo.RedirectStandardInput = false;
            pStartInfo.RedirectStandardOutput = false;
            if (!string.IsNullOrEmpty(workingDir))
                pStartInfo.WorkingDirectory = workingDir;
            return Process.Start(pStartInfo);
#endif
        }

        /// <summary>执行批处理或脚本后关闭进程句柄。</summary>
        public static void RunBat(string batfile, string args, string workingDir = "")
        {
            var p = CreateShellExProcess(batfile, args, workingDir);
            p.Close();
        }

        /// <summary>同步启动进程并读取标准错误；有错误输出时记日志并返回 <c>false</c>。</summary>
        /// <param name="file">可执行文件路径。</param>
        /// <param name="args">命令行参数。</param>
        public static bool ExecuteProcess(string file, string args)
        {
            var info = new ProcessStartInfo()
            {
                FileName = file,
                Arguments = args,
                UseShellExecute = false,
                ErrorDialog = true,
                RedirectStandardOutput = true,
                RedirectStandardInput = true,
                RedirectStandardError = true,
                WorkingDirectory = Path.GetDirectoryName(file),
            };

            var err = string.Empty;
            using (var process = Process.Start(info))
            {
                err = process.StandardError.ReadToEnd();
                if (!string.IsNullOrEmpty(err))
                    UnityEngine.Debug.LogError(err);
                else
                    UnityEngine.Debug.Log(file + args);
                process.WaitForExit();
            }

            return string.IsNullOrEmpty(err);
        }
    }
}
