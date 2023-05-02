using System;
using System.Collections;
using System.Collections.Generic;
using MiProduction.Extension;
using UnityEditor;
using UnityEngine;

namespace MiProduction.BroAudio.Data
{
	public class ClipEditingHelper
	{
		SerializedProperty _clipProperty = null;

		public (float Start, float End) PositionBeforeEdit = (0f, 0f);

		public bool IsEditing { get; private set; }

		public void StartEditing(SerializedProperty clipProperty, float startPos, float endPos)
		{
			_clipProperty = clipProperty;
			PositionBeforeEdit = (startPos, endPos);
			IsEditing = true;

		}

		public void Reset()
		{
			SetPlaybackPosition(PositionBeforeEdit.Start, PositionBeforeEdit.End);
			PositionBeforeEdit = (0f, 0f);
			_clipProperty = null;
			IsEditing = false;
		}



		public void Save(float newStartPos, float newEndPos, AudioClip audioClip, string saveName)
		{
			if (!IsEditing)
			{
				Utility.LogError("Can't save, the clip is not in edit mode");
				return;
			}

			if (newStartPos != PositionBeforeEdit.Start || newStartPos != PositionBeforeEdit.End)
			{
				SetPlaybackPosition(newStartPos, newEndPos);
				AudioClip trimmedClip = audioClip.Trim(newStartPos, newEndPos, saveName);
				StoreEditedAudioClipToDisk(audioClip, trimmedClip);
			}
			IsEditing = false;
		}

		private void SetPlaybackPosition(float start, float end)
		{
			if (IsEditing)
			{
				_clipProperty.FindPropertyRelative(nameof(BroAudioClip.StartPosition)).floatValue = start;
				_clipProperty.FindPropertyRelative(nameof(BroAudioClip.EndPosition)).floatValue = end;
				_clipProperty.serializedObject.ApplyModifiedProperties();
			}
		}

		private void StoreEditedAudioClipToDisk(AudioClip originClip, AudioClip editedClip)
		{
			string extension = System.IO.Path.GetExtension(AssetDatabase.GetAssetPath(originClip));
			string path = Utility.GetFilePath(Utility.EditedClipsPath, editedClip.name + extension);
			bool suecss = SavWav.Save(Utility.GetFullPath(path), editedClip);

			if (suecss)
			{
				AssetDatabase.SaveAssets();
				AssetDatabase.Refresh();
				var obj = AssetDatabase.LoadAssetAtPath(path, typeof(UnityEngine.Object));
				_clipProperty.FindPropertyRelative(nameof(BroAudioClip.EditedAudioClip)).objectReferenceValue = obj;
				_clipProperty.serializedObject.ApplyModifiedProperties();
			}
			else
			{
				Utility.LogError("save audio file failed");
			}
		}

	}

}