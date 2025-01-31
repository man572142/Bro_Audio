using UnityEngine;
using UnityEditor;
using Ami.Extension;
using System;
using System.IO;
using static Ami.BroAudio.Editor.Setting.BroAudioGUISetting;
using static Ami.Extension.EditorScriptingExtension;
using Ami.BroAudio.Tools;

namespace Ami.BroAudio.Editor
{
	public class ClipEditorWindow : EditorWindow, IHasCustomMenu
	{
		public const string SaveFilePanelTitle = "Save as a new file";
		public const string ConfirmOverwriteTitle = "Confirm overwrite";
        public const string PrefKey = "LastClipEditorSavePath";
		public const float Gap = 50f;
		public const string DefaultFileExt = "wav";
		public static Vector2 DefaultWindowSize => new Vector2(640f, 490f);

		public event Action OnChangeAudioClip;

		private AudioClip _targetClip = null;
		private DrawClipPropertiesHelper _clipPropHelper = new DrawClipPropertiesHelper();
		private Transport _transport = default;
		private bool _isReverse = false;
		private bool _isMono = false;
		private float _volume = AudioConstant.FullVolume;
		private bool _isVolumeSnapped = false;
		private MonoConversionMode _monoMode = MonoConversionMode.Downmixing;
		private GenericMenu _monoModeMenu = null;
		private bool _isLoop = false;
		private bool _isPlaying = false;
        private bool _isShowPreferences = false;
        private SerializedProperty _editNewClipProp = null;
        private SerializedProperty _pingNewClipProp = null;

		private string _currSavingFilePath = null;
		private BroInstructionHelper _instruction = new BroInstructionHelper();

		public bool HasEdited
		{
			get
			{
				return _volume != AudioConstant.FullVolume
					|| _transport.HasDifferentPosition
					|| _transport.HasFading
					|| _isReverse
					|| _isMono;
			}
		}

		public AudioClip TargetClip
		{
			get => _targetClip;
			private set
			{
				bool hasChanged = value != _targetClip;
				_targetClip = value;

				if (hasChanged)
				{
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
			ResetSetting();
		}

		private void OnDisable()
		{
			OnChangeAudioClip -= ResetSetting;
		}

        private void OnFocus()
        {
            EditorPlayAudioClip.Instance.AddPlaybackIndicatorListener(Repaint);
        }

        private void OnLostFocus()
		{
            EditorPlayAudioClip.Instance.RemovePlaybackIndicatorListener(Repaint);
            EditorPlayAudioClip.Instance.StopAllClips();
			_isPlaying = false;

        }

		private void OnGUI()
		{
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
			DrawClipPreview(position.height * 0.3f, _volume, out Rect previewRect);
            EditorGUILayout.Space();
            DrawClipPropertiesHelper.DrawPlaybackIndicator(position.SetPosition(0f,0f));

            DrawPlaybackBar(previewRect);
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            DrawVolumeSlider();
            DrawPlaybackPositionField();
            DrawFadingField();

            _isReverse = EditorGUILayout.Toggle("Reverse", _isReverse);

            using (new EditorGUI.DisabledScope(TargetClip.channels != 2))
            {
                _isMono = EditorGUILayout.Toggle("Convert To Mono", _isMono);
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
                FindPreferencePropertiesIfNeeded(out var editorSettingSO);
                editorSettingSO.Update();

                using (new EditorGUI.IndentLevelScope(1))
                {
                    _editNewClipProp.boolValue = EditorGUILayout.ToggleLeft("Edit The New Clip After [Save As]", _editNewClipProp.boolValue);
                    _pingNewClipProp.boolValue = EditorGUILayout.ToggleLeft("Show The New Clip In Project Window After [Save As]", _pingNewClipProp.boolValue);
                }
                
                editorSettingSO.ApplyModifiedProperties();
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
        }

        private void FindPreferencePropertiesIfNeeded(out SerializedObject serializedObj)
        {
            serializedObj = null;
            if (_editNewClipProp == null || _pingNewClipProp == null)
            {
                serializedObj = new SerializedObject(BroEditorUtility.EditorSetting);
                _editNewClipProp = serializedObj.FindProperty(nameof(EditorSetting.EditTheNewClipAfterSaveAs));
                _pingNewClipProp = serializedObj.FindProperty(nameof(EditorSetting.PingTheNewClipAfterSaveAs));
            }
            serializedObj = _editNewClipProp.serializedObject;
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
				_monoMode = mode;
			}
        }

        private void DrawVolumeSlider()
		{
            Rect rect = EditorGUILayout.GetControlRect();
			_volume = BroEditorUtility.DrawVolumeSlider(rect, new GUIContent("Volume"), _volume, _isVolumeSnapped, () => _isVolumeSnapped = !_isVolumeSnapped);
		}

		private void DrawAudioClipObjectField()
		{
			EditorGUI.BeginChangeCheck();
			TargetClip = EditorGUILayout.ObjectField("Audio Clip", TargetClip, typeof(AudioClip), false) as AudioClip;
			if(EditorGUI.EndChangeCheck() && TargetClip)
			{
				_transport = new Transport(TargetClip.length);
			}
		}

		private void DrawClipPreview(float height, float volume, out Rect previewRect)
		{
            previewRect = EditorGUILayout.GetControlRect(GUILayout.Height(height));
            previewRect.width = position.width * 0.95f;
            previewRect.x = (position.width - previewRect.width) * 0.5f;
            _clipPropHelper.DrawClipPreview(previewRect, _transport, TargetClip, volume, TargetClip.name, clipPath => _isPlaying = clipPath != null); // don't worry about any duplicate path, because there will only one clip in editing                                                                                                                                         //DrawEmptyLine(GetLineCountByPixels(height));
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
			GUIContent playButtonContent = new GUIContent(EditorGUIUtility.IconContent(icon).image, EditorPlayAudioClip.IgnoreSettingTooltip);
			if (GUILayout.Button(playButtonContent, width, height))
			{
				if(_isPlaying)
				{
					EditorPlayAudioClip.Instance.StopAllClips();
				}
				else
				{
					PreviewClip previewGUIClip;
					if(Event.current.button == 0) // Left Click
					{
                        var clipData = new EditorPlayAudioClip.Data(_targetClip, _volume, _transport);
                        EditorPlayAudioClip.Instance.PlayClipByAudioSource(clipData, _isLoop);
						previewGUIClip = new PreviewClip(_transport);
                    }
					else
					{
                        EditorPlayAudioClip.Instance.PlayClip(_targetClip, 0f, 0f, _isLoop);
                        previewGUIClip = new PreviewClip(_targetClip.length);
                    }

                    _isPlaying = true;
                    EditorPlayAudioClip.Instance.OnFinished = () => _isPlaying = false; ;
                    EditorPlayAudioClip.Instance.PlaybackIndicator.SetClipInfo(previewRect, previewGUIClip);
				}
			}
			_isLoop = DrawButtonToggleLayout(_isLoop, EditorGUIUtility.IconContent(IconConstant.LoopIcon), width, height);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
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

				if(_volume != AudioConstant.FullVolume)
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

            FindPreferencePropertiesIfNeeded(out _);

            AudioClip newClip = AssetDatabase.LoadAssetAtPath(_currSavingFilePath, typeof(AudioClip)) as AudioClip;
            bool isNewClip = newClip != TargetClip;
            if (isNewClip)
            {
                if (_pingNewClipProp.boolValue)
                {
                    EditorGUIUtility.PingObject(newClip);
                }

                if (_editNewClipProp.boolValue)
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
                _transport = new Transport(TargetClip.length);
            }
        }

        private void ResetSetting()
		{
			_currSavingFilePath = null;
			_transport = default;
			_isReverse = false;
		}

		public void AddItemsToMenu(GenericMenu menu)
		{
			menu.AddSeparator(string.Empty);
			menu.AddItem(new GUIContent("Default Window Size"), false, () => position = new Rect(position.position, DefaultWindowSize));
		}
	}
}