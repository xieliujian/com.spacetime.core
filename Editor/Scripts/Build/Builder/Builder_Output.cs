using System;
using System.IO;
using System.Text;
using UnityEditor;

namespace ST.Core
{
    /// <summary>Builder 的输出目录与清单清理职责。</summary>
    public static partial class Builder
    {
        /// <summary>创建输出目录，按需清空旧内容。</summary>
        /// <param name="context">本次打包上下文。</param>
        /// <param name="outputRootPath">输出根目录绝对路径。</param>
        static bool PrepareOutputDirectory(ResourceBuildContext context, string outputRootPath)
        {
            try
            {
                bool isDirectoryExists = Directory.Exists(outputRootPath);
                bool isCleanRequired = context.options.isCleanOutput;
                bool shouldDelete = isDirectoryExists && isCleanRequired;
                if (shouldDelete)
                {
                    Directory.Delete(outputRootPath, true);
                }

                Directory.CreateDirectory(outputRootPath);
                return true;
            }
            catch (Exception ex)
            {
                string message = string.Format(
                    BuildDefine.LOG_PREPARE_FAILED, outputRootPath, ex.Message);
                context.AddError(message);
                return false;
            }
        }

        /// <summary>输出可选的 builds.txt 构建清单。</summary>
        /// <param name="outputRootPath">输出根目录绝对路径。</param>
        /// <param name="context">本次打包上下文。</param>
        static void WriteBuildList(string outputRootPath, ResourceBuildContext context)
        {
            StringBuilder builder = new StringBuilder(s_MergedBuildList.Count * 64);
            string lineEnding = BuildDefine.LINE_ENDING_LF;
            for (int i = 0; i < s_MergedBuildList.Count; ++i)
            {
                AssetBundleBuild build = s_MergedBuildList[i];
                builder.Append(build.assetBundleName);
                builder.Append(lineEnding);

                string[] assetNames = build.assetNames;
                for (int j = 0; j < assetNames.Length; ++j)
                {
                    builder.Append('\t');
                    builder.Append(assetNames[j]);
                    builder.Append(lineEnding);
                }
            }

            string fileName = BuildDefine.BUILD_LIST_FILE_NAME;
            string filePath = Path.Combine(outputRootPath, fileName);
            string content = builder.ToString();
            File.WriteAllText(filePath, content, s_Utf8NoBom);

            string message = string.Format(BuildDefine.LOG_BUILD_LIST_WRITTEN, filePath);
            context.LogInfo(message);
        }

        /// <summary>删除 Unity 生成的清单文件。</summary>
        /// <param name="outputRootPath">输出根目录绝对路径。</param>
        static void CleanupManifestFiles(string outputRootPath)
        {
            DirectoryInfo outputDirectory = new DirectoryInfo(outputRootPath);
            string rootBundleName = outputDirectory.Name;
            string rootBundlePath = Path.Combine(outputRootPath, rootBundleName);
            bool isRootBundleExists = File.Exists(rootBundlePath);
            if (isRootBundleExists)
            {
                File.Delete(rootBundlePath);
            }

            string rootManifestPath = rootBundlePath + BuildDefine.MANIFEST_EXTENSION;
            bool isRootManifestExists = File.Exists(rootManifestPath);
            if (isRootManifestExists)
            {
                File.Delete(rootManifestPath);
            }

            string searchPattern = "*" + BuildDefine.MANIFEST_EXTENSION;
            string[] manifestFiles = Directory.GetFiles(
                outputRootPath, searchPattern, SearchOption.AllDirectories);
            for (int i = 0; i < manifestFiles.Length; ++i)
            {
                File.Delete(manifestFiles[i]);
            }
        }
    }
}
