using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using Ami.Extension;
using Ami.BroAudio.Data;
using Ami.BroAudio.Editor.Setting;
using System;

namespace Ami.BroAudio.Editor
{
	public class ReorderableClips
	{
		public Action<string> OnAudioClipChanged;

		public const MulticlipsPlayMode DefaultMulticlipsMode = MulticlipsPlayMode.Random;
		private const float Gap = 5f;
		private const float HeaderLabelWidth = 50f;
		private const float MulticlipsValueLabelWidth = 60f;
		private const float MulticlipsValueFieldWidth = 40f;
		private const float SliderLabelWidth = 25;
		private const float ObjectPickerRatio = 0.6f;

		private ReorderableList _reorderableList;
		private SerializedProperty _playModeProp;
		private int _currSelectedClipIndex = -1;
		private SerializedProperty _currSelectedClip;
		private Rect _previewRect = default;

		public bool IsMulticlips => _reorderableList.count > 1;
		public float Height => _reorderableList.GetHeight();
		public Rect PreviewRect => _previewRect;
		private Vector2 PlayButtonSize => new Vector2(30f, 20f);
		
		public SerializedProperty CurrentSelectedClip
		{
			get
			{
				if(_reorderableList.count > 0)
				{
					if(_reorderableList.index < 0)
					{
						_reorderableList.index = 0;
					}

					if (_currSelectedClipIndex != _reorderableList.index)
					{
						_currSelectedClip = _reorderableList.serializedProperty.GetArrayElementAtIndex(_reorderableList.index);
						_currSelectedClipIndex = _reorderableList.index;
					}
					else if (_currSelectedClip == null)
					{
						_currSelectedClip = _reorderableList.serializedProperty.GetArrayElementAtIndex(_reorderableList.index);
					}
				}
				else
				{
					_currSelectedClip = null;
				}
				return _currSelectedClip;
			}
		}

		public ReorderableClips(SerializedProperty entityProperty)
		{
			_playModeProp = entityProperty.FindPropertyRelative(AudioEntity.EditorPropertyName.MulticlipsPlayMode);
			_reorderableList = CreateReorderabeList(entityProperty);
			UpdatePlayMode();

			Undo.undoRedoPerformed += OnUndoRedoPerformed;
		}

		public void Dispose()
		{
			Undo.undoRedoPerformed -= OnUndoRedoPerformed;
		}

		public void SetPreviewRect(Rect rect)
		{
			_previewRect = rect;
		}

		public void SelectElement(int index)
		{
			if(_reorderableList != null)
			{
				_reorderableList.index = index;
			}
		}

		private void OnUndoRedoPerformed()
		{
			_reorderableList.serializedProperty.serializedObject.Update();
			int count = _reorderableList.count;
			if(count == _reorderableList.index || count == _currSelectedClipIndex)
			{
				_currSelectedClipIndex = count - 1;
				_reorderableList.index = count - 1;
				_currSelectedClip = null;
			}
		}

		public void DrawReorderableList(Rect position)
		{
			_reorderableList.DoList(position);
		}

		private ReorderableList CreateReorderabeList(SerializedProperty entityProperty)
		{
			SerializedProperty clipsProp = entityProperty.FindPropertyRelative(nameof(AudioEntity.Clips));
			var list = new ReorderableList(clipsProp.serializedObject, clipsProp);
			list.drawHeaderCallback = OnDrawHeader;
			list.drawElementCallback = OnDrawElement;
			list.drawFooterCallback = OnDrawFooter;
			list.onAddCallback = OnAdd;
			list.onRemoveCallback = OnRemove;
			list.onSelectCallback = OnSelect;
			return list;
		}

		private void UpdatePlayMode()
		{
			if (!IsMulticlips)
			{
				_playModeProp.enumValueIndex = 0;
			}
			else if (IsMulticlips && _playModeProp.enumValueIndex == 0)
			{
                _playModeProp.enumValueIndex = (int)DefaultMulticlipsMode;
			}
		}

		private void HandleClipsDragAndDrop(Rect rect)
		{
			EventType currType = Event.current.type;
            if((currType == EventType.DragUpdated || currType == EventType.DragPerform) && rect.Contains(Event.current.mousePosition))
			{
				DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
				if(currType == EventType.DragPerform && DragAndDrop.objectReferences?.Length > 0)
				{
					foreach(var clipObj in DragAndDrop.objectReferences)
					{
						SerializedProperty broClipProp = AddClip(_reorderableList);
						SerializedProperty audioClipProp = broClipProp.FindPropertyRelative(nameof(BroAudioClip.AudioClip));
						audioClipProp.objectReferenceValue = clipObj;
					}
                    UpdatePlayMode();
                    _reorderableList.serializedProperty.serializedObject.ApplyModifiedProperties();
					DragAndDrop.AcceptDrag();
				}
			}
		}

		#region ReorderableList Callback
		private void OnDrawHeader(Rect rect)
		{
			HandleClipsDragAndDrop(rect);

			Rect labelRect = new Rect(rect) { width = HeaderLabelWidth };
			Rect valueRect = new Rect(rect) { width = MulticlipsValueLabelWidth , x = rect.xMax - MulticlipsValueLabelWidth};
			Rect multiclipOptionRect = new Rect(rect) { width = (rect.width - labelRect.width - valueRect.width) * 0.5f, x = labelRect.xMax };

            EditorGUI.LabelField(labelRect, "Clips");
            if (IsMulticlips)
            {
                var playMode = (MulticlipsPlayMode)_playModeProp.enumValueIndex;
                playMode = (MulticlipsPlayMode)EditorGUI.EnumPopup(multiclipOptionRect, playMode);
				_playModeProp.enumValueIndex = (int)playMode;
                switch (playMode)
                {
                    case MulticlipsPlayMode.Sequence:
                        EditorGUI.LabelField(valueRect, "Index", GUIStyleHelper.MiddleCenterText);
                        break;
                    case MulticlipsPlayMode.Random:
                        EditorGUI.LabelField(valueRect, "Weight", GUIStyleHelper.MiddleCenterText);
                        break;
                }
                EditorGUI.LabelField(multiclipOptionRect.DissolveHorizontal(0.5f), "(PlayMode)".SetColor(Color.gray), GUIStyleHelper.MiddleCenterRichText);
            }
        }

		private void OnDrawElement(Rect rect, int index, bool isActive, bool isFocused)
		{
			SerializedProperty clipProp = _reorderableList.serializedProperty.GetArrayElementAtIndex(index);
			SerializedProperty audioClipProp = clipProp.FindPropertyRelative(nameof(BroAudioClip.AudioClip));
			SerializedProperty volProp = clipProp.FindPropertyRelative(nameof(BroAudioClip.Volume));

			Rect buttonRect = new Rect(rect) { width = PlayButtonSize.x, height = PlayButtonSize.y };
            buttonRect.y += (_reorderableList.elementHeight - PlayButtonSize.y) * 0.5f;
			Rect valueRect = new Rect(rect) { width = MulticlipsValueLabelWidth, x = rect.xMax - MulticlipsValueLabelWidth };

			float remainWidth = rect.width - buttonRect.width - valueRect.width;
            Rect clipRect = new Rect(rect) { width = (remainWidth * ObjectPickerRatio) - Gap, x = buttonRect.xMax + Gap};
			Rect sliderRect = new Rect(rect) { width = (remainWidth * (1 - ObjectPickerRatio)) - Gap, x = clipRect.xMax + Gap};

			DrawPlayClipButton();
			DrawObjectPicker();
			DrawVolumeSlider();
			DrawMulticlipsValue();

			void DrawObjectPicker()
			{
				EditorGUI.BeginChangeCheck();
				EditorGUI.PropertyField(clipRect, audioClipProp, GUIContent.none);
				if (EditorGUI.EndChangeCheck())
				{
					BroEditorUtility.ResetBroAudioClipPlaybackSetting(clipProp);
					OnAudioClipChanged?.Invoke(clipProp.propertyPath);
				}
			}

			void DrawPlayClipButton()
			{
				if(audioClipProp.objectReferenceValue is AudioClip audioClip)
				{
					bool isPlaying = EditorPlayAudioClip.CurrentPlayingClip == audioClip;
					var image = BroEditorUtility.GetPlaybackButtonIcon(isPlaying).image;
                    GUIContent buttonGUIContent = new GUIContent(image, EditorPlayAudioClip.PlayWithVolumeSetting);
					if (GUI.Button(buttonRect, buttonGUIContent))
					{
						if(isPlaying)
						{
							EditorPlayAudioClip.StopAllClips();
						}
						else
						{
							float startPos = clipProp.FindPropertyRelative(nameof(BroAudioClip.StartPosition)).floatValue;
							float endPos = clipProp.FindPropertyRelative(nameof(BroAudioClip.EndPosition)).floatValue;
							if (Event.current.button == 0)
							{
								EditorPlayAudioClip.PlayClip(audioClip, startPos, endPos);
							}
							else
							{
								EditorPlayAudioClip.PlayClipByAudioSource(audioClip, volProp.floatValue, startPos, endPos);
							}

							if (EditorPlayAudioClip.PlaybackIndicator.IsPlaying && EditorPlayAudioClip.CurrentPlayingClip == audioClip)
							{
								PreviewClip clip = new PreviewClip()
								{
									StartPosition = startPos,
									EndPosition = endPos,
									FullLength = audioClip.length,
								};

								EditorPlayAudioClip.PlaybackIndicator.SetClipInfo(_previewRect, clip);
							}
						}
					}
				}
			}

			void DrawVolumeSlider()
			{
				Rect labelRect = new Rect(sliderRect) { width = SliderLabelWidth };
				sliderRect.width -= SliderLabelWidth;
				sliderRect.x = labelRect.xMax;
				EditorGUI.LabelField(labelRect, EditorGUIUtility.IconContent(IconConstant.AudioSpeakerOn));
				float newVol = BroEditorUtility.DrawVolumeSlider(sliderRect, volProp.floatValue, out bool hasChanged, out float newSliderValue);
				if(hasChanged)
				{
					volProp.floatValue = newVol;
					BroEditorUtility.DrawDecibelValuePeeking(volProp.floatValue, 3f, sliderRect, newSliderValue);
				}
			}

			void DrawMulticlipsValue()
			{
				valueRect.width = MulticlipsValueFieldWidth;
				valueRect.x += (MulticlipsValueLabelWidth - MulticlipsValueFieldWidth) * 0.5f;
				MulticlipsPlayMode currentPlayMode = (MulticlipsPlayMode)_playModeProp.enumValueIndex;
				switch (currentPlayMode)
				{
					case MulticlipsPlayMode.Single:
						break;
					case MulticlipsPlayMode.Sequence:
						EditorGUI.LabelField(valueRect, index.ToString(), GUIStyleHelper.MiddleCenterText);
						break;
					case MulticlipsPlayMode.Random:
						SerializedProperty weightProp = clipProp.FindPropertyRelative(nameof(BroAudioClip.Weight));
						GUIStyle intFieldStyle = new GUIStyle(EditorStyles.numberField);
						intFieldStyle.alignment = TextAnchor.MiddleCenter;
						weightProp.intValue = EditorGUI.IntField(valueRect, weightProp.intValue, intFieldStyle);
						break;
				}
			}
		}


		private void OnDrawFooter(Rect rect)
		{
			ReorderableList.defaultBehaviours.DrawFooter(rect, _reorderableList);
			if (CurrentSelectedClip.TryGetPropertyObject(nameof(BroAudioClip.AudioClip), out AudioClip audioClip))
			{
				EditorGUI.LabelField(rect, audioClip.name.SetColor(BroAudioGUISetting.ClipLabelColor).ToBold(), GUIStyleHelper.RichText);
			}
		}

		private void OnRemove(ReorderableList list)
		{
			ReorderableList.defaultBehaviours.DoRemoveButton(list);
			UpdatePlayMode();
		}

		private void OnAdd(ReorderableList list)
        {
			AddClip(list);
            UpdatePlayMode();
        }

        private void OnSelect(ReorderableList list)
		{
			EditorPlayAudioClip.StopAllClips();
		}

		#endregion

		private SerializedProperty AddClip(ReorderableList list)
        {
            ReorderableList.defaultBehaviours.DoAddButton(list);
            var clipProp = list.serializedProperty.GetArrayElementAtIndex(list.count - 1);
            BroEditorUtility.ResetBroAudioClipSerializedProperties(clipProp);
			return clipProp;
        }
	} 
}
