using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace VRCFaceController
{
    public static class PuruBlinkCustomEditorUtils
    {
        public static Texture2D CreateColorTexture(Color color)
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        public static Texture2D CreateSimpleBorderTexture()
        {
            int size = 12;
            Texture2D texture = new Texture2D(size, size);
            
            Color fillColor = new Color(0, 0, 0, 0);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    texture.SetPixel(x, y, fillColor);
                }
            }
            
            Color borderColor = EditorGUIUtility.isProSkin 
                ? new Color(0.7f, 0.7f, 0.7f, 0.6f)
                : new Color(0.4f, 0.4f, 0.4f, 0.6f);
            
            for (int i = 0; i < size; i++)
            {
                texture.SetPixel(i, size - 1, borderColor);
                texture.SetPixel(i, 0, borderColor);
                texture.SetPixel(0, i, borderColor);
                texture.SetPixel(size - 1, i, borderColor);
            }
            
            texture.Apply();
            return texture;
        }
        
        public static string GetUniqueFilePath(string folder, string baseName, string extension)
        {
            string path = Path.Combine(folder, baseName + extension);
            if (!File.Exists(path))
                return path;
                
            int counter = 1;
            while (File.Exists(Path.Combine(folder, $"{baseName}_{counter}{extension}")))
            {
                counter++;
            }
            
            return Path.Combine(folder, $"{baseName}_{counter}{extension}");
        }
        
        public static void CreateOutputFolder(string folderPath)
        {
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                try
                {
                    string[] folderParts = folderPath.Split('/');
                    string currentPath = folderParts[0];
                    
                    for (int i = 1; i < folderParts.Length; i++)
                    {
                        string parentFolder = currentPath;
                        currentPath = Path.Combine(currentPath, folderParts[i]);
                        
                        if (!AssetDatabase.IsValidFolder(currentPath))
                        {
                            AssetDatabase.CreateFolder(parentFolder, folderParts[i]);
                        }
                    }
                }
                catch (Exception ex)
                {
                    EditorUtility.DisplayDialog(PuruBlinkCustomEditorLocalization.L("Error"), 
                        PuruBlinkCustomEditorLocalization.L("FolderCreationError", ex.Message), 
                        PuruBlinkCustomEditorLocalization.L("OK"));
                }
            }
        }
        
        public static void CollectAnimationClipsFromLayer(AnimatorStateMachine stateMachine, List<AnimationClip> layerClips)
        {
            foreach (var childState in stateMachine.states)
            {
                if (childState.state.motion is AnimationClip clip)
                {
                    layerClips.Add(clip);
                }
                else if (childState.state.motion is BlendTree blendTree)
                {
                    CollectAnimationClipsFromBlendTreeForLayer(blendTree, layerClips);
                }
            }
            
            foreach (var childStateMachine in stateMachine.stateMachines)
            {
                CollectAnimationClipsFromLayer(childStateMachine.stateMachine, layerClips);
            }
        }
        
        public static void CollectAnimationClipsFromBlendTreeForLayer(BlendTree blendTree, List<AnimationClip> layerClips)
        {
            foreach (var child in blendTree.children)
            {
                if (child.motion is AnimationClip clip)
                {
                    layerClips.Add(clip);
                }
                else if (child.motion is BlendTree childTree)
                {
                    CollectAnimationClipsFromBlendTreeForLayer(childTree, layerClips);
                }
            }
        }
    }
}
