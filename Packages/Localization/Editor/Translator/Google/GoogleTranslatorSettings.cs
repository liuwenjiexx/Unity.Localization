using SettingsManagement;
using System.Collections.Generic;
using Unity.Serialization;
using UnityEditor.Localizations;
using UnityEngine;
namespace UnityEditor.Localizations
{
    using SettingsScope = SettingsManagement.SettingsScope;

    public static class GoogleTranslatorSettings
    {

        private static Settings settings;

        private static Settings Settings
            => settings ??= new Settings(
                new PackageSettingRepository(LocalizationSettings.PackageName, SettingsScope.EditorUser, name: "GoogleTranslator"));

        private static Setting<GoogleTranslatorApiVersion> apiVersion = new(Settings, "ApiVersion", GoogleTranslatorApiVersion.Default, SettingsScope.EditorUser);

        public static GoogleTranslatorApiVersion ApiVersion
        {
            get => apiVersion.Value;
            set => apiVersion.Value = value;
        }

    }
    public enum GoogleTranslatorApiVersion
    {
        Default=0,
        V1 = 1,
    }
}