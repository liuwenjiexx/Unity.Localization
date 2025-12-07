using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

namespace UnityEngine.Localizations
{

    public interface ILanguageTranslator
    {
        int Priority { get; }

        bool CanTranslateLanguage(string sourceLang, string targetLang);

        Task<string> TranslateLanguage(string sourceLang, string targetLang, string sourceText);

    }

    public class AsyncResult<T>
    {
        public T Result;
    }
}