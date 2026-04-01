using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace ST.Core
{
    /// <summary>
    /// Unity 编辑器路径工具：<c>Assets/...</c> 与磁盘绝对路径互转。
    /// </summary>
    public class EditorUtils
    {
        /// <summary>
        /// 将项目内 Assets 相对路径转为操作系统绝对路径。
        /// </summary>
        /// <param name="assetsPath">以 <c>Assets</c> 开头的路径</param>
        /// <returns>完整绝对路径</returns>
        public static string AssetsPath2ABSPath(string assetsPath)
        {
            string assetRootPath = System.IO.Path.GetFullPath(Application.dataPath);
            return assetRootPath.Substring(0, assetRootPath.Length - 6) + assetsPath;
        }

        /// <summary>
        /// 将磁盘绝对路径转为 Unity <c>Assets/...</c> 形式（正斜杠）。
        /// </summary>
        /// <param name="absPath">绝对路径</param>
        /// <returns>Assets 相对路径</returns>
        public static string ABSPath2AssetsPath(string absPath)
        {
            string assetRootPath = System.IO.Path.GetFullPath(Application.dataPath);
            return "Assets" + System.IO.Path.GetFullPath(absPath).Substring(assetRootPath.Length).Replace("\\", "/");
        }
    }
}

