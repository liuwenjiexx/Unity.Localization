using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Localizations;

public class ResourcesLoad : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        if (!Localization.IsInitialized)
        {
            InitializeLocalization();
        }
    }

    static void InitializeLocalization()
    {
        var loader = new ResourcesLocalizationLoader(LocalizationSettings.ResourcesPath);
        Localization.Default = new LocalizationValues(loader);
        //初始化
        Localization.Initialize();
    }

    // Update is called once per frame
    void Update()
    {

    }

#if UNITY_EDITOR
    [InitializeOnLoadMethod]
    static void InitializeOnLoadMethod()
    {
        if (!Localization.IsInitialized)
        {
            InitializeLocalization();
        }
    }
#endif

}
