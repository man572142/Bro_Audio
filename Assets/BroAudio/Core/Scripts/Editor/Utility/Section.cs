using System;
using Ami.Extension;
using UnityEditor;
using UnityEngine;

namespace Ami.BroAudio.Editor
{
    public struct Section : IDisposable
    {
        private EditorGUI.IndentLevelScope _indentLevelScope;
        private EditorScriptingExtension.LabelWidthScope _labelWidthScope;

        public static Section NewSection(string title, Rect rect, float labelWidthMultiplier = 1)
        {
            GUIContent content = new GUIContent(title.ToWhiteBold());
            return new Section(content, rect, labelWidthMultiplier);
        }
            
        public static Section NewSection(GUIContent title, Rect rect, float labelWidthMultiplier = 1)
        {
            return new Section(title, rect, labelWidthMultiplier);
        }

        private Section(GUIContent title, Rect rect, float labelWidthMultiplier = 1f)
        {
            EditorGUI.LabelField(rect, title, GUIStyleHelper.RichText);
            _indentLevelScope = new EditorGUI.IndentLevelScope();
            _labelWidthScope = new EditorScriptingExtension.LabelWidthScope(EditorGUIUtility.labelWidth * labelWidthMultiplier);
        }

        public void Dispose()
        {
            _indentLevelScope?.Dispose();
            _labelWidthScope.Dispose();
        }
    }
}