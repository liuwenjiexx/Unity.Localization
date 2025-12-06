using SettingsManagement;
using System;
using UnityEngine;
using UnityEngine.Localizations;

public class LocalizationSettings
{

    private static Settings settings;

    private static Settings Settings
        => settings ??= new Settings(
            new PackageSettingRepository(PackageName, SettingsScope.RuntimeProject),
            new PackageSettingRepository(PackageName, SettingsScope.RuntimeUser));

    public static string PackageName => SettingsManagement.SettingsUtility.GetPackageName(typeof(LocalizationSettings).Assembly);

    private static Setting<SerializableType> customLoaderType = new(Settings, nameof(CustomLoaderType), null, SettingsScope.RuntimeProject);

    static SerializableType CustomLoaderTypeSetting
    {
        get
        {
            SerializableType type = customLoaderType.Value;
            if (type == null)
            {
                type = new SerializableType();
                customLoaderType.Value = type;
            }

            return type;
        }
    }

    public static Type CustomLoaderType
    {
        get
        {

            return CustomLoaderTypeSetting.Type;
        }
        set
        {
            if (CustomLoaderType != value)
            {
                CustomLoaderTypeSetting.Type = value;
                customLoaderType.SetValue(CustomLoaderTypeSetting, true);
            }
        }
    }

    public static string CustomLoaderTypeName
    {
        get
        {
            return CustomLoaderTypeSetting.TypeName;
        }
        set
        {
            if (CustomLoaderTypeName != value)
            {
                CustomLoaderTypeSetting.TypeName = value; 
                customLoaderType.SetDiry();
            }
        }
    }

    private static Setting<bool> updateOnLanguageChagned = new(Settings, nameof(UpdateOnLanguageChagned), true, SettingsScope.RuntimeProject);
    public static bool UpdateOnLanguageChagned
    {
        get => updateOnLanguageChagned.Value;
        set => updateOnLanguageChagned.SetValueWithCheck(value, true);
    }

    private static Setting<string> resourcesPath = new(Settings, nameof(ResourcesPath), "Localization", SettingsScope.RuntimeProject);
    public static string ResourcesPath
    {
        get => resourcesPath.Value;
        set => resourcesPath.SetValueWithCheck(value, true);
    }
    
    [HideInInspector]
    private static Setting<string> selectedLang = new(Settings, nameof(SelectedLang), null, SettingsScope.RuntimeUser);

    public static string SelectedLang
    {
        get => selectedLang.Value;
        set
        {
            if (selectedLang.SetValueWithCheck(value, true))
            {
                Localization.OnSelectedLangChanged();
                if (Localization.IsInitialized)
                {
                    string lang = Localization.CurrentLang;

                    Localization.currentLang = Localization.GetSupportedLang();
                    if (Localization.currentLang != lang)
                    {
                        Localization.LoadLang(Localization.currentLang);
                    }
                }
            }
        }
    }


}
