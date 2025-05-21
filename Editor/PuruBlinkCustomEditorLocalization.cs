using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Globalization;

namespace VRCFaceController
{

        public enum Language
        {
            日本語,
            English
        }

    public static class PuruBlinkCustomEditorLocalization
    {
        // 設定ファイルの保存先
        private static readonly string SETTINGS_PATH = "Library/PuruBlinkCustomEditor.lang";
        
        // 言語データ
        private static readonly List<Dictionary<string, string>> languages = new List<Dictionary<string, string>>();
        private static readonly List<string> languageCodes = new List<string>();
        private static string[] languageNames;
        private static int currentLanguageIndex = 0;
        private static bool isInitialized = false;
        
        // プロパティで現在の言語を取得・設定
        public static Language CurrentLanguage 
        { 
            get { return (Language)currentLanguageIndex; } 
            set 
            { 
                currentLanguageIndex = (int)value;
                SaveLanguageSetting();
            }
        }

        
        [InitializeOnLoadMethod]
        public static void Initialize()
        {
            if (isInitialized) return;
            
            // 言語ファイルのフォルダを取得
            string languageFolder = GetLanguageFolder();
            if (string.IsNullOrEmpty(languageFolder))
            {
                Debug.LogWarning("言語ファイルフォルダが見つかりませんでした。");
                // エラーメッセージ用の空のデータ
                SetupEmptyLanguage();
                return;
            }
            
            // 言語ファイルの読み込み
            LoadLanguageFiles(languageFolder);
            
            // 保存されていた言語設定の読み込み
            LoadLanguageSetting();
            
            isInitialized = true;
        }
        
        private static void SetupEmptyLanguage()
        {
            // 最小限のエラー表示用データのみ
            var emptyDict = new Dictionary<string, string>();
            
            languages.Clear();
            languageCodes.Clear();
            
            languages.Add(emptyDict);
            languageCodes.Add("empty");
            
            languageNames = new string[] { "Default" };
        }
        
        private static void LoadLanguageFiles(string folder)
{
    languages.Clear();
    languageCodes.Clear();
    
    string[] files = Directory.GetFiles(folder, "*.json");
    
    // 日本語とEnglishのインデックスを確保
    int jaIndex = -1;
    int enIndex = -1;
    List<string> sortedFiles = new List<string>();
    
    // 日本語と英語のファイルを特定
    string jaFile = Path.Combine(folder, "ja-JP.json");
    string enFile = Path.Combine(folder, "en-US.json");
    
    // 日本語ファイルが存在すれば先頭に追加
    if (File.Exists(jaFile))
    {
        sortedFiles.Add(jaFile);
        jaIndex = 0;
    }
    
    // 英語ファイルが存在すれば次に追加
    if (File.Exists(enFile))
    {
        sortedFiles.Add(enFile);
        enIndex = (jaIndex == 0) ? 1 : 0;
    }
    
    // その他のファイルを追加
    foreach (string file in files)
    {
        if (file != jaFile && file != enFile)
        {
            sortedFiles.Add(file);
        }
    }
    
    // この時点でsortedFilesはja-JP.json, en-US.json, その他のファイルの順
    List<string> names = new List<string>();
    
    // 通常の言語ファイル読み込み処理...
    foreach (string file in sortedFiles)
    {
        try
        {
            string code = Path.GetFileNameWithoutExtension(file);
            string json = File.ReadAllText(file);
            
            var langData = PuruBlinkJsonUtility.DeserializeStringDictionary(json);
            if (langData == null || langData.Count == 0) continue;
            
            languages.Add(langData);
            languageCodes.Add(code);
            
            string langName = langData.TryGetValue("Language", out string name) ? name : code;
            names.Add(langName);
        }
        catch (Exception e)
        {
            Debug.LogError($"言語ファイルの読み込みに失敗しました: {file}\n{e.Message}");
        }
    }
    
    languageNames = names.ToArray();
}
        
        private static string GetLanguageFolder()
        {
            string editorPath = GetEditorPath();
            if (string.IsNullOrEmpty(editorPath)) return null;
            
            string localizationFolder = Path.Combine(editorPath, "Localization");
            if (Directory.Exists(localizationFolder))
            {
                return localizationFolder;
            }
            
            // 言語ファイルフォルダが存在しない場合は作成のみ
            try
            {
                Directory.CreateDirectory(localizationFolder);
                Debug.Log($"言語ファイル用フォルダを作成しました: {localizationFolder}");
                return localizationFolder;
            }
            catch (Exception e)
            {
                Debug.LogError($"言語ファイルフォルダの作成に失敗しました: {e.Message}");
                return null;
            }
        }
        
        private static string GetEditorPath()
        {
            // このスクリプトが存在するパスを取得
            string[] guids = AssetDatabase.FindAssets("t:Script PuruBlinkCustomEditorLocalization");
            if (guids.Length == 0) return null;
            
            string scriptPath = AssetDatabase.GUIDToAssetPath(guids[0]);
            if (string.IsNullOrEmpty(scriptPath)) return null;
            
            return Path.GetDirectoryName(scriptPath);
        }
        
        // 言語ファイルの再読み込み
        [MenuItem("Tools/PuruBlink/Reload Language Files")]
        public static void ReloadLanguages()
        {
            isInitialized = false;
            Initialize();
        }
        
        private static void SaveLanguageSetting()
        {
            try
            {
                // 言語コードを保存
                string code = currentLanguageIndex < languageCodes.Count && currentLanguageIndex >= 0 
                    ? languageCodes[currentLanguageIndex] 
                    : "en-US";
                    
                File.WriteAllText(SETTINGS_PATH, code);
            }
            catch (Exception e)
            {
                Debug.LogError($"言語設定の保存に失敗しました: {e.Message}");
            }
        }
        
        private static void LoadLanguageSetting()
        {
            try
            {
                if (File.Exists(SETTINGS_PATH))
                {
                    string code = File.ReadAllText(SETTINGS_PATH);
                    int index = languageCodes.IndexOf(code);
                    
                    if (index >= 0)
                    {
                        currentLanguageIndex = index;
                        return;
                    }
                }
                
                // 設定ファイルがないか、言語コードが見つからない場合はシステム言語を検出
                string systemCode = CultureInfo.CurrentCulture.Name;
                int systemIndex = languageCodes.FindIndex(c => c.Equals(systemCode, StringComparison.OrdinalIgnoreCase));
                
                if (systemIndex >= 0)
                {
                    currentLanguageIndex = systemIndex;
                    return;
                }
                
                // 言語コードの前半部分だけで検索 (例: "ja-JP" → "ja")
                string shortCode = systemCode.Split('-')[0];
                int shortIndex = languageCodes.FindIndex(c => c.StartsWith(shortCode, StringComparison.OrdinalIgnoreCase));
                
                if (shortIndex >= 0)
                {
                    currentLanguageIndex = shortIndex;
                    return;
                }
                
                // 英語をデフォルトとして検索
                int englishIndex = languageCodes.IndexOf("en-US");
                if (englishIndex >= 0)
                {
                    currentLanguageIndex = englishIndex;
                    return;
                }
                
                // それでもなければ最初の言語
                currentLanguageIndex = 0;
            }
            catch (Exception e)
            {
                Debug.LogError($"言語設定の読み込みに失敗しました: {e.Message}");
                currentLanguageIndex = 0;
            }
        }
        
        // 言語選択UI
        public static bool SelectLanguageGUI()
        {
            if (!isInitialized)
            {
                Initialize();
            }
            
            if (languageNames == null || languageNames.Length == 0)
            {
                if (GUILayout.Button("Reload Language"))
                {
                    ReloadLanguages();
                }
                return false;
            }
            
            EditorGUI.BeginChangeCheck();
            currentLanguageIndex = EditorGUILayout.Popup("Editor Language", currentLanguageIndex, languageNames);
            if (EditorGUI.EndChangeCheck())
            {
                SaveLanguageSetting();
                return true;
            }
            return false;
        }
        
        // 矩形領域での言語選択UI
        public static void SelectLanguageGUI(Rect position)
        {
            if (!isInitialized)
            {
                Initialize();
            }
            
            if (languageNames == null || languageNames.Length == 0)
            {
                if (GUI.Button(position, "Reload Language"))
                {
                    ReloadLanguages();
                }
                return;
            }
            
            EditorGUI.BeginChangeCheck();
            currentLanguageIndex = EditorGUI.Popup(position, "Editor Language", currentLanguageIndex, languageNames);
            if (EditorGUI.EndChangeCheck())
            {
                SaveLanguageSetting();
            }
        }
        
        // 翻訳取得（パラメータなし）
        public static string L(string key)
        {
            if (!isInitialized)
            {
                Initialize();
            }
            
            // 現在の言語から翻訳を取得
            if (currentLanguageIndex >= 0 && currentLanguageIndex < languages.Count)
            {
                var lang = languages[currentLanguageIndex];
                if (lang.TryGetValue(key, out string translation) && !string.IsNullOrEmpty(translation))
                {
                    return translation;
                }
            }
            
            // 見つからない場合は英語をフォールバックとして検索
            int englishIndex = languageCodes.IndexOf("en-US");
            if (englishIndex >= 0 && englishIndex < languages.Count && englishIndex != currentLanguageIndex)
            {
                var engLang = languages[englishIndex];
                if (engLang.TryGetValue(key, out string engTranslation) && !string.IsNullOrEmpty(engTranslation))
                {
                    return engTranslation;
                }
            }
            
            // 最後の手段としてキーそのものを返す
            return key;
        }
        
        // 翻訳取得（フォーマットパラメータあり）
        public static string L(string key, params object[] args)
        {
            string text = L(key);
            if (args != null && args.Length > 0)
            {
                return string.Format(text, args);
            }
            return text;
        }
    }
}
