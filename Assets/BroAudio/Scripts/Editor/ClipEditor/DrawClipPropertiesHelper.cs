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
			public Texture Image;
			public Vector4 ImageBorder;
			public Color ColorTint;
			public Action<float> OnSetPlaybackPosition;

			public DraggablePoint(Rect position) : this()
			{
				Rect = position;
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
		
		public const float DragPointSizeLength = 20f;

		public float ClipPreviewHeight { get; private set; }

		private Color _silentMaskColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
		private Color _fadingMaskColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);
		private Color _startEndColor = Color.white;
		private Color _fadingLineColor = Color.green;

		private GUIContent[] FadeLabels = { new GUIContent("    In    "), new GUIContent(" Out ") };
		private GUIContent[] PlaybackLabels = { new GUIContent(" Start "), new GUIContent(" End ") };
		private Dictionary<string, Dictionary<TransportType, DraggablePoint>> _clipDraggablePointsDict = new Dictionary<string, Dictionary<TransportType, DraggablePoint>>();
		private KeyValuePair<string, DraggablePoint> _currDraggedPoint = default;

		private TransportType[] _allTransportType = Enum.GetValues(typeof(TransportType)) as TransportType[];

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

		public void DrawClipPreview(Rect clipViewRect, ITransport transport, AudioClip audioClip,string clipPath)
		{
			clipViewRect.height = ClipPreviewHeight;
			SplitRectHorizontal(clipViewRect, 0.1f, 15f, out Rect playbackRect, out Rect waveformRect);
			waveformRect.width -= 5f;
			var draggablePoints = GetOrCreateDraggablePoints(clipPath);

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
			HandleDraggable();

			void DrawWaveformPreview()
			{
				Texture2D waveformTexture = AssetPreview.GetAssetPreview(audioClip);
				if (waveformTexture != null)
				{
					if(Event.current.type == EventType.Repaint)
					{
						GUI.skin.window.Draw(waveformRect, false, false, false, false);
					}

					EditorGUI.DrawPreviewTexture(waveformRect, waveformTexture);
				}
			}

			void DrawPlaybackButton()
			{
				SplitRectVertical(playbackRect, 0.5f, 15f, out Rect playRect, out Rect stopRect);
				// Keep in square
				float maxHeight = playRect.height;
				playRect.width = Mathf.Clamp(playRect.width, playRect.width, maxHeight);
				playRect.height = playRect.width;
				stopRect.width = playRect.width;
				stopRect.height = playRect.height;

				if (GUI.Button(playRect, EditorGUIUtility.IconContent(PlayButton)))
				{
					EditorPlayAudioClip.PlaybackIndicator.SetClipInfo(waveformRect, transport);
					EditorPlayAudioClip.PlayClip(audioClip, transport.StartPosition, transport.EndPosition);
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
				ScaleMode scaleMode = ScaleMode.ScaleToFit;
				foreach (var transportType in _allTransportType)
				{
					DraggablePoint point = GetDraggablePoint(waveformRect, points, transport, transportType);
					draggablePoints[transportType] = point;
					EditorGUIUtility.AddCursorRect(point.Rect, MouseCursor.SlideArrow);
					GUI.DrawTexture(point.Rect, point.Image, scaleMode, true, 0f, point.ColorTint, point.ImageBorder, 0f);
				}	
			}

			void DrawClipLength()
			{
				Rect labelRect = new Rect(waveformRect);
				labelRect.height = EditorGUIUtility.singleLineHeight;
				labelRect.y = waveformRect.yMax - labelRect.height;
				float currentLength = audioClip.length - transport.StartPosition - transport.EndPosition;
				EditorGUI.DropShadowLabel(labelRect, currentLength.ToString("0.00") + "s");
			}

			void HandleDraggable()
			{
				Event currEvent = Event.current;
				if (currEvent.type == EventType.MouseDown)
				{
					foreach (DraggablePoint point in draggablePoints.Values)
					{
						if (point.Rect.Contains(currEvent.mousePosition))
						{
							_currDraggedPoint = new KeyValuePair<string, DraggablePoint>(clipPath, point);
							currEvent.Use();
							break;
						}
					}
				}
				else if (currEvent.type == EventType.MouseDrag && _currDraggedPoint.Key == clipPath && !_currDraggedPoint.Value.IsDefault())
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
				foreach (var point in draggablePoints.Values)
				{
					EditorGUI.DrawRect(point.Rect, new Color(1f, 1f, 1f, 0.3f));
				}
#endif
			}
		}

		public static void DrawPlaybackIndicator(Rect scope, Vector2 positionOffset = default)
		{
			var indicator = EditorPlayAudioClip.PlaybackIndicator;
			if (indicator.IsPlaying)
			{
				GUI.BeginClip(scope);
				{
					Rect indicatorRect = indicator.GetIndicatorPosition();
					EditorGUI.DrawRect(new Rect(indicatorRect.position + positionOffset, indicatorRect.size), indicator.Color);
				}
				GUI.EndClip();
			}
		}

		private Dictionary<TransportType, DraggablePoint> GetOrCreateDraggablePoints(string clipPath)
		{
			if (!_clipDraggablePointsDict.TryGetValue(clipPath, out var draggablePoints))
			{
				draggablePoints = new Dictionary<TransportType, DraggablePoint>()
					{
						{ TransportType.Start,default},{ TransportType.FadeIn,default},{ TransportType.FadeOut,default},{ TransportType.End,default},
					};
				_clipDraggablePointsDict.Add(clipPath, draggablePoints);
			}

			return draggablePoints;
		}

		private DraggablePoint GetDraggablePoint(Rect waveformRect,TransportVectorPoints points, ITransport transport,TransportType transportType)
		{
			Rect rect = GetDraggableRect(waveformRect, points, transportType);
			switch (transportType)
			{
				case TransportType.Start:
					return new DraggablePoint(rect)
					{
						Image = EditorGUIUtility.IconContent(PlaybackPosIcon).image,
						ImageBorder = new Vector4(DragPointSizeLength * 0.5f, 0f, 0f, 0f),
						ColorTint = _startEndColor,
						OnSetPlaybackPosition = posInSec => transport.SetValue(posInSec, transportType),
					};
				case TransportType.FadeIn:
					return new DraggablePoint(rect)
					{
						Image = EditorGUIUtility.IconContent(FadeInIcon).image,
						ColorTint = _fadingLineColor,
						OnSetPlaybackPosition = posInSec => transport.SetValue(posInSec - transport.StartPosition, transportType),
					};
				case TransportType.FadeOut:
					return new DraggablePoint(rect)
					{
						Image = EditorGUIUtility.IconContent(FadeOutIcon).image,
						ColorTint = _fadingLineColor,
						OnSetPlaybackPosition = posInSec => transport.SetValue(transport.Length - transport.EndPosition - posInSec, transportType),
					};
				case TransportType.End:
					return new DraggablePoint(rect)
					{
						Image = EditorGUIUtility.IconContent(PlaybackPosIcon).image,
						ImageBorder = new Vector4(0f, 0f, DragPointSizeLength * 0.5f, 0f),
						ColorTint = _startEndColor,
						OnSetPlaybackPosition = posInSec => transport.SetValue(transport.Length - posInSec, transportType),
					};
				default:
					Tools.BroLog.LogError($"No corresponding point for transport type {transportType}");
					return default;
			}
		}

		private Rect GetDraggableRect(Rect waveformRect,TransportVectorPoints points, TransportType transportType)
		{
			Vector2 offset = new Vector2(-DragPointSizeLength * 0.5f, -DragPointSizeLength);
			Vector2 dragPointSize = new Vector2(DragPointSizeLength, DragPointSizeLength);
			Vector2 position = default;
			switch (transportType)
			{
				case TransportType.Start:
					position = new Vector2(points.Start.x, 0f).DeScope(waveformRect, offset);
					break;
				case TransportType.FadeIn:
					position = new Vector2(points.FadeIn.x, dragPointSize.y).DeScope(waveformRect, offset);
					break;
				case TransportType.FadeOut:
					position = new Vector2(points.FadeOut.x, dragPointSize.y).DeScope(waveformRect, offset);
					break;
				case TransportType.End:
					position = new Vector2(points.End.x, 0f).DeScope(waveformRect, offset);
					break;
			}
			return new Rect(position, dragPointSize);
		}
	}
}
