using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localizations;

public interface ILocalizationLoader
{
    int Priority { get; }
   
    IEnumerable<LanguageInfo> GetSupportedLangs();

    IDictionary<string, LocalizationValue> LoadValues(string lang);
}

public class LanguageInfo
{
    private string name;
    private string displayName;

    public LanguageInfo(string langName, string displayName)
    {
        this.name = langName;
        this.displayName = displayName;
    }

    public LanguageInfo() { }

    public string Name { get => name; set => name = value; }
    public string DisplayName { get => displayName; set => displayName = value; }
}