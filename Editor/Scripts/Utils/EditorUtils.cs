using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace ST.Core
{
    /// <summary>
    /// 
    /// </summary>
    public class EditorUtils
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="assetsPath"></param>
        /// <returns></returns>
        public static string AssetsPath2ABSPath(string assetsPath)
        {
            string assetRootPath = System.IO.Path.GetFullPath(Application.dataPath);
            return assetRootPath.Substring(0, assetRootPath.Length - 6) + assetsPath;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="absPath"></param>
        /// <returns></returns>
        public static string ABSPath2AssetsPath(string absPath)
        {
            string assetRootPath = System.IO.Path.GetFullPath(Application.dataPath);
            return "Assets" + System.IO.Path.GetFullPath(absPath).Substring(assetRootPath.Length).Replace("\\", "/");
        }
    }
}

