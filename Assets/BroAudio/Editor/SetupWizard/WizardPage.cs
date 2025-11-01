using System;
using UnityEditor;
using UnityEngine;

namespace Ami.BroAudio.Editor.Setting
{
    public enum SetupDepth
    {
        Essential = 0,      // Minimal setup with only essential settings
        Advanced = 1,      // More detailed setup with advanced options
        Comprehensive = 2  // Complete setup with all available options
    }
    
    public abstract class WizardPage
    {
        private const float DocReferenceWidthMargin = 10f;
        private const float DocReferencesWidthRatio = 0.85f;

        private readonly GUIContent _buttonGUIContent = new GUIContent("Open Library Manager", 
            "It's recommended to open the Library Manager so you can see the difference side by side.\n" +
            "If you don't see any changes, try hovering your mouse over the Library Manager window to trigger a repaint.");
        public abstract string PageTitle { get; }
        public abstract string PageDescription { get; }
        public virtual SetupDepth RequiredDepth => SetupDepth.Essential;
        protected virtual (string Name, string Url)[] DocReferences { get; set; }
        protected PreferencesDrawer Drawer { get; private set; }
        
        public abstract void DrawContent();
        
        public virtual void OnEnable()
        {
            
        }

        public void DrawDocReferences()
        {
            if (DocReferences == null || DocReferences.Length == 0)
            {
                return;
            }
            
            DrawSectionHeader("Documentation References");
            
            EditorGUILayout.BeginHorizontal();
            float currentWidth = 0f;
            var linkContent = new GUIContent();
            foreach (var docPage in DocReferences)
            {
                linkContent.text = docPage.Name;
                linkContent.tooltip = docPage.Url;
                var width = EditorStyles.label.CalcSize(linkContent).x + DocReferenceWidthMargin;
                
                if (currentWidth + width > EditorGUIUtility.currentViewWidth * DocReferencesWidthRatio)
                {
                    EditorGUILayout.EndHorizontal();
                    currentWidth = 0;
                    EditorGUILayout.BeginHorizontal();
                }
                
                if (GUILayout.Button(linkContent, EditorStyles.linkLabel, GUILayout.Width(width)))
                {
                    Application.OpenURL(docPage.Url);
                }
                currentWidth += width;
            }
            EditorGUILayout.EndHorizontal();
        }

        public void SetDrawer(PreferencesDrawer drawer)
        {
            Drawer = drawer;
        }
        
        protected void DrawSectionHeader(string title)
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            EditorGUILayout.Space(2);
        }

        protected void DrawOpenLibraryManagerButton(bool selectDemo)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(_buttonGUIContent, GUILayout.Width(200), GUILayout.Height(30)))
            {
                if (selectDemo && BroEditorUtility.TryGetDemoData(out var demoAsset, out var entity))
                {
                    LibraryManagerWindow.ShowWindowAndLocateToEntity(demoAsset.AssetGUID, entity.ID);
                    EditorGUILayout.EndHorizontal();
                    return;
                }
                LibraryManagerWindow.ShowWindow();
            }
            
            DrawAdditionalTooltip();
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawAdditionalTooltip()
        {
            var rect = GUILayoutUtility.GetLastRect();
            rect.x = rect.xMax + 10f;
            rect.width = 20f;
            var content = new GUIContent(_buttonGUIContent) { text = "?"};
            var style = new GUIStyle(EditorStyles.linkLabel) { alignment = TextAnchor.MiddleCenter};
            EditorGUI.LabelField(rect, content, style);
        }
    }
    
    public static class WizardPageFactory
    {
        public static T CreatePage<T>(this PreferencesDrawer drawer) where T : WizardPage, new()
        {
            var page = Activator.CreateInstance<T>();
            page?.SetDrawer(drawer);
            return page;
        }
    }
}