using System;
using UnityEditor;
using UnityEngine;

namespace Ami.BroAudio.Editor.Setting
{
    public class SetupDepthPage : WizardPage
    {
        private const string PageCountDescription = "{0} pages to configure.";
        private const int NonConfigurablePage = 2;
        private const string AdditionalNotes = "* All settings use recommended defaults. If you're satisfied with a default or unsure what a setting does, just click <b>Next</b> to skip it.\n" +
                                               "* You can modify any option anytime under <b>Tools > BroAudio > Preferences</b>.";
        private const string SetupDepthDescription = "Choose how deep you want this setup to be.";

        private readonly string[] _depthNames = { nameof(SetupDepth.Essential), nameof(SetupDepth.Advanced), nameof(SetupDepth.Comprehensive) };
        private readonly string[] _depthDescriptions = {
            "Configure core settings only. Perfect for first-time users or those new to Unity audio implementation.",
            "Essential settings plus additional options. Recommended if you've used Bro Audio before or have Unity audio experience.",
            "Access to all settings (except editor themes). Best for power users who want complete control."
        };
        
        private readonly Action<SetupDepth> _onDepthChanged;
        private readonly Func<int> _onGetPageCount;

        private SetupDepth _selectedDepth = SetupDepth.Essential;
        private bool _isShowAdditionalNotes;
        
        public override string PageTitle => "Setup Wizard";
        public override string PageDescription => "This wizard will guide you through setting up Bro Audio's customizable options.";
        public SetupDepthPage(Action<SetupDepth> onDepthChanged, Func<int> onGetPageCount) 
        {
            _onDepthChanged = onDepthChanged;
            _onGetPageCount = onGetPageCount;
        }

        public override void DrawContent()
        {
            DrawSectionHeader("Setup Depth");
            EditorGUILayout.LabelField(SetupDepthDescription);
            DrawSlider();
            DrawDepthLabels();
            EditorGUILayout.Space(10);
            DrawDescription();
            DrawAdditionalNotes();
        }

        private void DrawSlider()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            int newValue = (int)Math.Round(GUILayout.HorizontalSlider((int)_selectedDepth, 0, 2), MidpointRounding.AwayFromZero);
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
            EditorGUILayout.LabelField(_depthDescriptions[(int)_selectedDepth], EditorStyles.wordWrappedLabel);
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField(string.Format(PageCountDescription, _onGetPageCount() - NonConfigurablePage), EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
        }
        
        private void DrawAdditionalNotes()
        {
            _isShowAdditionalNotes = EditorGUILayout.BeginFoldoutHeaderGroup(_isShowAdditionalNotes, "Additional Notes");
            if (_isShowAdditionalNotes)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    var style = new GUIStyle(EditorStyles.wordWrappedLabel);
                    style.richText = true;
                    EditorGUILayout.LabelField(AdditionalNotes, style);
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
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
