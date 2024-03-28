using UnityEngine;
using UnityEditor;
using Ami.Extension;
using System;
using System.IO;
using static Ami.BroAudio.Editor.Setting.BroAudioGUISetting;
using Ami.BroAudio.Tools;

namespace Ami.BroAudio.Editor
{
	public class ClipEditorWindow : MiEditorWindow
	{
		public const string SaveFilePanelTitle = "Save as a new file";
		public const string ConfirmOverwriteTitle = "Confirm overwrite";
	
		public const float Gap = 50f;
		public const string DefaultFileExt = "wav";

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

		private string _currSavingFilePath = null;
		private BroInstructionHelper _instruction = new BroInstructionHelper();
		public override float SingleLineSpace => EditorGUIUtility.singleLineHeight + 5f;

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
			window.minSize = new Vector2(640f,360f);
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
			EditorPlayAudioClip.AddPlaybackIndicatorListener(Repaint);
		}

		private void OnLostFocus()
		{
			EditorPlayAudioClip.StopAllClips();
			EditorPlayAudioClip.RemovePlaybackIndicatorListener(Repaint);
		}

		protected override void OnGUI()
		{
			base.OnGUI();

			Rect drawPosition = new Rect(Gap * 0.5f, 0f, position.width - Gap, position.height);

			DrawEmptyLine(1);
			DrawAudioClipObjectField(drawPosition);

			if (TargetClip == null || _transport == null)
			{
				Rect noClipRect = new Rect(drawPosition.width * 0.5f, drawPosition.height * 0.5f, 0f, 0f);
				EditorGUI.LabelField(noClipRect, "No Clip".SetSize(30).SetColor(Color.white), GUIStyleHelper.MiddleCenterRichText);
				return;
			}

			DrawEmptyLine(1);
			DrawClipPreview(drawPosition, position.height * 0.3f, _volume);
			DrawClipPropertiesHelper.DrawPlaybackIndicator(position.OverridePosition(0f,0f));

			drawPosition.x += Gap;
			drawPosition.width -= Gap * 2;

			DrawEmptyLine(1);
			DrawVolumeSlider(drawPosition);
			DrawPlaybackPositionField(drawPosition);
			DrawFadingField(drawPosition);

			_isReverse = EditorGUI.Toggle(GetRectAndIterateLine(drawPosition),"Reverse",_isReverse);

            using (new EditorGUI.DisabledScope(TargetClip.channels != 2))
			{
				Rect monoRect = GetRectAndIterateLine(drawPosition);
                _isMono = EditorGUI.Toggle(monoRect, "Convert To Mono", _isMono);
				using (new EditorGUI.DisabledScope(!_isMono))
				{
					Rect monoModeRect = monoRect.DissolveHorizontal(0.5f);
					if(TargetClip.channels > 2)
					{
						EditorGUI.LabelField(monoModeRect, "Surround sound is not supported");
					}
					else if(EditorGUI.DropdownButton(monoModeRect, new GUIContent(_monoMode.ToString()),FocusType.Keyboard))
					{
						ShowMonoModeMenu(monoModeRect);
					}
				}
            }


			EditorGUI.BeginDisabledGroup(!HasEdited);
			{
				DrawSavingButton(drawPosition);
			}
			EditorGUI.EndDisabledGroup();
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

        private void DrawVolumeSlider(Rect drawPosition)
		{
			Rect volRect = GetRectAndIterateLine(drawPosition);
			//EditorScriptingExtension.SplitRectHorizontal(volZoneRect, 0.3f, 0f, out Rect labelRect, out Rect toolBarRect);
			//EditorGUI.LabelField(labelRect, "Volume");
			//_currVolumeOption = GUI.Toolbar(toolBarRect, _currVolumeOption, VolumeOptionsText);

			_volume = BroEditorUtility.DrawVolumeSlider(volRect, new GUIContent("Volume"), _volume, _isVolumeSnapped, () => _isVolumeSnapped = !_isVolumeSnapped);
		}

		private void DrawAudioClipObjectField(Rect drawPosition)
		{
			Rect clipObjectRect = GetRectAndIterateLine(drawPosition);
			EditorGUI.BeginChangeCheck();
			TargetClip = EditorGUI.ObjectField(clipObjectRect, "Audio Clip", TargetClip, typeof(AudioClip), false) as AudioClip;
			if(EditorGUI.EndChangeCheck() && TargetClip)
			{
				_transport = new Transport(TargetClip.length);
			}
		}

		private void DrawClipPreview(Rect drawPosition,float height, float volume)
		{
			Rect previewRect = GetRectAndIterateLine(drawPosition);
			previewRect.height = height;
			_clipPropHelper.DrawClipPreview(previewRect, _transport, TargetClip, volume, TargetClip.name); // don't worry about any duplicate path, because there will only one clip in editing
			DrawEmptyLine(GetLineCountByPixels(height));
		}
		private void DrawPlaybackPositionField(Rect drawPosition)
		{
			Rect playbackRect = GetRectAndIterateLine(drawPosition);
			_clipPropHelper.DrawPlaybackPositionField(playbackRect, _transport);
		}

		private void DrawFadingField(Rect drawPosition)
		{
			Rect fadingRect = GetRectAndIterateLine(drawPosition);
			_clipPropHelper.DrawFadingField(fadingRect, _transport);
		}

		private int GetLineCountByPixels(float pixels)
		{
			return (int)Math.Round(pixels / SingleLineSpace, MidpointRounding.AwayFromZero);
		}

		private void DrawSavingButton(Rect drawPosition)
		{
			int lastLine = GetLineCountByPixels(position.height) - 1;
			DrawEmptyLine(lastLine - DrawLineCount -1);

			Rect savingZoneRect = GetRectAndIterateLine(drawPosition);
			savingZoneRect.x += Gap * 2;
			savingZoneRect.width -= Gap * 4;
			savingZoneRect.height = SingleLineSpace * 2;

			EditorScriptingExtension.SplitRectHorizontal(savingZoneRect, 0.5f, Gap, out Rect saveButton, out Rect saveAsButton);

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
				string newPath =  EditorUtility.SaveFilePanelInProject(SaveFilePanelTitle, TargetClip.name, DefaultFileExt, SaveFilePanelTitle);
				if(!string.IsNullOrEmpty(newPath))
				{
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
			if(string.IsNullOrEmpty(_currSavingFilePath))
			{
				return;
			}

			AudioClip newClip = AssetDatabase.LoadAssetAtPath(_currSavingFilePath, typeof(AudioClip)) as AudioClip;
			TargetClip = newClip;
			_transport = new Transport(TargetClip.length);
		}

		private void ResetSetting()
		{
			_currSavingFilePath = null;
			_transport = default;
			_isReverse = false;
		}
	}
}