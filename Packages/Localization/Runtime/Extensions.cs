using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.Localizations
{
    public static class Extensions
    {

        public static string Localization(this string key)
        {
            return Localizations.Localization.GetString(key);
        }
        public static bool TryLocalization(this string key,out string str)
        {
            return Localizations.Localization.TryGetString(key,out str);
        }
        public static T Localization<T>(this string key)
        {
            return (T)Localizations.Localization.GetItem(key).Value;
        }
    }

}