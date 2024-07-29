#if BroAudio_DevOnly
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System;
using Ami.BroAudio.Tools;
using System.Reflection;
using static Ami.BroAudio.Editor.Setting.BroAudioGUISetting;

namespace Ami.Extension
{
    public class GUIShowcase : EditorWindow
    {
        public const float CursorWidth = 200f;
        public const float GUIStyleWidth = 220f;
        public const float Gap = 10f;

        private bool _isHover = false;
        private bool _isActive = false;
        private bool _isOn = false;
        private bool _hasKeyboardFocus = false;
        private Vector2 _scrollPos = default;
        private List<GUIStyle> _guiStyleList = new List<GUIStyle>();
        private MouseCursor[] _allCursorTypes = null;
        public MouseCursor[] AllCursorTypes
        {
            get
            {
                _allCursorTypes ??= (MouseCursor[])Enum.GetValues(typeof(MouseCursor));
                return _allCursorTypes;
            }
        }

        [MenuItem(BroName.MenuItem_BroAudio + "GUI Showcase", priority = DevToolsMenuIndex + 1)]
        public static void ShowWindow()
        {
            EditorWindow window = EditorWindow.GetWindow<GUIShowcase>();
            window.minSize = new Vector2(960f, 540f);
            window.Show();
        }

        private void OnEnable()
        {
            Type styleClass = typeof(EditorStyles);
            PropertyInfo[] properties = styleClass.GetProperties();

            foreach (PropertyInfo property in properties)
            {
                _guiStyleList.Add(property.GetValue(null, null) as GUIStyle);
            }
        }

        private void OnGUI()
        {
            if (Event.current.type == EventType.Repaint)
            {
                float topGap = EditorGUIUtility.singleLineHeight * 2;
                Rect cursorWindow = new Rect(Gap, topGap - _scrollPos.y, CursorWidth, (AllCursorTypes.Length + 3) * EditorGUIUtility.singleLineHeight);
                GUI.skin.window.Draw(cursorWindow, false, false, false, false);

                Rect styleWindow = new Rect(Gap * 2 + cursorWindow.width, topGap - _scrollPos.y, GUIStyleWidth * 2, (_guiStyleList.Count + 3) * EditorGUIUtility.singleLineHeight);
                GUI.skin.window.Draw(styleWindow, false, false, false, false);
            }

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.Space();
                EditorGUILayout.BeginVertical();
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Cursor Type".SetSize(25), GUIStyleHelper.RichText);
                    EditorGUILayout.Space();

                    using (new EditorGUI.IndentLevelScope())
                    {
                        foreach (MouseCursor cursorType in AllCursorTypes)
                        {
                            EditorGUILayout.LabelField(cursorType.ToString());
                            EditorGUIUtility.AddCursorRect(GUILayoutUtility.GetLastRect(), cursorType);
                        }
                    }
                }
                EditorGUILayout.EndVertical();

                EditorGUILayout.BeginVertical();
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField("GUIStyle".SetSize(25), GUIStyleHelper.RichText);
                        EditorGUILayout.BeginVertical();
                        {
                            _isHover = EditorGUILayout.ToggleLeft("isHover", _isHover, GUILayout.Width(80f));
                            Rect onRect = GUILayoutUtility.GetLastRect(); onRect.x += 80f;
                            _isOn = EditorGUI.ToggleLeft(onRect, "isOn", _isOn);
                            _isActive = EditorGUILayout.ToggleLeft("isActive", _isActive, GUILayout.Width(80f));
                            Rect activeRect = GUILayoutUtility.GetLastRect(); activeRect.x += 80f;
                            _hasKeyboardFocus = EditorGUI.ToggleLeft(activeRect, "hasKeyboardFocus", _hasKeyboardFocus);
                        }
                        EditorGUILayout.EndVertical();
                    }
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.Space();
                    using (new EditorGUI.IndentLevelScope())
                    {
                        foreach (GUIStyle style in _guiStyleList)
                        {
                            if(style != null)
                            {
                                EditorGUILayout.LabelField(style.name.ToString());
                                if(Event.current.type == EventType.Repaint)
                                {
                                    Rect styleRect = GUILayoutUtility.GetLastRect();
                                    styleRect.x += GUIStyleWidth;
                                    styleRect.width = GUIStyleWidth;
                                    if(style.name.Contains("Label"))
                                    {
                                        EditorGUI.LabelField(styleRect, style.name, style);
                                    }
                                    else
                                    {
                                        style.Draw(styleRect, _isHover, _isActive, false, false);
                                    }
                                }
                            }
                        }
                    }
                }
                EditorGUILayout.EndVertical();
                GUILayout.FlexibleSpace();
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();

            EditorGUILayout.EndScrollView();
        }
    }
}
#endif