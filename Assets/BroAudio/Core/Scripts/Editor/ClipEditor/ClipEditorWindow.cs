using UnityEngine;
using UnityEditor;
using Ami.Extension;
using System;
using System.IO;
using static Ami.BroAudio.Editor.Setting.BroAudioGUISetting;
using static Ami.Extension.EditorScriptingExtension;
using Ami.BroAudio.Tools;
using Ami.BroAudio.Data;

namespace Ami.BroAudio.Editor
{
	public class ClipEditorWindow : EditorWindow, IHasCustomMenu
	{
        private const string SaveFilePanelTitle = "Save as a new file";
        private const string ConfirmOverwriteTitle = "Confirm overwrite";
        private const string PrefKey = "LastClipEditorSavePath";
        private const float Gap = 50f;
        private const string DefaultFileExt = "wav";
        private static Vector2 DefaultWindowSize => new Vector2(640f, 490f);

		public event Action OnChangeAudioClip;

        [SerializeField] private BroAudioClip _clip = null;
        [SerializeField] private bool _isReverse = false;
        [SerializeField] private bool _isMono = false;
        [SerializeField] private MonoConversionMode _monoMode = MonoConversionMode.Downmixing;
        [SerializeField] private float _volume = AudioConstant.FullVolume;
        private DrawClipPropertiesHelper _clipPropHelper = new DrawClipPropertiesHelper();
		private SerializedTransport _transport = default;		
		private bool _isVolumeSnapped = false;
		private GenericMenu _monoModeMenu = null;
		private bool _isLoop = false;
		private bool _isPlaying = false;
        private bool _isShowPreferences = false;
        private SerializedObject _serializedObject = null;
        private SerializedObject _editorSettingSO = null;
        private SerializedProperty _audioClipProp = null;

        private string _currSavingFilePath = null;
		private BroInstructionHelper _instruction = new BroInstructionHelper();
        private PreviewRequest _currentPreviewRequest;

        private bool HasEdited
		{
			get
			{
				return !Mathf.Approximately(_volume, AudioConstant.FullVolume)
					|| _transport.HasDifferentPosition
					|| _transport.HasFading
					|| _isReverse
					|| _isMono;
			}
		}

        private AudioClip TargetClip
		{
			get => _audioClipProp.objectReferenceValue as AudioClip;
            set
			{
				bool hasChanged = value != _audioClipProp.objectReferenceValue;
                _audioClipProp.objectReferenceValue = value;

				if (hasChanged)
				{
                    _audioClipProp.serializedObject.ApplyModifiedProperties();
                    OnChangeAudioClip?.Invoke();
				}
			}
		}

		[MenuItem(ClipEditorMenuPath,false,ClipEditorMenuIndex)]
		public static void ShowWindow()
		{
			EditorWindow window = GetWindow(typeof(ClipEditorWindow));
			window.minSize = DefaultWindowSize;
			window.titleContent = new GUIContent(BroName.MenuItem_ClipEditor, EditorGUIUtility.IconContent(IconConstant.AudioClip).image);
			window.Show();
		}

		private void OnEnable()
		{
            OnChangeAudioClip += ResetSetting;
            Undo.undoRedoPerformed += Repaint;

            _serializedObject = new SerializedObject(this);
            _audioClipProp =  _serializedObject.FindProperty(nameof(_clip)).FindPropertyRelative(BroAudioClip.NameOf.AudioClip);

			ResetSetting();
		}

        private void OnDisable()
		{
			OnChangeAudioClip -= ResetSetting;
            Undo.undoRedoPerformed -= Repaint;
        }

        private void OnFocus()
        {
            EditorAudioPreviewer.Instance.OnPlaybackIndicatorUpdate += Repaint;
        }

        private void OnLostFocus()
		{
            EditorAudioPreviewer.Instance.OnPlaybackIndicatorUpdate -= Repaint;
            EditorAudioPreviewer.Instance.StopAllClips();
			_isPlaying = false;

        }

		private void OnGUI()
		{
            _serializedObject.Update();
            EditorGUILayout.Space(EditorGUIUtility.singleLineHeight);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            EditorGUILayout.BeginVertical(GUILayout.Width(position.width * 0.8f));
			DrawAudioClipObjectField();
            EditorGUILayout.Space();

			if (TargetClip == null || _transport == null)
			{
                TargetClip = null;
                EditorGUILayout.Space(position.height * 0.3f);
                EditorGUILayout.LabelField("No Clip".SetSize(30).SetColor(Color.white), GUIStyleHelper.MiddleCenterRichText);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndVertical();
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
                return;
			}

            EditorGUILayout.Space();
			DrawClipWaveform(position.height * 0.3f, out Rect previewRect);
            EditorGUILayout.Space();
            EditorAudioPreviewer.Instance.PlaybackIndicator?.Draw(position.SetPosition(0f,0f));

            DrawPlaybackBar(previewRect);
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            DrawVolumeSlider();
            DrawPlaybackPositionField();
            DrawFadingField();

            var isReverseProp = _serializedObject.FindProperty(nameof(_isReverse));
            var isMonoProp = _serializedObject.FindProperty(nameof(_isMono));
            isReverseProp.boolValue = EditorGUILayout.Toggle("Reverse", isReverseProp.boolValue);

            using (new EditorGUI.DisabledScope(TargetClip.channels != 2))
            {
                isMonoProp.boolValue = EditorGUILayout.Toggle("Convert To Mono", isMonoProp.boolValue);
                using (new EditorGUI.DisabledScope(!_isMono))
                {
                    Rect monoModeRect = GUILayoutUtility.GetLastRect().DissolveHorizontal(0.5f);
                    if (TargetClip.channels > 2)
                    {
                        EditorGUI.LabelField(monoModeRect, "Surround sound is not supported");
                    }
                    else if (EditorGUI.DropdownButton(monoModeRect, new GUIContent(_monoMode.ToString()), FocusType.Keyboard))
                    {
                        ShowMonoModeMenu(monoModeRect);
                    }
                }
            }

            EditorGUILayout.Space(EditorGUIUtility.singleLineHeight);
            _isShowPreferences = EditorGUILayout.BeginFoldoutHeaderGroup(_isShowPreferences, "Preferences");
            if (_isShowPreferences)
            {
                using (new EditorGUI.IndentLevelScope(1))
                {
                    GetPreferenceProperties(out var editNewClipOptionProp, out var pingNewClipOptionProp);
                    editNewClipOptionProp.boolValue = EditorGUILayout.ToggleLeft("Edit The New Clip After [Save As]", editNewClipOptionProp.boolValue);
                    pingNewClipOptionProp.boolValue = EditorGUILayout.ToggleLeft("Show The New Clip In Project Window After [Save As]", pingNewClipOptionProp.boolValue);
                    _editorSettingSO.ApplyModifiedProperties();
                }               
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            GUILayout.FlexibleSpace();
            EditorGUI.BeginDisabledGroup(!HasEdited);
            {
                DrawSavingButton();
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.Space();
            EditorGUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            _serializedObject.ApplyModifiedProperties();
        }

        private void GetPreferenceProperties(out SerializedProperty editNewClipOptionProp, out SerializedProperty pingNewClipOptionProp)
        {
            _editorSettingSO ??= new SerializedObject(BroEditorUtility.EditorSetting);
            _editorSettingSO.Update();

            editNewClipOptionProp = _editorSettingSO.FindProperty(nameof(EditorSetting.EditTheNewClipAfterSaveAs));
            pingNewClipOptionProp = _editorSettingSO.FindProperty(nameof(EditorSetting.PingTheNewClipAfterSaveAs));
        }

		private void ShowMonoModeMenu(Rect rect)
        {
			if(_monoModeMenu == null)
			{
                _monoModeMenu = new GenericMenu();
                _monoModeMenu.AddDisabledItem(new GUIContent("Mix all channels into one"));
                AddModeItem(MonoConversionMode.Downmixing);
                _monoModeMenu.AddSeparator(string.Empty);

                _monoModeMenu.AddDisabledItem(new GUIContent("Select one channel"));
                AddModeItem(MonoConversionMode.Left);
                AddModeItem(MonoConversionMode.Right);
            }

			_monoModeMenu.DropDown(rect);

            void AddModeItem(MonoConversionMode mode)
			{
                _monoModeMenu.AddItem(new GUIContent(mode.ToString()), false, OnChangeMonoMode, mode);
            }
        }

        private void OnChangeMonoMode(object userData)
        {
            if(userData is MonoConversionMode mode)
			{
                var monoModeProp = _serializedObject.FindProperty(nameof(_monoMode));
                monoModeProp.enumValueIndex = (int)mode;
                _serializedObject.ApplyModifiedProperties();
			}
        }

        private void DrawVolumeSlider()
		{
            Rect rect = EditorGUILayout.GetControlRect();
            var volProp = _serializedObject.FindProperty(nameof(_volume));
            volProp.floatValue = BroEditorUtility.DrawVolumeSlider(rect, new GUIContent("Volume"), volProp.floatValue, _isVolumeSnapped, () => _isVolumeSnapped = !_isVolumeSnapped);
		}

		private void DrawAudioClipObjectField()
		{
			EditorGUI.BeginChangeCheck();
			TargetClip = EditorGUILayout.ObjectField("Audio Clip", TargetClip, typeof(AudioClip), false) as AudioClip;
			if(EditorGUI.EndChangeCheck() && TargetClip)
			{
                var clipProp = _serializedObject.FindProperty(nameof(_clip));
                _transport = new SerializedTransport(clipProp, TargetClip.length);
			}
		}

		private void DrawClipWaveform(float height, out Rect waveformRect)
		{
            waveformRect = EditorGUILayout.GetControlRect(GUILayout.Height(height));
            waveformRect.width = position.width * 0.95f;
            waveformRect.x = (position.width - waveformRect.width) * 0.5f;
            _clipPropHelper.DrawClipWaveformAndVisualEditing(waveformRect, _transport, TargetClip, TargetClip.name, PlayClip);                                                                                                                               //DrawEmptyLine(GetLineCountByPixels(height));
		}

		private void DrawPlaybackPositionField()
		{
            Rect rect = EditorGUILayout.GetControlRect();
			_clipPropHelper.DrawPlaybackPositionField(rect, _transport);
		}

		private void DrawFadingField()
		{
            Rect rect = EditorGUILayout.GetControlRect();
            _clipPropHelper.DrawFadingField(rect, _transport);
		}

		private void DrawPlaybackBar(Rect previewRect)
		{
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
			var width = GUILayout.Width(EditorGUIUtility.singleLineHeight * 2);
            var height = GUILayout.Height(EditorGUIUtility.singleLineHeight * 2);
            string icon = _isPlaying ? IconConstant.StopButton : IconConstant.PlayButton;
			GUIContent playButtonContent = new GUIContent(EditorGUIUtility.IconContent(icon).image, EditorAudioPreviewer.IgnoreSettingTooltip);
			if (GUILayout.Button(playButtonContent, width, height))
			{
				if(_isPlaying)
				{
					EditorAudioPreviewer.Instance.StopAllClips();
				}
				else if(_audioClipProp.objectReferenceValue is AudioClip clip)
				{
                    var evt = Event.current;
                    float volume = evt.button == 0 ? _volume : AudioConstant.FullVolume;
                    _currentPreviewRequest = evt.CreatePreviewRequest(clip, volume, _transport);
                    PlayClip(clip.name, _currentPreviewRequest);
                    EditorAudioPreviewer.Instance.PlaybackIndicator.SetClipInfo(previewRect, _currentPreviewRequest);
				}
			}
			_isLoop = DrawButtonToggleLayout(_isLoop, EditorGUIUtility.IconContent(IconConstant.LoopIcon), width, height);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
		}

        private void PlayClip(string clipPath, PreviewRequest req)
        {
            
            _isPlaying = true;
            EditorAudioPreviewer.Instance.OnFinished = () =>
            {
                _isPlaying = false;
                _currentPreviewRequest = null;
            };
        }

		private void DrawSavingButton()
		{
            Rect savingZoneRect = EditorGUILayout.GetControlRect(GUILayout.Height(EditorGUIUtility.singleLineHeight * 2));
            savingZoneRect.x += Gap * 2;
            savingZoneRect.width -= Gap * 4;

            SplitRectHorizontal(savingZoneRect, 0.5f, Gap, out Rect saveButton, out Rect saveAsButton);

			if (GUI.Button(saveButton, new GUIContent("Save")))
			{
				string path = AssetDatabase.GetAssetPath(TargetClip);
				string fullFilePath = BroEditorUtility.GetFullPath(path);

				if (File.Exists(fullFilePath) && EditorUtility.DisplayDialog(ConfirmOverwriteTitle, _instruction.GetText(Instruction.ClipEditorConfirmationDialog), "Yes","No"))
				{
					SaveClip(path);
				}
			}
			
			if(GUI.Button(saveAsButton,new GUIContent("Save As")))
			{
                string lastPath = SessionState.GetString(PrefKey, string.Empty);
                lastPath = string.IsNullOrEmpty(lastPath) ? "Assets" : lastPath;
				string newPath = EditorUtility.SaveFilePanelInProject(SaveFilePanelTitle, TargetClip.name, DefaultFileExt, SaveFilePanelTitle, lastPath);
				if(!string.IsNullOrEmpty(newPath))
				{
                    SessionState.SetString(PrefKey, newPath);
					SaveClip(newPath);
				}
			}
		}

		private void SaveClip(string savePath)
		{
			using (AudioClipEditingHelper helper = new AudioClipEditingHelper(TargetClip))
			{
				if (_transport.StartPosition != 0f || _transport.EndPosition != 0f)
				{
					helper.Trim(_transport.StartPosition, _transport.EndPosition);
				}

                if (_isMono)
                {
                    helper.ConvertToMono(_monoMode);
                }

                if (_transport.Delay > 0f)
				{
					helper.AddSlient(_transport.Delay);
				}

				if(!Mathf.Approximately(_volume, AudioConstant.FullVolume))
				{
					helper.AdjustVolume(_volume);
				}

				if(_transport.FadeIn != 0f)
				{
					helper.FadeIn(_transport.FadeIn);
				}

				if(_transport.FadeOut != 0f)
				{
					helper.FadeOut(_transport.FadeOut);
				}

				if(_isReverse)
				{
					helper.Reverse();
				}

				if (HasEdited)
				{
					SavWav.Save(savePath, helper.GetResultClip());
                    _currSavingFilePath = savePath;
                    AssetDatabase.Refresh();
				}
			}
		}

		public void OnPostprocessAllAssets()
        {
            if (string.IsNullOrEmpty(_currSavingFilePath))
            {
                return;
            }

            AudioClip newClip = AssetDatabase.LoadAssetAtPath(_currSavingFilePath, typeof(AudioClip)) as AudioClip;
            bool isNewClip = newClip != TargetClip;
            if (isNewClip)
            {
                GetPreferenceProperties(out var editNewClipOptionProp, out var pingNewClipOptionProp);
                if (editNewClipOptionProp.boolValue)
                {
                    EditorGUIUtility.PingObject(newClip);
                }

                if (pingNewClipOptionProp.boolValue)
                {
                    TargetClip = newClip;
                    ResetTransport();
                }
            }
            else
            {
                ResetTransport();
            }         

            void ResetTransport()
            {
                var clipProp = _serializedObject.FindProperty(nameof(_clip));
                _transport = new SerializedTransport(clipProp, TargetClip.length);
            }
        }

        private void ResetSetting()
		{
			_currSavingFilePath = null;
			_transport = default;
        }

		public void AddItemsToMenu(GenericMenu menu)
		{
			menu.AddSeparator(string.Empty);
			menu.AddItem(new GUIContent("Default Window Size"), false, () => position = new Rect(position.position, DefaultWindowSize));
		}
	}
}