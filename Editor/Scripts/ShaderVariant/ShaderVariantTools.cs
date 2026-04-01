using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace ST.Core
{
    /// <summary>
    /// Shader 变体收集辅助：校验材质上 Pass 与关键字组合是否可构造有效 <see cref="ShaderVariantCollection.ShaderVariant"/>。
    /// </summary>
    public class ShaderVariantTools
    {
        /// <summary>
        /// 尝试用指定 Pass 类型与关键字列表创建变体；抛异常则视为非法组合。
        /// </summary>
        /// <param name="mat">材质</param>
        /// <param name="passType">Pass 类型</param>
        /// <param name="keywords">着色器关键字数组</param>
        /// <returns>是否可构造成功</returns>
        public static bool IsValidPassTypeKeywords(Material mat, PassType passType, string[] keywords)
        {
            try
            {
                ShaderVariantCollection.ShaderVariant sv = new ShaderVariantCollection.ShaderVariant(mat.shader, passType, keywords);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// 遍历 AssetDatabase 中所有 <c>.mat</c> 资源并加载为 <see cref="Material"/> 列表。
        /// </summary>
        /// <returns>工程内全部材质实例</returns>
        public static List<Material> GetAllMaterials()
        {
            List<Material> mats = new List<Material>();
            string[] assetPaths = AssetDatabase.GetAllAssetPaths();

            foreach (string assetPath in assetPaths)
            {
                if (!assetPath.EndsWith(".mat"))
                {
                    continue;
                }

                Material mat = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
                if (mat != null)
                {
                    mats.Add(mat);
                }
            }

            return mats;
        }
    }
}

