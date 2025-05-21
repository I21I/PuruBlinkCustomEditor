using UnityEngine;
using System.Collections.Generic;

namespace VRCFaceController
{
    public enum Language
    {
        日本語,
        English
    }

    public static class PuruBlinkCustomEditorLocalization
    {
        private static Language currentLanguage = Language.日本語;
        private static Dictionary<string, Dictionary<Language, string>> localizedStrings = new Dictionary<string, Dictionary<Language, string>>();

        public static Language CurrentLanguage 
        { 
            get { return currentLanguage; } 
            set { currentLanguage = value; }
        }

        public static void Initialize()
        {
            if (localizedStrings.Count > 0)
                return;
                
            AddLocalization("WindowTitle", "PuruBlink Custom Editor", "PuruBlink Custom Editor");
            
            AddLocalization("AnimatorController", "AnimatorController", "AnimatorController");
            AddLocalization("Refresh", "更新", "Refresh");
            AddLocalization("ParameterDriverSettings", "VRC Parameter Driver Settings", "VRC Parameter Driver Settings");
            AddLocalization("AllowMultipleSelection", "複数選択を許可", "Allow Multiple Selection");
            AddLocalization("AdvancedFeatures", "拡張機能", "Advanced");
            AddLocalization("Layer", "Layer:", "Layer:");
            AddLocalization("All", "すべて", "All");
            AddLocalization("FilterParameter", "Filter Parameter:", "Filter Parameter:");
            AddLocalization("None", "なし", "None");
            AddLocalization("Value", "Value:", "Value:");
            AddLocalization("ValuesNotFound", "値が見つかりません", "Values not found");
            
            AddLocalization("LeftHandGesture", "左手ジェスチャー (GestureLeft)", "Left Hand Gesture (GestureLeft)");
            AddLocalization("RightHandGesture", "右手ジェスチャー (GestureRight)", "Right Hand Gesture (GestureRight)");
            AddLocalization("ParametersNotFound", "パラメータが見つかりませんでした。", "Parameters not found.");
            AddLocalization("AddParameter", "パラメータを追加", "Add Parameter");
            AddLocalization("Delete", "削除", "Del");
            
            AddLocalization("AnimationReplacement", "Animation 置換", "Animation Replacement");
            AddLocalization("OutputSettings", "出力設定", "Output Settings");
            AddLocalization("OutputFolder", "出力フォルダ:", "Output Folder:");
            AddLocalization("FilePrefix", "ファイル接頭語:", "File Prefix:");
            AddLocalization("CopyAndReplaceController", "AnimatorControllerを複製して置換:", "Copy and Replace AnimatorController:");
            AddLocalization("ReplacePrefab", "Prefab内を置換:", "Replace Prefab Contents:");
            AddLocalization("PrefabVariantReplace", "Variantとして作成", "Replace with Prefab Variant");
            AddLocalization("SearchFromProject", "プロジェクトから検索", "Search from Project");
            AddLocalization("Clear", "クリア", "Clear");
            AddLocalization("AnimationList", "アニメーション一覧", "Animation List");
            AddLocalization("SearchAnimations", "アニメーションを検索", "Search Animations");
            AddLocalization("ReplaceAnimations", "アニメーションを置換", "Replace Animations");
            
            AddLocalization("ExportDateFolder", "Export[日付]", "Export[Date]");
            
            AddLocalization("DragDropController", "AnimatorControllerをここにドラッグ＆ドロップ", "Drag & Drop AnimatorController Here");
            
            AddLocalization("SelectController", "アニメーターコントローラを選択してください。", "Please select an Animator Controller.");
            AddLocalization("Error", "エラー", "Error");
            AddLocalization("NoControllersBeforeSearch", "検索する前にアニメーターコントローラを設定してください。", "Please set AnimatorController before searching.");
            AddLocalization("SearchComplete", "検索完了", "Search Complete");
            AddLocalization("PrefabsFound", "{0}個の該当するPrefabを発見し、追加しました。", "Found and added {0} matching Prefabs.");
            AddLocalization("NoPrefabsFound", "指定されたAnimatorControllerを持つPrefabが見つかりませんでした。", "No Prefabs found with the specified AnimatorController.");
            AddLocalization("NoDuplicatePrefabs", "新しいPrefabは見つかりませんでした。（既存のPrefabと重複していました）", "No new Prefabs found. (Duplicates with existing Prefabs)");
            AddLocalization("NoAnimationsFound", "アニメーションクリップが見つかりませんでした。", "No animation clips found.");
            AddLocalization("AnimationsFound", "{0}個のアニメーションクリップを見つけました。", "Found {0} animation clips.");
            AddLocalization("ReplaceComplete", "置換完了", "Replacement Complete");
            AddLocalization("AnimationsReplaced", "{0}個のアニメーションを置換しました。", "Replaced {0} animations.");
            AddLocalization("ProcessedControllers", "処理されたコントローラ:", "Processed Controllers:");
            AddLocalization("PrefabsCopied", "{0}個のPrefabを複製・更新しました:", "Copied and updated {0} Prefabs:");
            AddLocalization("NoAnimationsToReplace", "置換するアニメーションが見つかりませんでした。", "No animations to replace were found.");
            AddLocalization("OK", "OK", "OK");
            
            AddLocalization("Remove", "×", "×");
            AddLocalization("ParameterAdded", "パラメータ '{0}' ({1}) を '{2}' に追加しました", "Added parameter '{0}' ({1}) to '{2}'");
            AddLocalization("ParameterRemoved", "パラメータを削除しました", "Parameter removed");
            AddLocalization("ParameterUpdated", "パラメータを更新しました", "Parameter updated");
            AddLocalization("StateNotFound", "{0} {1}に対応するステートが見つかりませんでした。", "State for {0} {1} not found.");
            AddLocalization("StatePathNotFound", "ステート {0} が見つかりませんでした。", "State {0} not found.");
            AddLocalization("DriverNotFound", "VRCAvatarParameterDriverとModularAvatarのParameterSyncStepが見つかりませんでした。VRChat SDKまたはModularAvatarがインポートされているか確認してください。", "VRCAvatarParameterDriver and ModularAvatar ParameterSyncStep not found. Please check if VRChat SDK or ModularAvatar is imported.");
            AddLocalization("ErrorAddingDriver", "パラメータドライバーの追加中にエラーが発生しました: {0}", "Error occurred while adding parameter driver: {0}");
            AddLocalization("ParametersPropertyNotFound", "parametersプロパティが見つかりませんでした。", "Parameters property not found.");
            AddLocalization("ControllerCopyFailed", "コントローラの複製に失敗しました: {0}", "Failed to copy controller: {0}");
            AddLocalization("ControllerCopyError", "コントローラの複製中にエラーが発生しました: {0}", "Error occurred while copying controller: {0}");
            AddLocalization("PrefabReplaceError", "Prefab '{0}' の置換中にエラーが発生しました: {1}", "Error occurred while replacing Prefab '{0}': {1}");
            AddLocalization("CopyingController", "コントローラのコピー中", "Copying controller");
            AddLocalization("FolderCreationError", "フォルダの作成中にエラーが発生しました: {0}", "Error occurred while creating folders: {0}");
        }
        
        private static void AddLocalization(string key, string japanese, string english)
        {
            localizedStrings[key] = new Dictionary<Language, string>
            {
                { Language.日本語, japanese },
                { Language.English, english }
            };
        }
        
        public static string L(string key, params object[] args)
        {
            if (localizedStrings.TryGetValue(key, out Dictionary<Language, string> translations))
            {
                if (translations.TryGetValue(currentLanguage, out string translation))
                {
                    if (args != null && args.Length > 0)
                    {
                        return string.Format(translation, args);
                    }
                    return translation;
                }
            }
            
            return key;
        }
    }
}
