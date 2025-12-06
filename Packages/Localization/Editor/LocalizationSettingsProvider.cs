using SettingsManagement;
using SettingsManagement.Editor;
using System;
using System.Configuration;
using UnityEngine;
using UnityEngine.UIElements;
namespace UnityEditor.Localizations
{
    public class LocalizationSettingsProvider : SettingsProvider
    {
        private const string SettingsPath = "Tool/Localization";

        public LocalizationSettingsProvider()
            : base(SettingsPath, UnityEditor.SettingsScope.Project)
        {
        }

        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider()
        {
            var provider = new LocalizationSettingsProvider();
            provider.keywords = new string[] { "localization" };
            return provider;
        }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            VisualElement windowContent;

            rootElement.styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(EditorSettingsUtility.SettingsUSSPath));

            StyleSheet styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(EditorSettingsUtility.GetEditorUSSPath(LocalizationSettings.PackageName, "Settings"));
            if (styleSheet != null)
                rootElement.styleSheets.Add(styleSheet);

            windowContent = EditorSettingsUtility.CreateSettingsWindow(rootElement, "Localization");
            windowContent = EditorSettingsUtility.LoadUXML(windowContent, EditorSettingsUtility.GetEditorUXMLPath(LocalizationSettings.PackageName, "LocalizationSettings"));

            EditorSettingsUtility.CreateSettingView(windowContent, typeof(LocalizationSettings));

            var loaderTypeField = rootElement.Q<TextField>("loader_type");
            loaderTypeField.isDelayed = true;
            loaderTypeField.RegisterValueChangedCallback(e =>
            {
                string newTypeName = e.newValue?.Trim();

                if (LocalizationSettings.LoaderTypeName != newTypeName)
                {
                    LocalizationSettings.LoaderTypeName = newTypeName; 
                }

            });
            loaderTypeField.SetValueWithoutNotify(LocalizationSettings.LoaderTypeName);
        }
    }
}