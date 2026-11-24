using System;
using System.IO;
using ST.Core.Logging;

namespace ST.Core
{
    /// <summary>Builder 的命令行参数解析职责。</summary>
    public static partial class Builder
    {
        /// <summary>把命令行参数应用到打包选项。</summary>
        /// <param name="options">待填充选项。</param>
        /// <param name="args">进程启动参数。</param>
        static void ApplyCommandLineArguments(ResourceBuildOptions options, string[] args)
        {
            bool isArgsNull = args == null;
            if (isArgsNull)
            {
                return;
            }

            bool hasLegacyScope = HasAnyLegacyScope(args);
            bool hasScopesArg = HasArg(args, BuildDefine.ARG_SCOPES);
            bool shouldClearScopes = hasLegacyScope || hasScopesArg;
            if (shouldClearScopes)
            {
                options.ClearScopes();
            }

            ApplyLegacyScope(options, args, BuildDefine.ARG_BUILD_SCENE,
                EResourceBuildScope.Scene);
            ApplyLegacyScope(options, args, BuildDefine.ARG_BUILD_STORY,
                EResourceBuildScope.Story);
            ApplyLegacyScope(options, args, BuildDefine.ARG_BUILD_UI,
                EResourceBuildScope.Ui);
            ApplyLegacyScope(options, args, BuildDefine.ARG_BUILD_CHARACTER,
                EResourceBuildScope.Character);
            ApplyLegacyScope(options, args, BuildDefine.ARG_BUILD_PARTICLE,
                EResourceBuildScope.Particle);
            ApplyLegacyScope(options, args, BuildDefine.ARG_BUILD_AUDIO,
                EResourceBuildScope.Audio);
            ApplyLegacyScope(options, args, BuildDefine.ARG_BUILD_SHADER,
                EResourceBuildScope.Shader);

            string scopesValue;
            bool hasScopes = TryGetValue(args, BuildDefine.ARG_SCOPES, out scopesValue);
            if (hasScopes)
            {
                ApplyScopes(options, scopesValue);
            }

            string platformValue;
            bool hasPlatform = TryGetValue(args, BuildDefine.ARG_PLATFORM, out platformValue);
            if (!hasPlatform)
            {
                hasPlatform = TryGetValue(args, BuildDefine.ARG_TARGET_NAME, out platformValue);
            }

            if (hasPlatform)
            {
                ApplyPlatform(options, platformValue);
            }

            string outputValue;
            bool hasOutput = TryGetValue(args, BuildDefine.ARG_OUTPUT, out outputValue);
            if (!hasOutput)
            {
                hasOutput = TryGetValue(args, BuildDefine.ARG_OUTPUT_PATH, out outputValue);
            }

            if (hasOutput)
            {
                options.outputRootPath = Path.GetFullPath(outputValue);
            }

            bool isClean = HasArg(args, BuildDefine.ARG_CLEAN);
            options.isCleanOutput = isClean;
        }

        /// <summary>旧工程开关存在时启用对应类别。</summary>
        /// <param name="options">待填充选项。</param>
        /// <param name="args">进程启动参数。</param>
        /// <param name="argName">开关名。</param>
        /// <param name="scope">对应类别。</param>
        static void ApplyLegacyScope(ResourceBuildOptions options, string[] args,
            string argName, EResourceBuildScope scope)
        {
            bool isPresent = HasArg(args, argName);
            if (!isPresent)
            {
                return;
            }

            options.AddScope(scope);
        }

        /// <summary>判断是否出现任一旧工程类别开关。</summary>
        /// <param name="args">进程启动参数。</param>
        static bool HasAnyLegacyScope(string[] args)
        {
            bool hasScene = HasArg(args, BuildDefine.ARG_BUILD_SCENE);
            bool hasStory = HasArg(args, BuildDefine.ARG_BUILD_STORY);
            bool hasUi = HasArg(args, BuildDefine.ARG_BUILD_UI);
            bool hasCharacter = HasArg(args, BuildDefine.ARG_BUILD_CHARACTER);
            bool hasParticle = HasArg(args, BuildDefine.ARG_BUILD_PARTICLE);
            bool hasAudio = HasArg(args, BuildDefine.ARG_BUILD_AUDIO);
            bool hasShader = HasArg(args, BuildDefine.ARG_BUILD_SHADER);
            bool hasAny = hasScene || hasStory || hasUi || hasCharacter
                          || hasParticle || hasAudio || hasShader;
            return hasAny;
        }

        /// <summary>解析逗号分隔的类别列表。</summary>
        /// <param name="options">待填充选项。</param>
        /// <param name="scopesValue">类别文本。</param>
        static void ApplyScopes(ResourceBuildOptions options, string scopesValue)
        {
            bool isValueMissing = string.IsNullOrEmpty(scopesValue);
            if (isValueMissing)
            {
                return;
            }

            string[] parts = scopesValue.Split(',');
            for (int i = 0; i < parts.Length; ++i)
            {
                string scopeText = parts[i].Trim();
                bool isTextEmpty = string.IsNullOrEmpty(scopeText);
                if (isTextEmpty)
                {
                    continue;
                }

                EResourceBuildScope scope;
                bool isParsed = Enum.TryParse<EResourceBuildScope>(scopeText, true, out scope);
                if (isParsed)
                {
                    options.AddScope(scope);
                }
                else
                {
                    STLogger.LogWarningF(BuildDefine.LOG_SCOPE_UNKNOWN, scopeText);
                }
            }
        }

        /// <summary>解析平台文本。</summary>
        /// <param name="options">待填充选项。</param>
        /// <param name="platformValue">平台文本。</param>
        static void ApplyPlatform(ResourceBuildOptions options, string platformValue)
        {
            bool isValueMissing = string.IsNullOrEmpty(platformValue);
            if (isValueMissing)
            {
                return;
            }

            string normalizedValue = platformValue.Trim().ToLowerInvariant();
            switch (normalizedValue)
            {
                case BuildDefine.PLATFORM_VALUE_ANDROID:
                    options.platform = EResourceBuildPlatform.Android;
                    break;
                case BuildDefine.PLATFORM_VALUE_IOS:
                case BuildDefine.PLATFORM_VALUE_IPHONE:
                    options.platform = EResourceBuildPlatform.IOS;
                    break;
                case BuildDefine.PLATFORM_VALUE_STANDALONE:
                case BuildDefine.PLATFORM_VALUE_WINDOWS:
                    options.platform = EResourceBuildPlatform.Windows;
                    break;
                default:
                    STLogger.LogWarningF(BuildDefine.LOG_PLATFORM_UNKNOWN, platformValue);
                    break;
            }
        }

        /// <summary>读取 key=value 或 -key value 形式的参数值。</summary>
        /// <param name="args">进程启动参数。</param>
        /// <param name="key">参数名。</param>
        /// <param name="value">参数值。</param>
        static bool TryGetValue(string[] args, string key, out string value)
        {
            value = string.Empty;
            string normalizedKey = NormalizeKey(key);
            string equalsPrefix = normalizedKey + "=";
            for (int i = 0; i < args.Length; ++i)
            {
                string normalizedArg = NormalizeKey(args[i]);
                bool isEqualsForm = normalizedArg.StartsWith(
                    equalsPrefix, StringComparison.OrdinalIgnoreCase);
                if (isEqualsForm)
                {
                    value = normalizedArg.Substring(equalsPrefix.Length);
                    return true;
                }

                bool isKeyMatch = string.Equals(
                    normalizedArg, normalizedKey, StringComparison.OrdinalIgnoreCase);
                if (isKeyMatch)
                {
                    bool hasNext = (i + 1) < args.Length;
                    if (hasNext)
                    {
                        value = args[i + 1];
                    }

                    return true;
                }
            }

            return false;
        }

        /// <summary>判断参数是否存在。</summary>
        /// <param name="args">进程启动参数。</param>
        /// <param name="key">参数名。</param>
        static bool HasArg(string[] args, string key)
        {
            string normalizedKey = NormalizeKey(key);
            for (int i = 0; i < args.Length; ++i)
            {
                string normalizedArg = NormalizeKey(args[i]);
                bool isMatch = string.Equals(
                    normalizedArg, normalizedKey, StringComparison.OrdinalIgnoreCase);
                if (isMatch)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>去掉参数名前导横线，兼容 -key 与 key 两种写法。</summary>
        /// <param name="text">原参数文本。</param>
        static string NormalizeKey(string text)
        {
            bool isTextMissing = string.IsNullOrEmpty(text);
            if (isTextMissing)
            {
                return string.Empty;
            }

            bool hasDash = text.StartsWith("-", StringComparison.Ordinal);
            if (!hasDash)
            {
                return text;
            }

            string normalizedText = text.Substring(1);
            return normalizedText;
        }
    }
}
