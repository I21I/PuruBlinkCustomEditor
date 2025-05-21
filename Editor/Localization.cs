using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PuruBlinkCustom
{
    internal partial class Localization : ScriptableSingleton<Localization>
    {
        private Dictionary<string, string> strings = new Dictionary<string, string>();
        private static readonly Dictionary<string, GUIContent> guicontents = new Dictionary<string, GUIContent>();
        
        private static string localizationFolder = "Assets/21CSX/PuruBlinkCustomEditor/Localization";
        private static string settingsPath = "Library/PuruBlinkCustomEditor.lang";
        private static Language currentLanguage = Language.日本語; // デフォルトは日本語

        public enum Language
        {
            日本語,
            English
        }

        public static Language CurrentLanguage
        {
            get { return currentLanguage; }
            set 
            { 
                currentLanguage = value;
                SaveLanguageSetting();
                Load();
            }
        }

        [InitializeOnLoadMethod]
        internal static void Initialize()
        {
            LoadLanguageSetting();
            Load();
        }

        internal static void Load()
        {
            guicontents.Clear();
            instance.strings.Clear();
            
            // 日本語はデフォルトなのでPOファイルをロードする必要はない
            if (currentLanguage != Language.日本語)
            {
                string langCode = GetLanguageCode(currentLanguage);
                var path = Path.Combine(localizationFolder, langCode + ".po");
                
                if (File.Exists(path))
                {
                    LoadPOFile(path);
                }
                else
                {
                    Debug.LogWarning($"Language file not found: {path}");
                }
            }
        }

        private static void LoadPOFile(string path)
        {
            string currentMsgid = string.Empty;
            string currentMsgstr = string.Empty;
            bool inMsgid = false;
            bool inMsgstr = false;

            foreach (string line in File.ReadAllLines(path))
            {
                string trimmedLine = line.Trim();
                if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("#"))
                    continue;

                if (trimmedLine.StartsWith("msgid "))
                {
                    if (inMsgstr && !string.IsNullOrEmpty(currentMsgid) && !string.IsNullOrEmpty(currentMsgstr))
                    {
                        instance.strings[currentMsgid] = currentMsgstr;
                    }

                    currentMsgid = ExtractString(trimmedLine, "msgid ");
                    inMsgid = true;
                    inMsgstr = false;
                }
                else if (trimmedLine.StartsWith("msgstr "))
                {
                    currentMsgstr = ExtractString(trimmedLine, "msgstr ");
                    inMsgid = false;
                    inMsgstr = true;
                }
                else if (trimmedLine.StartsWith("\"") && trimmedLine.EndsWith("\""))
                {
                    string content = trimmedLine.Substring(1, trimmedLine.Length - 2);
                    content = System.Text.RegularExpressions.Regex.Unescape(content);

                    if (inMsgid)
                        currentMsgid += content;
                    else if (inMsgstr)
                        currentMsgstr += content;
                }
            }

            if (inMsgstr && !string.IsNullOrEmpty(currentMsgid) && !string.IsNullOrEmpty(currentMsgstr))
            {
                instance.strings[currentMsgid] = currentMsgstr;
            }
        }

        private static string ExtractString(string line, string prefix)
        {
            string value = line.Substring(prefix.Length).Trim();
            if (value.StartsWith("\"") && value.EndsWith("\""))
            {
                value = value.Substring(1, value.Length - 2);
                value = System.Text.RegularExpressions.Regex.Unescape(value);
            }
            return value;
        }

        private static string GetLanguageCode(Language language)
        {
            switch (language)
            {
                case Language.日本語: return "ja-JP";
                case Language.English: return "en-US";
                default: return "ja-JP"; // デフォルトは日本語
            }
        }

        private static void SaveLanguageSetting()
        {
            try
            {
                string code = GetLanguageCode(currentLanguage);
                File.WriteAllText(settingsPath, code);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"言語設定の保存に失敗しました: {e.Message}");
            }
        }

        private static void LoadLanguageSetting()
        {
            try
            {
                if (File.Exists(settingsPath))
                {
                    string code = File.ReadAllText(settingsPath);
                    
                    if (code == "ja-JP")
                        currentLanguage = Language.日本語;
                    else if (code == "en-US")
                        currentLanguage = Language.English;
                    else
                        SetLanguageBySystemLocale();
                }
                else
                {
                    SetLanguageBySystemLocale();
                }
            }
            catch (System.Exception)
            {
                SetLanguageBySystemLocale();
            }
        }

        private static void SetLanguageBySystemLocale()
        {
            string systemCode = CultureInfo.CurrentCulture.Name;
            
            if (systemCode.StartsWith("ja"))
                currentLanguage = Language.日本語;
            else
                currentLanguage = Language.English;
        }

        internal static string L(string key)
        {
            // 日本語の場合は翻訳なしでキーをそのまま返す
            if (currentLanguage == Language.日本語)
                return key;
                
            // 他の言語の場合は翻訳を試みる
            if (instance.strings.TryGetValue(key, out string value) && !string.IsNullOrEmpty(value))
                return value;
                
            // 翻訳が見つからない場合はキーをそのまま返す（デフォルト言語のテキスト）
            return key;
        }

        internal static string L(string key, params object[] args)
        {
            string text = L(key);
            if (args != null && args.Length > 0)
            {
                return string.Format(text, args);
            }
            return text;
        }

        private static GUIContent G(string key) => G(key, null, "");
        private static GUIContent G(string[] key) => key.Length == 2 ? G(key[0], null, key[1]) : G(key[0], null, null);
        internal static GUIContent G(string key, string tooltip) => G(key, null, tooltip);
        private static GUIContent G(string key, Texture image, string tooltip)
        {
            var k = key + tooltip;
            if (guicontents.TryGetValue(k, out var content)) return content;
            return guicontents[k] = new GUIContent(L(key), image, L(tooltip));
        }

        [MenuItem("21CSX/PuruBlink/Reload Language Files")]
        public static void ReloadLanguages()
        {
            Load();
        }
    }
}
