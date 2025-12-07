using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEngine.Localizations
{
    public class LocalizationValues
    {
        public string Lang { get; private set; }



        private Dictionary<string, IDictionary<string, LocalizationValue>> cached;
        private Dictionary<string, string> defaultValue2Key;
        public bool IsInitialized { get; private set; }

        //List 支持顺序
        private List<LanguageInfo> supportedLanguages;

        public IReadOnlyList<LanguageInfo> SupportedLanguages { get => supportedLanguages; }

        public string NotFoundKeyFormat { get; set; }

        public LocalizationValues(ILocalizationLoader loader)
        {
            supportedLanguages = new();
            this.loader = loader;
        }

        private ILocalizationLoader loader;

        public virtual void Initialize()
        {
            if (IsInitialized)
                return;
            IsInitialized = true;
            if (supportedLanguages == null)
                supportedLanguages = new();
            supportedLanguages.Clear();

            foreach (var item in loader.GetSupportedLangs())
            {
                supportedLanguages.Add(item);
            }
            string supportedLang = Localization.GetSupportedLang(supportedLanguages);
            LoadLang(supportedLang);
            Localization.All.Add(this);
        }



        public virtual void LoadLang(string lang)
        {
            lang = lang ?? string.Empty;
            if (cached == null)
                cached = new Dictionary<string, IDictionary<string, LocalizationValue>>();


            string baseLang = Localization.DefaultLang;
            foreach (var part in GetBaseLangList(lang))
            {
                IDictionary<string, LocalizationValue> content = null;


                if (!cached.TryGetValue(part, out content))
                {
                    content = loader.LoadValues(part);
                    if (content != null)
                    {
                        cached[part] = content;

                        if (part == LocalizationSettings.DefaultLang)
                        {
                            if (defaultValue2Key == null)
                                defaultValue2Key = new Dictionary<string, string>();
                            defaultValue2Key.Clear();
                            foreach (var item in content)
                            {
                                string strValue = item.Value.StringValue;
                                if (string.IsNullOrEmpty(strValue))
                                    continue;
                                defaultValue2Key[strValue] = item.Key;
                            }
                        }
                        else if (!Localization.BaseLangMapping.ContainsKey(part))
                        {
                            Localization.BaseLangMapping[part] = baseLang;
                        }
                        baseLang = part;
                    }
                }
                else
                {
                    baseLang = part;
                }

            }


            Lang = lang;
        }



        IEnumerable<string> GetBaseLangList(string lang)
        {
            HashSet<string> set = new HashSet<string>();

            if (!string.IsNullOrEmpty(Localization.DefaultLang))
            {
                set.Add(Localization.DefaultLang);
                yield return Localization.DefaultLang;
            }



            string[] parts = lang.Split(Localization.LangSeparator);

            string partLang = null;
            for (int i = 0; i < parts.Length; i++)
            {
                if (i == 0)
                {
                    partLang = parts[i];
                }
                else
                {
                    partLang += Localization.LangSeparator + parts[i];
                }
                if (!set.Contains(partLang))
                {
                    set.Add(partLang);
                    yield return partLang;
                }
            }
        }


        public bool TryGetValue(string lang, string key, out LocalizationValue value)
        {
            if (string.IsNullOrEmpty(lang))
            {
                value = default;
                return false;
            }
            IDictionary<string, LocalizationValue> langDic;
            if (cached.TryGetValue(lang, out langDic))
            {
                if (langDic.TryGetValue(key, out value))
                    return true;
            }

            string baseLang = lang;
            while (true)
            {
                if (!Localization.BaseLangMapping.TryGetValue(baseLang, out baseLang))
                {
                    break;
                }
                if (string.IsNullOrEmpty(baseLang))
                    break;
                if (cached.TryGetValue(baseLang, out langDic) && langDic.TryGetValue(key, out value))
                {
                    return true;
                }
            }
            value = default;
            return false;
        }

        public bool HasItem(string key)
        {
            return key != null && TryGetValue(Localization.CurrentLang, key, out var value);
        }

        #region Get Value

        public LocalizationValue GetValue(string key)
        {
            LocalizationValue value;

            if (key != null && TryGetValue(Localization.CurrentLang, out value))
                return value;
            return default;
        }

        public bool TryGetValue(string key, out LocalizationValue value)
        {
            if (key != null && TryGetValue(Localization.CurrentLang, key, out value))
                return true;
            value = default;
            return false;
        }


        public string GetString(string key)
        {
            LocalizationValue value;
            if (!TryGetValue(key, out value))
            {
                if (string.IsNullOrEmpty(NotFoundKeyFormat))
                    return key;
                return string.Format(NotFoundKeyFormat, key);
            }
            return value.StringValue;
        }

        public Texture2D GetTexture2D(string key)
        {
            return GetValue(key).Value as Texture2D;
        }

        public Color GetColor(string key)
        {
            LocalizationValue value;
            if (!TryGetValue(key, out value))
            {
                return Color.clear;
            }
            return (Color)value.Value;
        }

        #endregion

        public bool TryGetKey(string defaultValue, out string key)
        {
            if (defaultValue2Key == null || defaultValue == null)
            {
                key = null;
                return false;
            }
            return defaultValue2Key.TryGetValue(defaultValue, out key);
        }

        public bool TryGetValueByDefault(string defaultValue, out string value)
        {
            if (defaultValue2Key == null || defaultValue == null)
            {
                value = null;
                return false;
            }
            string key;
            if (defaultValue2Key.TryGetValue(defaultValue, out key))
            {
                if (TryGetValue(Localization.CurrentLang, key, out var v))
                {
                    value = v.StringValue;
                    return true;
                }
            }
            value = null;
            return false;
        }


        public IDisposable BeginScope(string lang = null)
        {
            if (string.IsNullOrEmpty(lang))
                lang = Localization.CurrentLang;
            return new Localization.LocalizationScope(this, lang);
        }

    }


}
