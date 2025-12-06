using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using System.Text;

namespace UnityEngine.Localizations
{

    public class DirectoryLocalizationLoader : ILocalizationLoader
    {
        public DirectoryLocalizationLoader(string directoryPath)
        {
            DirectoryPath = directoryPath;
        }

        public string DirectoryPath { get; private set; }

        public IEnumerable<LanguageInfo> GetSupportedLangs()
        {
            foreach (var file in Localization.GetLocalizationFiles(DirectoryPath))
            {
                string lang = Localization.ParseLangNameByFileName(file);
                yield return new(lang, lang);
            }
        }

        public IDictionary<string, LocalizationValue> LoadValues(string lang)
        {
            if (DirectoryPath == null)
                return null;
            IDictionary<string, LocalizationValue> content = null;
            string path = Path.Combine(DirectoryPath, lang + "." + Localization.ExtensionName);
            if (File.Exists(path))
            {
                string text = File.ReadAllText(path, Encoding.UTF8);
                content = new Dictionary<string, LocalizationValue>();
                Localization.LoadFromXml(text, content);
            }

            return content;
        }
    }

}