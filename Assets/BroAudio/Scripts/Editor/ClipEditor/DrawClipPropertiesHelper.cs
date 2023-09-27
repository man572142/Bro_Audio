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
		public struct TransportVectorPoints
		{
			public readonly ITransport Transport;
			public readonly Vector2 DrawingSize;
			public readonly float ClipLength;

			public TransportVectorPoints(ITransport transport, Vector2 drawingSize, float clipLength)
			{
				Transport = transport;
				DrawingSize = drawingSize;
				ClipLength = clipLength;
			}

			public Vector3 Start => new Vector3(Mathf.Lerp(0f, DrawingSize.x, Transport.StartPosition / ClipLength), DrawingSize.y);
			public Vector3 FadeIn => new Vector3(Mathf.Lerp(0f, DrawingSize.x, (Transport.StartPosition + Transport.FadeIn) / ClipLength), 0f);
			public Vector3 FadeOut => new Vector3(Mathf.Lerp(0f, DrawingSize.x, (ClipLength - Transport.EndPosition - Transport.FadeOut) / ClipLength), 0f);
			public Vector3 End => new Vector3(Mathf.Lerp(0f, DrawingSize.x, (ClipLength - Transport.EndPosition) / ClipLength), DrawingSize.y);
			public Vector3[] GetVectorsClockwise()
			{
				return new Vector3[] { Start, FadeIn, FadeOut, End };
			}
		}

		public class DraggablePoint
		{
			public Rect Rect;
			public readonly Action<float> OnSetPlaybackPosition;
			public readonly Func<float> OnGetDrawPositionX;

			public DraggablePoint(Rect position, Action<float> onSetPlaybackPos, Func<float> onGetDrawPosition)
			{
				Rect = position;
				OnSetPlaybackPosition = onSetPlaybackPos;
				OnGetDrawPositionX = onGetDrawPosition;
			}

			public void SetPlaybackPosition(float value)
			{
				OnSetPlaybackPosition?.Invoke(value);
			}

			public void RefreshRect()
			{
				Rect.position = new Vector2(OnGetDrawPositionX.Invoke(), Rect.position.y);
			}
		}

		public const float DragPointWidth = 15f;
		public const int FloatFieldDigits = 2;

		public float ClipPreviewHeight { get; private set; }

		private Color _silentColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
		private Color _fadingColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);
		private Color _startEndColor = Color.white;
		private Color _fadingLineColor = Color.green;

		private GUIContent[] FadeLabels = { new GUIContent("    In    "), new GUIContent(" Out ") };
		private GUIContent[] PlaybackLabels = { new GUIContent(" Start "), new GUIContent(" End ") };
		private List<DraggablePoint> _draggablePoints = null;
		private DraggablePoint _currDraggedPoint = null;

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
			EditorGUI.MultiFloatField(position, new GUIContent("Playback Position"), PlaybackLabels, transport.GetMultiFloatValues(TransportType.PlaybackPosition));
			transport.ClampAndSetProperty(TransportType.PlaybackPosition);
		}

		public void DrawFadingField(Rect position, ITransport transport)
		{
			EditorGUI.MultiFloatField(position, new GUIContent("Fade"), FadeLabels, transport.GetMultiFloatValues(TransportType.Fading));
			transport.ClampAndSetProperty(TransportType.Fading);
		}

		public void DrawClipPreview(Rect position, ITransport transport, AudioClip audioClip)
		{
			Rect clipViewRect = position;
			clipViewRect.height = ClipPreviewHeight;
			SplitRectHorizontal(clipViewRect, 0.1f, 15f, out Rect playbackRect, out Rect waveformRect);

			DrawWaveformPreview();
			DrawPlaybackButton();
			
			if(Event.current.type == EventType.Layout || waveformRect.width <= 0f)
			{
				return;
			}
			TransportVectorPoints points = new TransportVectorPoints(transport, new Vector2(waveformRect.width,ClipPreviewHeight), audioClip.length);
			DrawClipPlaybackLine();
			DrawDraggable();


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
				GUI.BeginClip(waveformRect);
				{
					Handles.color = _fadingLineColor;
					Handles.DrawAAPolyLine(2f, points.GetVectorsClockwise());

					Handles.color = _startEndColor;
					Handles.DrawAAPolyLine(1f, points.Start, new Vector3(points.Start.x, 0f));
					Handles.DrawAAPolyLine(1f, points.End, new Vector3(points.End.x, 0f));

					Handles.color = _silentColor;
					Vector3[] silentToStart = new Vector3[4];
					silentToStart[0] = Vector3.zero;
					silentToStart[1] = new Vector3(points.Start.x, 0f);
					silentToStart[2] = points.Start;
					silentToStart[3] = new Vector3(0f, ClipPreviewHeight);
					Handles.DrawAAConvexPolygon(silentToStart);

					Handles.color = _fadingColor;
					Vector3[] startToFadeIn = new Vector3[3];
					startToFadeIn[0] = new Vector3(points.Start.x, 0f);
					startToFadeIn[1] = points.FadeIn;
					startToFadeIn[2] = points.Start;
					Handles.DrawAAConvexPolygon(startToFadeIn);

					Handles.color = _fadingColor;
					Vector3[] fadeOutToEnd = new Vector3[3];
					fadeOutToEnd[0] = points.FadeOut;
					fadeOutToEnd[1] = new Vector3(points.End.x, 0f);
					fadeOutToEnd[2] = points.End;
					Handles.DrawAAConvexPolygon(fadeOutToEnd);

					Handles.color = _silentColor;
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
				Rect dragValuesRect = new Rect(waveformRect);
				dragValuesRect.height = EditorGUIUtility.singleLineHeight;
				dragValuesRect.y -= EditorGUIUtility.singleLineHeight;

				// TODO: Clip太剛好而導致Icon超出Clip而看不見
				GUI.BeginClip(dragValuesRect);
				{
					Vector2 dragPointSize = new Vector2(DragPointWidth, DragPointWidth);
					Rect startRect = new Rect(new Vector2(GetPointPosX(points.Start), 0f), dragPointSize);
					Rect fadeInRect = new Rect(new Vector2(GetPointPosX(points.FadeIn), 4f), dragPointSize);
					Rect fadeOutRect = new Rect(new Vector2(GetPointPosX(points.FadeOut), 4f), dragPointSize);
					Rect endRect = new Rect(new Vector2(GetPointPosX(points.End), 0f), dragPointSize);

					if (_draggablePoints == null)
					{
						_draggablePoints = new List<DraggablePoint>()
						{
							new DraggablePoint(startRect, posInSec => transport.StartPosition = posInSec,() => GetPointPosX(points.Start)),
							new DraggablePoint(fadeInRect, posInSec => transport.FadeIn = posInSec - transport.StartPosition , () => GetPointPosX(points.FadeIn)),
							new DraggablePoint(fadeOutRect, posInSec => transport.FadeOut = audioClip.length - transport.EndPosition - posInSec, () => GetPointPosX(points.FadeOut)),
							new DraggablePoint(endRect, posInSec => transport.EndPosition = audioClip.length - posInSec , () => GetPointPosX(points.End)),
						};
					}

					Vector4 leftPointBorder = new Vector4(DragPointWidth * 0.5f,0f,0f,0f);
					Vector4 rightPointBorder = new Vector4(0f, 0f, DragPointWidth * 0.5f + 1f, 0f);
					GUI.DrawTexture(startRect, EditorGUIUtility.IconContent(PlaybackPosition).image, ScaleMode.ScaleToFit, true, 0f, _startEndColor, leftPointBorder,0f);
					GUI.DrawTexture(fadeInRect, EditorGUIUtility.IconContent(FadingPosition).image, ScaleMode.ScaleToFit, true, 0f, _fadingLineColor, leftPointBorder, 0f);
					GUI.DrawTexture(fadeOutRect, EditorGUIUtility.IconContent(FadingPosition).image, ScaleMode.ScaleToFit, true, 0f, _fadingLineColor, rightPointBorder, 0f);
					GUI.DrawTexture(endRect, EditorGUIUtility.IconContent(PlaybackPosition).image, ScaleMode.ScaleToFit, true, 0f, _startEndColor, rightPointBorder, 0f);

					Event currEvent = Event.current;
					if (currEvent.type == EventType.MouseDown)
					{
						foreach (DraggablePoint point in _draggablePoints)
						{
							if (point.Rect.Contains(currEvent.mousePosition))
							{
								_currDraggedPoint = point;
								currEvent.Use();
								break;
							}
						}
					}
					else if(currEvent.type == EventType.MouseDrag && _currDraggedPoint != null)
					{
						float posInSeconds = currEvent.mousePosition.x / dragValuesRect.width * audioClip.length;
						_currDraggedPoint.SetPlaybackPosition(posInSeconds);
						foreach (var point in _draggablePoints)
						{
							point.RefreshRect();
						}
						currEvent.Use();
					}
					else if(currEvent.type == EventType.MouseUp)
					{
						_currDraggedPoint = null;
					}
				}
				GUI.EndClip();
			}

			float GetPointPosX(Vector3 pos)
			{
				return pos.x - DragPointWidth * 0.5f;
			}
		}
	}
}
