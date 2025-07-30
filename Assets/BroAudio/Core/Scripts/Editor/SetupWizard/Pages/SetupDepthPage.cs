using System;
using UnityEditor;
using UnityEngine;

namespace Ami.BroAudio.Editor.Setting
{
    public class SetupDepthPage : WizardPage
    {
        private const string PageCountDescription = "{0} pages to configure.";
        private const int NonConfigurablePage = 2;
        private readonly string[] _depthNames = { nameof(SetupDepth.Essential), nameof(SetupDepth.Advanced), nameof(SetupDepth.Comprehensive) };
        private readonly string[] _depthDescriptions = {
            "Configure only the essential settings needed to get started.",
            "Configure important settings with additional customization options.",
            "Configure all available settings for complete customization."
        };
        
        private readonly Action<SetupDepth> _onDepthChanged;
        private readonly Func<int> _onGetPageCount;

        private SetupDepth _selectedDepth = SetupDepth.Essential;
        
        public override string PageTitle => "Setup Wizard";
        public override string PageDescription => " You can always change individual settings later.";
        public SetupDepthPage(Action<SetupDepth> onDepthChanged, Func<int> onGetPageCount) 
        {
            _onDepthChanged = onDepthChanged;
            _onGetPageCount = onGetPageCount;
        }

        public override void DrawContent()
        {
            EditorGUILayout.Space(10);
            
            DrawSectionHeader("Setup Depth");
            EditorGUILayout.LabelField("Choose how deep you want this setup to be.");
            DrawSlider();
            DrawDepthLabels();
            EditorGUILayout.Space(20);
            DrawDescription();
            EditorGUILayout.EndVertical();
        }

        private void DrawSlider()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            int newValue = (int)GUILayout.HorizontalSlider((int)_selectedDepth, 0, 2);
            if (EditorGUI.EndChangeCheck())
            {
                _selectedDepth = (SetupDepth)newValue;
                _onDepthChanged?.Invoke(_selectedDepth);
            }
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawDepthLabels()
        {
            EditorGUILayout.Space(EditorGUIUtility.singleLineHeight);
            EditorGUILayout.BeginHorizontal();
            GUIStyle labelStyle = new GUIStyle(EditorStyles.miniLabel);
            var originalColor = GUI.color;
            for (int i = 0; i < _depthNames.Length; i++)
            {
                GUI.color = i == (int)_selectedDepth ? originalColor : new Color(0.7f, 0.7f, 0.7f);
                labelStyle.alignment = GetAlignment(i);
                GUILayout.Label(_depthNames[i], labelStyle);
            }
            GUI.color = originalColor;
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawDescription()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(_depthNames[(int)_selectedDepth], EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField(_depthDescriptions[(int)_selectedDepth], EditorStyles.wordWrappedLabel);
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField(string.Format(PageCountDescription, _onGetPageCount() - NonConfigurablePage), EditorStyles.miniLabel);
        }

        private static TextAnchor GetAlignment(int index) => index switch
        {
            0 => TextAnchor.MiddleLeft,
            1 => TextAnchor.MiddleCenter,
            2 => TextAnchor.MiddleRight,
            _ => TextAnchor.MiddleCenter
        };
    }
}
