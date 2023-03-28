using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using MiProduction.Extension;
using MiProduction.BroAudio.Asset.Core;

namespace MiProduction.BroAudio.Asset
{
	public class ReorderableClips
	{
		private ReorderableList _reorderableList;
		private MulticlipsPlayMode _currPlayMode;

		private SerializedProperty _playModeProp;
		private IEditorDrawLineCounter _editorDrawer;

		private Dictionary<string, Transport> _clipTransportDict = new Dictionary<string, Transport>();

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
				}
				else
				{
					_currSelectedClip = null;
				}
				return _currSelectedClip;
			}
		}


		public ReorderableClips(SerializedProperty audioSetProperty,IEditorDrawLineCounter editorDrawer)
		{
			_playModeProp = audioSetProperty.FindPropertyRelative("MulticlipsPlayMode");
			_reorderableList = CreateReorderabeList(audioSetProperty);
			_currPlayMode = GetCurrentPlayMode(_playModeProp);
			_editorDrawer = editorDrawer;
			
			if (CurrentSelectedClip != null)
			{
				RecordOriginValue(CurrentSelectedClip);
			}
		}

		public void DrawReorderableList(Rect position)
		{
			_reorderableList.DoList(position);
		}

		private ReorderableList CreateReorderabeList(SerializedProperty audioSetProperty)
		{
			SerializedProperty clipsProp = audioSetProperty.FindPropertyRelative("Clips");
			var list = new ReorderableList(clipsProp.serializedObject, clipsProp);
			list.drawHeaderCallback = OnDrawHeader;
			list.drawElementCallback = OnDrawElement;
			list.drawFooterCallback = OnDrawFooter;
			list.onAddCallback = OnAdd;
			list.onRemoveCallback = OnRemove;
			list.onSelectCallback = OnSelect;
			return list;
		}

		private MulticlipsPlayMode GetCurrentPlayMode(SerializedProperty playModeProp)
		{
			if (!IsMulticlips)
			{
				playModeProp.enumValueIndex = 0;
			}
			else if (IsMulticlips && playModeProp.enumValueIndex == 0)
			{
				playModeProp.enumValueIndex = 1;
			}
			return (MulticlipsPlayMode)playModeProp.enumValueIndex;
		}

		#region ReorderableList Callback
		private void OnDrawHeader(Rect rect)
		{
			float[] ratio = { 0.2f, 0.5f, 0.18f, 0.12f };
			if (EditorScriptingExtension.TrySplitRectHorizontal(rect, ratio, 15f, out Rect[] newRects))
			{
				EditorGUI.LabelField(newRects[0], "Clips");
				if (IsMulticlips)
				{
					_playModeProp.enumValueIndex = (int)(MulticlipsPlayMode)EditorGUI.EnumPopup(newRects[1], _currPlayMode);
					_currPlayMode = (MulticlipsPlayMode)_playModeProp.enumValueIndex;
					switch (_currPlayMode)
					{
						case MulticlipsPlayMode.Sequence:
							EditorGUI.LabelField(newRects[ratio.Length - 1], "Index");
							break;
						case MulticlipsPlayMode.Random:
							EditorGUI.LabelField(newRects[ratio.Length - 1], "Weight");
							break;
					}
					EditorGUI.LabelField(newRects[1].DissolveHorizontal(0.4f), "(PlayMode)".SetColor(Color.gray), GUIStyleHelper.Instance.RichText);
				}
				_editorDrawer.DrawLineCount++;
			}
		}

		private void OnDrawElement(Rect rect, int index, bool isActive, bool isFocused)
		{
			SerializedProperty clipProp = _reorderableList.serializedProperty.GetArrayElementAtIndex(index);
			SerializedProperty audioClipProp = clipProp.FindPropertyRelative(nameof(BroAudioClip.OriginAudioClip));
			EditorScriptingExtension.SplitRectHorizontal(rect, 0.9f, 15f, out Rect clipRect, out Rect valueRect);
			EditorGUI.PropertyField(clipRect, audioClipProp, new GUIContent(""));


			switch (_currPlayMode)
			{
				case MulticlipsPlayMode.Single:
					break;
				case MulticlipsPlayMode.Sequence:
					EditorGUI.LabelField(valueRect, index.ToString(), GUIStyleHelper.Instance.MiddleCenterText);
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
			if (CurrentSelectedClip.TryGetPropertyObject(nameof(BroAudioClip.OriginAudioClip), out AudioClip audioClip))
			{
				Rect labelRect = new Rect(rect);
				labelRect.y += 5f;
				EditorGUI.LabelField(labelRect, audioClip.name.SetColor(BroAudioGUISetting.ClipLabelColor).ToBold(), GUIStyleHelper.Instance.RichText);
			}
			_editorDrawer.DrawLineCount++;
		}

		private void OnRemove(ReorderableList list)
		{
			_clipTransportDict.Remove(CurrentSelectedClip.propertyPath);
			ReorderableList.defaultBehaviours.DoRemoveButton(list);
		}

		private void OnAdd(ReorderableList list)
		{
			ReorderableList.defaultBehaviours.DoAddButton(list);
			int addedIndex = list.count - 1;
			var clipProp = list.serializedProperty.GetArrayElementAtIndex(addedIndex);
			//CurrentSelectedClip = clipProp;
			if (addedIndex > 0)
			{
				BroAudioClip.ResetAllSerializedProperties(clipProp);
			}
			RecordOriginValue(clipProp);
			//_clipTransportDict.Add(clipProp.propertyPath,new Transport(clipProp));
		}

		private void OnSelect(ReorderableList list)
		{
			//CurrentSelectedClip = _reorderableList.serializedProperty.GetArrayElementAtIndex(list.index);
		}

		public IChangesTrackable GetCurrentSelectedClipChanges()
		{
			return _clipTransportDict[CurrentSelectedClip.propertyPath];
		}
		#endregion

		private void RecordOriginValue(SerializedProperty clipProp)
		{
			if(clipProp != null)
			{
				_clipTransportDict.Add(clipProp.propertyPath, new Transport(clipProp));
			}
		}
	} 
}
