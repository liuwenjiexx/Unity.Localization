using Codice.Client.Common;
using NUnit.Framework.Interfaces;
using SettingsManagement;
using SettingsManagement.Editor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading;
using System.Xml;
using Unity.EditorCoroutines.Editor;
using Unity.Serialization;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.PackageManager;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Localizations;
using UnityEngine.UIElements;

namespace UnityEditor.Localizations
{
    using static Codice.Client.Commands.WkTree.WorkspaceTreeNode;
    using static Codice.Client.Common.Locks.ServerLocks.ForWorkingBranchOnRepoByItem;
    using static Codice.CM.WorkspaceServer.DataStore.WkTree.WriteWorkspaceTree;
    using Localization = UnityEngine.Localizations.Localization;
    using SettingsScope = SettingsManagement.SettingsScope;

    public class LocalizationWindow : EditorWindow
    {

        private bool isDataDirted;
        private LocalizationFile baseData;
        private List<LocalizationFile> files = new();

        private const string Menu_All = "All";
        private const string Menu_New = "New";

        bool isBaseDataDirted;
        IEnumerable<string> keys;


        static bool isLoaded;

        VisualElement content;
        DropdownField selectedLangField;
        Label langStatusLabel;
        TextField newKeyField;
        ToolbarMenu newValueTypeField;
        ToolbarPopupSearchField searchField;
        private string filterStr;
        [SerializeField]
        private string langDir;
        private int listDataVersion;
        private int cacheListDataVersion;
        private ListView listView;
        VisualElement listHeaderContainer;
        List<VisualElement> addedListHeaders = new();
        private HashSet<string> keySet = new();
        private List<string> keyList = new();
        private List<string> filteredKeyList = new();
        [SerializeField]
        private string newKey;
        [SerializeField]
        private string newValueTypeName = "string";
        Label statusLabel;
        LocalizationFile ActiveFile
        {
            get
            {
                if (files == null || files.Count == 0) return null;
                return files[0];
            }
        }
        private void OnEnable()
        {
            if (!isLoaded)
            {
                isLoaded = true;

            }

            using (EditorLocalizationUtility.EditorLocalizationValues.BeginScope())
            {
                titleContent = new GUIContent("Localization".Localization());

                //if (baseData == null)
                //    baseData = new LangFile();

                //baseData.Load();
                if (!string.IsNullOrEmpty(langDir))
                {
                    LoadLangDir(langDir);
                }

                RefreshItems();
                foreach (var item in files)
                {
                    item.Load();
                }

                EditorLocalizationUtility.GetValueDrawer("string");
            }

            Refresh();
        }


        void CreateGUI()
        {
            content = EditorSettingsUtility.LoadUXML(rootVisualElement, EditorSettingsUtility.GetEditorUXMLPath(LocalizationSettings.PackageName, nameof(LocalizationWindow)));
            content.styleSheets.Add(EditorSettingsUtility.LoadUSS(EditorSettingsUtility.GetEditorUSSPath(LocalizationSettings.PackageName, nameof(LocalizationWindow))));
            content.style.flexGrow = 1f;
            selectedLangField = content.Q<DropdownField>("selected_lang");
            langStatusLabel = content.Q<Label>("lang_status");
            var toolbar = content.Q<Toolbar>();
            newKeyField = toolbar.Q<TextField>("new_key");
            newValueTypeField = toolbar.Q<ToolbarMenu>("new_value_type");
            var newFileButton = toolbar.Q<ToolbarButton>("new-file");
            searchField = toolbar.Q<ToolbarPopupSearchField>("search");
            var list = content.Q("list");
            listView = list.Q<ListView>();
            listHeaderContainer = list.Q(className: "list-header-container");
            statusLabel = content.Q<Label>("status");
            var refreshButton = content.Q<ToolbarButton>("refresh");

            content.RegisterCallback<KeyDownEvent>(e =>
            {
                if (e.keyCode == KeyCode.Escape)
                {
                    CancelEdit();
                }
            }, CallbackOptions.TrickleDown);


            selectedLangField.formatListItemCallback = (v) =>
            {
                if (string.IsNullOrEmpty(v))
                {
                    return "Auto".Localization();
                }
                return v;
            };
            selectedLangField.formatSelectedValueCallback = selectedLangField.formatListItemCallback;
            selectedLangField.RegisterValueChangedCallback(e =>
            {
                if (e.newValue != LocalizationSettings.SelectedLang)
                {
                    LocalizationSettings.SelectedLang = e.newValue;
                    Refresh();
                }
            });

            newKeyField.isDelayed = false;
            newKeyField.SetValueWithoutNotify(newKey);
            newKeyField.RegisterValueChangedCallback(e =>
            {
                newKey = e.newValue;

            });

            newKeyField.RegisterCallback<KeyUpEvent>(e =>
            {
                if (e.keyCode == KeyCode.Return || e.character == '\n')
                {
                    e.StopImmediatePropagation();

                    newKey = newKey?.Trim();
                    if (string.IsNullOrEmpty(newKey)) return;
                    if (string.IsNullOrEmpty(newValueTypeName)) return;
                    var activeFile = files[0];
                    if (activeFile == null) return;
                    if (activeFile.HasKey(newKey))
                        return;

                    var value = new LocalizationValue(newValueTypeName, Localization.GetValueProvider(newValueTypeName).DefaultValue);

                    activeFile[newKey] = value;

                    newKey = null;
                    newKeyField.SetValueWithoutNotify(null);
                    activeFile.SaveIfChagne();
                    RefreshList();
                }
            });

            newFileButton.clicked += () =>
            {
                string path = NewLocalizationFile();
                if (!string.IsNullOrEmpty(path))
                {
                    var dir = Path.GetDirectoryName(path);
                    LoadLangDir(dir);
                }
            };

            refreshButton.clicked += () =>
            {
                if (!string.IsNullOrEmpty(langDir))
                {
                    LoadLangDir(langDir);
                }
            };


            newValueTypeField.menu.ClearItems();
            foreach (var valueTypeName in EditorLocalizationUtility.GetValueTypeNames())
            {
                newValueTypeField.menu.AppendAction(valueTypeName, (act) =>
                {
                    this.newValueTypeName = valueTypeName;
                    newValueTypeField.text = valueTypeName;
                }, this.newValueTypeName == valueTypeName ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);

            }

            searchField.SetValueWithoutNotify(filterStr);
            searchField.RegisterValueChangedCallback(e =>
            {
                filterStr = e.newValue;
                RefreshList();
            });

            listView.makeItem = () =>
            {
                Label label;
                TextField text;

                VisualElement view = new VisualElement();
                view.AddToClassList("list-item");

                VisualElement keyContainer = new VisualElement();
                keyContainer.AddToClassList("lang-key");
                //key.AddToClassList("key");
                VisualElement readonlyContainer;
                readonlyContainer = new VisualElement();
                readonlyContainer.name = "readonly";
                label = new Label();
                label.name = "value";
                label.AddToClassList("lang-key-label");
                label.RegisterCallback<PointerUpEvent>(e =>
                {
                    var key = view.userData as string;
                    if (e.button == 0)
                    {
                        EditKey(key);
                    }
                });
                readonlyContainer.Add(label);
                keyContainer.Add(readonlyContainer);


                var editContainer = new VisualElement();
                editContainer.name = "edit";
                text = new TextField();
                text.name = "text";
                text.AddToClassList("lang-key-text");
                text.RegisterValueChangedCallback(e =>
                {
                    //CancelEdit();
                    //CommitEdit();
                    if (editValue != e.newValue)
                    {
                        editValue = e.newValue;
                        editChanged = true;
                    }
                });
                text.RegisterCallback<FocusOutEvent>(e =>
                {
                    var textField = e.currentTarget as TextField;
                    if (EditorApplication.timeSinceStartup > startEdtingTime + 0.05f)
                    {
                        CommitEdit();
                    }
                });
                editContainer.Add(text);
                keyContainer.Add(editContainer);
                view.Add(keyContainer);


                //VisualElement keyEdit = new VisualElement();
                //key.AddToClassList("key-edit");
                //text = new TextField();
                ////text.label = null; 
                //text.RegisterValueChangedCallback(e =>
                //{

                //});
                //key.Add(text);
                //view.Add(key);

                view.AddManipulator(new ContextualMenuManipulator(build =>
                {
                    string key = view.userData as string;

                    build.menu.AppendAction("Translate All".Localization(), (act) =>
                    {
                        List<TranslateItem> list = new List<TranslateItem>();
                        foreach (var file in files)
                        {
                            if (file.lang != ActiveFile.lang)
                            {
                                list.Add(new TranslateItem() { file = file, key = key });
                            }
                        }
                        Translate(ActiveFile, list, false);
                    });


                    build.menu.AppendAction("Delete".Localization(), act =>
                    {
                        //Debug.Log("Delete " + key);
                        DeleteKey(key);
                    });
                }));

                return view;
            };


            listView.bindItem = (view, index) =>
            {
                string key = filteredKeyList[index];
                view.userData = key;
                var keyContainer = view.Children().First();
                var readonlyKeyContainer = keyContainer.Q("readonly");
                var editKeyContainer = keyContainer.Q("edit");
                bool inheritValue;
                LocalizationValue? baseValue = null;
                LocalizationValue value;
                LocalizationFile baseFile;

                keyContainer.RemoveFromClassList("missing-key");

                if (files.Count > 0)
                {
                    baseFile = files[0];
                    if (baseFile.values.TryGetValue(key, out var t))
                    {
                        baseValue = t;
                    }
                }

                if (!baseValue.HasValue)
                {
                    keyContainer.AddToClassList("missing-key");
                }

                if (editIndex == index && isEditKey)
                {
                    readonlyKeyContainer.style.display = DisplayStyle.None;
                    var textField = editKeyContainer.Q<TextField>();

                    //textField.SetValueWithoutNotify(key);

                    textField.SetValueWithoutNotify(editValue);

                    if (editKeyContainer.style.display == DisplayStyle.None)
                    {
                        editKeyContainer.style.display = DisplayStyle.Flex;
                        EditorApplication.delayCall += () =>
                        {
                            textField.Focus();
                            textField.SelectAll();
                        };
                    }
                }
                else
                {
                    readonlyKeyContainer.style.display = DisplayStyle.Flex;
                    editKeyContainer.style.display = DisplayStyle.None;

                    var keyLabel = readonlyKeyContainer.Q<Label>("value");
                    keyLabel.text = key;
                }

                VisualElement valueEle;
                VisualElement labelContainer;
                VisualElement editContainer;
                VisualElement missingKeyContainer;
                bool isMissingKey;

                Label label;
                TextField text;
                bool isEdit;

                for (int i = 0; i < files.Count; i++)
                {
                    int viewIndex = i + 1;
                    var file = files[i];
                    var fileIndex = i;
                    isEdit = false;
                    if (fileIndex == editFileIndex && editIndex == index)
                    {
                        isEdit = true;
                    }

                    if (viewIndex >= view.childCount)
                    {
                        valueEle = new VisualElement();
                        valueEle.AddToClassList("lang-value");

                        labelContainer = new VisualElement();
                        labelContainer.name = "label";
                        label = new Label();
                        label.AddToClassList("lang-value-label");

                        label.RegisterCallback<PointerDownEvent>(e =>
                        {
                            if (e.button == 0)
                            {
                                EditValue(index, key, fileIndex);
                            }
                        });
                        labelContainer.Add(label);
                        valueEle.Add(labelContainer);

                        editContainer = new VisualElement();
                        editContainer.name = "edit";
                        editContainer.AddToClassList("lang-value-edit");
                        text = new TextField();
                        //text.label = null;
                        text.AddToClassList("lang-value-text");

                        text.RegisterValueChangedCallback(e =>
                        {
                            if (editValue != e.newValue)
                            {
                                editValue = e.newValue;
                                editChanged = true;
                            }
                            //CancelEdit();
                        });
                        text.RegisterCallback<FocusInEvent>(e =>
                        {
                            if (EditorApplication.timeSinceStartup < startEdtingTime + 0.1f)
                            {
                                //var textField = e.currentTarget as TextField;
                                //textField.Focus();
                                //textField.SelectAll();
                            }
                        });
                        text.RegisterCallback<FocusOutEvent>(e =>
                        {
                            var textField = e.currentTarget as TextField;
                            if (EditorApplication.timeSinceStartup > startEdtingTime + 0.05f)
                            {
                                //CancelEdit();
                                CommitEdit();
                            }
                            //else
                            //{
                            //    textField.Focus();
                            //    textField.SelectAll();
                            //}
                        });
                        editContainer.Add(text);
                        valueEle.Add(editContainer);
                        /*
                        missingKeyContainer = new VisualElement();
                        missingKeyContainer.name = "missing-key";
                        missingKeyContainer.AddToClassList("lang-value-missing-key");
                        label = new Label();
                        label.text = "(Missing)";
                        missingKeyContainer.Add(label);
                        valueEle.Add(missingKeyContainer);
                        */

                        if (ActiveFile != file)
                        {
                            valueEle.AddManipulator(new ContextualMenuManipulator(build =>
                            {
                                build.menu.AppendAction("Translate".Localization() + $"[{file.lang}]", (act) =>
                                {
                                    List<TranslateItem> list = new List<TranslateItem>();

                                    if (file.lang != ActiveFile.lang)
                                    {
                                        list.Add(new TranslateItem() { file = file, key = key });
                                    }
                                    Translate(ActiveFile, list, false);
                                });
                            }));
                        }

                        view.Add(valueEle);
                    }
                    else
                    {
                        valueEle = view.Children().ElementAt(viewIndex);
                    }
                    valueEle.style.display = DisplayStyle.Flex;
                    labelContainer = valueEle.Q("label");
                    editContainer = valueEle.Q("edit");
                    // missingKeyContainer = valueEle.Q("missing-key");
                    Label labelField = labelContainer.Q<Label>();
                    TextField textField = editContainer.Q<TextField>();

                    valueEle.RemoveFromClassList("lang-value-missing-key");
                    valueEle.RemoveFromClassList("lang-value-inherit");

                    isMissingKey = false;
                    inheritValue = !file.values.ContainsKey(key);
                    if (isEdit)
                    {
                        textField.SetValueWithoutNotify(editValue);
                    }
                    labelField.text = null;
                    if (file.values.TryGetValue(key, out value))
                    {
                        //if (isEdit)
                        //    textField.SetValueWithoutNotify(value.StringValue);
                        //else
                        labelField.text = value.StringValue;
                        inheritValue = false;
                    }
                    else
                    {
                        inheritValue = true;
                        if (baseValue.HasValue)
                        {
                            //if (isEdit)
                            //    textField.SetValueWithoutNotify(baseValue.Value.StringValue);
                            //else
                            labelField.text = baseValue.Value.StringValue;
                        }
                    }

                    if (!baseValue.HasValue)
                    {
                        isMissingKey = true;
                    }


                    if (inheritValue)
                    {
                        valueEle.AddToClassList("lang-value-inherit");
                    }

                    if (isEdit)
                    {
                        labelContainer.style.display = DisplayStyle.None;
                        if (editContainer.style.display == DisplayStyle.None)
                        {
                            editContainer.style.display = DisplayStyle.Flex;
                            EditorApplication.delayCall += () =>
                            {
                                textField.Focus();
                                textField.SelectAll();
                            };
                        }
                    }
                    else
                    {

                        labelContainer.style.display = DisplayStyle.Flex;
                        editContainer.style.display = DisplayStyle.None;

                        if (isMissingKey)
                        {
                            valueEle.AddToClassList("lang-value-missing-key");
                        }

                    }



                }


                for (int i = files.Count + 1; i < view.childCount; i++)
                {
                    var c = view.Children().ElementAt(i);
                    c.style.display = DisplayStyle.None;
                }
            };
            listView.itemsSource = filteredKeyList;

            Refresh();
        }

        [NonSerialized]
        private int editFileIndex = -1;
        private int editIndex;
        private bool isEditKey;
        private string editKey;
        private string editValue;
        private bool editChanged;
        private double startEdtingTime;

        void EditValue(string key, int fileIndex)
        {
            if (key == null) return;
            int index = filteredKeyList.IndexOf(key);
            if (index < 0 || index >= filteredKeyList.Count) return;
            EditValue(index, key, fileIndex);
        }
        void EditValue(int index, string key, int fileIndex)
        {
            key = key?.Trim();
            if (index < 0 || index >= filteredKeyList.Count || fileIndex < 0 || fileIndex >= files.Count) return;
            if (editIndex >= 0)
            {
                CommitEdit();
            }
            var langFile = files[fileIndex];
            if (langFile == null)
                return;

            if (langFile.values.TryGetValue(key, out var v))
            {
                editValue = v.StringValue;
            }
            else if (ActiveFile.TryGetValue(key, out v))
            {
                editValue = v.StringValue;
            }
            else
            {
                editValue = null;
            }

            editFileIndex = fileIndex;
            editIndex = index;
            editKey = key;
            editChanged = false;
            startEdtingTime = EditorApplication.timeSinceStartup;
            listView.RefreshItem(editIndex);
        }
        void EditKey(string key)
        {
            if (key == null) return;
            int index = filteredKeyList.IndexOf(key);
            if (index < 0) return;
            if (editIndex >= 0)
            {
                CancelEdit();
            }

            editIndex = index;
            isEditKey = true;
            editKey = key;
            editValue = key;
            editChanged = false;
            startEdtingTime = EditorApplication.timeSinceStartup;
            listView.RefreshItem(editIndex);
        }

        void CommitEdit()
        {
            if (editIndex < 0)
                return;
            //Debug.Log("CommitEdit");
            int index = editIndex;
            bool refreshItem = true;
            if (editChanged)
            {
                if (isEditKey)
                {
                    string originKey = filteredKeyList[index];
                    string newKey = editValue?.Trim();
                    if (originKey != newKey && !string.IsNullOrEmpty(newKey))
                    {
                        if (RenameKey(originKey, newKey))
                        {
                            SaveIfChange();
                            refreshItem = false;
                        }
                    }
                }
                else
                {

                    var editFile = files[editFileIndex];

                    if (!string.IsNullOrEmpty(editValue))
                    {
                        LocalizationValue value;
                        value = new LocalizationValue(FindValueTypeName(editKey, EditorLocalizationUtility.DefaultValueTypeName), editValue);
                        editFile.SetValue(editKey, value);
                    }
                    else
                    {
                        editFile.RemoveKey(editKey);
                    }
                }
            }


            editIndex = -1;
            editFileIndex = -1;
            isEditKey = false;
            editKey = null;
            editValue = null;

            if (/*refreshItem && */index >= 0 && index < filteredKeyList.Count)
            {
                listView.RefreshItem(index);
            }
            //else
            //{
            //    RefreshList();
            //}

        }

        void CancelEdit()
        {
            if (editIndex < 0)
                return;
            //Debug.Log("CancelEdit");
            int index = editIndex;
            editIndex = -1;
            editFileIndex = -1;
            isEditKey = false;
            editValue = null;
            if (index >= 0)
            {
                listView.RefreshItem(index);
            }
        }



        [MenuItem(EditorLocalizationUtility.MenuPrefix + "Localization", priority = EditorLocalizationUtility.MenuPriority)]
        public static void ShowWindow()
        {
            GetWindow<LocalizationWindow>().Show();
        }

        public string FindValueTypeName(string key, string defaultTypeName)
        {
            foreach (var file in files)
            {
                if (file.TryGetValue(key, out var v))
                {
                    if (!string.IsNullOrEmpty(v.TypeName))
                        return v.TypeName;
                }
            }
            return defaultTypeName;
        }

        void Refresh()
        {
            if (langStatusLabel == null) return;
            langStatusLabel.text = GetLangStatusText();
            var allLangNames = files.Select(_ => _.lang).OrderBy(_ => _).ToArray();
            selectedLangField.choices.Clear();
            selectedLangField.choices.Add(null);
            foreach (var lang in allLangNames)
            {
                selectedLangField.choices.Add(lang);
            }

            if (string.IsNullOrEmpty(newValueTypeName))
            {
                newValueTypeName = EditorLocalizationUtility.GetValueTypeNames().FirstOrDefault();
            }
            newValueTypeField.text = newValueTypeName;


            RefreshList();
        }

        public bool IsLoadedFile(string path)
        {
            return files.Where(o => o.path == path).Count() > 0;
        }

        LocalizationFile LoadLangFile(string path)
        {
            if (IsLoadedFile(path))
                return null;
            LocalizationFile item = new LocalizationFile() { path = path };
            item.lang = Localization.ParseLangNameByFileName(path);
            item.Load();
            files.Add(item);
            return item;
        }

        LocalizationFile FindByPath(string path)
        {
            foreach (var item in files)
            {
                if (item.path == path)
                    return item;
            }
            return null;
        }

        public void RemoveItem(int itemIndex)
        {
            if (itemIndex >= files.Count)
                return;

            files.RemoveAt(itemIndex);
        }

        public void LoadLangDir(string dir)
        {
            langDir = dir?.ReplacePathSeparator();

            files = null;


            HashSet<string> allLangPaths = new();
            if (!string.IsNullOrEmpty(dir))
            {
                files = UserSettings.GetLangDataFiles(langDir);

                foreach (var file in Localization.GetLocalizationFiles(dir))
                {
                    //item.lang = Localization.ParseLangNameByFileName(file);
                    //item.path = file.ReplacePathSeparator();
                    string path = file.ReplacePathSeparator();
                    allLangPaths.Add(path);
                    var langFile = files.FirstOrDefault(_ => _.path == path);
                    if (langFile == null)
                    {
                        langFile = new LocalizationFile(path);
                        int index = -1;
                        for (int i = files.Count - 1; i >= 1; i--)
                        {
                            int n = string.Compare(files[i].lang, langFile.lang);
                            if (string.Compare(files[i].lang, langFile.lang) <= 0)
                            {
                                index = i + 1;
                                break;
                            }
                            index = i;
                        }
                        if (index >= 0 && index < files.Count)
                        {
                            files.Insert(index, langFile);
                        }
                        else
                        {
                            files.Add(langFile);
                        }
                        UserSettings.Diry();
                    }
                }
            }

            if (files == null)
                files = new();


            for (var i = files.Count - 1; i >= 0; i--)
            {
                var langFile = files[i];
                if (string.IsNullOrEmpty(langFile.path) || !allLangPaths.Contains(langFile.path))
                {
                    files.RemoveAt(i);
                    UserSettings.Diry();
                }
            }


            for (var i = 0; i < files.Count; i++)
            {
                var langFile = files[i];

                //if (baseIndex == i)
                //{
                //    langFile.isBase = true;
                //}
                //else
                //{
                //    langFile.isBase = false;
                //}

                if (!langFile.isLoaded)
                {
                    langFile.Load();
                }

            }

            /*
            foreach (var path in allLangPaths)
            {
                if (IsLoadedFile(path))
                    continue;
                var item = LoadLangFile(path);
                //if (baseData.path == path)
                //{
                //    ShowIndex(item, 0);
                //}
            }
            */

            //UpdateBaseIndex();
            SortFileIndex();


            RefreshItems();
            RefreshList();
        }

        void UpdateBaseIndex()
        {

            int baseIndex = FindBaesIndex();
            if (baseIndex == -1 && files.Count > 0)
            {
                baseIndex = 0;
            }
            if (baseIndex >= 0 && baseIndex != 0)
            {
                var tmp = files[0];
                files[0] = files[baseIndex];
                files[baseIndex] = tmp;
                UserSettings.Diry();
            }
        }

        void SortFileIndex()
        {
            if (files.Count <= 0) return;

            UpdateBaseIndex();

            var baseFile = files[0];
            var list = files.ToList();
            files.Clear();
            var items = list.OrderBy(_ => _ == baseFile ? 0 : 1)
                .ThenBy(_ => _.isFiexd ? 0 : 1)
                .ThenBy(_ => _.lang);

            files.AddRange(items);

            UserSettings.Diry();

        }

        static string FindBaseFileName(IEnumerable<string> files)
        {
            string baseFile = null;
            if (!string.IsNullOrEmpty(EditorLocalizationUtility.BaseLang))
            {
                baseFile = files.FirstOrDefault(_ => Localization.ParseLangNameByFileName(_) == EditorLocalizationUtility.BaseLang);
            }
            if (baseFile == null)
            {
                baseFile = files.FirstOrDefault(_ => Localization.ParseLangNameByFileName(_) == "en");
            }
            if (baseFile == null)
            {
                baseFile = files.FirstOrDefault(_ => Localization.ParseLangNameByFileName(_) == "zh");
            }
            return baseFile;
        }

        int FindBaesIndex()
        {
            LocalizationFile current = null;

            string baseFile = FindBaseFileName(files.Select(_ => _.path));

            if (baseFile == null) return -1;
            current = FindByPath(baseFile);
            if (current == null) return -1;
            return files.IndexOf(current);
        }

        void RefreshItems()
        {
            //itemDatas.Sort((a, b) => string.Compare(a.lang, b.lang));
            //if (!string.IsNullOrEmpty(EditorLocalizationUtility.FirstLang))
            //{
            //    itemDatas.Sort((a, b) => (a.lang == EditorLocalizationUtility.FirstLang ? 0 : 1) - (b.lang == EditorLocalizationUtility.FirstLang ? 0 : 1));
            //}
        }

        void RefreshList()
        {
            if (listView == null) return;
            cacheListDataVersion = listDataVersion;

            keySet.Clear();
            keyList.Clear();
            filteredKeyList.Clear();
            if (files == null) files = new();
            if (files.Count > 0)
            {
                var baseData = files[0];
                for (int i = 0; i < files.Count; i++)
                {
                    foreach (var key in files[i].values.Keys)
                    {
                        keySet.Add(key);
                    }
                }

                keyList.AddRange(keySet.OrderBy(o => o));
            }

            filteredKeyList.AddRange(keyList.Where(_ => IsFilterKey(_)));

            for (int i = 0; i < files.Count; i++)
            {
                int dataIndex = i;
                int viewIndex = i + 1;
                var file = files[i];
                VisualElement header;
                Label label;
                if (viewIndex >= listHeaderContainer.childCount)
                {
                    header = new VisualElement();
                    header.AddToClassList("list-header");

                    label = new Label();
                    label.AddToClassList("list-header-fixed");
                    label.text = "◆";
                    header.Add(label);

                    label = new Label();
                    label.AddToClassList("list-header-label");
                    header.Add(label);


                    header.AddManipulator(new ContextualMenuManipulator(build =>
                    {
                        var file = files[dataIndex];
                        build.menu.AppendAction("Sort", act =>
                        {
                            SortFileIndex();

                            RefreshList();
                        });

                        build.menu.AppendAction("Fixed", act =>
                        {
                            file.isFiexd = !file.isFiexd;
                            UserSettings.Diry();
                            SortFileIndex();
                            RefreshList();

                            //int fiexdIndex = -1;
                            //for (int i = files.Count - 1; i >= 1; i--)
                            //{
                            //    if (files[i].isFiexd)
                            //    {
                            //        fiexdIndex = i;
                            //        break;
                            //    }
                            //}

                            //if (fiexdIndex >= 0 && fiexdIndex < files.Count)
                            //{
                            //    fiexdIndex++;
                            //}
                            //else
                            //{
                            //    fiexdIndex = 1;
                            //}

                            //if (dataIndex != fiexdIndex && fiexdIndex < files.Count)
                            //{
                            //    var tmp = files[dataIndex];
                            //    files[dataIndex] = files[fiexdIndex];
                            //    files[fiexdIndex] = tmp;
                            //    UserSettings.Diry();
                            //}

                        }, file.isFiexd ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);

                        int minFixed = -1, maxFixed = -1;

                        for (int i = 1; i < files.Count; i++)
                        {
                            var f = files[i];
                            if (f.isFiexd)
                            {
                                if (minFixed == -1 || i < minFixed)
                                    minFixed = i;
                                if (maxFixed == -1 || i > maxFixed)
                                    maxFixed = i;
                            }
                        }
                        /*
                        build.menu.AppendAction("←", act =>
                        {
                            if (dataIndex > 1)
                            {
                                int newIndex = dataIndex - 1;
                                if (dataIndex != newIndex)
                                {
                                    files[dataIndex] = files[newIndex];
                                    files[newIndex] = file;
                                    file.isFiexd = true;
                                    UserSettings.Diry();
                                    SortFileIndex();
                                    RefreshList();
                                }
                            }
                        }, file.isFiexd && dataIndex > 1 && dataIndex > minFixed ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

                        build.menu.AppendAction("→", act =>
                        {
                            if (dataIndex < files.Count - 1)
                            {
                                int newIndex = dataIndex - 1;
                                if (dataIndex != newIndex)
                                {
                                    files[dataIndex] = files[newIndex];
                                    files[newIndex] = file;
                                    file.isFiexd = true;
                                    UserSettings.Diry();
                                    SortFileIndex();
                                    RefreshList();
                                }
                            }
                        }, file.isFiexd && dataIndex < maxFixed ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
                        */
                    }));


                    listHeaderContainer.Add(header);
                }
                else
                {
                    header = listHeaderContainer.Children().ElementAt(viewIndex);
                }
                header.style.display = DisplayStyle.Flex;
                label = header.Q<Label>(className: "list-header-label");
                var fiexd = header.Q(className: "list-header-fixed");
                label.text = file.lang;
                fiexd.style.display = file.isFiexd ? DisplayStyle.Flex : DisplayStyle.None;
            }

            for (int i = files.Count + 1; i < listHeaderContainer.childCount; i++)
            {
                var c = listHeaderContainer.Children().ElementAt(i);
                c.style.display = DisplayStyle.None;
            }

            listView.RefreshItems();

        }

        void ShowIndex(LocalizationFile item, int index)
        {
            if (item.displayIndex == index)
                return;

            if (item.displayIndex >= 0)
            {
                foreach (var it in files)
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
                foreach (var it in files)
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
            foreach (var it in files)
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
            foreach (var it in files)
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
            foreach (var it in files)
            {
                if (it.displayIndex >= 0)
                {
                    count++;
                }
            }
            return count;
        }
        int FindIndexWithPath(string path)
        {
            int index = -1;

            for (int i = 0; i < files.Count; i++)
            {
                if (string.Equals(files[i].path, path, StringComparison.InvariantCultureIgnoreCase))
                {
                    index = i;
                    break;
                }
            }
            return index;
        }
        void LoadBase(string path)
        {
            baseData.path = path;
            baseData.lang = Localization.ParseLangNameByFileName(path);
            baseData.Load();
        }
        void LoadAll()
        {
            foreach (var itemData in files)
            {
                itemData.Load();
            }
        }


        void Load(string path)
        {
            foreach (var item in files)
            {
                if (item.path == path)
                {
                    item.Load();
                    break;
                }
            }
        }



        //void DirtyData(LangFile item)
        //{
        //    //Debug.Log("dirty");
        //    isDataDirted = true;
        //    GUIUtility.keyboardControl = -1;

        //    item.SaveIfChagne();
        //    if (item.path == baseData.path)
        //    {
        //        baseData.Load();
        //    }
        //}

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

        bool IsFilterKey(string key)
        {
            if (string.IsNullOrEmpty(filterStr))
                return true;

            return key.IndexOf(filterStr, StringComparison.InvariantCultureIgnoreCase) >= 0;
        }

        string NewLocalizationFile()
        {
            string dir = "Assets";
            if (!string.IsNullOrEmpty(langDir))
            {
                dir = langDir;
            }

            string path = EditorUtility.SaveFilePanel("Create New Localization".Localization(), dir, "", Localization.ExtensionName);
            if (string.IsNullOrEmpty(path))
                return null;
            string tmp;
            if (path.ToRelativePath(".", out tmp))
            {
                path = tmp;
            }
            path = CreateNewFile(path);
            //if (itemDatas.Count == 0)
            //{
            //    LoadLangDir(dir);
            //}
            //ShowIndex(LoadLangFile(path), GetShowCount());
            //RefreshItems();

            return path;
        }

        class TranslateItem
        {
            public LocalizationFile file;
            public string key;
        }

        int translateCurrent;
        bool isTranslateDone;
        int translateChanged;
        bool translateCanceled;
        void Translate(LocalizationFile baseData, List<TranslateItem> items, bool force)
        {
            EditorCoroutineUtility.StartCoroutine(_Translate(baseData, items, force), this);
        }
        IEnumerator _Translate(LocalizationFile baseData, List<TranslateItem> items, bool force)
        {
            int total = items.Count;
            if (total == 0)
            {
                yield break;
            }

            //HashSet<LangFile> changeds = new HashSet<LangFile>();
            translateCurrent = 0;
            isTranslateDone = false;
            //translateChanged = 0;
            translateCanceled = false;
            EditorCoroutineUtility.StartCoroutine(TranslateRunner(baseData, items,/* changeds,*/ force), this);

            try
            {
                while (!isTranslateDone)
                {
                    var item = items[translateCurrent];
                    if (EditorUtility.DisplayCancelableProgressBar("Translate", $"{baseData.lang} > {item.file.lang} [{translateCurrent}/{total}]", translateCurrent / (float)total))
                    {
                        translateCanceled = true;
                    }
                    yield return null;
                }


                if (SaveIfChange())
                {
                    RefreshList();
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            Debug.Log($"Translate complete total: {total}");
        }

        IEnumerator TranslateRunner(LocalizationFile baseData, List<TranslateItem> items/*, HashSet<LangFile> changeds*/, bool force)
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
                    if (!item.file.values.TryGetValue(item.key, out value))
                    {
                        if (!force)
                        {
                            continue;
                        }
                        value.TypeName = baseData.values[item.key].TypeName;
                    }

                    ILanguageTranslator translator = EditorLocalizationUtility.GetLanguageTranslator(baseData.lang, item.file.lang);
                    if (translator == null)
                    {
                        continue;
                    }
                    AsyncResult<string> asyncResult = new AsyncResult<string>();
                    var task = translator.TranslateLanguage(baseData.lang, item.file.lang, srcText);
                    yield return new WaitUntil(() => task.IsCompleted);
                    Debug.Assert(task.IsCompleted);
                    try
                    {
                        string result = task.Result;
                        Debug.Log($"Translate [{translator.GetType().Name}] [{baseData.lang}] => [{item.file.lang}]\n" + srcText + "\nResult\n" + result);
                        if (!string.IsNullOrEmpty(result) && !object.Equals(value.Value, result))
                        {
                            value.Value = result;
                            item.file[item.key] = value;
                            translateChanged++;
                            //if (!changeds.Contains(item.itemData))
                            //    changeds.Add(item.itemData);
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

        string GetLangStatusText()
        {

            StringBuilder builder = new StringBuilder();

            builder.Append($"{"Current".Localization()}={Localization.CurrentLang}");
            //var allLangNames = itemDatas.Select(_ => _.lang).OrderBy(_ => _).ToArray();
            //PopupLang(allLangNames, expandWidth: false);

            builder.Append($" {"Default".Localization()}={Localization.DefaultLang} {"CurrentUICulture".Localization()}={Thread.CurrentThread.CurrentUICulture.Name} {"CurrentCulture".Localization()}={Thread.CurrentThread.CurrentCulture.Name} {"systemLanguage".Localization()}={Application.systemLanguage}");
            return builder.ToString();
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
                    ShowWindow();
                    assetPath = assetPath.ReplacePathSeparator();
                    var win = GetWindow<LocalizationWindow>();
                    //if (string.IsNullOrEmpty(win.baseData.path))

                    var item = win.FindByPath(assetPath);
                    if (item == null)
                    {
                        win.LoadLangDir(Path.GetDirectoryName(assetPath));
                        item = win.FindByPath(assetPath);
                    }
                    if (!win.IsShowWithPath(assetPath))
                        win.ShowIndex(item, win.GetShowCount());
                     
                    return true;
                }
            }
            return false;
        }

        public bool RenameKey(string key, string newKey)
        {
            newKey = newKey?.Trim();
            if (string.IsNullOrEmpty(newKey))
            {
                Debug.LogError("Empty key");
                return false;
            }

            foreach (var it in files)
            {
                if (it.values.ContainsKey(newKey))
                {
                    Debug.LogError($"Already exits new key, lang: [{it.lang}], key: [{newKey}]");
                    return false;
                }
            }

            bool changed = false;
            foreach (var itemData in files)
            {
                if (itemData.RenameKey(key, newKey))
                {
                    changed = true;
                }

            }

            return changed;
        }

        public bool DeleteKey(string key)
        {
            key = key?.Trim();
            if (string.IsNullOrEmpty(key))
            {
                Debug.LogError("Empty key");
                return false;
            }
            bool changed = false;
            foreach (var itemData in files)
            {
                if (itemData.RemoveKey(key))
                {
                    changed = true;
                }

            }

            return changed;
        }

        public bool SaveIfChange()
        {
            bool chagned = false;
            foreach (var itemData in files)
            {
                chagned |= itemData.SaveIfChagne();
            }
            return chagned;
        }

        [MenuItem("Test/ sort")]
        public static void TestSort()
        {
            //Debug.Log(string.Compare("d", "a"));
            //Debug.Log(string.Compare("a", "d"));
            //Debug.Log(string.Compare("e5", "e4"));
            //Debug.Log(string.Compare("en", "e4"));
            //Debug.Log(string.Compare("en", "e1"));
            //Debug.Log(string.Compare("d", "e4"));
            //Debug.Log(string.Compare("f", "e4"));


            Debug.Log(string.Compare("zh-TW", "e1"));
            Debug.Log(string.Compare("en", "e1"));
        }

        /*
        [MenuItem("Test/ json")]
        public static void TestJson()
        {
            //var s = JsonUtility.ToJson(langDataFiles.Value);
            List<LocalizationFile> list = new List<LocalizationFile>();
            list.Add(new LocalizationFile());
            var r1 = JsonUtility.ToJson(list);
            var r = JsonUtility.ToJson(new LocalizationFile());
            Debug.Log(r1);
            List<string> lists = new List<string>();
            lists.Add("A");
            Debug.Log(" List<string>: " + JsonUtility.ToJson(lists));


            string[] strArray = new string[] { "A" };
            Debug.Log("string[]: " + JsonUtility.ToJson(strArray));

            Debug.Log(ValueWrapper<string[]>.Serialize(strArray));
            Debug.Log(ValueWrapper<List<string>>.Serialize(lists));



        }
        */

        private int cacheFileVersion;

        private void Update()
        {
            if (ActiveFile == null) return;
            string msg = null, error = null;
            if (!string.IsNullOrEmpty(newKey))
            {
                if (ActiveFile.HasKey(newKey))
                {
                    error = string.Format("key <{0}> base exists", newKey);
                }
            }
            if (!string.IsNullOrEmpty(error))
            {
                statusLabel.text = "<color=red>" + error + "</color>";
            }
            else
            {
                statusLabel.text = msg;
            }


            if (files != null)
            {
                bool changed = false;
                foreach (var file in files)
                {
                    if (file.LastLoadTimeUtc < file.LastWriteTimeUtc)
                    {
                        file.Load();
                        changed = true;
                    }
                }
                //if (changed)
                //{
                //    RefreshList();
                //}
            }


            listDataVersion = 0;
            foreach (var item in files)
            {
                if (item.isLoaded)
                {
                    if (item.ExistsFile)
                    {
                        listDataVersion = HashCode.Combine(listDataVersion, item.version);
                    }
                    else
                    {
                        listDataVersion = HashCode.Combine(listDataVersion, 0);
                    }
                }
            }
            if (cacheListDataVersion != listDataVersion)
            {
                RefreshList();
            }
        }


        class UserSettings
        {

            private static Settings settings;

            private static Settings Settings
                => settings ??= new Settings(
                    new PackageSettingRepository(LocalizationSettings.PackageName, SettingsScope.EditorUser, name: nameof(LocalizationWindow)));


            private static Setting<SerializableDictionary<string, ValueWrapper<List<LocalizationFile>>>> langDataFiles = new(Settings, "LangDataFiles", null, SettingsScope.EditorUser);
            public static List<LocalizationFile> GetLangDataFiles(string dir)
            {
                dir = dir?.Trim().ReplacePathSeparator();
                if (string.IsNullOrEmpty(dir)) return null;
                var dic = langDataFiles.Value;
                if (dic == null)
                {
                    dic = new();
                    langDataFiles.Value = dic;
                }
                List<LocalizationFile> files;
                if (!dic.TryGetValue(dir.ToLower(), out var v) || v?.value == null)
                {
                    files = new();
                    dic[dir.ToLower()] = new() { value = files };
                    langDataFiles.SetDiry(true);
                }
                files = v.value;
                return files;
            }

            //public static LangFile GetLangFile(string file)
            //{

            //}

            public static void Diry()
            {

                langDataFiles.SetDiry(true);
            }



        }

    }

    //解决数组序列化
    [Serializable]
    sealed class ValueWrapper<T>
    {
        //#if PRETTY_PRINT_JSON
        //const bool PrettyPrintJson = true;
        //#else
        const bool PrettyPrintJson = false;
        //#endif

        [SerializeField]
        public T value;

        public static string Serialize(T value)
        {
            var obj = new ValueWrapper<T>() { value = value };

            return JsonUtility.ToJson(obj, PrettyPrintJson);
        }

        public static T Deserialize(string json)
        {
            var value = new ValueWrapper<T>();
            JsonUtility.FromJsonOverwrite(json, value);
            return value.value;
        }

        public static T Copy(T value)
        {
            if (typeof(ValueType).IsAssignableFrom(typeof(T)))
                return value;
            var str = Serialize(value);
            return Deserialize(str);
        }
    }
}