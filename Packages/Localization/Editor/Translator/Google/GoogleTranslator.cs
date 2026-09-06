using SimpleJSON;
using System;
using System.Collections;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localizations;
using UnityEngine.Networking;

namespace UnityEditor.Localizations
{

    /**
HTTP状态码	含义	            可能原因
429	        Too Many Requests	超过每秒/每天配额
403	        Quota Exceeded	    日配额耗尽
500	        Internal Error	    后端过载或临时故障
503	        Service Unavailable	服务暂时不可用
     */
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
        //避免频繁请求被 ban
        public float interval = 0.1f;

        public bool CanTranslateLanguage(string sourceLang, string targetLang)
        {
            return true;
        }
        public DateTime nextTime;

        public async Task<string> TranslateLanguage(string sourceLang, string targetLang, string sourceText)
        {
            if (DateTime.Now < nextTime)
            {
                await Task.Delay((int)(nextTime - DateTime.Now).TotalMilliseconds);
            }
            nextTime = DateTime.Now.AddSeconds(interval);
            if (string.IsNullOrEmpty(sourceLang))
                sourceLang = "auto";
            string url;
            var version = GoogleTranslatorSettings.ApiVersion;
            switch (version)
            {
                case  GoogleTranslatorApiVersion.V1:
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
                    StringBuilder builder = new StringBuilder();
                    builder.Append($"Google Translate Error: ").AppendLine(webRequest.error);
                    builder.AppendLine("Code:").AppendLine(webRequest.responseCode.ToString());
                    builder.Append("Url: ").AppendLine(url);
                    if (webRequest.responseCode == 429)
                    {
                        builder.AppendLine($"[Retry-After: {webRequest.GetResponseHeader("Retry-After")}]");
                    }
            
                    Debug.LogError(builder.ToString());
                    throw new Exception(webRequest.error);
                }

                string resp = webRequest.downloadHandler?.text;

                string result;
                try
                {
                    var N = JSONNode.Parse(resp);
                    if (version == GoogleTranslatorApiVersion.V1)
                    {
                        result = N[0];
                    }
                    else
                    {
                        result = N[0][0][0];
                    }
                    return result;
                }
                catch (Exception e)
                {
                    Debug.LogError("Parse error " + resp + "\n" + url + "\n" + resp);
                    Debug.LogException(e);

                }
            }
            return null;
        }
    }
}