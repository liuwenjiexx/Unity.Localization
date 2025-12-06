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

    private static Setting<SerializableType> loaderType = new(Settings, nameof(LoaderType), null, SettingsScope.RuntimeProject);

    static SerializableType LoaderTypeSetting
    {
        get
        {
            SerializableType type = loaderType.Value;
            if (type == null)
            {
                type = new SerializableType();
                loaderType.Value = type;
            }

            return type;
        }
    }

    public static Type LoaderType
    {
        get
        {

            return LoaderTypeSetting.Type;
        }
        set
        {
            if (LoaderType != value)
            {
                LoaderTypeSetting.Type = value;
                loaderType.SetValue(LoaderTypeSetting, true);
            }
        }
    }

    public static string LoaderTypeName
    {
        get
        {
            return LoaderTypeSetting.TypeName;
        }
        set
        {
            if (LoaderTypeName != value)
            {
                LoaderTypeSetting.TypeName = value;
                //loaderType.SetValue(LoaderTypeSetting, true);
                loaderType.SetDiry();
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
