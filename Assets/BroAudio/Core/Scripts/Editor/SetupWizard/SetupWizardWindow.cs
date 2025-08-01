using System.Collections.Generic;
using Ami.Extension;
using UnityEditor;
using UnityEngine;

namespace Ami.BroAudio.Editor.Setting
{
    public class SetupWizardWindow : EditorWindow
    {
        public const float WindowPadding = 10f;
        private const float WindowWidth = 420f;
        private const float WindowHeight = 350f;
        private const string TitleFormat = "<b><size=25>{0}</size></b>";
        
        private WizardPage[] _allPages;
        private List<WizardPage> _activePages = new List<WizardPage>();
        private int _currentPageIndex = 0;
        private SetupDepth _setupDepth = SetupDepth.Essential;
        private GUIStyle _middleCenterLabel;
        private SerializedObject _runtimeSettingSO;
        private SerializedObject _editorSettingSO;
        private PreferencesDrawer _preferencesDrawer;
        private Vector2 _scrollPosition;

        [MenuItem(BroAudioGUISetting.SetupWizardMenuPath, false, BroAudioGUISetting.SetupWizardMenuIndex)]
        public static void ShowWindow()
        {
            var window = GetWindow<SetupWizardWindow>();
            window.minSize = new Vector2(WindowWidth, WindowHeight);
            window.maxSize = new Vector2(WindowWidth * 1.5f, WindowHeight * 1.5f);
            window.titleContent = new GUIContent("Setup Wizard", EditorGUIUtility.IconContent(IconConstant.SetupWizard).image);
            window.Show();
        }

        public static void CheckAndShowForFirstSetup()
        {
            if (BroEditorUtility.EditorSetting.HasSetupWizardAutoLaunched)
            {
                return;
            }
            
            ShowWindow();
            BroEditorUtility.EditorSetting.HasSetupWizardAutoLaunched = true;
            EditorUtility.SetDirty(BroEditorUtility.EditorSetting);
        }

        private void OnEnable()
        {
            _runtimeSettingSO = new SerializedObject(BroEditorUtility.RuntimeSetting);
            _editorSettingSO = new SerializedObject(BroEditorUtility.EditorSetting);
            _preferencesDrawer = new PreferencesDrawer(_runtimeSettingSO, _editorSettingSO, new BroInstructionHelper());
            InitializePages();
            UpdateActivePages();

            foreach (var page in _allPages)
            {
                page.OnEnable();
            }
        }

        private void InitializePages()
        {
            _allPages = new WizardPage[]
            {
                 new SetupDepthPage(OnDepthChanged, GetActivePageCount),
                // Essential
                CreatePage<UpdateModePage>(),
                CreatePage<AlwaysPlayAsBGMPage>(),
                CreatePage<DisplayedPropertiesPage>(),

                // Advanced
                CreatePage<AudioPlayerSettingsPage>(),
                CreatePage<OutputPathPage>(),
                CreatePage<DefaultPlaybackGroupPage>(),
                CreatePage<ChainedPlayModePage>(),
                
                // Comprehensive
                CreatePage<AudioFilterSlopePage>(),
                CreatePage<EasingPage>(),
                CreatePage<GUICustomizationPage>(),
#if PACKAGE_ADDRESSABLES
                CreatePage<AddressableConversionPage>(),
#endif
                // Final page
                new CompletionPage()
            };
        }

        private void OnDepthChanged(SetupDepth newDepth)
        {
            _setupDepth = newDepth;
            UpdateActivePages();
            _currentPageIndex = 0;
        }

        private void UpdateActivePages()
        {
            _activePages.Clear();

            foreach (var page in _allPages)
            {
                if (page.RequiredDepth <= _setupDepth || page is SetupDepthPage || page is CompletionPage)
                {
                    _activePages.Add(page);
                }
            }
        }

        private T CreatePage<T>() where T : WizardPage, new()
        {
            return _preferencesDrawer.CreatePage<T>();
        }
        
        private int GetActivePageCount()
        {
            return _activePages.Count;
        }

        private void OnGUI()
        {
            if (_activePages.Count == 0 || _currentPageIndex >= _activePages.Count)
            {
                return;
            }

            if (_middleCenterLabel == null)
            {
                _middleCenterLabel = new GUIStyle(GUI.skin.label);
                _middleCenterLabel.alignment = TextAnchor.MiddleCenter;
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.Space(WindowPadding);

            EditorGUILayout.BeginVertical();
            var currentPage = _activePages[_currentPageIndex];
            // Draw page title
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField(string.Format(TitleFormat, currentPage.PageTitle), GUIStyleHelper.MiddleCenterRichText);
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField(currentPage.PageDescription, EditorStyles.wordWrappedLabel);
            EditorGUI.DrawRect(EditorGUILayout.GetControlRect(GUILayout.Height(1f)), Color.gray);
            EditorGUILayout.Space(10);
            
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            
            currentPage.DrawContent();
            _runtimeSettingSO.ApplyModifiedProperties();
            _editorSettingSO.ApplyModifiedProperties();
            
            GUILayout.FlexibleSpace();
            currentPage.DrawDocReferences();
            EditorGUILayout.EndScrollView();
            
            EditorGUILayout.Space(10);
            DrawNavigationFooter();

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(WindowPadding);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawNavigationFooter()
        {
            EditorGUILayout.LabelField($"Page {_currentPageIndex + 1} / {_activePages.Count}", EditorStyles.centeredGreyMiniLabel);

            EditorGUILayout.BeginHorizontal();

            EditorGUI.BeginDisabledGroup(_currentPageIndex <= 0);
            if (GUILayout.Button("Back", GUILayout.Width(100)))
            {
                _currentPageIndex--;
            }
            EditorGUI.EndDisabledGroup();

            GUILayout.FlexibleSpace();
            
            string buttonText = _currentPageIndex < _activePages.Count - 1 ? "Next" : "Finish";
            if (GUILayout.Button(buttonText, GUILayout.Width(100)))
            {
                if (_currentPageIndex < _activePages.Count - 1)
                {
                    _currentPageIndex++;
                }
                else
                {
                    Close();
                }
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}
