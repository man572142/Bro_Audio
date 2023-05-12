using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using MiProduction.Extension;
using static MiProduction.Extension.EditorScriptingExtension;
using MiProduction.BroAudio.Data;
using MiProduction.BroAudio.ClipEditor;

namespace MiProduction.BroAudio.AssetEditor
{
	public abstract class AudioLibraryPropertyDrawer : PropertyDrawer, IEditorDrawLineCounter
	{
		protected const float ClipPreviewHeight = 100f;

		private bool _hasOpenedLibraryManager = false;
		private Dictionary<string, ReorderableClips> _reorderableClipsDict = new Dictionary<string, ReorderableClips>();
		private LibraryManagerWindow _editorWindow = null;
		private DrawClipPropertiesHelper _clipPropHelper = new DrawClipPropertiesHelper(ClipPreviewHeight);
		
		public float SingleLineSpace => EditorGUIUtility.singleLineHeight + 3f;
		public int DrawLineCount { get; set; }
		public bool IsEnable { get; private set; } = false;
		protected abstract int BasePropertiesLineCount { get; }
		protected abstract int ClipPropertiesLineCount { get; }

		protected abstract void DrawAdditionalBaseProperties(Rect position, SerializedProperty property);
		protected abstract void DrawAdditionalClipProperties(Rect position, SerializedProperty property);

		public Rect GetRectAndIterateLine(Rect position)
		{
			return EditorScriptingExtension.GetRectAndIterateLine(this, position);
		}

		private void Enable()
		{
			_hasOpenedLibraryManager = EditorWindow.HasOpenInstances<LibraryManagerWindow>();

			if(_hasOpenedLibraryManager)
			{
				_editorWindow = EditorWindow.GetWindow(typeof(LibraryManagerWindow)) as LibraryManagerWindow;
				_editorWindow.OnCloseLibraryManagerWindow += Disable;
				_editorWindow.OnSelectAsset += Disable;
			}

			IsEnable = true;
		}

		private void Disable()
		{
			_reorderableClipsDict.Clear();

			if(_editorWindow)
			{
				_editorWindow.OnCloseLibraryManagerWindow -= Disable;
				_editorWindow.OnSelectAsset -= Disable;
				_editorWindow = null;
			}
			
			IsEnable = false;
		}


		#region Unity Entry Overrider
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			if (!IsEnable)
			{
				Enable();
			}
			else if(!_hasOpenedLibraryManager)
			{
				return;
			}
			
			EditorGUIUtility.wideMode = true;
			DrawLineCount = 0;
			SerializedProperty nameProp = property.FindPropertyRelative(GetAutoBackingFieldName(nameof(IAudioEntity.Name)));

			property.isExpanded = EditorGUI.Foldout(GetRectAndIterateLine(position), property.isExpanded, nameProp.stringValue);
			if (property.isExpanded)
			{
				nameProp.stringValue = EditorGUI.TextField(GetRectAndIterateLine(position), "Name", nameProp.stringValue);
				DrawAdditionalBaseProperties(position, property);

				#region Clip Properties
				ReorderableClips currClipList = DrawReorderableClipsList(position, property);
				SerializedProperty currSelectClip = currClipList.CurrentSelectedClip;
				if (currSelectClip.TryGetPropertyObject(nameof(BroAudioClip.OriginAudioClip),out AudioClip audioClip))
				{
					DrawClipProperties(position, currClipList, audioClip);
					DrawAdditionalClipProperties(position, property);

					SerializedProperty isShowClipProp = property.FindPropertyRelative(nameof(AudioLibrary.IsShowClipPreview));
					isShowClipProp.boolValue = EditorGUI.Foldout(GetRectAndIterateLine(position), isShowClipProp.boolValue, "Preview");
					bool isShowPreview = isShowClipProp.boolValue && audioClip != null;
					if (isShowPreview)
					{
						float startPos = currSelectClip.FindPropertyRelative(nameof(BroAudioClip.StartPosition)).floatValue;
						float endPos = currSelectClip.FindPropertyRelative(nameof(BroAudioClip.EndPosition)).floatValue;
						_clipPropHelper.DrawClipPreview(GetRectAndIterateLine(position), startPos, endPos, audioClip);
					}
				}
				#endregion	
			}
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			float height = SingleLineSpace;
			
			if (property.isExpanded)
			{
				if (_reorderableClipsDict.TryGetValue(property.propertyPath, out ReorderableClips clipList))
				{
					height += clipList.Height;
					bool isShowClipProp =
						clipList.CurrentSelectedClip != null &&
						clipList.CurrentSelectedClip.TryGetPropertyObject(nameof(BroAudioClip.OriginAudioClip), out AudioClip audioClip);
					bool isShowClipPreview = isShowClipProp && property.FindPropertyRelative(nameof(AudioLibrary.IsShowClipPreview)).boolValue;

					if(isShowClipProp)
					{
						height += ClipPropertiesLineCount * SingleLineSpace;
					}
					if(isShowClipPreview)
					{
						height += ClipPreviewHeight;
					}
				}
				height += BasePropertiesLineCount * SingleLineSpace;
			}
			return height;
		} 
		#endregion

		private ReorderableClips DrawReorderableClipsList(Rect position, SerializedProperty property)
		{
			bool hasReorderableClips = _reorderableClipsDict.TryGetValue(property.propertyPath, out var reorderableClips);
			if (!hasReorderableClips)
			{
				reorderableClips = new ReorderableClips(property,this);
				_reorderableClipsDict.Add(property.propertyPath, reorderableClips);
			}

			reorderableClips.DrawReorderableList(GetRectAndIterateLine(position));
			return reorderableClips;
		}

		private void DrawClipProperties(Rect position,ReorderableClips reorderableClips ,AudioClip audioClip)
		{
			SerializedProperty clipProp = reorderableClips.CurrentSelectedClip;
			SerializedProperty volumeProp = clipProp.FindPropertyRelative(nameof(BroAudioClip.Volume));
			SerializedProperty startPosProp = clipProp.FindPropertyRelative(nameof(BroAudioClip.StartPosition));
			SerializedProperty endPosProp = clipProp.FindPropertyRelative(nameof(BroAudioClip.EndPosition));
			SerializedProperty fadeInProp = clipProp.FindPropertyRelative(nameof(BroAudioClip.FadeIn));
			SerializedProperty fadeOutProp = clipProp.FindPropertyRelative(nameof(BroAudioClip.FadeOut));

			Transport currTransport = new Transport();
			currTransport.StartPosition = startPosProp.floatValue;
			currTransport.EndPosition = endPosProp.floatValue;
			currTransport.FadeIn = fadeInProp.floatValue;
			currTransport.FadeOut = fadeOutProp.floatValue;
			currTransport.FullLength = audioClip.length;

			_clipPropHelper.SetCurrentTransport(currTransport);

			Rect volRect = GetRectAndIterateLine(position);
			volumeProp.floatValue = _clipPropHelper.DrawVolumeField(volRect, nameof(BroAudioClip.Volume), volumeProp.floatValue,new RangeFloat(0f,1f));

			Rect playbackRect = GetRectAndIterateLine(position);
			_clipPropHelper.DrawPlaybackPositionField(playbackRect, currTransport, out var newPos);
			startPosProp.floatValue = newPos.StartPosition;
			endPosProp.floatValue = newPos.EndPosition;

			Rect fadingRect = GetRectAndIterateLine(position);
			_clipPropHelper.DrawFadingField(fadingRect, currTransport,out var newFading);
			fadeInProp.floatValue = newFading.FadeIn;
			fadeOutProp.floatValue = newFading.FadeOut;
		}

	}
}