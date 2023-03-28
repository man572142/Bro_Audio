using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MiProduction.BroAudio.Asset.Core
{
	public class Transport : IChangesTrackable
	{
		private float StartPosition;
		private float EndPosition;
		private float FadeIn;
		private float FadeOut;

		private SerializedProperty _clipProperty;

		public int ChangedID { get; set; }

		public Transport(SerializedProperty clipProp)
		{
			_clipProperty = clipProp;
			SetValues(clipProp);
		}

		public static bool IsDifferent(Transport x, Transport y)
		{
			Debug.Log($"x start:{x.StartPosition} y start:{y.StartPosition}");
			return
				x.StartPosition != y.StartPosition ||
				x.EndPosition != y.EndPosition ||
				x.FadeIn != y.FadeIn ||
				x.FadeOut != y.FadeOut;
		}

		// Only set values while Constructing and CommittingChanges
		private void SetValues(SerializedProperty clipProp)
		{
			StartPosition = clipProp.FindPropertyRelative(nameof(StartPosition)).floatValue;
			EndPosition = clipProp.FindPropertyRelative(nameof(EndPosition)).floatValue;
			FadeIn = clipProp.FindPropertyRelative(nameof(FadeIn)).floatValue;
			FadeOut = clipProp.FindPropertyRelative(nameof(FadeOut)).floatValue;
		}
		public void CommitChanges()
		{
			Debug.Log("生成新的AudioClip");
			// 生成新的AudioClip

			SetValues(_clipProperty);
		}

		public void DiscardChanges()
		{
			_clipProperty.FindPropertyRelative(nameof(StartPosition)).floatValue = StartPosition;
			_clipProperty.FindPropertyRelative(nameof(EndPosition)).floatValue = EndPosition;
			_clipProperty.FindPropertyRelative(nameof(FadeIn)).floatValue = FadeIn;
			_clipProperty.FindPropertyRelative(nameof(FadeOut)).floatValue = FadeOut;
		}

		public bool IsDirty()
		{
			return IsDifferent(this, new Transport(_clipProperty));
		}
	}
}