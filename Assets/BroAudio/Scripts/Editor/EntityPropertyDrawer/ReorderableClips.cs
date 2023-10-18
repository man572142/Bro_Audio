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

		private ReorderableList _reorderableList;
		private SerializedProperty _playModeProp;
		private IEditorDrawLineCounter _editorDrawer;

		public bool IsMulticlips => _reorderableList.count > 1;
		public bool HasAnyClip => _reorderableList.count > 0;
		public float Height => _reorderableList.GetHeight();

		private int _currSelectedClipIndex = -1;
		private SerializedProperty _currSelectedClip;
		public SerializedProperty CurrentSelectedClip
		{
			get
			{
				if(HasAnyClip)
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

		public ReorderableClips(SerializedProperty entityProperty,IEditorDrawLineCounter editorDrawer)
		{
			_playModeProp = entityProperty.FindPropertyRelative(AudioEntity.NameOf.MulticlipsPlayMode);
			_reorderableList = CreateReorderabeList(entityProperty);
			UpdatePlayMode();
			_editorDrawer = editorDrawer;
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

		private MulticlipsPlayMode UpdatePlayMode()
		{
			if (!IsMulticlips)
			{
				_playModeProp.enumValueIndex = 0;
			}
			else if (IsMulticlips && _playModeProp.enumValueIndex == 0)
			{
				_playModeProp.enumValueIndex = 1;
			}
			return (MulticlipsPlayMode)_playModeProp.enumValueIndex;
		}

		#region ReorderableList Callback
		private void OnDrawHeader(Rect rect)
		{
			float[] ratio = { 0.3f, 0.4f, 0.18f, 0.12f };
			if (EditorScriptingExtension.TrySplitRectHorizontal(rect, ratio, 15f, out Rect[] newRects))
			{
				EditorGUI.LabelField(newRects[0], "Clips");
				if (IsMulticlips)
				{
					GUIStyle popupStyle = new GUIStyle(EditorStyles.popup);
					popupStyle.alignment = TextAnchor.MiddleLeft;
					MulticlipsPlayMode currentPlayMode =(MulticlipsPlayMode)_playModeProp.enumValueIndex;
					_playModeProp.enumValueIndex = (int)(MulticlipsPlayMode)EditorGUI.EnumPopup(newRects[1], currentPlayMode, popupStyle);
					currentPlayMode = (MulticlipsPlayMode)_playModeProp.enumValueIndex;
					switch (currentPlayMode)
					{
						case MulticlipsPlayMode.Sequence:
							EditorGUI.LabelField(newRects[ratio.Length - 1], "Index");
							break;
						case MulticlipsPlayMode.Random:
							EditorGUI.LabelField(newRects[ratio.Length - 1], "Weight");
							break;
					}
					EditorGUI.LabelField(newRects[1].DissolveHorizontal(0.5f), "(PlayMode)".SetColor(Color.gray), GUIStyleHelper.MiddleCenterRichText);
				}
				_editorDrawer.DrawLineCount++;
			}
		}

		private void OnDrawElement(Rect rect, int index, bool isActive, bool isFocused)
		{
			SerializedProperty clipProp = _reorderableList.serializedProperty.GetArrayElementAtIndex(index);
			SerializedProperty audioClipProp = clipProp.FindPropertyRelative(nameof(BroAudioClip.AudioClip));
			EditorScriptingExtension.SplitRectHorizontal(rect, 0.9f, 15f, out Rect clipRect, out Rect valueRect);
			EditorGUI.BeginChangeCheck();
			EditorGUI.PropertyField(clipRect, audioClipProp, GUIContent.none);
			if (EditorGUI.EndChangeCheck())
			{
				BroEditorUtility.ResetBroAudioClipPlaybackSetting(clipProp);
				OnAudioClipChanged?.Invoke(clipProp.propertyPath);
			}

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
			_editorDrawer.DrawLineCount++;
		}

		private void OnDrawFooter(Rect rect)
		{
			ReorderableList.defaultBehaviours.DrawFooter(rect, _reorderableList);
			if (CurrentSelectedClip.TryGetPropertyObject(nameof(BroAudioClip.AudioClip), out AudioClip audioClip))
			{
				Rect labelRect = new Rect(rect);
				labelRect.y += 5f;
				EditorGUI.LabelField(labelRect, audioClip.name.SetColor(BroAudioGUISetting.ClipLabelColor).ToBold(), GUIStyleHelper.RichText);
			}
			_editorDrawer.DrawLineCount++;
		}

		private void OnRemove(ReorderableList list)
		{
			ReorderableList.defaultBehaviours.DoRemoveButton(list);
			UpdatePlayMode();
		}

		private void OnAdd(ReorderableList list)
		{
			ReorderableList.defaultBehaviours.DoAddButton(list);
			var clipProp = list.serializedProperty.GetArrayElementAtIndex(list.count - 1);
			BroEditorUtility.ResetBroAudioClipSerializedProperties(clipProp);

			UpdatePlayMode();
		}

		private void OnSelect(ReorderableList list)
		{
			EditorPlayAudioClip.StopAllClips();
		}

		#endregion

	} 
}
