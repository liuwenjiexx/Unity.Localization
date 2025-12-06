using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace UnityEngine.Localizations
{
    public class ResourcesLocalizationValues : ILocalizationLoader
    {
        public ResourcesLocalizationValues(string resourcesPath)
        {
            this.ResourcesPath = resourcesPath;
        }

        public string ResourcesPath { get; private set; }



        public IEnumerable<LanguageInfo> GetSupportedLangs()
        {

            foreach (var item in Resources.LoadAll<TextAsset>(ResourcesPath))
            {
                string name = item.name;
                if (name.EndsWith(".lang"))
                {
                    name = name.Substring(0, name.Length - 5);
                    yield return new LanguageInfo(name, name);
                }
            }

        }

        public IDictionary<string, LocalizationValue> LoadValues(string lang)
        {
            IDictionary<string, LocalizationValue> content = null;
            TextAsset txt = Resources.Load<TextAsset>(ResourcesPath + "/" + lang + ".lang");
            if (txt == null)
            {
                //Debug.LogError("not found lang: " + lang);
                return null;
            }
            content = new Dictionary<string, LocalizationValue>();
            Localization.LoadFromXml(txt.text, content);
            return content;
        }
    }
}
