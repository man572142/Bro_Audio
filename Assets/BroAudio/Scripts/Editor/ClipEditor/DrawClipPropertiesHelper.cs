using UnityEditor;
using UnityEngine;
using static Ami.Extension.EditorScriptingExtension;
using static Ami.BroAudio.Editor.Setting.BroAudioGUISetting;
using static Ami.BroAudio.Editor.IconConstant;
using Ami.Extension;

namespace Ami.BroAudio.Editor
{
	public class DrawClipPropertiesHelper
	{
		public float ClipPreviewHeight { get; private set; }

		private GUIContent[] FadeLabels = { new GUIContent("    In    "), new GUIContent(" Out ") };
		private float[] FadeValues = new float[2];
		private GUIContent[] PlaybackLabels = { new GUIContent(" Start "), new GUIContent(" End ") };
		private float[] PlaybackValues = new float[2];

		public DrawClipPropertiesHelper()
		{
		}

		public DrawClipPropertiesHelper(float clipPreviewHeight)
		{
			ClipPreviewHeight = clipPreviewHeight;
		}

		public void SetPreviewHeight(float value)
		{
			ClipPreviewHeight = value;
		}

		public float DrawVolumeField(Rect position, string label, float currentValue,RangeFloat range)
		{
			return EditorGUI.Slider(position, label, currentValue, range.Min,range.Max);
		}

		public void SetCurrentTransport(Transport transport)
		{
			PlaybackValues[0] = transport.StartPosition;
			PlaybackValues[1] = transport.EndPosition;
			FadeValues[0] = transport.FadeIn;
			FadeValues[1] = transport.FadeOut;
		}

		public void DrawPlaybackPositionField(Rect position, Transport oldValue, out Transport newValue)
		{
			newValue = new Transport();
			EditorGUI.MultiFloatField(position, new GUIContent("Playback Position"), PlaybackLabels, PlaybackValues);
			newValue.StartPosition = Mathf.Clamp(PlaybackValues[0], 0f, GetLengthLimit(0, oldValue.EndPosition, oldValue.FadeIn, oldValue.FadeOut, oldValue.FullLength));
			PlaybackValues[0] = newValue.StartPosition;
			newValue.EndPosition = Mathf.Clamp(PlaybackValues[1], 0f, GetLengthLimit(oldValue.StartPosition, 0f, oldValue.FadeIn, oldValue.FadeOut, oldValue.FullLength));
			PlaybackValues[1] = newValue.EndPosition;
		}

		public void DrawFadingField(Rect position, Transport oldValue, out Transport newValue)
		{
			newValue = new Transport();
			EditorGUI.MultiFloatField(position, new GUIContent("Fade"), FadeLabels, FadeValues);
			newValue.FadeIn = Mathf.Clamp(FadeValues[0], 0f, GetLengthLimit(oldValue.StartPosition, oldValue.EndPosition, 0f, oldValue.FadeOut, oldValue.FullLength));
			FadeValues[0] = newValue.FadeIn;
			newValue.FadeOut = Mathf.Clamp(FadeValues[1], 0f, GetLengthLimit(oldValue.StartPosition, oldValue.EndPosition, oldValue.FadeIn, 0f, oldValue.FullLength));
			FadeValues[1] = newValue.FadeOut;
		}

		public void DrawClipPreview(Rect position, Transport transport, AudioClip audioClip)
		{
			Rect clipViewRect = position;
			clipViewRect.height = ClipPreviewHeight;
			SplitRectHorizontal(clipViewRect, 0.1f, 15f, out Rect playbackRect, out Rect waveformRect);

			DrawWaveformPreview();
			DrawPlaybackButton();
			DrawClipPlaybackLine();

			void DrawWaveformPreview()
			{
				Texture2D waveformTexture = AssetPreview.GetAssetPreview(audioClip);
				if (waveformTexture != null)
				{
					GUI.DrawTexture(waveformRect, waveformTexture);
				}
				EditorGUI.DrawRect(waveformRect, ShadowMaskColor);
			}

			void DrawPlaybackButton()
			{
				SplitRectVertical(playbackRect, 0.5f, 15f, out Rect playRect, out Rect stopRect);
				// 保持在正方形
				float maxHeight = playRect.height;
				playRect.width = Mathf.Clamp(playRect.width, playRect.width, maxHeight);
				playRect.height = playRect.width;
				stopRect.width = playRect.width;
				stopRect.height = playRect.height;

				if (GUI.Button(playRect, EditorGUIUtility.IconContent(PlayButton)))
				{
					EditorPlayAudioClip.StopAllClips();
					EditorPlayAudioClip.PlayClip(audioClip, Mathf.RoundToInt(AudioSettings.outputSampleRate * transport.StartPosition));

					float duration = audioClip.length - transport.StartPosition - transport.EndPosition;
					AsyncTaskExtension.DelayDoAction(duration, EditorPlayAudioClip.StopAllClips);
				}
				if (GUI.Button(stopRect, EditorGUIUtility.IconContent(StopButton)))
				{
					EditorPlayAudioClip.StopAllClips();
				}

				EditorGUI.DrawRect(playRect, PlayButtonColor);
				EditorGUI.DrawRect(stopRect, StopButtonColor);
			}

			void DrawClipPlaybackLine()
			{
				Vector3[] points = GetClipLinePoints(waveformRect.width, audioClip.length);
				if (points.Length < 4)
				{
					return;
				}

				GUI.BeginClip(waveformRect);
				Handles.color = Color.green;
				Handles.DrawAAPolyLine(2f, points);

				Handles.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);
				Vector3[] leftBlock = new Vector3[4];
				leftBlock[0] = Vector3.zero;
				leftBlock[1] = points[1];
				leftBlock[2] = points[0];
				leftBlock[3] = new Vector3(0f, ClipPreviewHeight, 0f);
				Handles.DrawAAConvexPolygon(leftBlock);

				Vector3[] rightBlock = new Vector3[4];
				rightBlock[0] = points[2];
				rightBlock[1] = new Vector3(waveformRect.width, 0f, 0f);
				rightBlock[2] = new Vector3(waveformRect.width, ClipPreviewHeight, 0f);
				rightBlock[3] = points[3];
				Handles.DrawAAConvexPolygon(rightBlock);

				GUI.EndClip();
			}
		}

		private Vector3[] GetClipLinePoints(float width, float clipLength)
		{
			if (width <= 0f)
				return new Vector3[0];

			Vector3[] points = new Vector3[4];
			// Start
			points[0] = new Vector3(Mathf.Lerp(0f, width, PlaybackValues[0] / clipLength), ClipPreviewHeight, 0f);
			// FadeIn
			points[1] = new Vector3(Mathf.Lerp(0f, width, (PlaybackValues[0] + FadeValues[0]) / clipLength), 0f, 0f);
			// FadeOut
			points[2] = new Vector3(Mathf.Lerp(0f, width, (clipLength - PlaybackValues[1] - FadeValues[1]) / clipLength), 0f, 0f);
			// End
			points[3] = new Vector3(Mathf.Lerp(0f, width, (clipLength - PlaybackValues[1]) / clipLength), ClipPreviewHeight, 0f);

			return points;
		}

		private float GetLengthLimit(float start, float end, float fadeIn, float fadeOut, float clipLength)
		{
			return clipLength - start - end - fadeIn - fadeOut;
		}
	} 
}
