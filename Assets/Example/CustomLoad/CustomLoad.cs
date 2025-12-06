//***** 运行该例子，请取消该注释 *****
#define TEST_CUSTOM 
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localizations;
using UnityEngine.UI;

namespace UnityEngine.Localizations
{

#if TEST_CUSTOM

    /// <summary>
    /// 定制数据源
    /// Project Settings/Tool/Localization/LoaderType = UnityEngine.Localizations.LocalizationDataLoader 
    /// </summary>
    class LocalizationDataLoader : ILocalizationLoader
    {
        private static List<LanguageInfo> supportedLangs = new()
        {
          new  ("en", "English" ),
           new ("zh","中文" ),
             new ("zh-TW", "正體字" )
        };

        public int Priority => 1;

        public IEnumerable<LanguageInfo> GetSupportedLangs()
        {
            return supportedLangs;
        }

        public IDictionary<string, LocalizationValue> LoadValues(string lang)
        {
            IDictionary<string, LocalizationValue> result = null;
            switch (lang.ToLower())
            {
                case "zh":
                    result = LocalizationValue.StringDictionary(new Dictionary<string, string>()
                        {
                            {"Source","Custom-zh" },
                            {"Hello World","你好世界"},
                            {"Language","语言"  }
                        });
                    break;
                case "en":
                    result = LocalizationValue.StringDictionary(new Dictionary<string, string>()
                        {
                            {"Source","Custom-en" },
                            {"Hello World","Hello World" },
                            {"Language","Language" }
                        });
                    break;
                case "zh-tw":
                    result = LocalizationValue.StringDictionary(new Dictionary<string, string>() {
                            {"Source","Custom-zh-TW" },
                        { "Language", "語言" }
                    });
                    break;
            }
            return result;
        }
    }

#endif


    public class CustomLoad : MonoBehaviour
    {
        public Dropdown listLanguage;
        public Text text;

        /// <summary>
        /// 下拉列表语言选项
        /// </summary>
        private static Dictionary<string, string> langs = new Dictionary<string, string>()
        {
            {"en", "English" },
            {"zh","中文" },
              {"zh-TW","正體字" }
        };

        static LocalizationDataLoader dataLocalizationValues = new LocalizationDataLoader();

        //#if UNITY_EDITOR
        //        [InitializeOnLoadMethod]
        //        static void InitializeOnLoadMethod()
        //        {
        //            Localization.Default = dataLocalizationValues;
        //            Debug.Log("InitializeOnLoadMethod");
        //        }
        //#endif

        //[RuntimeInitializeOnLoadMethod]
        //static void RuntimeInitializeOnLoadMethod()
        //{
        //    Localization.Default = dataLocalizationValues;
        //    Debug.Log("RuntimeInitializeOnLoadMethod");
        //}

        // Start is called before the first frame update
        void Start()
        {

#if !TEST_CUSTOM
            throw new System.Exception("not define '#define TEST_CUSTOM'");
#endif

            //初始化
            Localization.Initialize();

            InitializeLanguageDropdown();


            UpdateUI();
        }

        /// <summary>
        /// 初始化语言选项下拉列表
        /// </summary>
        void InitializeLanguageDropdown()
        {

            listLanguage.onValueChanged.AddListener((v) =>
            {
                string lang = null;
                if (v == 0)
                {
                    LocalizationSettings.SelectedLang = null;
                    return;
                }
                int i = 1;
                foreach (var item in langs)
                {
                    if (i == v)
                    {
                        lang = item.Key;
                        break;
                    }
                    i++;
                }

                //设置选择的语言
                LocalizationSettings.SelectedLang = lang;
            });
            listLanguage.ClearOptions();

            listLanguage.AddOptions(new string[] { "Follow  System" }.Concat(langs.Values).ToList());

            UpdateLanguageDropdown();
        }


        void UpdateUI()
        {
            text.text = "Language".Localization();
        }

        void UpdateLanguageDropdown()
        {

            int j = 0;
            foreach (var item in langs)
            {
                if (item.Key == Localization.CurrentLang)
                {
                    listLanguage.value = j + 1;
                    break;
                }
                j++;
            }
        }
        private void OnEnable()
        {
            Localization.LanguageChanged += OnLanguageChanged;
        }

        private void OnDisable()
        {
            Localization.LanguageChanged -= OnLanguageChanged;
        }


        void OnLanguageChanged()
        {
            Debug.Log("OnLanguageChanged");
            UpdateLanguageDropdown();
            UpdateUI();
        }
    }
}