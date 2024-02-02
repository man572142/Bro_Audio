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
		public const int DefaultVolumeOption = 2;
		public const string DefaultFileExt = "wav";

		public event Action OnChangeAudioClip;

		private readonly float[] _volumeOptions = { -6, -3, 0, 3, 6 };
		private int _currVolumeOption = DefaultVolumeOption;
		private string[] _volumeOptionsTexts = null;

		private AudioClip _targetClip = null;
		private DrawClipPropertiesHelper _clipPropHelper = new DrawClipPropertiesHelper();
		private Transport _transport = default;
		private bool _isReverse = false;

		private string _currSavingFilePath = null;
		private BroInstructionHelper _instruction = new BroInstructionHelper();
		public override float SingleLineSpace => EditorGUIUtility.singleLineHeight + 5f;

		public bool HasEdited
		{
			get
			{
				return _currVolumeOption != DefaultVolumeOption
					|| _transport.HasDifferentPosition
					|| _transport.HasFading
					|| _isReverse;
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

		public string[] VolumeOptionsText
		{
			get
			{
				if(_volumeOptionsTexts == null)
				{
					_volumeOptionsTexts = new string[_volumeOptions.Length];
					for (int i = 0; i < _volumeOptionsTexts.Length; i++)
					{
						if (_volumeOptions[i] > 0)
						{
							_volumeOptionsTexts[i] = "+";
						}
						_volumeOptionsTexts[i] += $"{_volumeOptions[i]} dB";
					}
				}
				return _volumeOptionsTexts;
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
			DrawClipPreview(drawPosition, position.height * 0.3f);
			DrawClipPropertiesHelper.DrawPlaybackIndicator(position.Scoping(position));

			drawPosition.x += Gap;
			drawPosition.width -= Gap * 2;

			DrawPlaybackPositionField(drawPosition);
			DrawFadingField(drawPosition);
			DrawVolumeChangeToolBar(drawPosition);

			_isReverse = EditorGUI.Toggle(GetRectAndIterateLine(drawPosition),"Reverse",_isReverse);

			EditorGUI.BeginDisabledGroup(!HasEdited);
			{
				DrawSavingButton(drawPosition);
			}
			EditorGUI.EndDisabledGroup();
		}

		private void DrawVolumeChangeToolBar(Rect drawPosition)
		{
			Rect volZoneRect = GetRectAndIterateLine(drawPosition);
			EditorScriptingExtension.SplitRectHorizontal(volZoneRect, 0.3f, 0f, out Rect labelRect, out Rect toolBarRect);
			EditorGUI.LabelField(labelRect, "Volume");
			_currVolumeOption = GUI.Toolbar(toolBarRect, _currVolumeOption, VolumeOptionsText);
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

		private void DrawClipPreview(Rect drawPosition,float height)
		{
			Rect previewRect = GetRectAndIterateLine(drawPosition);
			_clipPropHelper.SetPreviewHeight(height);
			_clipPropHelper.DrawClipPreview(previewRect, _transport, TargetClip,TargetClip.name); // don't worry about any duplicate path, cause there will only one clip in editing
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
			return Mathf.RoundToInt(pixels / SingleLineSpace);
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

				if(_transport.Delay > 0f)
				{
					helper.AddSlient(_transport.Delay);
				}

				float boostVolume = _volumeOptions[_currVolumeOption];
				if(boostVolume != 0f)
				{
					helper.Boost(boostVolume);
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
			_currVolumeOption = DefaultVolumeOption;
			_transport = default;
			_isReverse = false;
		}
	}
}