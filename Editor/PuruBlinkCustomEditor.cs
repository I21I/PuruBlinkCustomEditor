using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PuruBlinkCustom
{
    public class PuruBlinkCustomEditor : EditorWindow
    {
        private List<AnimatorController> targetControllers = new List<AnimatorController>();
        private int selectedControllerIndex = 0;
        private AnimatorController currentController => targetControllers.Count > selectedControllerIndex && selectedControllerIndex >= 0 ? targetControllers[selectedControllerIndex] : null;

        private static bool registeredPlayModeCallback = false;
        
        private Vector2 scrollPosition;
        private Vector2 gestureScrollPosition;
        private Vector2 animScrollPosition;
        private Vector2 controllersScrollPosition;
        private Vector2 prefabsScrollPosition;
        
        private int selectedTab = 0;
        private readonly string[] tabNames = { "VRC Parameter Driver 編集", "Animation 置換" };

        private List<string> availableParameters = new List<string>();
        private string selectedParameter = "";
        private Dictionary<string, float> parameterValues = new Dictionary<string, float>();
        
        private List<int> selectedValues = new List<int>() { 0, 1, 2, 3 };
        private List<string> valueLabels = new List<string>() { "値=0", "値=1", "値=2", "値=3" };
        private int currentValueIndex = 0;

        private string[] layerNames = new string[0];
        private int selectedLayerIndex = -1;

        private string GestureNames(int index)
        {
            switch (index)
            {
                case 0: return Localization.L("ニュートラル");
                case 1: return Localization.L("グー");
                case 2: return Localization.L("パー");
                case 3: return Localization.L("指差し");
                case 4: return Localization.L("ピース");
                case 5: return Localization.L("ロック");
                case 6: return Localization.L("銃");
                case 7: return Localization.L("グッド");
                default: return $"Gesture {index}";
            }
        }

        private Dictionary<string, List<ParameterDriverState>> gestureParameterMap = new Dictionary<string, List<ParameterDriverState>>();
        private List<AnimationClip> controllerClips = new List<AnimationClip>();
        private Dictionary<AnimationClip, AnimationClip> animationReplacementMap = new Dictionary<AnimationClip, AnimationClip>();
        private Dictionary<string, Dictionary<string, List<AnimationClip>>> controllerLayerAnimationMap = new Dictionary<string, Dictionary<string, List<AnimationClip>>>();
        
        private string outputFolder = "Assets/21CSX/PuruBlinkCustomEditor";
        private string outputPrefix = "Exp_";
        private bool copyController = true;
        
        private bool showEditButtons = false;
        private bool allowMultipleSelection = false;
        private bool outputSettingsFoldout = true;
        
        private List<GameObject> targetPrefabs = new List<GameObject>();
        private bool createPrefabVariant = true;
        
        private GUIStyle headerStyle;
        private GUIStyle cellStyle;
        private GUIStyle enabledStyle;
        private GUIStyle disabledStyle;
        private GUIStyle separatorStyle;
        private GUIStyle dropAreaStyle;
        private GUIStyle dropAreaTextStyle;
        private GUIStyle boldFoldoutStyle;
        private GUIStyle thinSeparatorStyle;
        private GUIStyle duplicateIconStyle;

        private const string VRCParameterDriverTypeName = "VRC.SDK3.Avatars.Components.VRCAvatarParameterDriver";
        private const string ModularAvatarDriverTypeName = "nadena.dev.modular_avatar.core.ParameterSyncStep";

        [MenuItem("21CSX/PuruBlink Custom Editor")]
        public static void ShowWindow()
        {
            GetWindow<PuruBlinkCustomEditor>("PuruBlink Custom Editor");
        }

        private void OnEnable()
        {
            EditorApplication.delayCall += () => {
                try {
                    if (!this) return; 
                    
                    if (EditorGUIUtility.whiteTexture != null) {
                        InitializeStyles();
                        Localization.Initialize();
                        
                        if (!gestureParameterMap.ContainsKey("GestureLeft"))
                            gestureParameterMap["GestureLeft"] = new List<ParameterDriverState>();
                        if (!gestureParameterMap.ContainsKey("GestureRight"))
                            gestureParameterMap["GestureRight"] = new List<ParameterDriverState>();
                    }
                }
                catch (System.Exception) { 
                }
            };
        }

        private void InitializeStyles()
        {
            if (EditorStyles.boldLabel == null)
                return;

            headerStyle = new GUIStyle(EditorStyles.boldLabel);
            headerStyle.alignment = TextAnchor.MiddleCenter;
            headerStyle.fontSize = 12;
            
            cellStyle = new GUIStyle(EditorStyles.label);
            cellStyle.alignment = TextAnchor.MiddleLeft;
            cellStyle.padding = new RectOffset(5, 5, 3, 3);
            
            enabledStyle = new GUIStyle(EditorStyles.miniButton);
            enabledStyle.normal.background = PuruBlinkCustomEditorUtils.CreateColorTexture(new Color(0.2f, 0.7f, 0.3f, 0.5f));
            enabledStyle.hover.background = PuruBlinkCustomEditorUtils.CreateColorTexture(new Color(0.3f, 0.8f, 0.4f, 0.7f));
            
            disabledStyle = new GUIStyle(EditorStyles.miniButton);
            disabledStyle.normal.background = PuruBlinkCustomEditorUtils.CreateColorTexture(new Color(0.7f, 0.2f, 0.2f, 0.5f));
            disabledStyle.hover.background = PuruBlinkCustomEditorUtils.CreateColorTexture(new Color(0.8f, 0.3f, 0.3f, 0.7f));
            
            separatorStyle = new GUIStyle();
            separatorStyle.normal.background = EditorGUIUtility.whiteTexture;
            separatorStyle.margin = new RectOffset(0, 0, 4, 4);
            separatorStyle.fixedHeight = 1;
            
            dropAreaStyle = new GUIStyle();
            dropAreaStyle.normal.background = PuruBlinkCustomEditorUtils.CreateSimpleBorderTexture();
            dropAreaStyle.border = new RectOffset(3, 3, 3, 3);
            dropAreaStyle.margin = new RectOffset(5, 5, 8, 8);
            dropAreaStyle.padding = new RectOffset(5, 5, 5, 5);
            dropAreaStyle.alignment = TextAnchor.MiddleCenter;
            
            dropAreaTextStyle = new GUIStyle(EditorStyles.boldLabel);
            dropAreaTextStyle.normal.textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black;
            dropAreaTextStyle.alignment = TextAnchor.MiddleCenter;
            dropAreaTextStyle.fontSize = 12;
            
            boldFoldoutStyle = new GUIStyle(EditorStyles.foldout);
            boldFoldoutStyle.fontStyle = FontStyle.Bold;
            
            thinSeparatorStyle = new GUIStyle();
            thinSeparatorStyle.normal.background = EditorGUIUtility.whiteTexture;
            thinSeparatorStyle.margin = new RectOffset(0, 0, 2, 2);
            thinSeparatorStyle.fixedHeight = 1;

            duplicateIconStyle = new GUIStyle(EditorStyles.label);
            duplicateIconStyle.alignment = TextAnchor.MiddleCenter;
            duplicateIconStyle.fontSize = 16;
        }

        private void OnGUI()
        {
            if (headerStyle == null)
            {
                InitializeStyles();
                if (EditorStyles.boldLabel == null) return;
            }

            EditorGUILayout.BeginVertical();
            
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(Localization.L("PuruBlink Custom Editor"), EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            
EditorGUI.BeginChangeCheck();
int newLang = EditorGUILayout.Popup("", (int)Localization.CurrentLanguage, 
    System.Enum.GetNames(typeof(Localization.Language)), GUILayout.Width(120));
if (EditorGUI.EndChangeCheck())
{
    Localization.CurrentLanguage = (Localization.Language)newLang;
    Repaint();
}
EditorGUILayout.EndHorizontal();
            
            DrawThinSeparator();

            DrawControllerSelectionArea();

            selectedTab = GUILayout.Toolbar(selectedTab, new string[] { Localization.L("VRC Parameter Driver 設定"), Localization.L("Animation 置換") });
            
            EditorGUILayout.Space();
            
            if (selectedTab == 1)
            {
                DrawAnimationReplaceTab();
            }
            else
            {
                DrawParameterEditorTab();
            }
            
            EditorGUILayout.EndVertical();
        }

        private void DrawThinSeparator()
        {
            Color color = EditorGUIUtility.isProSkin ? new Color(0.5f, 0.5f, 0.5f, 1.0f) : new Color(0.3f, 0.3f, 0.3f, 1.0f);
            
            Rect rect = GUILayoutUtility.GetRect(GUIContent.none, thinSeparatorStyle);
            if (Event.current.type == EventType.Repaint)
            {
                Color old = GUI.color;
                GUI.color = color;
                thinSeparatorStyle.Draw(rect, false, false, false, false);
                GUI.color = old;
            }
        }

        private void DrawParameterEditorTab()
        {
            if (targetControllers.Count == 0 || currentController == null)
            {
                EditorGUILayout.HelpBox(Localization.L("アニメーターコントローラを選択してください。"), MessageType.Info);
                return;
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(Localization.L("VRC Parameter Driver 設定"), EditorStyles.boldLabel);
            GUILayout.Space(20);
            
            allowMultipleSelection = EditorGUILayout.Toggle(allowMultipleSelection, GUILayout.Width(15));
            EditorGUILayout.LabelField(Localization.L("複数選択を許可"), GUILayout.Width(Localization.CurrentLanguage == Localization.Language.English ? 160 : 120));
            
            GUILayout.FlexibleSpace();
            
            EditorGUI.BeginChangeCheck();
            showEditButtons = GUILayout.Toggle(showEditButtons, Localization.L("拡張機能"), "Button", GUILayout.Width(Localization.CurrentLanguage == Localization.Language.English ? 90 : 100));
            if (EditorGUI.EndChangeCheck())
            {
                Repaint();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("AnimatorController" + ":", GUILayout.Width(130));
            
            List<string> controllerNames = targetControllers.Select(c => c != null ? c.name : "None").ToList();
            
            EditorGUI.BeginChangeCheck();
            selectedControllerIndex = EditorGUILayout.Popup(selectedControllerIndex, controllerNames.ToArray(), GUILayout.Width(200));
            if (EditorGUI.EndChangeCheck())
            {
                AnalyzeController();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Layer:", GUILayout.Width(70));
            
            string[] layerOptions = new string[layerNames.Length + 1];
            layerOptions[0] = Localization.L("すべて");
            for (int i = 0; i < layerNames.Length; i++)
            {
                layerOptions[i + 1] = layerNames[i];
            }
            
            int displayLayerIndex = selectedLayerIndex + 1;
            
            EditorGUI.BeginChangeCheck();
            displayLayerIndex = EditorGUILayout.Popup(displayLayerIndex, layerOptions, GUILayout.Width(200));
            if (EditorGUI.EndChangeCheck())
            {
                selectedLayerIndex = displayLayerIndex - 1;
                AnalyzeController();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Filter Parameter:", GUILayout.Width(Localization.CurrentLanguage == Localization.Language.English ? 150 : 130));
            
            List<string> paramOptions = new List<string>();
            paramOptions.Add(Localization.L("なし"));
            paramOptions.AddRange(availableParameters);
            
            int paramIndex = string.IsNullOrEmpty(selectedParameter) ? 0 : paramOptions.IndexOf(selectedParameter);
            if (paramIndex < 0) paramIndex = 0;
            
            EditorGUI.BeginChangeCheck();
            paramIndex = EditorGUILayout.Popup(paramIndex, paramOptions.ToArray(), GUILayout.Width(150));
            if (EditorGUI.EndChangeCheck())
            {
                selectedParameter = paramIndex == 0 ? "" : paramOptions[paramIndex];
                
                UpdateParameterValueOptions();
                
                AnalyzeController();
            }
            
            if (!string.IsNullOrEmpty(selectedParameter))
            {
                EditorGUILayout.LabelField("Value:", GUILayout.Width(50));
                EditorGUI.BeginChangeCheck();
                if (valueLabels.Count > 0)
                {
                    currentValueIndex = EditorGUILayout.Popup(currentValueIndex, valueLabels.ToArray(), GUILayout.Width(150));
                    if (EditorGUI.EndChangeCheck())
                    {
                        AnalyzeController();
                    }
                }
                else
                {
                    EditorGUILayout.LabelField(Localization.L("値が見つかりません"), GUILayout.Width(150));
                }
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
            
            DrawThinSeparator();
            
            gestureScrollPosition = EditorGUILayout.BeginScrollView(gestureScrollPosition, GUILayout.ExpandWidth(true));
            
            EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(position.width / 2 - 12));
            GUILayout.Label(Localization.L("左手ジェスチャー (GestureLeft)"), headerStyle);
            DrawGestureSection("GestureLeft");
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(position.width / 2 - 12));
            GUILayout.Label(Localization.L("右手ジェスチャー (GestureRight)"), headerStyle);
            DrawGestureSection("GestureRight");
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndScrollView();
        }

        private void DrawGestureSection(string gestureType)
        {
            if (!gestureParameterMap.ContainsKey(gestureType) || gestureParameterMap[gestureType].Count == 0)
            {
                EditorGUILayout.HelpBox(Localization.L("パラメータが見つかりませんでした。"), MessageType.Info);
                return;
            }
            
            var stateList = gestureParameterMap[gestureType];
            
            int startGesture = 1;
            
            Dictionary<int, List<ParameterDriverState>> groupedByGesture = new Dictionary<int, List<ParameterDriverState>>();
            
            for (int i = 0; i < 8; i++)
            {
                groupedByGesture[i] = new List<ParameterDriverState>();
            }
            
            foreach (var state in stateList)
            {
                if (state.gestureValue >= 0 && state.gestureValue < 8)
                {
                    groupedByGesture[state.gestureValue].Add(state);
                }
            }
            
            for (int gestureValue = startGesture; gestureValue < 8; gestureValue++)
            {
                var filteredParams = groupedByGesture[gestureValue];
                if (filteredParams.Count == 0)
                {
                    continue;
                }
                
                var groupedByState = filteredParams.GroupBy(p => p.statePath)
                    .OrderBy(g => g.Key.Split('/').Last())
                    .ToList();
                
                foreach (var stateGroup in groupedByState)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Label($"{gestureValue}: {GestureNames(gestureValue)}", EditorStyles.boldLabel, GUILayout.Width(Localization.CurrentLanguage == Localization.Language.English ? 100 : 80));
                    
                    string stateName = stateGroup.Key.Split('/').Last();
                    GUILayout.Label(stateName, EditorStyles.label);
                    
                    EditorGUILayout.EndHorizontal();
                    
                    if (showEditButtons && GUILayout.Button(Localization.L("パラメータを追加"), EditorStyles.miniButton))
                    {
                        ShowAddParameterMenu(gestureType, gestureValue);
                    }
                    
                    foreach (var paramState in stateGroup)
                    {
                        DrawParameterToggle(paramState);
                    }
                    
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space(5);
                }
            }
        }

        private void DrawParameterToggle(ParameterDriverState paramState)
        {
            bool isEnabled = Math.Abs(paramState.value - 1.0f) < 0.001f;
            string buttonText = isEnabled ? "ON" : "OFF";
            GUIStyle currentStyle = isEnabled ? enabledStyle : disabledStyle;
            
            Rect rect = EditorGUILayout.GetControlRect();
            
            float currentX = rect.x;
            float totalWidth = rect.width;
            
            float deleteButtonWidth = Localization.CurrentLanguage == Localization.Language.English ? 35f : 40f;
            float totalButtonArea = showEditButtons ? deleteButtonWidth : 0f;
            
            float paramNameWidth = totalWidth - 75f - totalButtonArea;
            paramNameWidth = Mathf.Max(paramNameWidth, 80f);
            
            Rect paramNameRect = new Rect(currentX, rect.y, paramNameWidth, rect.height);
            EditorGUI.LabelField(paramNameRect, paramState.parameterName);
            currentX += paramNameWidth + 3f;
            
            Rect valueRect = new Rect(currentX, rect.y, 30f, rect.height);
            EditorGUI.LabelField(valueRect, paramState.value.ToString("F1"));
            currentX += 33f;
            
            Rect toggleRect = new Rect(currentX, rect.y, 40f, rect.height);
            if (GUI.Button(toggleRect, buttonText, currentStyle))
            {
                float newValue = isEnabled ? 0.0f : 1.0f;
                
                if (!allowMultipleSelection && newValue == 1.0f)
                {
                    var sameStateParams = gestureParameterMap[paramState.gestureType]
                        .Where(p => p.gestureValue == paramState.gestureValue && p.statePath == paramState.statePath && p != paramState)
                        .ToList();
                    
                    foreach (var otherParam in sameStateParams)
                    {
                        if (Math.Abs(otherParam.value - 1.0f) < 0.001f)
                        {
                            UpdateParameterValue(otherParam, 0.0f);
                        }
                    }
                }
                
                UpdateParameterValue(paramState, newValue);
            }
            currentX += 43f;
            
            if (showEditButtons)
            {
                Rect deleteRect = new Rect(currentX, rect.y, deleteButtonWidth, rect.height);
                if (GUI.Button(deleteRect, Localization.L("削除")))
                {
                    RemoveParameterFromState(paramState);
                }
            }
        }

        private void ShowAddParameterMenu(string gestureType, int gestureValue)
        {
            var menu = new GenericMenu();
            
            foreach (var param in currentController.parameters)
            {
                string parameter = param.name;
                
                bool exists = gestureParameterMap[gestureType].Any(p => 
                    p.gestureValue == gestureValue && p.parameterName == parameter);
                
                menu.AddItem(new GUIContent(parameter), exists, () => {
                    AddParameterToGesture(gestureType, gestureValue, parameter);
                });
            }
            
            menu.ShowAsContext();
        }

        private void AddParameterToGesture(string gestureType, int gestureValue, string parameterName)
        {
            var existingParam = gestureParameterMap[gestureType].FirstOrDefault(p => 
                p.gestureValue == gestureValue && p.parameterName == parameterName);
                
            if (existingParam != null)
            {
                UpdateParameterValue(existingParam, 1.0f);
                return;
            }
            
            var statesForGesture = gestureParameterMap[gestureType]
                .Where(p => p.gestureValue == gestureValue)
                .Select(p => p.statePath)
                .Distinct()
                .ToList();
                
            string targetStatePath = null;
            
            if (statesForGesture.Count > 0)
            {
                targetStatePath = statesForGesture[0];
            }
            else
            {
                targetStatePath = FindGestureStatePathFromTransitions(gestureType, gestureValue);
            }
            
            if (string.IsNullOrEmpty(targetStatePath))
            {
                EditorUtility.DisplayDialog(Localization.L("エラー"), Localization.L("{0} {1}に対応するステートが見つかりませんでした。", gestureType, gestureValue), "OK");
                return;
            }
            
            AnimatorState state = FindStateByPath(targetStatePath);
            if (state == null)
            {
                EditorUtility.DisplayDialog(Localization.L("エラー"), Localization.L("ステート {0} が見つかりませんでした。", targetStatePath), "OK");
                return;
            }
            
            AddParameterToState(state, parameterName, 1.0f);
            
            AnalyzeController();
        }

        private void RemoveParameterFromState(ParameterDriverState paramState)
        {
            if (paramState.behaviour == null)
                return;

            var serializedObj = new SerializedObject(paramState.behaviour);
            
            var parametersProp = serializedObj.FindProperty("parameters");
            
            if (parametersProp == null)
            {
                parametersProp = serializedObj.FindProperty("parameterDrivers");
            }
            
            if (parametersProp != null && parametersProp.isArray && paramState.propertyIndex < parametersProp.arraySize)
            {
                parametersProp.DeleteArrayElementAtIndex(paramState.propertyIndex);
                serializedObj.ApplyModifiedProperties();
                
                EditorUtility.SetDirty(paramState.behaviour);
                EditorUtility.SetDirty(currentController);
                
                AnalyzeController();
            }
        }

        private string FindGestureStatePathFromTransitions(string gestureType, int gestureValue)
        {
            foreach (var layer in currentController.layers)
            {
                if (selectedLayerIndex >= 0 && layer != currentController.layers[selectedLayerIndex])
                    continue;
                    
                if (layer.stateMachine != null)
                {
                    foreach (var transition in layer.stateMachine.anyStateTransitions)
                    {
                        if (transition.destinationState == null) continue;
                        
                        bool matchFound = false;
                        
                        foreach (var condition in transition.conditions)
                        {
                            if (condition.parameter == gestureType && 
                                Mathf.RoundToInt(condition.threshold) == gestureValue)
                            {
                                bool shouldInclude = string.IsNullOrEmpty(selectedParameter);
                                
                                if (!shouldInclude && !string.IsNullOrEmpty(selectedParameter))
                                {
                                    if (selectedValues.Count > currentValueIndex && currentValueIndex >= 0)
                                    {
                                        foreach (var c in transition.conditions)
                                        {
                                            if (c.parameter == selectedParameter && 
                                                Mathf.RoundToInt(c.threshold) == selectedValues[currentValueIndex])
                                            {
                                                shouldInclude = true;
                                                break;
                                            }
                                        }
                                    }
                                }
                                
                                if (shouldInclude)
                                {
                                    matchFound = true;
                                    break;
                                }
                            }
                        }
                        
                        if (matchFound)
                        {
                            return $"{layer.name}/{transition.destinationState.name}";
                        }
                    }
                    
                    if (layer.stateMachine.defaultState != null)
                    {
                        foreach (var transition in layer.stateMachine.defaultState.transitions)
                        {
                            if (transition.destinationState == null) continue;
                            
                            bool matchFound = false;
                            
                            foreach (var condition in transition.conditions)
                            {
                                if (condition.parameter == gestureType && 
                                    Mathf.RoundToInt(condition.threshold) == gestureValue)
                                {
                                    bool shouldInclude = string.IsNullOrEmpty(selectedParameter);
                                    
                                    if (!shouldInclude && !string.IsNullOrEmpty(selectedParameter))
                                    {
                                        if (selectedValues.Count > currentValueIndex && currentValueIndex >= 0)
                                        {
                                            foreach (var c in transition.conditions)
                                            {
                                                if (c.parameter == selectedParameter && 
                                                    Mathf.RoundToInt(c.threshold) == selectedValues[currentValueIndex])
                                                {
                                                    shouldInclude = true;
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                    
                                    if (shouldInclude)
                                    {
                                        matchFound = true;
                                        break;
                                    }
                                }
                            }
                            
                            if (matchFound)
                            {
                                return $"{layer.name}/{transition.destinationState.name}";
                            }
                        }
                    }
                }
            }
            
            return null;
        }
        
        private AnimatorState FindStateByPath(string statePath)
        {
            string[] pathParts = statePath.Split('/');
            
            if (pathParts.Length < 2)
                return null;
                
            string layerName = pathParts[0];
            
            AnimatorControllerLayer layer = null;
            foreach (var l in currentController.layers)
            {
                if (l.name == layerName)
                {
                    layer = l;
                    break;
                }
            }
            
            if (layer == null || layer.stateMachine == null)
                return null;
                
            string subPath = string.Join("/", pathParts.Skip(1));
            
            return FindStateInStateMachine(layer.stateMachine, subPath);
        }
        
        private AnimatorState FindStateInStateMachine(AnimatorStateMachine stateMachine, string path)
        {
            string[] pathParts = path.Split('/');
            string currentName = pathParts[0];
            
            foreach (var childState in stateMachine.states)
            {
                if (childState.state.name == currentName)
                {
                    if (pathParts.Length == 1)
                        return childState.state;
                    else
                        return null;
                }
            }
            
            if (pathParts.Length > 1)
            {
                foreach (var childStateMachine in stateMachine.stateMachines)
                {
                    if (childStateMachine.stateMachine.name == currentName)
                    {
                        string subPath = string.Join("/", pathParts.Skip(1));
                        return FindStateInStateMachine(childStateMachine.stateMachine, subPath);
                    }
                }
            }
            
            return null;
        }
        
        private void AddParameterToState(AnimatorState state, string parameterName, float value)
        {
            StateMachineBehaviour driver = null;
            
            foreach (var behaviour in state.behaviours)
            {
                if (behaviour == null)
                    continue;
                    
                string typeName = behaviour.GetType().FullName;
                
                if (typeName == VRCParameterDriverTypeName || 
                    typeName.EndsWith("VRCAvatarParameterDriver") ||
                    typeName == ModularAvatarDriverTypeName || 
                    typeName.EndsWith("ParameterSyncStep"))
                {
                    driver = behaviour;
                    break;
                }
            }
            
            if (driver == null)
            {
                try
                {
                    Type driverType = AppDomain.CurrentDomain.GetAssemblies()
                        .SelectMany(a => a.GetTypes())
                        .FirstOrDefault(t => t.FullName == VRCParameterDriverTypeName || t.Name == "VRCAvatarParameterDriver");
                        
                    if (driverType == null)
                    {
                        driverType = AppDomain.CurrentDomain.GetAssemblies()
                            .SelectMany(a => a.GetTypes())
                            .FirstOrDefault(t => t.FullName == ModularAvatarDriverTypeName || t.Name == "ParameterSyncStep");
                            
                        if (driverType == null)
                        {
                            EditorUtility.DisplayDialog(Localization.L("エラー"), Localization.L("VRCAvatarParameterDriverとModularAvatarのParameterSyncStepが見つかりませんでした。VRChat SDKまたはModularAvatarがインポートされているか確認してください。"), "OK");
                            return;
                        }
                    }
                    
                    driver = state.AddStateMachineBehaviour(driverType);
                    if (driver != null)
                    {
                        Undo.RegisterCreatedObjectUndo(driver, "Add VRC Avatar Parameter Driver");
                        EditorUtility.SetDirty(state);
                    }
                }
                catch (Exception ex)
                {
                    EditorUtility.DisplayDialog(Localization.L("エラー"), Localization.L("パラメータドライバーの追加中にエラーが発生しました: {0}", ex.Message), "OK");
                    return;
                }
            }
            
            if (driver != null)
            {
                var serializedObj = new SerializedObject(driver);
                
                var parametersProp = serializedObj.FindProperty("parameters");
                
                if (parametersProp == null)
                {
                    parametersProp = serializedObj.FindProperty("parameterDrivers");
                }
                
                if (parametersProp == null)
                {
                    EditorUtility.DisplayDialog(Localization.L("エラー"), Localization.L("parametersプロパティが見つかりませんでした。"), "OK");
                    return;
                }
                
                bool exists = false;
                for (int i = 0; i < parametersProp.arraySize; i++)
                {
                    var paramProp = parametersProp.GetArrayElementAtIndex(i);
                    var nameProp = paramProp.FindPropertyRelative("name");
                    
                    if (nameProp != null && nameProp.stringValue == parameterName)
                    {
                        var valueProp = paramProp.FindPropertyRelative("value");
                        if (valueProp != null)
                        {
                            valueProp.floatValue = value;
                            exists = true;
                            break;
                        }
                    }
                }
                
                if (!exists)
                {
                    parametersProp.arraySize++;
                    var newParamProp = parametersProp.GetArrayElementAtIndex(parametersProp.arraySize - 1);
                    
                    var nameProp = newParamProp.FindPropertyRelative("name");
                    var valueProp = newParamProp.FindPropertyRelative("value");
                    var typeProp = newParamProp.FindPropertyRelative("type");
                    
                    if (nameProp != null && valueProp != null)
                    {
                        nameProp.stringValue = parameterName;
                        valueProp.floatValue = value;
                        
                        if (typeProp != null)
                        {
                            typeProp.intValue = 0;
                        }
                    }
                }
                
                serializedObj.ApplyModifiedProperties();
                EditorUtility.SetDirty(driver);
                EditorUtility.SetDirty(state);
                EditorUtility.SetDirty(currentController);
            }
        }

        private void DrawAnimationReplaceTab()
        {
            if (targetControllers.Count == 0)
            {
                EditorGUILayout.HelpBox(Localization.L("アニメーターコントローラを選択してください。"), MessageType.Info);
                return;
            }
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            outputSettingsFoldout = EditorGUILayout.Foldout(outputSettingsFoldout, Localization.L("出力設定"), true);
            
            if (outputSettingsFoldout)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(Localization.L("出力フォルダ:"), GUILayout.Width(Localization.CurrentLanguage == Localization.Language.English ? 120 : 100));
                EditorGUILayout.LabelField(outputFolder + "/" + Localization.L("Export[日付]"));
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(Localization.L("ファイル接頭語:"), GUILayout.Width(Localization.CurrentLanguage == Localization.Language.English ? 120 : 100));
                outputPrefix = EditorGUILayout.TextField(outputPrefix);
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(Localization.L("AnimatorControllerを複製して置換:"), GUILayout.Width(Localization.CurrentLanguage == Localization.Language.English ? 250 : 200));
                copyController = EditorGUILayout.Toggle(copyController);
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(Localization.L("Prefab内を置換:"), GUILayout.Width(Localization.CurrentLanguage == Localization.Language.English ? 180 : 130));
                
                Rect toggleRect = GUILayoutUtility.GetRect(15, 18, GUILayout.Width(15));
                createPrefabVariant = EditorGUI.Toggle(toggleRect, createPrefabVariant);
                EditorGUI.LabelField(new Rect(toggleRect.x + 18, toggleRect.y, 200, 18), Localization.L("Variantとして作成"));
                
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.BeginHorizontal();
                GameObject newPrefab = (GameObject)EditorGUILayout.ObjectField(
                    null, typeof(GameObject), false, GUILayout.ExpandWidth(true));
                
                if (GUILayout.Button(Localization.L("プロジェクトから検索"), GUILayout.Width(Localization.CurrentLanguage == Localization.Language.English ? 180 : 150)))
                {
                    SearchPrefabsWithControllers();
                }
                
                if (GUILayout.Button(Localization.L("クリア"), GUILayout.Width(60)))
                {
                    targetPrefabs.Clear();
                }
                EditorGUILayout.EndHorizontal();
                
                if (newPrefab != null && !targetPrefabs.Contains(newPrefab) && PrefabUtility.IsPartOfPrefabAsset(newPrefab))
                {
                    targetPrefabs.Add(newPrefab);
                }
                
                if (targetPrefabs.Count > 0)
                {
                    int prefabCount = targetPrefabs.Count;
                    float dynamicHeight = Mathf.Clamp(prefabCount * 22 + 10, 40, 80);
                    
                    prefabsScrollPosition = EditorGUILayout.BeginScrollView(prefabsScrollPosition, GUILayout.Height(dynamicHeight));
                    
                    for (int i = 0; i < targetPrefabs.Count; i++)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.ObjectField(targetPrefabs[i], typeof(GameObject), false);
                        
                        var prefabController = GetPrefabController(targetPrefabs[i]);
                        if (prefabController != null)
                        {
                            EditorGUILayout.LabelField($"→ {prefabController.name}", GUILayout.Width(180));
                        }
                        else
                        {
                            EditorGUILayout.LabelField("→ コントローラなし", GUILayout.Width(180));
                        }
                        
                        if (GUILayout.Button(Localization.L("×"), GUILayout.Width(25)))
                        {
                            targetPrefabs.RemoveAt(i);
                            GUIUtility.ExitGUI();
                            break;
                        }
                        
                        EditorGUILayout.EndHorizontal();
                    }
                    
                    EditorGUILayout.EndScrollView();
                }
            }
            
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(5);
            
            if (controllerLayerAnimationMap.Count == 0)
            {
                if (GUILayout.Button(Localization.L("アニメーションを検索"), GUILayout.Height(30)))
                {
                    FindAnimationsInController();
                }
                
                return;
            }
            
            EditorGUILayout.LabelField(Localization.L("アニメーション一覧"), EditorStyles.boldLabel);
            
            DrawThinSeparator();
            EditorGUILayout.Space(3);
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            animScrollPosition = EditorGUILayout.BeginScrollView(animScrollPosition, GUILayout.ExpandHeight(true));
            
            foreach (var controllerPair in controllerLayerAnimationMap)
            {
                string controllerName = controllerPair.Key;
                var layerDict = controllerPair.Value;
                bool hasContent = layerDict.Values.Any(clips => clips.Count > 0);
                
                if (!hasContent) continue;
                
                EditorGUILayout.LabelField(controllerName, EditorStyles.boldLabel);
                EditorGUILayout.BeginVertical("box");
                
                foreach (var layerPair in layerDict)
                {
                    string layerName = layerPair.Key;
                    var clips = layerPair.Value;
                    
                    if (clips.Count == 0) continue;
                    
                    if (layerDict.Count > 1)
                    {
                        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                        EditorGUILayout.LabelField($"Layer: {layerName}", EditorStyles.miniLabel);
                    }
                    
                    foreach (var clip in clips)
                    {
                        DrawAnimationReplaceRow(clip);
                    }
                    
                    if (layerDict.Count > 1)
                    {
                        EditorGUILayout.EndVertical();
                    }
                    
                    EditorGUILayout.Space(3);
                }
                
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(8);
            }
            
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
            
            if (animationReplacementMap.Count > 0)
            {
                if (GUILayout.Button(Localization.L("アニメーションを置換"), GUILayout.Height(30)))
                {
                    ReplaceAnimations();
                }
            }
        }
        
        private void SearchPrefabsWithControllers()
        {
            if (!PuruBlinkCustomEditorUtils.CheckPlayModeOperation())
                return;

            if (targetControllers.Count == 0)
            {
                EditorUtility.DisplayDialog(Localization.L("エラー"), Localization.L("検索する前にアニメーターコントローラを設定してください。"), "OK");
                return;
            }
            
            List<GameObject> foundPrefabs = new List<GameObject>();
            
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");
            
            foreach (string guid in prefabGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                
                if (prefab != null)
                {
                    AnimatorController controller = GetPrefabController(prefab);
                    if (controller != null && targetControllers.Contains(controller) && !targetPrefabs.Contains(prefab))
                    {
                        foundPrefabs.Add(prefab);
                    }
                }
            }
            
            if (foundPrefabs.Count > 0)
            {
                int addedCount = 0;
                foreach (var prefab in foundPrefabs)
                {
                    if (!targetPrefabs.Contains(prefab))
                    {
                        targetPrefabs.Add(prefab);
                        addedCount++;
                    }
                }
                
                if (addedCount > 0)
                {
                    EditorUtility.DisplayDialog(
                        Localization.L("検索完了"), 
                        Localization.L("Prefabが見つかりました", addedCount), 
                        "OK");
                }
                else
                {
                    EditorUtility.DisplayDialog(
                        Localization.L("検索完了"), 
                        Localization.L("新しいPrefabは見つかりませんでした。（既存のPrefabと重複していました）"), 
                        "OK");
                }
            }
            else
            {
                EditorUtility.DisplayDialog(
                    Localization.L("検索完了"), 
                    Localization.L("指定されたAnimatorControllerを持つPrefabが見つかりませんでした。"), 
                    "OK");
            }
        }
        
        private AnimatorController GetPrefabController(GameObject prefab)
        {
            if (prefab == null) return null;
            
            var animator = prefab.GetComponent<Animator>();
            if (animator != null && animator.runtimeAnimatorController is AnimatorController controller)
            {
                return controller;
            }
            
            var animators = prefab.GetComponentsInChildren<Animator>(true);
            foreach (var anim in animators)
            {
                if (anim.runtimeAnimatorController is AnimatorController ctrl)
                {
                    return ctrl;
                }
            }
            
            try
            {
                var mergeAnimatorComponents = prefab.GetComponentsInChildren<Component>(true)
                    .Where(c => c != null && c.GetType().Name.Contains("MergeAnimator"))
                    .ToArray();
                
                foreach (var maComponent in mergeAnimatorComponents)
                {
                    SerializedObject serializedMA = new SerializedObject(maComponent);
                    SerializedProperty animatorProp = serializedMA.FindProperty("animator");
                    
                    if (animatorProp != null && animatorProp.propertyType == SerializedPropertyType.ObjectReference && 
                        animatorProp.objectReferenceValue != null)
                    {
                        if (animatorProp.objectReferenceValue is AnimatorController maController)
                        {
                            return maController;
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"ModularAvatarコンポーネントの検索中にエラーが発生しました: {ex.Message}");
            }
            
            return null;
        }
        
        private void FindAnimationsInController()
        {
            if (!PuruBlinkCustomEditorUtils.CheckPlayModeOperation())
                return;

            controllerClips.Clear();
            animationReplacementMap.Clear();
            controllerLayerAnimationMap.Clear();
            
            foreach (var controller in targetControllers)
            {
                if (controller == null) continue;
                
                string controllerName = controller.name;
                if (!controllerLayerAnimationMap.ContainsKey(controllerName))
                {
                    controllerLayerAnimationMap[controllerName] = new Dictionary<string, List<AnimationClip>>();
                }
                
                foreach (var layer in controller.layers)
                {
                    if (layer.stateMachine != null)
                    {
                        string layerName = layer.name;
                        if (!controllerLayerAnimationMap[controllerName].ContainsKey(layerName))
                        {
                            controllerLayerAnimationMap[controllerName][layerName] = new List<AnimationClip>();
                        }
                        
                        PuruBlinkCustomEditorUtils.CollectAnimationClipsFromLayer(layer.stateMachine, controllerLayerAnimationMap[controllerName][layerName]);
                        
                        controllerLayerAnimationMap[controllerName][layerName] = controllerLayerAnimationMap[controllerName][layerName]
                            .Distinct()
                            .OrderBy(c => c.name)
                            .ToList();
                    }
                }
            }
            
            foreach (var controllerPair in controllerLayerAnimationMap)
            {
                foreach (var layerPair in controllerPair.Value)
                {
                    controllerClips.AddRange(layerPair.Value);
                }
            }
            controllerClips = controllerClips.Distinct().OrderBy(c => c.name).ToList();
            
            if (controllerClips.Count == 0)
            {
                EditorUtility.DisplayDialog(
                    Localization.L("検索完了"), 
                    Localization.L("アニメーションクリップが見つかりませんでした。"), 
                    "OK");
            }
            else
            {
                EditorUtility.DisplayDialog(
                    Localization.L("検索完了"), 
                    Localization.L("{0}個のアニメーションクリップを見つけました。", controllerClips.Count), 
                    "OK");
            }
        }
        
        private void DrawAnimationReplaceRow(AnimationClip controllerClip)
        {
            if (controllerClip == null)
                return;
                
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            
            EditorGUILayout.ObjectField(controllerClip, typeof(AnimationClip), false, GUILayout.Width(200));
            
            EditorGUILayout.LabelField("→", GUILayout.Width(20));
            
            AnimationClip replacementClip = null;
            if (animationReplacementMap.ContainsKey(controllerClip))
            {
                replacementClip = animationReplacementMap[controllerClip];
            }
            
            EditorGUI.BeginChangeCheck();
            replacementClip = (AnimationClip)EditorGUILayout.ObjectField(replacementClip, typeof(AnimationClip), false, GUILayout.Width(200));
            
            if (EditorGUI.EndChangeCheck())
            {
                if (replacementClip != null)
                {
                    foreach (var controllerPair in controllerLayerAnimationMap)
                    {
                        foreach (var layerPair in controllerPair.Value)
                        {
                            foreach (var clip in layerPair.Value)
                            {
                                if (clip == controllerClip)
                                {
                                    animationReplacementMap[clip] = replacementClip;
                                }
                            }
                        }
                    }
                }
                else if (animationReplacementMap.ContainsKey(controllerClip))
                {
                    List<AnimationClip> clipsToRemove = new List<AnimationClip>();
                    foreach (var controllerPair in controllerLayerAnimationMap)
                    {
                        foreach (var layerPair in controllerPair.Value)
                        {
                            foreach (var clip in layerPair.Value)
                            {
                                if (clip == controllerClip && animationReplacementMap.ContainsKey(clip))
                                {
                                    clipsToRemove.Add(clip);
                                }
                            }
                        }
                    }
                    
                    foreach (var clipToRemove in clipsToRemove)
                    {
                        animationReplacementMap.Remove(clipToRemove);
                    }
                }
            }
            
            if (replacementClip != null)
            {
                int actualReplacementCount = 0;
                foreach (var controllerPair in controllerLayerAnimationMap)
                {
                    foreach (var layerPair in controllerPair.Value)
                    {
                        actualReplacementCount += layerPair.Value.Count(c => c == controllerClip && animationReplacementMap.ContainsKey(c));
                    }
                }
                
                if (actualReplacementCount > 1)
                {
                    GUI.color = new Color(1f, 0.8f, 0.2f, 1f);
                    EditorGUILayout.LabelField($"(×{actualReplacementCount})", GUILayout.Width(40));
                    GUI.color = Color.white;
                }
                else
                {
                    GUILayout.Space(40);
                }
            }
            else
            {
                GUILayout.Space(40);
            }
            
            if (animationReplacementMap.ContainsKey(controllerClip) && GUILayout.Button(Localization.L("クリア"), GUILayout.Width(60)))
            {
                List<AnimationClip> clipsToRemove = new List<AnimationClip>();
                foreach (var controllerPair in controllerLayerAnimationMap)
                {
                    foreach (var layerPair in controllerPair.Value)
                    {
                        foreach (var clip in layerPair.Value)
                        {
                            if (clip == controllerClip && animationReplacementMap.ContainsKey(clip))
                            {
                                clipsToRemove.Add(clip);
                            }
                        }
                    }
                }
                
                foreach (var clipToRemove in clipsToRemove)
                {
                    animationReplacementMap.Remove(clipToRemove);
                }
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void ReplaceAnimations()
        {
            if (!PuruBlinkCustomEditorUtils.CheckPlayModeOperation())
                return;

             if (targetControllers.Count == 0 || animationReplacementMap.Count == 0)
            {
                Debug.LogWarning("アニメーション置換: コントローラまたは置換対象のアニメーションが選択されていません。");
                return;
            }

            string timestamp = DateTime.Now.ToString("yyMMddHHmmss");
            string dynamicOutputFolder = outputFolder + "/Export" + timestamp;
            
            PuruBlinkCustomEditorUtils.CreateOutputFolder(dynamicOutputFolder);
            
            int totalReplacedCount = 0;
            List<string> processedControllers = new List<string>();
            Dictionary<AnimatorController, AnimatorController> controllerReplacementMap = new Dictionary<AnimatorController, AnimatorController>();
            
            foreach (var originalController in targetControllers)
            {
                if (originalController == null) continue;
                
                AnimatorController controller = originalController;
                
                if (copyController)
                {
                    string sourcePath = AssetDatabase.GetAssetPath(originalController);
                    string defaultName = Path.GetFileNameWithoutExtension(sourcePath);
                    string destinationPath = PuruBlinkCustomEditorUtils.GetUniqueFilePath(dynamicOutputFolder, $"{outputPrefix}{defaultName}", ".controller");
                    
                    try
                    {
                        AssetDatabase.CopyAsset(sourcePath, destinationPath);
                        AssetDatabase.SaveAssets();
                        AssetDatabase.Refresh();
                        
                        controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(destinationPath);
                        if (controller == null)
                        {
                            EditorUtility.DisplayDialog(Localization.L("エラー"), Localization.L("コントローラの複製に失敗しました: {0}", destinationPath), "OK");
                            continue;
                        }
                        
                        controllerReplacementMap[originalController] = controller;
                    }
                    catch (Exception ex)
                    {
                        EditorUtility.DisplayDialog(Localization.L("エラー"), Localization.L("コントローラの複製中にエラーが発生しました: {0}", ex.Message), "OK");
                        continue;
                    }
                }
                else
                {
                    controllerReplacementMap[originalController] = originalController;
                }
                
                int replacedCount = 0;
                
                foreach (var layer in controller.layers)
                {
                    if (layer.stateMachine != null)
                    {
                        replacedCount += ReplaceAnimationsInStateMachine(layer.stateMachine);
                    }
                }
                
                if (replacedCount > 0)
                {
                    EditorUtility.SetDirty(controller);
                    totalReplacedCount += replacedCount;
                    processedControllers.Add(controller.name);
                }
            }
            
            int processedPrefabCount = 0;
            List<string> processedPrefabs = new List<string>();
            
            if (targetPrefabs.Count > 0 && controllerReplacementMap.Count > 0)
            {
                foreach (GameObject prefab in targetPrefabs)
                {
                    if (prefab != null)
                    {
                        try
                        {
                            var currentPrefabController = GetPrefabController(prefab);
                            AnimatorController newController = null;
                            
                            if (currentPrefabController != null && controllerReplacementMap.ContainsKey(currentPrefabController))
                            {
                                newController = controllerReplacementMap[currentPrefabController];
                            }
                            
                            if (newController != null)
                            {
                                string prefabPath = AssetDatabase.GetAssetPath(prefab);
                                string prefabName = Path.GetFileNameWithoutExtension(prefabPath);
                                string newPrefabPath = PuruBlinkCustomEditorUtils.GetUniqueFilePath(dynamicOutputFolder, $"{outputPrefix}{prefabName}", ".prefab");
                                
                                GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                                
                                Animator[] animators = instance.GetComponentsInChildren<Animator>(true);
                                foreach (var anim in animators)
                                {
                                    if (anim.runtimeAnimatorController == currentPrefabController)
                                    {
                                        anim.runtimeAnimatorController = newController;
                                    }
                                }
                                
                                try
                                {
                                    var mergeAnimatorComponents = instance.GetComponentsInChildren<Component>(true)
                                        .Where(c => c != null && c.GetType().Name.Contains("MergeAnimator"))
                                        .ToArray();
                                    
                                    foreach (var maComponent in mergeAnimatorComponents)
                                    {
                                        SerializedObject serializedMA = new SerializedObject(maComponent);
                                        SerializedProperty animatorProp = serializedMA.FindProperty("animator");
                                        
                                        if (animatorProp != null && animatorProp.objectReferenceValue == currentPrefabController)
                                        {
                                            animatorProp.objectReferenceValue = newController;
                                            serializedMA.ApplyModifiedProperties();
                                        }
                                    }
                                }
                                catch (System.Exception ex)
                                {
                                    Debug.LogWarning($"ModularAvatarコンポーネントの更新中にエラーが発生しました: {ex.Message}");
                                }
                                
                                GameObject newPrefab;
                                
                                if (createPrefabVariant)
                                {
                                    newPrefab = PrefabUtility.SaveAsPrefabAsset(instance, newPrefabPath);
                                }
                                else
                                {
                                    newPrefab = PrefabUtility.SaveAsPrefabAsset(instance, newPrefabPath);
                                }
                                
                                UnityEngine.Object.DestroyImmediate(instance);
                                
                                if (newPrefab != null)
                                {
                                    processedPrefabCount++;
                                    processedPrefabs.Add(newPrefab.name);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            EditorUtility.DisplayDialog(Localization.L("エラー"), Localization.L("Prefab '{0}' の置換中にエラーが発生しました: {1}", prefab.name, ex.Message), "OK");
                        }
                    }
                }
            }
            
            if (totalReplacedCount > 0 || processedPrefabCount > 0)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                
                string message = Localization.L("{0}個のアニメーションを置き換えました。", totalReplacedCount);
                if (processedControllers.Count > 0)
                {
                    message += $"\n\n{Localization.L("処理されたコントローラ:")}\n{string.Join("\n", processedControllers)}";
                }
                if (processedPrefabCount > 0)
                {
                    message += $"\n\n{Localization.L("{0}個のPrefabを複製・更新しました:", processedPrefabCount)}\n{string.Join("\n", processedPrefabs)}";
                }
                
                EditorUtility.DisplayDialog(Localization.L("置換完了"), message, "OK");
            }
            else
            {
                EditorUtility.DisplayDialog(Localization.L("置換完了"), Localization.L("置き換えるアニメーションが見つかりませんでした。"), "OK");
            }
        }
        
        private int ReplaceAnimationsInStateMachine(AnimatorStateMachine stateMachine)
        {
            int replacedCount = 0;
            
            foreach (var childState in stateMachine.states)
            {
                AnimatorState state = childState.state;
                
                if (state.motion is AnimationClip clip && animationReplacementMap.ContainsKey(clip))
                {
                    state.motion = animationReplacementMap[clip];
                    EditorUtility.SetDirty(state);
                    replacedCount++;
                }
                else if (state.motion is BlendTree blendTree)
                {
                    replacedCount += ReplaceAnimationsInBlendTree(blendTree);
                }
            }
            
            foreach (var childStateMachine in stateMachine.stateMachines)
            {
                replacedCount += ReplaceAnimationsInStateMachine(childStateMachine.stateMachine);
            }
            
            return replacedCount;
        }
        
        private int ReplaceAnimationsInBlendTree(BlendTree blendTree)
        {
            int replacedCount = 0;
            
            var children = blendTree.children;
            bool hasChanges = false;
            
            for (int i = 0; i < children.Length; i++)
            {
                var child = children[i];
                
                if (child.motion is AnimationClip clip && animationReplacementMap.ContainsKey(clip))
                {
                    child.motion = animationReplacementMap[clip];
                    children[i] = child;
                    hasChanges = true;
                    replacedCount++;
                }
                else if (child.motion is BlendTree childTree)
                {
                    replacedCount += ReplaceAnimationsInBlendTree(childTree);
                }
            }
            
            if (hasChanges)
            {
                blendTree.children = children;
                EditorUtility.SetDirty(blendTree);
            }
            
            return replacedCount;
        }

        private void DrawControllerSelectionArea()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("AnimatorController", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            
            if (targetControllers.Count > 0 && targetControllers.Any(c => c != null))
            {
                if (GUILayout.Button(Localization.L("更新"), GUILayout.Width(60)))
                {
                    AnalyzeController();
                }
            }
            EditorGUILayout.EndHorizontal();
            
            if (targetControllers.Count > 0)
            {
                int controllerCount = Mathf.Max(1, targetControllers.Count);
                float dynamicHeight = Mathf.Clamp(controllerCount * 22 + 10, 40, 120);
                
                controllersScrollPosition = EditorGUILayout.BeginScrollView(controllersScrollPosition, GUILayout.Height(dynamicHeight));
                
                for (int i = 0; i < targetControllers.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    
                    EditorGUI.BeginChangeCheck();
                    targetControllers[i] = (AnimatorController)EditorGUILayout.ObjectField(
                        targetControllers[i], typeof(AnimatorController), false, GUILayout.ExpandWidth(true));
                    
                    if (EditorGUI.EndChangeCheck())
                    {
                        AnalyzeController();
                    }
                    
                    if (GUILayout.Button(Localization.L("削除"), GUILayout.Width(Localization.CurrentLanguage == Localization.Language.English ? 60 : 60)))
                    {
                        targetControllers.RemoveAt(i);
                        if (selectedControllerIndex >= targetControllers.Count)
                        {
                            selectedControllerIndex = Mathf.Max(0, targetControllers.Count - 1);
                        }
                        AnalyzeController();
                    }
                    
                    EditorGUILayout.EndHorizontal();
                }
                
                EditorGUILayout.EndScrollView();
            }

            Rect dropArea = GUILayoutUtility.GetRect(0.0f, 35.0f, GUILayout.ExpandWidth(true));
            dropArea.x += 5;
            dropArea.width -= 10;
            GUI.Box(dropArea, "", dropAreaStyle);
            EditorGUI.LabelField(dropArea, Localization.L("AnimatorControllerをここにドラッグ＆ドロップ"), dropAreaTextStyle);
            HandleDragAndDrop(dropArea);
            
            EditorGUILayout.EndVertical();
        }

        private void HandleDragAndDrop(Rect dropArea)
        {
            Event evt = Event.current;
            
            if (!dropArea.Contains(evt.mousePosition))
                return;
                
            switch (evt.type)
            {
                case EventType.DragUpdated:
                    bool hasAnimatorController = DragAndDrop.objectReferences
                        .Any(obj => obj is AnimatorController);
                        
                    DragAndDrop.visualMode = hasAnimatorController ? 
                        DragAndDropVisualMode.Copy : DragAndDropVisualMode.Rejected;
                        
                    evt.Use();
                    break;
                case EventType.DragPerform:
                    DragAndDrop.AcceptDrag();
                    
                    foreach (UnityEngine.Object draggedObject in DragAndDrop.objectReferences)
                    {
                        if (draggedObject is AnimatorController controller)
                        {
                            if (!targetControllers.Contains(controller))
                            {
                                targetControllers.Add(controller);
                            }
                        }
                    }
                    
                    if (targetControllers.Count > 0)
                    {
                        AnalyzeController();
                    }
                    
                    evt.Use();
                    break;
            }
        }

        private void AnalyzeController()
        {
            if (!PuruBlinkCustomEditorUtils.CheckPlayModeOperation())
                return;

            if (currentController == null)
                return;

            gestureParameterMap.Clear();
            gestureParameterMap["GestureLeft"] = new List<ParameterDriverState>();
            gestureParameterMap["GestureRight"] = new List<ParameterDriverState>();
            
            layerNames = currentController.layers.Select(l => l.name).ToArray();
            
            availableParameters.Clear();
            parameterValues.Clear();
            
            foreach (var parameter in currentController.parameters)
            {
                if (!availableParameters.Contains(parameter.name) && !string.IsNullOrEmpty(parameter.name))
                {
                    availableParameters.Add(parameter.name);
                    parameterValues[parameter.name] = parameter.defaultFloat;
                }
            }
            
            List<string> sortedParameters = new List<string>();
            
            var controlParams = availableParameters.Where(p => p.Contains("F_Set")).ToList();
                
            sortedParameters.AddRange(controlParams);
            
            var otherParams = availableParameters.Except(controlParams).ToList();
            sortedParameters.AddRange(otherParams);
            
            availableParameters = sortedParameters;
            
            UpdateParameterValueOptions();

            List<AnimatorControllerLayer> targetLayers = new List<AnimatorControllerLayer>();
            if (selectedLayerIndex >= 0 && selectedLayerIndex < currentController.layers.Length)
            {
                targetLayers.Add(currentController.layers[selectedLayerIndex]);
            }
            else
            {
                targetLayers.AddRange(currentController.layers);
            }

            foreach (var layer in targetLayers)
            {
                if (layer.stateMachine != null)
                {
                    int leftCount = ExtensiveSearchForGestureTransitions(layer.stateMachine, layer.name, "GestureLeft");
                    int rightCount = ExtensiveSearchForGestureTransitions(layer.stateMachine, layer.name, "GestureRight"); 
                }
            }
            
            controllerClips.Clear();
            animationReplacementMap.Clear();
            controllerLayerAnimationMap.Clear();
            
            foreach (var controller in targetControllers)
            {
                if (controller == null) continue;
                
                string controllerName = controller.name;
                if (!controllerLayerAnimationMap.ContainsKey(controllerName))
                {
                    controllerLayerAnimationMap[controllerName] = new Dictionary<string, List<AnimationClip>>();
                }
                
                foreach (var layer in controller.layers)
                {
                    if (layer.stateMachine != null)
                    {
                        string layerName = layer.name;
                        if (!controllerLayerAnimationMap[controllerName].ContainsKey(layerName))
                        {
                            controllerLayerAnimationMap[controllerName][layerName] = new List<AnimationClip>();
                        }
                        
                        PuruBlinkCustomEditorUtils.CollectAnimationClipsFromLayer(layer.stateMachine, controllerLayerAnimationMap[controllerName][layerName]);
                        
                        controllerLayerAnimationMap[controllerName][layerName] = controllerLayerAnimationMap[controllerName][layerName]
                            .Distinct()
                            .OrderBy(c => c.name)
                            .ToList();
                    }
                }
            }
            
            foreach (var controllerPair in controllerLayerAnimationMap)
            {
                foreach (var layerPair in controllerPair.Value)
                {
                    controllerClips.AddRange(layerPair.Value);
                }
            }
            controllerClips = controllerClips.Distinct().OrderBy(c => c.name).ToList();
        }

        private int ExtensiveSearchForGestureTransitions(AnimatorStateMachine stateMachine, string layerPath, string gestureType)
        {
            int foundCount = 0;
            Dictionary<int, HashSet<AnimatorState>> gestureValueToStatesMap = new Dictionary<int, HashSet<AnimatorState>>();
            
            for (int i = 0; i < 8; i++)
            {
                gestureValueToStatesMap[i] = new HashSet<AnimatorState>();
            }
            
            foreach (var transition in stateMachine.anyStateTransitions)
            {
                if (transition.destinationState == null) continue;
                
                bool hasGestureCondition = false;
                int gestureValue = -1;
                
                foreach (var condition in transition.conditions)
                {
                    if (condition.parameter == gestureType)
                    {
                        hasGestureCondition = true;
                        gestureValue = Mathf.RoundToInt(condition.threshold);
                        break;
                    }
                }
                
                if (hasGestureCondition && gestureValue >= 0 && gestureValue <= 7)
                {
                    bool shouldInclude = string.IsNullOrEmpty(selectedParameter);
                    
                    if (!shouldInclude && !string.IsNullOrEmpty(selectedParameter))
                    {
                        if (selectedValues.Count > currentValueIndex && currentValueIndex >= 0)
                        {
                            int targetValue = selectedValues[currentValueIndex];
                            
                            foreach (var condition in transition.conditions)
                            {
                                if (condition.parameter == selectedParameter && 
                                    Mathf.RoundToInt(condition.threshold) == targetValue)
                                {
                                    shouldInclude = true;
                                    break;
                                }
                            }
                        }
                    }
                    
                    if (shouldInclude)
                    {
                        gestureValueToStatesMap[gestureValue].Add(transition.destinationState);
                    }
                }
            }
            
            if (stateMachine.defaultState != null)
            {
                foreach (var transition in stateMachine.defaultState.transitions)
                {
                    if (transition.destinationState == null) continue;
                    
                    bool hasGestureCondition = false;
                    int gestureValue = -1;
                    
                    foreach (var condition in transition.conditions)
                    {
                        if (condition.parameter == gestureType)
                        {
                            hasGestureCondition = true;
                            gestureValue = Mathf.RoundToInt(condition.threshold);
                            break;
                        }
                    }
                    
                    if (hasGestureCondition && gestureValue >= 0 && gestureValue <= 7)
                    {
                        bool shouldInclude = string.IsNullOrEmpty(selectedParameter);
                        
                        if (!shouldInclude && !string.IsNullOrEmpty(selectedParameter))
                        {
                            if (selectedValues.Count > currentValueIndex && currentValueIndex >= 0)
                            {
                                int targetValue = selectedValues[currentValueIndex];
                                
                                foreach (var condition in transition.conditions)
                                {
                                    if (condition.parameter == selectedParameter && 
                                        Mathf.RoundToInt(condition.threshold) == targetValue)
                                    {
                                        shouldInclude = true;
                                        break;
                                    }
                                }
                            }
                        }
                        
                        if (shouldInclude)
                        {
                            gestureValueToStatesMap[gestureValue].Add(transition.destinationState);
                        }
                    }
                }
            }
            
            foreach (var childState in stateMachine.states)
            {
                foreach (var transition in childState.state.transitions)
                {
                    if (transition.destinationState == null) continue;
                    
                    bool hasGestureCondition = false;
                    int gestureValue = -1;
                    
                    foreach (var condition in transition.conditions)
                    {
                        if (condition.parameter == gestureType)
                        {
                            hasGestureCondition = true;
                            gestureValue = Mathf.RoundToInt(condition.threshold);
                            break;
                        }
                    }
                    
                    if (hasGestureCondition && gestureValue >= 0 && gestureValue <= 7)
                    {
                        bool shouldInclude = string.IsNullOrEmpty(selectedParameter);
                        
                        if (!shouldInclude && !string.IsNullOrEmpty(selectedParameter))
                        {
                            if (selectedValues.Count > currentValueIndex && currentValueIndex >= 0)
                            {
                                int targetValue = selectedValues[currentValueIndex];
                                
                                foreach (var condition in transition.conditions)
                                {
                                    if (condition.parameter == selectedParameter && 
                                        Mathf.RoundToInt(condition.threshold) == targetValue)
                                    {
                                        shouldInclude = true;
                                        break;
                                    }
                                }
                            }
                        }
                        
                        if (shouldInclude)
                        {
                            gestureValueToStatesMap[gestureValue].Add(transition.destinationState);
                        }
                    }
                }
            }
            
            for (int gestureValue = 0; gestureValue < 8; gestureValue++)
            {
                foreach (var state in gestureValueToStatesMap[gestureValue])
                {
                    string statePath = $"{layerPath}/{state.name}";
                    
                    foreach (var behaviour in state.behaviours)
                    {
                        if (behaviour == null)
                            continue;
                            
                        string typeName = behaviour.GetType().FullName;
                        
                        if (typeName == VRCParameterDriverTypeName || 
                            typeName.EndsWith("VRCAvatarParameterDriver") ||
                            typeName == ModularAvatarDriverTypeName || 
                            typeName.EndsWith("ParameterSyncStep"))
                        {
                            var parameters = GetParametersFromDriver(behaviour);
                            
                            foreach (var paramState in parameters)
                            {
                                paramState.statePath = statePath;
                                paramState.gestureType = gestureType;
                                paramState.gestureValue = gestureValue;
                                paramState.behaviour = behaviour;
                                
                                gestureParameterMap[gestureType].Add(paramState);
                                foundCount++;
                            }
                        }
                    }
                }
            }
            
            foreach (var childStateMachine in stateMachine.stateMachines)
            {
                string subPath = $"{layerPath}/{childStateMachine.stateMachine.name}";
                foundCount += ExtensiveSearchForGestureTransitions(childStateMachine.stateMachine, subPath, gestureType);
            }
            
            return foundCount;
        }
        
        private void UpdateParameterValueOptions()
        {
            selectedValues.Clear();
            valueLabels.Clear();
            
            if (string.IsNullOrEmpty(selectedParameter))
            {
                selectedValues.Add(0);
                valueLabels.Add("値=0");
                currentValueIndex = 0;
                return;
            }
            
            HashSet<int> usedValues = new HashSet<int>();
            
            if (selectedParameter.StartsWith("F_") || selectedParameter.EndsWith("_set") || 
                selectedParameter.Contains("Set") || selectedParameter.Contains("set"))
            {
                foreach (var layer in currentController.layers)
                {
                    if (layer.stateMachine != null)
                    {
                        CollectUsedParameterValues(layer.stateMachine, layer.name, selectedParameter, usedValues);
                    }
                }
                
                if (usedValues.Count == 0)
                {
                    usedValues.Add(0);
                    usedValues.Add(1);
                }
            }
            else
            {
                usedValues.Add(0);
                usedValues.Add(1);
            }
            
            var sortedValues = usedValues.ToList();
            sortedValues.Sort();
            
            foreach (int value in sortedValues)
            {
                selectedValues.Add(value);
                valueLabels.Add($"値={value}");
            }
            
            if (currentValueIndex >= valueLabels.Count)
            {
                currentValueIndex = 0;
            }
        }
        
        private void CollectUsedParameterValues(AnimatorStateMachine stateMachine, string path, string paramName, HashSet<int> usedValues)
        {
            foreach (var transition in stateMachine.anyStateTransitions)
            {
                foreach (var condition in transition.conditions)
                {
                    if (condition.parameter == paramName)
                    {
                        usedValues.Add(Mathf.RoundToInt(condition.threshold));
                    }
                }
            }
            
            foreach (var childState in stateMachine.states)
            {
                foreach (var transition in childState.state.transitions)
                {
                    foreach (var condition in transition.conditions)
                    {
                        if (condition.parameter == paramName)
                        {
                            usedValues.Add(Mathf.RoundToInt(condition.threshold));
                        }
                    }
                }
            }
            
            foreach (var childStateMachine in stateMachine.stateMachines)
            {
                string subPath = $"{path}/{childStateMachine.stateMachine.name}";
                CollectUsedParameterValues(childStateMachine.stateMachine, subPath, paramName, usedValues);
            }
        }
        
        private List<ParameterDriverState> GetParametersFromDriver(StateMachineBehaviour driver)
        {
            var result = new List<ParameterDriverState>();
            
            var serializedDriver = new SerializedObject(driver);
            
            var parametersProp = serializedDriver.FindProperty("parameters");
            
            if (parametersProp == null)
            {
                parametersProp = serializedDriver.FindProperty("parameterDrivers");
            }
            
            if (parametersProp != null && parametersProp.isArray)
            {
                for (int i = 0; i < parametersProp.arraySize; i++)
                {
                    var paramProp = parametersProp.GetArrayElementAtIndex(i);
                    var nameProp = paramProp.FindPropertyRelative("name");
                    var valueProp = paramProp.FindPropertyRelative("value");
                    
                    if (nameProp != null && valueProp != null)
                    {
                        result.Add(new ParameterDriverState
                        {
                            parameterName = nameProp.stringValue,
                            value = valueProp.floatValue,
                            propertyIndex = i,
                            serializedProperty = paramProp,
                            behaviour = driver
                        });
                    }
                }
            }
            
            return result;
        }

        private void UpdateParameterValue(ParameterDriverState paramState, float newValue)
        {
            if (paramState.behaviour == null)
                return;

            var serializedObj = new SerializedObject(paramState.behaviour);
            
            var parametersProp = serializedObj.FindProperty("parameters");
            
            if (parametersProp == null)
            {
                parametersProp = serializedObj.FindProperty("parameterDrivers");
            }
            
            if (parametersProp != null && parametersProp.isArray && paramState.propertyIndex < parametersProp.arraySize)
            {
                var paramProp = parametersProp.GetArrayElementAtIndex(paramState.propertyIndex);
                var valueProp = paramProp.FindPropertyRelative("value");
                
                if (valueProp != null)
                {
                    valueProp.floatValue = newValue;
                    serializedObj.ApplyModifiedProperties();
                    paramState.value = newValue;
                    
                    EditorUtility.SetDirty(paramState.behaviour);
                    EditorUtility.SetDirty(currentController);
                    
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }
            }
        }

        [InitializeOnLoadMethod]
        private static void RegisterPlayModeStateChanged()
        {
            if (registeredPlayModeCallback)
                return;
                
            registeredPlayModeCallback = true;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                EditorApplication.delayCall += () => {
                    var windows = Resources.FindObjectsOfTypeAll<PuruBlinkCustomEditor>();
                    foreach (var window in windows)
                    {
                        try {
                            window.Repaint();
                        }
                        catch (System.Exception) {
                        }
                    }
                };
            }
        }
    }

    public class ParameterDriverState
    {
        public string parameterName;
        public float value;
        public string statePath;
        public string gestureType;
        public int gestureValue;
        public StateMachineBehaviour behaviour;
        public SerializedProperty serializedProperty;
        public int propertyIndex;
    }

    public class GestureStateInfo
    {
        public string gestureType;
        public int gestureValue;
    }
}
