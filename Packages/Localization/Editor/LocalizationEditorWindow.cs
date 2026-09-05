using GluonGui.Dialog;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml;
using TextMateSharp.Internal.Parser;
using Unity.EditorCoroutines.Editor;
using UnityEngine;
using UnityEngine.Localizations;

namespace UnityEditor.Localizations
{
    using Localization = UnityEngine.Localizations.Localization;

    public class LocalizationEditorWindow : EditorWindow
    {
        private Vector2 scrollPos;
        private string newKey;
        private string newValueTypeName;
        private bool isDataDirted;

        private ItemData baseData;
        private List<ItemData> itemDatas = new();

        private const string Menu_All = "All";
        private const string Menu_New = "New";

        string searchKey;
        bool isBaseDataDirted;
        IEnumerable<string> keys;
        int itemWidth = 180;
        int itemHeight;

        static bool isLoaded;



        private void OnEnable()
        {
            if (!isLoaded)
            {
                isLoaded = true;

            }

            using (EditorLocalizationUtility.EditorLocalizationValues.BeginScope())
            {
                titleContent = new GUIContent("Localization".Localization());

                if (baseData == null)
                    baseData = new ItemData();

                baseData.Load();
                RefreshItems();
                foreach (var item in itemDatas)
                {
                    item.Load();
                }

                EditorLocalizationUtility.GetValueDrawer("string");
            }
        }


        [MenuItem(EditorLocalizationUtility.MenuPrefix + "Localization", priority = EditorLocalizationUtility.MenuPriority)]
        public static void Show_Menu()
        {
            GetWindow<LocalizationEditorWindow>().Show();
        }

        public bool HasItem(string path)
        {
            return itemDatas.Where(o => o.path == path).Count() > 0;
        }

        ItemData AddItem(string path)
        {
            if (HasItem(path))
                return null;
            ItemData item = new ItemData() { path = path };
            item.lang = Localization.ParseLangNameByFileName(path);
            item.Load();
            itemDatas.Add(item);
            return item;
        }

        ItemData FindByPath(string path)
        {
            foreach (var item in itemDatas)
            {
                if (item.path == path)
                    return item;
            }
            return null;
        }

        public void RemoveItem(int itemIndex)
        {
            if (itemIndex >= itemDatas.Count)
                return;

            itemDatas.RemoveAt(itemIndex);
        }


        public void SelectBase(string dir)
        {
            List<string> allLangPaths = new();
            if (!string.IsNullOrEmpty(dir))
            {
                foreach (var file in Localization.GetLocalizationFiles(dir))
                {
                    //item.lang = Localization.ParseLangNameByFileName(file);
                    //item.path = file.ReplacePathSeparator();
                    allLangPaths.Add(file.ReplacePathSeparator());
                }
            }

            string current = baseData.path;
            if (string.IsNullOrEmpty(current))
            {
                if (!string.IsNullOrEmpty(EditorLocalizationUtility.BaseLang))
                {
                    current = allLangPaths.FirstOrDefault(_ => Localization.ParseLangNameByFileName(_) == EditorLocalizationUtility.BaseLang);
                }
                if (current == null)
                {
                    current = allLangPaths.FirstOrDefault(_ => Localization.ParseLangNameByFileName(_) == "en");
                }
                if (current == null)
                {
                    current = allLangPaths.FirstOrDefault(_ => Localization.ParseLangNameByFileName(_) == "zh");
                }
                if (current != null)
                {
                    baseData.path = current;
                }
            }

            LoadBase(current);

            foreach (var path in allLangPaths)
            {
                if (HasItem(path))
                    continue;
                var item = AddItem(path);
                //if (baseData.path == path)
                //{
                //    ShowIndex(item, 0);
                //}
            }

            RefreshItems();
        }
        void RefreshItems()
        {
            itemDatas.Sort((a, b) => string.Compare(a.lang, b.lang));
            if (!string.IsNullOrEmpty(EditorLocalizationUtility.FirstLang))
            {
                itemDatas.Sort((a, b) => (a.lang == EditorLocalizationUtility.FirstLang ? 0 : 1) - (b.lang == EditorLocalizationUtility.FirstLang ? 0 : 1));
            }
        }
        void ShowIndex(ItemData item, int index)
        {
            if (item.displayIndex == index)
                return;

            if (item.displayIndex >= 0)
            {
                foreach (var it in itemDatas)
                {
                    if (it.displayIndex > index)
                    {
                        it.displayIndex--;
                    }
                }
                item.displayIndex = -1;
            }

            if (index >= 0)
            {
                foreach (var it in itemDatas)
                {
                    if (it.displayIndex >= index)
                    {
                        it.displayIndex++;
                    }
                }
                item.displayIndex = index;
            }
        }

        public bool IsShow(string lang)
        {
            foreach (var it in itemDatas)
            {
                if (it.lang == lang)
                {
                    return it.displayIndex >= 0;
                }
            }
            return false;
        }

        public bool IsShowWithPath(string path)
        {
            foreach (var it in itemDatas)
            {
                if (it.path == path)
                {
                    return it.displayIndex >= 0;
                }
            }
            return false;
        }

        int GetShowCount()
        {
            int count = 0;
            foreach (var it in itemDatas)
            {
                if (it.displayIndex >= 0)
                {
                    count++;
                }
            }
            return count;
        }

        /*
        int FindIndexWithLangName(string lang)
        {
            int index = -1;
            for (int i = 0; i < allLangNames.Length; i++)
            {
                if (string.Equals(allLangNames[i], lang, StringComparison.InvariantCultureIgnoreCase))
                {
                    index = i;
                    break;
                }
            }
            return index;
        }*/
        //int FindIndexWithPath(string path)
        //{
        //    int index = -1;

        //    for (int i = 0; i < allLangPaths.Count; i++)
        //    {
        //        if (string.Equals(allLangPaths[i].path, path, StringComparison.InvariantCultureIgnoreCase))
        //        {
        //            index = i;
        //            break;
        //        }
        //    }
        //    return index;
        //}
        int FindIndexWithPath(string path)
        {
            int index = -1;

            for (int i = 0; i < itemDatas.Count; i++)
            {
                if (string.Equals(itemDatas[i].path, path, StringComparison.InvariantCultureIgnoreCase))
                {
                    index = i;
                    break;
                }
            }
            return index;
        }

        //LangItem FindLangItemWithPath(string path)
        //{
        //    for (int i = 0; i < allLangPaths.Count; i++)
        //    {
        //        if (string.Equals(allLangPaths[i].path, path, StringComparison.InvariantCultureIgnoreCase))
        //        {
        //            return allLangPaths[i];
        //        }
        //    }
        //    return null;
        //}

        void LoadBase(string path)
        {
            baseData.path = path;
            baseData.lang = Localization.ParseLangNameByFileName(path);
            baseData.Load();
        }

        void LoadAll()
        {
            foreach (var itemData in itemDatas)
            {
                itemData.Load();
            }
        }


        void Load(string path)
        {
            foreach (var item in itemDatas)
            {
                if (item.path == path)
                {
                    item.Load();
                    break;
                }
            }
        }



        void DirtyData(ItemData item)
        {
            //Debug.Log("dirty");
            isDataDirted = true;
            GUIUtility.keyboardControl = -1;

            item.Save();
            if (item.path == baseData.path)
            {
                baseData.Load();
            }
        }

        void DiryBaseData()
        {
            isBaseDataDirted = true;
            EditorApplication.delayCall += () =>
            {
                if (isBaseDataDirted)
                {
                    baseData.Save();
                    Load(baseData.path);
                }
            };

        }

        public static string CreateNewFile(string filePath)
        {
            if (!Directory.Exists(Path.GetDirectoryName(filePath)))
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));

            if (File.Exists(filePath))
            {
                EditorUtility.DisplayDialog("error", $"file exists <{filePath}>", "ok");
                return null;
            }

            XmlDocument doc = new XmlDocument();
            doc.AppendChild(doc.CreateXmlDeclaration("1.0", "utf-8", null));
            XmlElement elemRoot = doc.CreateElement(EditorLocalizationUtility.RootNodeName);
            elemRoot.SetOrAddAttributeValue("xmlns", EditorLocalizationUtility.XMLNS);

            doc.AppendChild(elemRoot);
            doc.Save(filePath);

            AssetDatabase.ImportAsset(filePath, ImportAssetOptions.ForceUpdate);
            return filePath;
        }

        public static bool PopupLang(string[] langNames, bool expandWidth = true, params GUILayoutOption[] options)
        {
            if (langNames == null)
                return false;
            GUIContent[] items;
            items = new GUIContent[] { new GUIContent("None".Localization()) }.Concat(langNames.Select(o => new GUIContent(o))).ToArray();
            bool changed = false;
            int selectedIndex = -1;
            for (int i = 0; i < langNames.Length; i++)
            {
                if (langNames[i] == LocalizationSettings.SelectedLang)
                {
                    selectedIndex = i;
                    break;
                }
            }

            if (!expandWidth)
            {
                float maxLength;
                if (selectedIndex == -1)
                {
                    maxLength = EditorStyles.popup.CalcSize(items[0]).x;
                }
                else
                {
                    maxLength = EditorStyles.popup.CalcSize(items[selectedIndex + 1]).x;
                }

                options = options.Concat(new GUILayoutOption[] { GUILayout.Width(maxLength) }).ToArray();
            }


            int newIndex = EditorGUILayout.Popup(selectedIndex + 1, items, options);
            if (selectedIndex + 1 != newIndex)
            {
                newIndex--;
                if (newIndex < 0)
                    LocalizationSettings.SelectedLang = null;
                else
                    LocalizationSettings.SelectedLang = langNames[newIndex];
                changed = true;
                GUI.changed = true;
            }
            return changed;
        }

        bool IsFilterKey(string key)
        {
            if (string.IsNullOrEmpty(searchKey))
                return true;

            return key.IndexOf(searchKey, StringComparison.InvariantCultureIgnoreCase) >= 0;
        }

        private void OnGUI()
        {
            using (EditorLocalizationUtility.EditorLocalizationValues.BeginScope())
            {
                GUILangStatus();

                GUINewItem();

                itemHeight = 20;


                bool inheritValue;
                using (var sv = new GUILayout.ScrollViewScope(scrollPos))
                {
                    scrollPos = sv.scrollPosition;
                    keys = baseData.values.Keys;
                    keys = keys.Concat(itemDatas.SelectMany(o => o.values.Keys));
                    keys = keys.Distinct().Where(o => IsFilterKey(o)).OrderBy(o => o).ToArray();

                    using (new GUILayout.HorizontalScope())
                    {
                        GUILangKeys();
                        int itemIndex = 0;
                        int changeItemIndex = -1;
                        int newItemIndex = -1;
                        foreach (var item in itemDatas)
                        //for (int itemIndex = 0; itemIndex < itemDatas.Length; itemIndex++)
                        {
                            //var item = itemDatas[itemIndex];
                            //if (item.displayIndex < 0)
                            //    continue;
                            //int itemIndex = -1;
                            //for (int i = 0; i < itemDatas.Count; i++)
                            //{
                            //    if (itemDatas[i] == item)
                            //    {
                            //        itemIndex = i;
                            //        break;
                            //    }
                            //}
                            bool isBaseEditing = baseData.path == item.path;

                            using (new GUILayout.VerticalScope(GUILayout.Width(itemWidth)))
                            {

                                using (new GUILayout.HorizontalScope())
                                {
                                    //var langItem = FindLangItemWithPath(item.path);
                                    /*
                                    //float width = (Screen.width - EditorGUIUtility.labelWidth) * 0.3f;
                                    //width = Mathf.Min(width, 150);
                                    GUILayoutOption[] options = new GUILayoutOption[] { GUILayout.ExpandWidth(true) };
                                    //if (width < EditorStyles.popup.fixedWidth)
                                    //    options = options.Concat(new GUILayoutOption[] { GUILayout.MaxWidth(width) }).ToArray();
                                    newIndex = EditorGUILayout.Popup(GUIContent.none, selectedIndex, allLangNames, options);
                                    if (newIndex != selectedIndex)
                                    {
                                        selectedIndex = newIndex;
                                        if (newIndex != -1)
                                        {
                                            item.path = allLangPaths[newIndex];
                                            item.Load();
                                        }
                                    }

                                    if (EditorGUILayoutx.PingButton(item.path))
                                    {
                                    }*/


                                    /*
                                    if (GUILayout.Button("×", "label", GUILayout.ExpandWidth(false)))
                                    {
                                        //RemoveItem(i);

                                        //i--;
                                        ShowIndex(item, -1);
                                        continue;
                                    }*/
                                    if (GUILayout.Button("<", "label", GUILayout.ExpandWidth(false)))
                                    {

                                        if (itemIndex > 0)
                                        {
                                            changeItemIndex = itemIndex;
                                            //index--;
                                            newItemIndex = 0;
                                            EditorLocalizationUtility.FirstLang = item.lang;
                                        }
                                    }
                                    GUIStyle style = new GUIStyle("label");
                                    style.alignment = TextAnchor.MiddleCenter;
                                    if (GUILayout.Button(item.lang, style))
                                    {
                                        EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(item.path));
                                    }


                                    if (item.lang != baseData.lang)
                                    {

                                        if (GUILayout.Button("T", TranslateButtonStyle, GUILayout.ExpandWidth(false)))
                                        {

                                            if (EditorUtility.DisplayDialog("Translate", $"Auto Translate All {baseData.lang}=>{item.lang}", "ok", "cancel"))
                                            {

                                                List<TranslateItem> list = new List<TranslateItem>();
                                                foreach (var key in item.values.Keys)
                                                {
                                                    list.Add(new TranslateItem() { itemData = item, key = key });
                                                }
                                                Translate(baseData, list, false);
                                            }
                                        }

                                        bool allSet = true;
                                        foreach (var key in baseData.values.Keys)
                                        {
                                            if (!item.values.ContainsKey(key))
                                            {
                                                allSet = false;
                                                break;
                                            }
                                        }
                                        if (GUILayout.Toggle(allSet, GUIContent.none, GUILayout.ExpandWidth(false)) != allSet)
                                        {
                                            allSet = !allSet;
                                            if (allSet)
                                            {
                                                foreach (var key in baseData.values.Keys)
                                                {
                                                    if (!item.values.ContainsKey(key))
                                                    {
                                                        item.values.Add(key, baseData.values[key].Clone());
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    /*
                                    if (GUILayout.Button(">", "label", GUILayout.ExpandWidth(false)))
                                    {
                                        int index = itemDatas.IndexOf(item);
                                        if (index < itemDatas.Count - 1)
                                        {
                                            index++;
                                            itemDatas.Remove(item);
                                            itemDatas.Insert(index, item);
                                        }
                                    }*/
                                }

                                if (!string.IsNullOrEmpty(item.loadError))
                                {
                                    EditorGUILayout.HelpBox(item.loadError, MessageType.Error);
                                    continue;
                                }

                                using (var itemChecker = new EditorGUI.ChangeCheckScope())
                                {

                                    foreach (var key in keys)
                                    {

                                        inheritValue = !item.values.ContainsKey(key);
                                        using (new GUILayout.HorizontalScope(GUILayout.MaxHeight(itemHeight), GUILayout.Height(itemHeight)))
                                        {
                                            if (inheritValue && !baseData.values.ContainsKey(key))
                                            {
                                                using (new GUIUtilityx.Scopes.ColorScope(Color.red))
                                                {
                                                    GUILayout.Label("(missing)");
                                                }
                                                continue;
                                            }

                                            LocalizationValue value;
                                            if (inheritValue)
                                                value = baseData.values[key];
                                            else
                                                value = item.values[key];

                                            GUIItemNode(item, key, value, itemIndex);

                                            // using (new EditorGUI.DisabledGroupScope(inheritValue))
                                            {
                                                bool isEditValue = GUILayout.Toggle(!inheritValue, GUIContent.none, GUILayout.ExpandWidth(false));
                                                if (!inheritValue != isEditValue)
                                                {
                                                    if (isEditValue)
                                                    {
                                                        item.values.Add(key, baseData.values[key].Clone());
                                                    }
                                                    else
                                                    {
                                                        item.values.Remove(key);
                                                    }
                                                }
                                            }


                                            //using (new GUIx.Scopes.ChangedScope())
                                            //{
                                            //    if (GUILayout.Button("◥", "label", GUILayout.ExpandWidth(false)))
                                            //    {
                                            //        GenericMenu menu = new GenericMenu();

                                            //        if (item.values.ContainsKey(key))
                                            //        {
                                            //            menu.AddItem(new GUIContent("Delete".Localization() + $" [{key}]"), false, (o) =>
                                            //            {
                                            //                object[] arr = (object[])o;
                                            //                ItemData item1 = (ItemData)arr[0];
                                            //                string key1 = (string)arr[1];
                                            //                item1.values.Remove(key1);
                                            //                DirtyData(item1);
                                            //            }, new object[] { item, key });
                                            //        }
                                            //        else
                                            //        {
                                            //            menu.AddDisabledItem(new GUIContent("Delete".Localization() + $" [{key}]"), false);
                                            //        }

                                            //        /*    if (isBaseEditing)
                                            //            {
                                            //                menu.AddItem(new GUIContent("Delete".Localization() + $" [{key}]"), false, (o) =>
                                            //                  {
                                            //                      string key1 = (string)o;
                                            //                      values.Remove(key1);
                                            //                      ReloadBaseData();
                                            //                      DirtyData();
                                            //                  }, key);
                                            //            }
                                            //            else
                                            //            {
                                            //               // if (missingKey)
                                            //              //  {
                                            //                    menu.AddItem(new GUIContent("Delete".Localization() + $" [{key}]"), false, (o) =>
                                            //                    {
                                            //                        string key1 = (string)o;
                                            //                        values.Remove(key1);
                                            //                        DirtyData();
                                            //                    }, key);
                                            //           //     }
                                            //            //    else
                                            //              //  {
                                            //             //       menu.AddDisabledItem(new GUIContent("Delete".Localization() + $" [{key}]"), false);
                                            //             //   }
                                            //            }*/
                                            //        menu.ShowAsContext();
                                            //    }

                                            //}

                                        }
                                    }
                                    if (itemChecker.changed)
                                    {
                                        DirtyData(item);

                                        GUIUtility.keyboardControl = -1;
                                        GUI.changed = false;
                                    }
                                }
                            }
                            itemIndex++;
                        }

                        if (changeItemIndex >= 0)
                        {
                            var item = itemDatas[changeItemIndex];
                            itemDatas.Remove(item);
                            itemDatas.Insert(newItemIndex, item);
                            RefreshItems();
                        }


                        using (new GUILayout.HorizontalScope(GUILayout.Width(itemWidth)))
                        {
                            List<string> list = new List<string>();
                            list.Add(Menu_New);
                            //list.Add(Menu_All);
                            //foreach (var item in allLangPaths)
                            //{
                            //    list.Add(item.lang);
                            //}

                            int selectedIndex = -1;
                            selectedIndex = EditorGUILayout.Popup(GUIContent.none, selectedIndex, list.ToArray(), GUILayout.Width(80));

                            if (selectedIndex != -1)
                            {
                                switch (list[selectedIndex])
                                {
                                    case Menu_New:
                                        NewItem();
                                        break;
                                    case Menu_All:

                                        break;
                                    default:
                                        //AddItem();
                                        //ShowIndex(itemDatas[indexs[selectedIndex]], GetShowCount());
                                        break;
                                }
                            }

                            //if (GUILayout.Button("Create New".Localization(), GUILayout.ExpandWidth(false)))
                            //{

                            //}
                        }
                    }

                }
            }
        }


        void NewItem()
        {
            string dir = "Assets";
            if (!string.IsNullOrEmpty(baseData.path))
            {
                dir = Path.GetDirectoryName(baseData.path);
            }

            string path = EditorUtility.SaveFilePanel("Create New Localization".Localization(), dir, "", Localization.ExtensionName);
            if (!string.IsNullOrEmpty(path))
            {
                string tmp;
                if (path.ToRelativePath(".", out tmp))
                {
                    path = tmp;
                }
                path = CreateNewFile(path);
                dir = Path.GetDirectoryName(path);
                if (itemDatas.Count == 0)
                {
                    SelectBase(dir);
                }
                ShowIndex(AddItem(path), GetShowCount());
                RefreshItems();
            }
        }

        class TranslateItem
        {
            public ItemData itemData;
            public string key;
        }

        int translateCurrent;
        bool isTranslateDone;
        int translateChanged;
        bool translateCanceled;
        void Translate(ItemData baseData, List<TranslateItem> items, bool force)
        {
            EditorCoroutineUtility.StartCoroutine(_Translate(baseData, items, force), this);
        }
        IEnumerator _Translate(ItemData baseData, List<TranslateItem> items, bool force)
        {
            int total = items.Count;
            if (total == 0)
            {
                yield break;
            }

            HashSet<ItemData> changeds = new HashSet<ItemData>();
            translateCurrent = 0;
            isTranslateDone = false;
            translateChanged = 0;
            translateCanceled = false;
            EditorCoroutineUtility.StartCoroutine(TranslateRunner(baseData, items, changeds, force), this);

            while (!isTranslateDone)
            {
                var item = items[translateCurrent];
                if (EditorUtility.DisplayCancelableProgressBar("Translate", $"{baseData.lang} > {item.itemData.lang} [{translateCurrent}/{total}]", translateCurrent / (float)total))
                {
                    translateCanceled = true;
                }
                yield return null;
            }

            if (changeds.Count > 0)
            {
                foreach (var data in changeds)
                {
                    DirtyData(data);
                }
            }
            EditorUtility.ClearProgressBar();
            Debug.Log($"Translate complete total: {total}, chagned: {translateChanged}");
        }

        IEnumerator TranslateRunner(ItemData baseData, List<TranslateItem> items, HashSet<ItemData> changeds, bool force)
        {
            try
            {
                for (int i = 0; i < items.Count; i++)
                {
                    if (translateCanceled)
                        break;
                    var item = items[i];
                    translateCurrent = i;
                    if (!baseData.values.ContainsKey(item.key))
                    {
                        Debug.LogError("base data not contains key: " + item.key);
                        continue;
                    }


                    string srcText = (string)baseData.values[item.key].Value;
                    LocalizationValue value = default;
                    if (!item.itemData.values.TryGetValue(item.key, out value))
                    {
                        if (!force)
                        {
                            continue;
                        }
                        value.TypeName = baseData.values[item.key].TypeName;
                    }

                    ILanguageTranslator translator = EditorLocalizationUtility.GetLanguageTranslator(baseData.lang, item.itemData.lang);
                    if (translator == null)
                    {
                        continue;
                    }
                    AsyncResult<string> asyncResult = new AsyncResult<string>();
                    var task = translator.TranslateLanguage(baseData.lang, item.itemData.lang, srcText);
                    yield return new WaitUntil(() => task.IsCompleted);
                    Debug.Assert(task.IsCompleted);
                    try
                    {
                        string result = task.Result;
                        Debug.Log($"Translate [{translator.GetType().Name}] [{baseData.lang}] => [{item.itemData.lang}]\n" + srcText + "\nResult\n" + result);
                        if (!string.IsNullOrEmpty(result) && !object.Equals(value.Value, result))
                        {
                            value.Value = result;
                            item.itemData.values[item.key] = value;
                            translateChanged++;
                            if (!changeds.Contains(item.itemData))
                                changeds.Add(item.itemData);
                        }

                    }
                    catch (Exception e)
                    {
                        isTranslateDone = true;
                        Debug.LogException(e);
                        yield break;
                    }

                }
            }
            finally
            {
                isTranslateDone = true;
            }
        }

        void GUILangStatus()
        {

            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label($"{"Current".Localization()}[{Localization.CurrentLang}] {"Selected".Localization()} ", GUILayout.ExpandWidth(false));
                var allLangNames = itemDatas.Select(_ => _.lang).OrderBy(_ => _).ToArray();
                PopupLang(allLangNames, expandWidth: false);

                GUILayout.Label($"{"Default".Localization()}[{Localization.DefaultLang}] {"CurrentUICulture".Localization()}[{Thread.CurrentThread.CurrentUICulture.Name}] {"CurrentCulture".Localization()}[{Thread.CurrentThread.CurrentCulture.Name}] {"systemLanguage".Localization()}[{Application.systemLanguage}]");
            }
        }

        void GUIBase()
        {
            int selectedBaseIndex;
            var allLangNames = itemDatas.OrderBy(_ => _.lang).ToList();
            selectedBaseIndex = allLangNames.FindIndex(_ => _.path == baseData.path);
            int newIndex = EditorGUILayout.Popup(GUIContent.none, selectedBaseIndex, allLangNames.Select(_ => _.lang).ToArray(), GUILayout.MaxWidth(EditorGUIUtility.labelWidth));
            if (newIndex != selectedBaseIndex)
            {
                selectedBaseIndex = newIndex;
                if (selectedBaseIndex != -1)
                {
                    string path = allLangNames[selectedBaseIndex].path;
                    EditorLocalizationUtility.BaseLang = Localization.ParseLangNameByFileName(path);
                    LoadBase(path);
                }
            }
        }

        void GUINewItem()
        {
            if (string.IsNullOrEmpty(baseData.path))
                return;

            string error = null;
            using (new GUILayout.HorizontalScope())
            {

                searchKey = GUIUtilityx.SearchTextField(searchKey, GUIContent.none, GUILayout.Width(EditorGUIUtility.labelWidth));

                int typeNameIndex = 0;
                string[] typeNames = EditorLocalizationUtility.GetValueTypeNames().ToArray();
                for (int i = 0; i < typeNames.Length; i++)
                {
                    if (typeNames[i] == newValueTypeName)
                    {
                        typeNameIndex = i;
                        break;
                    }
                }
                float width = (Screen.width - EditorGUIUtility.labelWidth) * 0.3f;
                width = Mathf.Min(width, 150);
                typeNameIndex = EditorGUILayout.Popup(typeNameIndex, typeNames.Select(o => ("type_" + o).Localization()).ToArray(), GUILayout.Width(width));
                newValueTypeName = typeNames[typeNameIndex];

                string newKeyCurrent;
                newKey = GUIUtilityx.DelayedPlaceholderField(newKey ?? string.Empty, out newKeyCurrent, new GUIContent("New Key".Localization())/*, GUILayout.Width(EditorGUIUtility.labelWidth)*/);



                if (!string.IsNullOrEmpty(newKeyCurrent))
                {
                    if (baseData.values.ContainsKey(newKeyCurrent))
                    {
                        error = string.Format("key <{0}> base exists", newKeyCurrent);
                    }
                }

                if (!string.IsNullOrEmpty(newKey) && error == null)
                {

                    var value = new LocalizationValue(newValueTypeName, Localization.GetValueProvider(newValueTypeName).DefaultValue);

                    if (newValueTypeName == "string")
                        value.Value = newKey;

                    baseData.values[newKey] = value;
                    newKey = string.Empty;
                    GUIUtility.keyboardControl = -1;
                    DiryBaseData();
                }
            }

            if (error != null)
            {
                EditorGUILayout.HelpBox(error, MessageType.Error);
            }

        }

        void GUILangKeys()
        {
            using (new GUILayout.VerticalScope(GUILayout.Width(EditorGUIUtility.labelWidth)))
            {
                GUIBase();
                bool oldChanged;
                foreach (var key in keys)
                {
                    bool baseMissingKey = !baseData.values.ContainsKey(key);
                    using (new GUILayout.HorizontalScope(GUILayout.Width(EditorGUIUtility.labelWidth), GUILayout.Height(itemHeight + 2)))
                    {
                        oldChanged = GUI.changed;
                        if (GUILayout.Button("◤", "label", GUILayout.ExpandWidth(false)))
                        {
                            GenericMenu menu = new GenericMenu();

                            if (baseData.values.ContainsKey(key))
                            {
                                menu.AddItem(new GUIContent("Delete".Localization()), false, (o) =>
                               {
                                   string key1 = (string)o;
                                   baseData.values.Remove(key1);
                                   DiryBaseData();
                               }, key);
                            }
                            else
                            {
                                menu.AddDisabledItem(new GUIContent("Delete".Localization()), false);
                            }

                            menu.AddItem(new GUIContent("Translate".Localization()), false, () =>
                            {
                                List<TranslateItem> list = new List<TranslateItem>();
                                foreach (var itemData in itemDatas)
                                {
                                    if (itemData.lang != baseData.lang)
                                    {
                                        list.Add(new TranslateItem() { itemData = itemData, key = key });
                                    }
                                }
                                Translate(baseData, list, false);
                            });
                            menu.AddItem(new GUIContent("Translate All".Localization()), false, () =>
                            {
                                List<TranslateItem> list = new List<TranslateItem>();
                                foreach (var itemData in itemDatas)
                                {
                                    if (itemData.lang != baseData.lang)
                                    {
                                        list.Add(new TranslateItem() { itemData = itemData, key = key });
                                    }
                                }
                                Translate(baseData, list, true);
                            });
                            menu.ShowAsContext();
                            GUIUtility.keyboardControl = 0;
                            GUI.changed = oldChanged;
                        }




                        GUIStyle style;
                        /*
                                                            if (inheritValue)
                                                            {
                                                                style = new GUIStyle("label");
                                                                style.normal.textColor = Color.grey;
                                                                style.hover.textColor = style.normal.textColor;
                                                                style.active.textColor = style.normal.textColor;
                                                            }
                                                            else*/
                        if (baseMissingKey)
                        {
                            style = new GUIStyle("label");
                            style.normal.textColor = Color.red;
                            style.hover.textColor = style.normal.textColor;
                            style.active.textColor = style.normal.textColor;
                        }
                        else
                        {
                            style = "label";
                        }
                        oldChanged = GUI.changed;
                        //  using (new GUILayout.HorizontalScope())
                        {
                            // if (isBaseEditing)
                            //{
                            string newKey = GUIUtilityx.DelayedEditableLabel(key, labelStyle: style);
                            if (newKey != key && !string.IsNullOrEmpty(newKey))
                            {

                                /*   if (!values.ContainsKey(newKey))
                                   {
                                       if (values.ContainsKey(key))
                                       {
                                           values[newKey] = values[key];
                                           values.Remove(key);
                                       }
                                       else
                                       {
                                           if (baseData.values.ContainsKey(key))
                                           {
                                               values[newKey] = baseData.values[key];
                                           }
                                       }
                                   }*/
                                
                                if (baseData.values.ContainsKey(key) && !baseData.values.ContainsKey(newKey))
                                {
                                    foreach (var itemData in itemDatas)
                                    {
                                        if (itemData.values.ContainsKey(key) && !itemData.values.ContainsKey(newKey))
                                        {
                                            itemData.values[newKey] = itemData.values[key];
                                            itemData.values.Remove(key);
                                            DirtyData(itemData);
                                        }
                                    }
                                    GUIUtility.keyboardControl = 0;
                                }

                                //if (baseData.values.ContainsKey(key) && !baseData.values.ContainsKey(newKey))
                                //{
                                //    baseData.values[newKey] = baseData.values[key];
                                //    baseData.values.Remove(key);
                                //    DiryBaseData();

                                //    GUIUtility.keyboardControl = 0;
                                //}

                                //if (isBaseEditing)
                                //{
                                //    ReloadBaseData();
                                //}

                                GUI.changed = true;
                                continue;
                            }
                            //}
                            //else
                            //{
                            //    GUILayout.Label(key, style);
                            //}
                        }



                        Rect labelRect = GUILayoutUtility.GetLastRect();
                        if (labelRect.Contains(Event.current.mousePosition))
                        {
                            if (Event.current.type == EventType.MouseDown)
                            {
                                EditorGUIUtility.systemCopyBuffer = key;
                            }
                        }
                        GUI.changed = oldChanged;

                    }
                }
            }
        }

        static GUIStyle translateButtonStyle;
        static GUIStyle TranslateButtonStyle
        {
            get
            {
                if (translateButtonStyle == null)
                {
                    translateButtonStyle = new GUIStyle("button");
                    //Debug.Log(translateButtonStyle.padding);
                    translateButtonStyle.padding = new RectOffset(4, 5, 3, 2);
                    //translateButtonStyle.margin = new RectOffset();
                    translateButtonStyle.fontSize -= 2;
                }
                return translateButtonStyle;
            }
        }


        void GUIItemNode(ItemData item, string key, LocalizationValue value, int itemIndex)
        {
            ILocalizationValueDrawer drawer;
            drawer = EditorLocalizationUtility.GetValueDrawer(value.TypeName);
            if (drawer == null)
            {
                drawer = EditorLocalizationUtility.GetValueDrawer("string");
            }

            bool ineritValue = !item.values.ContainsKey(key);

            if (!ineritValue)
            {
                value.Value = drawer.OnGUI(value.Value);
                item.values[key] = value;

                if (baseData.lang != item.lang)
                {
                    if (GUILayout.Button("T", TranslateButtonStyle, GUILayout.ExpandWidth(false)))
                    {

                        Translate(baseData, new List<TranslateItem>() { new TranslateItem() { itemData = item, key = key } }, false);

                        //EditorUtility.DisplayProgressBar("Translate", "", 0f);

                        //GoogleTranslator.Process(itemDatas[0].lang, item.lang, (string)baseData.values[key].Value, (b, result) =>
                        //{
                        //    EditorUtility.ClearProgressBar();
                        //    if (b)
                        //    {
                        //        value.Value = result;
                        //        item.values[key] = value;
                        //        DirtyData(item);
                        //    }
                        //});
                    }
                }
            }
            else
            {
                var baseValue = baseData.values[key];
                using (new GUIUtilityx.Scopes.ColorScope(GUI.color * new Color(1, 1, 1, 0.5f)))
                using (var checker = new EditorGUI.ChangeCheckScope())
                {
                    object newValue = drawer.OnGUI(baseValue.Value);
                    if (checker.changed)
                    {
                        var clone = baseValue.Clone();
                        clone.Value = newValue;
                        item.values[key] = clone;
                        GUI.changed = true;
                    }
                }
            }
        }


        [UnityEditor.Callbacks.OnOpenAsset(-1)]
        static bool OnOpenAsset(int instanceID, int line)
        {
            string assetPath;
            assetPath = AssetDatabase.GetAssetPath(instanceID);
            if (!string.IsNullOrEmpty(assetPath))
            {
                if (Localization.IsLocalizationFile(assetPath))
                {
                    Show_Menu();
                    assetPath = assetPath.ReplacePathSeparator();
                    var win = GetWindow<LocalizationEditorWindow>();
                    //if (string.IsNullOrEmpty(win.baseData.path))

                    var item = win.FindByPath(assetPath);
                    if (item == null)
                    {
                        win.SelectBase(Path.GetDirectoryName(assetPath));
                        item = win.FindByPath(assetPath);
                    }
                    if (!win.IsShowWithPath(assetPath))
                        win.ShowIndex(item, win.GetShowCount());
                    return true;
                }
            }
            return false;
        }

        [Serializable]
        class ItemData
        {
            public string lang;
            public string path;
            [NonSerialized]
            public Dictionary<string, LocalizationValue> values;
            public string loadError;
            public int displayIndex = -1;
            [NonSerialized]
            public bool isLoaded;
            public bool isShow;
            public ItemData()
            {
                values = new Dictionary<string, LocalizationValue>();
            }

            public void Load()
            {
                values.Clear();
                loadError = null;
                isLoaded = false;

                if (string.IsNullOrEmpty(path))
                    return;

                if (!File.Exists(path))
                    return;

                try
                {
                    Localization.LoadFromFile(path, values);
                    isLoaded = true;
                }
                catch (Exception ex)
                {
                    loadError = ex.Message;
                }
                /*
                string filename = Path.GetFileName(path);
                if (path.EndsWith("." + Localization.ExtensionName, StringComparison.InvariantCultureIgnoreCase))
                {
                    lang = filename.Substring(0, filename.Length - Localization.ExtensionName.Length - 1);
                }
                else
                {
                    lang = Path.GetFileNameWithoutExtension(filename);
                }*/
                lang = Localization.ParseLangNameByFileName(path);
            }
            public void Save()
            {
                Localization.SaveToXml(path, values);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }

            public override string ToString()
            {
                return $"{lang}";
            }
        }

    }


}