using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Localizations;

namespace UnityEditor.Localizations
{

    /// <summary>
    /// 中文简体与繁体转换
    /// </summary>
    public class ChineseTraditionalTranslator : ILanguageTranslator
    {
        public int Priority => 10;

        string[] SimplifiedLangs = new string[] { "zh", "zh-CN" };
        string[] TraditionalLangs = new string[] { "zh-TW" };

#if UNITY_EDITOR_WIN
        public bool CanTranslateLanguage(string sourceLang, string targetLang)
        {
            if (!(SimplifiedLangs.Contains(sourceLang, StringComparer.InvariantCultureIgnoreCase) || TraditionalLangs.Contains(sourceLang, StringComparer.InvariantCultureIgnoreCase)))
            {
                return false;
            }
            if (!(SimplifiedLangs.Contains(targetLang, StringComparer.InvariantCultureIgnoreCase) || TraditionalLangs.Contains(targetLang, StringComparer.InvariantCultureIgnoreCase)))
            {
                return false;
            }
            return true;
        }

        public Task<string> TranslateLanguage(string sourceLang, string targetLang, string sourceText)
        {
            string result = null;
            if (SimplifiedLangs.Contains(sourceLang, StringComparer.InvariantCultureIgnoreCase))
            {
                if (TraditionalLangs.Contains(targetLang, StringComparer.InvariantCultureIgnoreCase))
                {
                    result = SimplifiedToTraditional(sourceText);
                }
            }
            else if (TraditionalLangs.Contains(sourceLang, StringComparer.InvariantCultureIgnoreCase))
            {
                if (SimplifiedLangs.Contains(targetLang, StringComparer.InvariantCultureIgnoreCase))
                {
                    result = TraditionalToSimplified(sourceText);
                }
            }
            return Task.FromResult(result);
        }

        private const int LocaleSystemDefault = 0x0800;
        private const int LcmapSimplifiedChinese = 0x02000000;
        private const int LcmapTraditionalChinese = 0x04000000;


        [DllImport("kernel32", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int LCMapString(int locale, int dwMapFlags, string lpSrcStr, int cchSrc, string lpDestStr, int cchDest);
        /// <summary>
        /// 繁体转简体
        /// </summary>
        public static string TraditionalToSimplified(string source)
        {
            var buffer = new String(' ', source.Length);
            LCMapString(LocaleSystemDefault, LcmapSimplifiedChinese, source, source.Length, buffer, source.Length);
            return buffer;
        }

        /// <summary>
        /// 简体转繁体
        /// </summary>
        public static string SimplifiedToTraditional(string source)
        {
            var t = new String(' ', source.Length);
            LCMapString(LocaleSystemDefault, LcmapTraditionalChinese, source, source.Length, t, source.Length);
            return t;
        }

#else
    public bool CanTranslateLanguage(string sourceLang, string targetLang)
    {
        return false;
    }
    public Task<string> TranslateLanguage(string sourceLang, string targetLang, string sourceText)
    {
        throw new NotImplementedException();
    }
#endif
    }
}