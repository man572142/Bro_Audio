using UnityEditor;
using UnityEngine;
using static Ami.Extension.EditorScriptingExtension;
using static Ami.BroAudio.Editor.Setting.BroAudioGUISetting;
using static Ami.BroAudio.Editor.IconConstant;
using Ami.Extension;
using System;
using System.Collections.Generic;

namespace Ami.BroAudio.Editor
{
	public class DrawClipPropertiesHelper
	{
		public struct DraggablePoint
		{
			public Rect Rect;
			public readonly Action<float> OnSetPlaybackPosition;

			public DraggablePoint(Rect position, Action<float> onSetPlaybackPos)
			{
				Rect = position;
				OnSetPlaybackPosition = onSetPlaybackPos;
			}

			public void SetPlaybackPosition(float value)
			{
				OnSetPlaybackPosition?.Invoke(value);
			}

			public bool IsDefault()
			{
				return Rect == default;
			}
		}

		public class ClipData
		{
			public Texture WaveformTexture;
			public Dictionary<TransportType, DraggablePoint> DraggablePoints = new Dictionary<TransportType, DraggablePoint>();
		}
		
		public const float DragPointSizeLength = 20f;

		public float ClipPreviewHeight { get; private set; }

		private Color _silentMaskColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
		private Color _fadingMaskColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);
		private Color _startEndColor = Color.white;
		private Color _fadingLineColor = Color.green;

		private GUIContent[] FadeLabels = { new GUIContent("    In    "), new GUIContent(" Out ") };
		private GUIContent[] PlaybackLabels = { new GUIContent(" Start "), new GUIContent(" End ") };
		private Dictionary<string, ClipData> _clipDataDict = new Dictionary<string, ClipData>();

		private KeyValuePair<string, DraggablePoint> _currDraggedPoint = default;

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

		public void DrawPlaybackPositionField(Rect position, ITransport transport)
		{		
			EditorGUI.BeginChangeCheck();
			EditorGUI.MultiFloatField(position, new GUIContent("Playback Position"), PlaybackLabels, transport.PlaybackValues);
			if(EditorGUI.EndChangeCheck())
			{
				transport.SetValue(transport.PlaybackValues[0], TransportType.Start);
				transport.SetValue(transport.PlaybackValues[1], TransportType.End);
			}
		}

		public void DrawFadingField(Rect position, ITransport transport)
		{
			EditorGUI.BeginChangeCheck();
			EditorGUI.MultiFloatField(position, new GUIContent("Fade"), FadeLabels, transport.FadingValues);
			if (EditorGUI.EndChangeCheck())
			{
				transport.SetValue(transport.FadingValues[0], TransportType.FadeIn);
				transport.SetValue(transport.FadingValues[1], TransportType.FadeOut);
			}
		}

		public void DrawClipPreview(Rect clipViewRect, ITransport transport, AudioClip audioClip,string propertyPath)
		{
			clipViewRect.height = ClipPreviewHeight;
			SplitRectHorizontal(clipViewRect, 0.1f, 15f, out Rect playbackRect, out Rect waveformRect);
			ClipData clipData = GetOrCreateClipData(propertyPath, audioClip);

			DrawWaveformPreview();
			DrawPlaybackButton();
			
			if(Event.current.type == EventType.Layout || waveformRect.width <= 0f)
			{
				return;
			}
			TransportVectorPoints points = new TransportVectorPoints(transport, new Vector2(waveformRect.width,ClipPreviewHeight), audioClip.length);
			DrawClipPlaybackLine();
			DrawDraggable();
			DrawClipLength();

			void DrawWaveformPreview()
			{
				if (clipData != null)
				{
					EditorGUI.DrawPreviewTexture(waveformRect, clipData.WaveformTexture);
					EditorGUI.DrawRect(waveformRect, ShadowMaskColor);
				}
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
				GUI.BeginClip(waveformRect);
				{
					Handles.color = _fadingLineColor;
					Handles.DrawAAPolyLine(2f, points.GetVectorsClockwise());

					Handles.color = _startEndColor;
					Handles.DrawAAPolyLine(1f, points.Start, new Vector3(points.Start.x, 0f));
					Handles.DrawAAPolyLine(1f, points.End, new Vector3(points.End.x, 0f));

					Handles.color = _silentMaskColor;
					Vector3[] silentToStart = new Vector3[4];
					silentToStart[0] = Vector3.zero;
					silentToStart[1] = new Vector3(points.Start.x, 0f);
					silentToStart[2] = points.Start;
					silentToStart[3] = new Vector3(0f, ClipPreviewHeight);
					Handles.DrawAAConvexPolygon(silentToStart);

					Handles.color = _fadingMaskColor;
					Vector3[] startToFadeIn = new Vector3[3];
					startToFadeIn[0] = new Vector3(points.Start.x, 0f);
					startToFadeIn[1] = points.FadeIn;
					startToFadeIn[2] = points.Start;
					Handles.DrawAAConvexPolygon(startToFadeIn);

					Handles.color = _fadingMaskColor;
					Vector3[] fadeOutToEnd = new Vector3[3];
					fadeOutToEnd[0] = points.FadeOut;
					fadeOutToEnd[1] = new Vector3(points.End.x, 0f);
					fadeOutToEnd[2] = points.End;
					Handles.DrawAAConvexPolygon(fadeOutToEnd);

					Handles.color = _silentMaskColor;
					Vector3[] endToSilent = new Vector3[4];
					endToSilent[0] = new Vector3(points.End.x, 0f);
					endToSilent[1] = new Vector3(waveformRect.width, 0f, 0f);
					endToSilent[2] = new Vector3(waveformRect.width, ClipPreviewHeight, 0f);
					endToSilent[3] = points.End;
					Handles.DrawAAConvexPolygon(endToSilent);
				}
				GUI.EndClip();
			}

			void DrawDraggable()
			{
				Vector2 offset = new Vector2(-DragPointSizeLength * 0.5f, -DragPointSizeLength);
				Vector2 dragPointSize = new Vector2(DragPointSizeLength, DragPointSizeLength);

				Vector2 startPos = new Vector2(points.Start.x, 0f).DeScope(waveformRect, offset);
				Vector2 fadeInPos = new Vector2(points.FadeIn.x, dragPointSize.y).DeScope(waveformRect, offset);
				Vector2 fadeOutPos = new Vector2(points.FadeOut.x, dragPointSize.y).DeScope(waveformRect, offset);
				Vector2 endPos = new Vector2(points.End.x, 0f).DeScope(waveformRect, offset);

				Rect startRect = new Rect(startPos, dragPointSize);
				Rect fadeInRect = new Rect(fadeInPos, dragPointSize);
				Rect fadeOutRect = new Rect(fadeOutPos, dragPointSize);
				Rect endRect = new Rect(endPos, dragPointSize);

				clipData.DraggablePoints[TransportType.Start] = GetDraggablePoint(startRect, transport, TransportType.Start);
				clipData.DraggablePoints[TransportType.FadeIn] = GetDraggablePoint(fadeInRect, transport, TransportType.FadeIn);
				clipData.DraggablePoints[TransportType.FadeOut] = GetDraggablePoint(fadeOutRect, transport, TransportType.FadeOut);
				clipData.DraggablePoints[TransportType.End] = GetDraggablePoint(endRect, transport, TransportType.End);

				Vector4 leftPointBorder = new Vector4(DragPointSizeLength * 0.5f, 0f, 0f, 0f);
				Vector4 rightPointBorder = new Vector4(0f, 0f, DragPointSizeLength * 0.5f, 0f);
				GUI.DrawTexture(startRect, EditorGUIUtility.IconContent(PlaybackPosIcon).image, ScaleMode.ScaleToFit, true, 0f, _startEndColor, leftPointBorder, 0f);
				GUI.DrawTexture(fadeInRect, EditorGUIUtility.IconContent(FadeInIcon).image, ScaleMode.ScaleToFit, true, 0f, _fadingLineColor, 0f, 0f);
				GUI.DrawTexture(fadeOutRect, EditorGUIUtility.IconContent(FadeOutIcon).image, ScaleMode.ScaleToFit, true, 0f, _fadingLineColor, 0f, 0f);
				GUI.DrawTexture(endRect, EditorGUIUtility.IconContent(PlaybackPosIcon).image, ScaleMode.ScaleToFit, true, 0f, _startEndColor, rightPointBorder, 0f);

				Event currEvent = Event.current;
				if (currEvent.type == EventType.MouseDown)
				{
					foreach (DraggablePoint point in clipData.DraggablePoints.Values)
					{
						if (point.Rect.Contains(currEvent.mousePosition))
						{
							_currDraggedPoint = new KeyValuePair<string, DraggablePoint>(propertyPath, point);
							currEvent.Use();
							break;
						}
					}
				}
				else if (currEvent.type == EventType.MouseDrag && _currDraggedPoint.Key == propertyPath && !_currDraggedPoint.Value.IsDefault())
				{
					float posInSeconds = currEvent.mousePosition.Scoping(waveformRect).x / waveformRect.width * audioClip.length;
					_currDraggedPoint.Value.SetPlaybackPosition(posInSeconds);
					currEvent.Use();
				}
				else if (currEvent.type == EventType.MouseUp)
				{
					_currDraggedPoint = default;
				}

#if BroAudio_DevOnly && BroAudio_ShowClipDraggableArea
				foreach (var point in pointsDict.Values)
				{
					EditorGUI.DrawRect(point.Rect, new Color(1f, 1f, 1f, 0.3f));
				}
#endif
			}

			void DrawClipLength()
			{
				Rect labelRect = new Rect(waveformRect);
				labelRect.height = EditorGUIUtility.singleLineHeight;
				labelRect.y = waveformRect.yMax - labelRect.height;
				float currentLength = audioClip.length - transport.StartPosition - transport.EndPosition;
				EditorGUI.DropShadowLabel(labelRect, currentLength.ToString("0.00") + "s");
			}
		}

		private ClipData GetOrCreateClipData(string propertyPath,AudioClip audioClip)
		{
			if (!_clipDataDict.TryGetValue(propertyPath, out var clipData))
			{
				clipData = new ClipData();
				clipData.WaveformTexture = AssetPreview.GetAssetPreview(audioClip);
				clipData.DraggablePoints = new Dictionary<TransportType, DraggablePoint>()
					{
						{ TransportType.Start,default},{ TransportType.FadeIn,default},{ TransportType.FadeOut,default},{ TransportType.End,default},
					};
				_clipDataDict.Add(propertyPath, clipData);
			}

			return clipData;
		}

		private DraggablePoint GetDraggablePoint(Rect rect, ITransport transport,TransportType transportType)
		{
			switch (transportType)
			{
				case TransportType.Start:
					return new DraggablePoint(rect, posInSec => transport.SetValue(posInSec,transportType));
				case TransportType.FadeIn:
					return new DraggablePoint(rect, posInSec => transport.SetValue(posInSec - transport.StartPosition,transportType));
				case TransportType.FadeOut:
					return new DraggablePoint(rect, posInSec => transport.SetValue(transport.FullLength - transport.EndPosition - posInSec,transportType));
				case TransportType.End:
					return new DraggablePoint(rect, posInSec => transport.SetValue(transport.FullLength - posInSec,transportType));
				default:
					Tools.BroLog.LogError($"No corresponding point for transport type {transportType}");
					return default;
			}
		}
	}
}
