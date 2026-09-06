using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localizations;

namespace UnityEditor.Localizations
{

    using Localization = UnityEngine.Localizations.Localization;

    [Serializable]
    public class LocalizationFile
    {
        public string lang;
        public string path;
        [NonSerialized]
        public Dictionary<string, LocalizationValue> values;
        public string loadError;
        public int displayIndex = -1;
        [NonSerialized]
        public bool isLoaded;
        public bool isShow;
        public bool isBase;
        [NonSerialized]
        public int version;
        [NonSerialized]
        public int savedVersion;

        public bool isFiexd;

        public LocalizationFile()
        {
            values = new Dictionary<string, LocalizationValue>();
        }
        public LocalizationFile(string path)
            : this()
        {
            this.path = path;
            lang = Localization.ParseLangNameByFileName(path);
        }

        public LocalizationValue this[string key]
        {
            get
            {
                if (!values.TryGetValue(key, out var value))
                {
                    throw new Exception("Not found key " + key);
                }
                return value;
            }
            set
            {
                SetValue(key, value);
            }
        }

        public bool IsChanged => savedVersion != version;

        public bool ExistsFile => File.Exists(path);

        public DateTime LastWriteTimeUtc
        {
            get
            {
                var fInfo = new FileInfo(path);
                if (fInfo.Exists)
                    return fInfo.LastWriteTimeUtc;
                return DateTime.MinValue;
            }
        }

        public DateTime LastLoadTimeUtc { get; private set; }

        public bool HasKey(string key)
        {
            return values.ContainsKey(key);
        }

        public IEnumerable<string> EnumerateKeys()
        {
            return values.Keys;
        }

        public bool RenameKey(string key, string newKey)
        {
            if (!values.ContainsKey(key)) return false;

            if (values.ContainsKey(newKey))
            {
                return false;
            }

            values[newKey] = values[key];
            values.Remove(key);
            Diry();
            return true;
        }

        public void Load()
        {
            values.Clear();
            loadError = null;
            isLoaded = false;
            version++;

            if (string.IsNullOrEmpty(path))
                return;

            if (!ExistsFile)
                return;

            try
            {
                var loadTimeUtc = LastWriteTimeUtc;
                Localization.LoadFromFile(path, values);
                isLoaded = true;
                LastLoadTimeUtc = loadTimeUtc;
            }
            catch (Exception ex)
            {
                loadError = ex.Message;
            }
            /*
            string filename = Path.GetFileName(path);
            if (path.EndsWith("." + Localization.ExtensionName, StringComparison.InvariantCultureIgnoreCase))
            {
                lang = filename.Substring(0, filename.Length - Localization.ExtensionName.Length - 1);
            }
            else
            {
                lang = Path.GetFileNameWithoutExtension(filename);
            }*/
            lang = Localization.ParseLangNameByFileName(path);
        }

        public bool TryGetValue(string key, out LocalizationValue value)
        {
            key = key?.Trim();
            if (string.IsNullOrEmpty(key))
            {
                value = default;
                return false;
            }
            return values.TryGetValue(key, out value);
        }

        public bool SetValue(string key, LocalizationValue value)
        {
            key = key?.Trim();
            if (string.IsNullOrEmpty(key)) return false;

            if (values.TryGetValue(key, out var old) && old == value) return false;

            values[key] = value;
            Diry();
            return true;
        }



        public bool RemoveKey(string key)
        {
            key = key?.Trim();
            if (string.IsNullOrEmpty(key)) return false;
            if (values.Remove(key))
            {
                Diry();
                return true;
            }
            return false;
        }

        public void Diry()
        {
            version++;
        }

        public bool SaveIfChagne()
        {
            if (IsChanged)
            {
                Save();
                return true;
            }
            return false;
        }

        public void Save()
        {
            savedVersion = version;
            Localization.SaveToXml(path, values);
            LastLoadTimeUtc = LastWriteTimeUtc;
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }

        public override string ToString()
        {
            return $"{lang}: {path}";
        }
    }

}