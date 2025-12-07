using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Localizations;

namespace UnityEditor.Localizations
{
    using Localization = UnityEngine.Localizations.Localization;

    public static class EditorLocalizationUtility
    {
        public const string RootNodeName = "localization";
        public const string XMLNS = "urn:schema-localization";

        public const string MenuPrefix = "Window/General/";
        public const int MenuPriority = 2000;
        public const string PackageId = "unity.localization";
        private static Dictionary<string, ILocalizationValueDrawer> valueDrawers;
        private static string packageDir;
        private static LocalizationValues editorLocalizationValues;


        public static string PackageDir
        {
            get
            {
                if (string.IsNullOrEmpty(packageDir))
                    packageDir = GetPackageDirectory(PackageId);
                return packageDir;
            }
        }
        public static LocalizationValues EditorLocalizationValues
        {
            get
            {
                if (editorLocalizationValues == null)
                    editorLocalizationValues = new LocalizationValues(new DirectoryLocalizationLoader(Path.Combine(PackageDir, "Editor/Localization")));
                return editorLocalizationValues;
            }
        }

        //2020/9/1
        private static string GetPackageDirectory(string packageName)
        {
            string path = Path.Combine("Packages", packageName);
            if (Directory.Exists(path) && File.Exists(Path.Combine(path, "package.json")))
                return path;

            foreach (var dir in Directory.GetDirectories("Assets", "*", SearchOption.AllDirectories))
            {
                if (string.Equals(Path.GetFileName(dir), packageName, StringComparison.InvariantCultureIgnoreCase))
                {
                    if (File.Exists(Path.Combine(dir, "package.json")))
                        return dir;
                }
            }

            foreach (var pkgPath in Directory.GetFiles("Assets", "package.json", SearchOption.AllDirectories))
            {
                try
                {
                    if (JsonUtility.FromJson<UnityPackage>(File.ReadAllText(pkgPath, System.Text.Encoding.UTF8)).name == packageName)
                    {
                        return Path.GetDirectoryName(pkgPath);
                    }
                }
                catch { }
            }

            return null;
        }
        [Serializable]
        class UnityPackage
        {
            public string name;
        }

        private static Dictionary<string, ILocalizationValueDrawer> GetValueDrawers()
        {
            if (valueDrawers == null)
            {
                valueDrawers = new Dictionary<string, ILocalizationValueDrawer>();
                foreach (var type in AppDomain.CurrentDomain.GetAssemblies()
                    .Referenced(typeof(ILocalizationValueDrawer).Assembly)
                    .SelectMany(o => o.GetTypes()))
                {
                    if (type.IsAbstract)
                        continue;
                    if (typeof(ILocalizationValueDrawer).IsAssignableFrom(type))
                    {
                        var provider = Activator.CreateInstance(type) as ILocalizationValueDrawer;
                        valueDrawers.Add(provider.TypeName, provider);
                    }
                }
            }
            return valueDrawers;
        }

        public static IEnumerable<string> GetValueTypeNames()
        {
            return GetValueDrawers().Select(o => o.Key)
             .OrderBy(o => o)
             .OrderBy(o => o == "string" ? 0 : 1);
        }


        public static ILocalizationValueDrawer GetValueDrawer(string typeName)
        {
            ILocalizationValueDrawer valueDrawer;
            if (!GetValueDrawers().TryGetValue(typeName, out valueDrawer))
                return null;
            return valueDrawer;
        }


        static List<ILanguageTranslator> languageTranslators;
        static Dictionary<string, ILanguageTranslator> translatorsMap;

        public static List<ILanguageTranslator> GetLanguageTranslators()
        {
            if (languageTranslators != null)
                return languageTranslators;

            languageTranslators = new List<ILanguageTranslator>();
            foreach (var type in TypeCache.GetTypesDerivedFrom<ILanguageTranslator>())
            {
                if (type.IsAbstract) continue;
                if (type.GetConstructor(Type.EmptyTypes) == null) continue;
                ILanguageTranslator translator = Activator.CreateInstance(type) as ILanguageTranslator;
                languageTranslators.Add(translator);
            }
            languageTranslators.Sort((a, b) => -(a.Priority - b.Priority));
            return languageTranslators;
        }

        public static ILanguageTranslator GetLanguageTranslator(string sourceLang, string targetLang)
        {
            if (translatorsMap == null)
                translatorsMap = new Dictionary<string, ILanguageTranslator>();
            string key = sourceLang + "+" + targetLang;
            ILanguageTranslator translator = null;
            if (!translatorsMap.TryGetValue(key, out translator))
            {
                foreach (var t in GetLanguageTranslators())
                {
                    if (t.CanTranslateLanguage(sourceLang, targetLang))
                    {
                        translator = t;
                        break;
                    }
                }
                translatorsMap[key] = translator;
            }
            return translator;
        }

        public static IEnumerator TaskToCorutine(Task task)
        {
            while (!task.IsCompleted)
            {
                yield return null;
            }
        }

    }
}