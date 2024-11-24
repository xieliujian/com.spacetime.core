using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace ST.Core
{
    /// <summary>
    /// 
    /// </summary>
    public class ShaderVariantTools
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="mat"></param>
        /// <param name="passType"></param>
        /// <param name="keywords"></param>
        /// <returns></returns>
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
        /// 
        /// </summary>
        /// <returns></returns>
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

