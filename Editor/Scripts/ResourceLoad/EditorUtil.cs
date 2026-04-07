using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using UnityEngine;
using UnityEditor;

namespace ST.Core.Editor
{
    class EditorUtil
    {
        public static System.Diagnostics.Process CreateShellExProcess(string cmd, string args, string workingDir = "")
        {
#if UNITY_EDITOR_OSX
            var pStartInfo = new System.Diagnostics.ProcessStartInfo();
            pStartInfo.FileName = "/bin/sh";
            pStartInfo.UseShellExecute = false;
            pStartInfo.RedirectStandardOutput = true;
            pStartInfo.Arguments = cmd;
            if (!string.IsNullOrEmpty(workingDir))
                pStartInfo.WorkingDirectory = workingDir;
            var p = System.Diagnostics.Process.Start(pStartInfo);
            string strOutput = p.StandardOutput.ReadToEnd();
            p.WaitForExit();
            return p;
#else
            var pStartInfo = new System.Diagnostics.ProcessStartInfo(cmd);
            pStartInfo.Arguments = args;
            pStartInfo.CreateNoWindow = false;
            pStartInfo.UseShellExecute = true;
            pStartInfo.RedirectStandardError = false;
            pStartInfo.RedirectStandardInput = false;
            pStartInfo.RedirectStandardOutput = false;
            if (!string.IsNullOrEmpty(workingDir))
                pStartInfo.WorkingDirectory = workingDir;
            return System.Diagnostics.Process.Start(pStartInfo);
#endif
        }

        public static void RunBat(string batfile, string args, string workingDir = "")
        {
            var p = CreateShellExProcess(batfile, args, workingDir);
            p.Close();
        }

        public static string FormatPath(string path)
        {
            path = path.Replace("/", "\\");
            if (Application.platform == RuntimePlatform.OSXEditor)
                path = path.Replace("\\", "/");
            return path;
        }

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
