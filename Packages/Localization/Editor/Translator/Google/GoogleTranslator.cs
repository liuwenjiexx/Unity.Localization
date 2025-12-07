using SimpleJSON;
using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localizations;
using UnityEngine.Networking;

namespace UnityEditor.Localizations
{
    public class GoogleTranslator : ILanguageTranslator
    {
        public int Priority => -1;
        /*
public static void Process(string targetLang, string sourceText, Action<bool, string> callback)
{
    EditorStartCoroutine(_Process(null, targetLang, sourceText, callback));
}

public static void Process(string sourceLang, string targetLang, string sourceText, Action<bool, string> callback)
{
    EditorStartCoroutine(_Process(sourceLang, targetLang, sourceText, callback));
}

static IEnumerator _Process(string sourceLang, string targetLang, string sourceText, Action<bool, string> callback)
{
    if (string.IsNullOrEmpty(sourceLang))
        sourceLang = "auto";
    string url = "https://translate.googleapis.com/translate_a/single?client=gtx&sl="
        + sourceLang + "&tl=" + targetLang + "&dt=t&q=" + UnityWebRequest.EscapeURL(sourceText);

    using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
    {
        yield return webRequest.SendWebRequest();

        if (string.IsNullOrEmpty(webRequest.error))
        {
            var N = JSONNode.Parse(webRequest.downloadHandler.text);

            string result = N[0][0][0];
            callback(true, result);
        }
        else
        {
            callback(false, webRequest.error);
        }
    }
}

static void EditorStartCoroutine(IEnumerator coroutine)
{
    if (!coroutine.MoveNext())
        return;

    EditorApplication.CallbackFunction callback = null;
    callback = () =>
    {
        if (coroutine.Current != null)
        {
            var oper = coroutine.Current as AsyncOperation;
            if (oper != null)
            {
                if (!oper.isDone)
                {
                    EditorApplication.update += callback;
                    return;
                }
            }
            if (coroutine.MoveNext())
            {
                EditorApplication.update += callback;
            }
        }
    };

    EditorApplication.update += callback;
}
*/
        public bool CanTranslateLanguage(string sourceLang, string targetLang)
        {
            return true;
        }

        public async Task<string> TranslateLanguage(string sourceLang, string targetLang, string sourceText)
        {
            if (string.IsNullOrEmpty(sourceLang))
                sourceLang = "auto";
            string url;
            int version = 0;
            switch (version)
            {
                case 1:
                    //HTTP/1.1 429 Too Many Requests
                    url = "https://clients5.google.com/translate_a/t?client=dict-chrome-ex&sl="
                        + sourceLang + "&tl=" + targetLang + "&dt=t&q=" + UnityWebRequest.EscapeURL(sourceText);
                    break;
                default:
                    //HTTP/1.1 429 Too Many Requests
                    url = "https://translate.googleapis.com/translate_a/single?client=gtx&sl="
                        + sourceLang + "&tl=" + targetLang + "&dt=t&q=" + UnityWebRequest.EscapeURL(sourceText);
                    break;

            }


            using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
            {
                await webRequest.SendWebRequest();

                if (!string.IsNullOrEmpty(webRequest.error))
                {
                    Debug.LogError("Google Translate Url\n" + url);
                    throw new Exception(webRequest.error);
                }
                string resp = webRequest.downloadHandler.text;
                string result;
                try
                {
                    var N = JSONNode.Parse(resp);
                    result = N[0][0][0];
                    return result;
                }
                catch (Exception e)
                {
                    Debug.LogError("Parse error " + resp + "\n" + url);
                    Debug.LogException(e);
                }
            }
            return null;
        }
    }
}