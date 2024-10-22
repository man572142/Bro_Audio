using UnityEditorInternal;
using UnityEditor;
using UnityEngine;
using Ami.Extension;
using static Ami.BroAudio.SpectrumAnalyzer;
using static Ami.Extension.EditorScriptingExtension;
using static Ami.Extension.AudioConstant;
using System.Collections.Generic;
using System;

namespace Ami.BroAudio.Editor
{
    [CustomEditor(typeof(SpectrumAnalyzer))]
    public class SpectrumAnalyzerEditor : UnityEditor.Editor
    {
        private struct PropertyData
        {
            public SerializedProperty Property;
            public GUIContent Label;
        }

        public const int MinResolutionScale = 6;
        public const int MaxResolutionScale = 13;
        public const int BandElementLineCount = 3;
        public const float SpectrumViewHeight = 120f;
        public const float SpectrumViewLabelHeight = 20f;
        public const float SpectrumViewLabelWidth = 30f;
        public const float SPectrumViewOffset = 6f;
        public const int AmplitubeScaleCount = 4;

        private PropertyData _soundSourceProp, _resolutionProp, _channelProp, _windowProp, 
            _meteringProp, _attackProp, _decayProp,_smoothProp, _bandsProp;

        ReorderableList _bandsList = null;

        private int[] _resolutionValues = new int[MaxResolutionScale - MinResolutionScale + 1];
        private GUIContent[] _resolutionLabels = new GUIContent[MaxResolutionScale - MinResolutionScale + 1];
        private Rect[] _bandRects = new Rect[BandElementLineCount];
        private float[] _bandRectRatios = { 0.34f, 0.33f, 0.33f };
        private List<(float freq, float logFreq)> _referenceFrequencies = null;
        private float[] _referenceLabels = { 20f, 50f, 100f, 200f, 500f, 1000f, 2000f, 5000f, 10000f, 20000f };

        private GUIContent _indicator = null;
        private IReadOnlyList<Band> _bands = null;

        private Vector2 BandHandleSize
        {
            get
            {
                float width = Mathf.Clamp(EditorGUIUtility.currentViewWidth * 0.05f, 10f, 30f);
                return new Vector2(width, SpectrumViewHeight + SpectrumViewLabelHeight * 2);
            }
        }

        private PropertyData CreateProperty(string propertyPath, string tooltip, string overridedName = null)
        {
            var data = new PropertyData();
            data.Property = serializedObject.FindProperty(propertyPath);
            data.Label = new GUIContent(overridedName == null ? data.Property.displayName : overridedName, tooltip);
            return data;
        }

        private void OnEnable()
        {
            var so = serializedObject;
            _soundSourceProp = new PropertyData() { Property = so.FindProperty(NameOf.SoundSource) };
            _bandsProp = new PropertyData() { Property = so.FindProperty(NameOf.Bands) };
            _resolutionProp = CreateProperty(NameOf.ResolutionScale, "Number of data samples used for FFT, which can also be considered as frequency resolution", "Block Size");
            _channelProp = CreateProperty(NameOf.Channel, "The target channel the spectrum is sampled from");
            _windowProp = CreateProperty(NameOf.WindowType, "The FFT window type to use when sampling");
            _meteringProp = CreateProperty(NameOf.Metering, "The metering type used to calculate the amplitube of the band");
            _attackProp = CreateProperty(NameOf.Attack, "The time it takes to 'raise' a level of 20dB in milliseconds");
            _decayProp = CreateProperty(NameOf.Decay, "The time it takes to 'reduce' a level of 20dB in milliseconds");
            _smoothProp = CreateProperty(NameOf.Smooth, "Increase this value to smooth the spectrum changes. The higher the value, the smoother it becomes");

            for (int i = 0; i < _resolutionValues.Length; i++)
            {
                int scale = MinResolutionScale + i;
                _resolutionValues[i] = scale;
                _resolutionLabels[i] = new GUIContent($"{1 << scale} samples");
            }

            _bandsList = new ReorderableList(so, _bandsProp.Property)
            {
                elementHeight = (EditorGUIUtility.singleLineHeight + 1f) * BandElementLineCount + ReorderableList.Defaults.padding,
                drawHeaderCallback = OnDrawBandListHeader,
                drawElementCallback = OnDrawBandElement,
                onAddCallback = OnAddElement,
            };

            _indicator = EditorGUIUtility.IconContent(IconConstant.Indicator);

            if(Application.isPlaying && so.targetObject is SpectrumAnalyzer target)
            {
                target.OnUpdate -= OnUpdateSpectrum;
                target.OnUpdate += OnUpdateSpectrum;
                _bands = target.Bands;
            }
        }

        private void OnDrawBandListHeader(Rect rect)
        {
            EditorGUI.LabelField(rect, "Bands");
        }

        private void OnAddElement(ReorderableList list)
        {
            ReorderableList.defaultBehaviours.DoAddButton(list);
            var newElement = list.serializedProperty.GetArrayElementAtIndex(list.count - 1);

            var freqProp = newElement.FindPropertyRelative(nameof(Band.Frequency));
            if(list.count <= 1)
            {
                freqProp.floatValue = MinFrequency;
            }
            else
            {
                var lastElement = list.serializedProperty.GetArrayElementAtIndex(list.count - 2);
                var lastFreqProp = lastElement.FindPropertyRelative(nameof(Band.Frequency));
                freqProp.floatValue = Mathf.Min(lastFreqProp.floatValue + 1f, MaxFrequency);
            }

            var weightedProp = newElement.FindPropertyRelative(Band.NameOf.Weighted);
            weightedProp.floatValue = 1f;
            list.serializedProperty.serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private void OnDrawBandElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            rect.height -= ReorderableList.Defaults.padding;
            var elementProp = _bandsList.serializedProperty.GetArrayElementAtIndex(index);
            var freqProp = elementProp.FindPropertyRelative(nameof(Band.Frequency));
            var weightedProp = elementProp.FindPropertyRelative(Band.NameOf.Weighted);
            float lastFreq = GetLastFrequency();
            SplitRectVertical(rect, 1f, _bandRects, _bandRectRatios);
            string label = $"{lastFreq}Hz ~ " + $"{freqProp.floatValue}Hz".ToWhiteBold();
            EditorGUI.LabelField(_bandRects[0], label, GUIStyleHelper.RichText);

            EditorGUI.BeginChangeCheck();

            freqProp.floatValue = EditorGUI.FloatField(_bandRects[1], freqProp.displayName,freqProp.floatValue);
            if(EditorGUI.EndChangeCheck())
            {
                float freq = Mathf.Floor(freqProp.floatValue);
                freqProp.floatValue = Mathf.Clamp(freq, lastFreq, GetMaxFrequencyOfThisRange());
            }

            EditorGUI.PropertyField(_bandRects[2], weightedProp);

            float GetLastFrequency()
            {
                if (index > 0)
                {
                    var last = _bandsList.serializedProperty.GetArrayElementAtIndex(index - 1);
                    return last.FindPropertyRelative(nameof(Band.Frequency)).floatValue;
                }
                return MinFrequency;
            }

            float GetMaxFrequencyOfThisRange()
            {
                if (index < _bandsList.count - 1)
                {
                    var next = _bandsList.serializedProperty.GetArrayElementAtIndex(index + 1);
                    return next.FindPropertyRelative(nameof(Band.Frequency)).floatValue - 1f;
                }
                return MaxFrequency;
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawPropertyField(_soundSourceProp);
            if(_soundSourceProp.Property.objectReferenceValue == null && EditorGUIUtility.currentViewWidth > 350f)
            {
                Rect soundSourceRect = GUILayoutUtility.GetLastRect();
                soundSourceRect.x = soundSourceRect.xMax -= 70f;
                EditorGUI.LabelField(soundSourceRect, "Optional".SetColor(Color.grey).ToItalics(), GUIStyleHelper.RichText);
            }
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("FFT Settings", EditorStyles.boldLabel);
            _resolutionProp.Property.intValue =
                EditorGUILayout.IntPopup(_resolutionProp.Label, _resolutionProp.Property.intValue, _resolutionLabels, _resolutionValues);
            DrawPropertyField(_windowProp);
            
            DrawPropertyField(_channelProp);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Amplitube Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_meteringProp.Property, _meteringProp.Label);
            DrawPositiveNumberField(_attackProp, 0);
            DrawPositiveNumberField(_decayProp, 0);
            DrawPositiveNumberField(_smoothProp, 0);

            DrawSpectrumView();
            _bandsList.DoLayoutList();

            serializedObject.ApplyModifiedProperties();

            static void DrawPropertyField(PropertyData data)
            {
                if(data.Label != null)
                {
                    EditorGUILayout.PropertyField(data.Property, data.Label);
                }
                else
                {
                    EditorGUILayout.PropertyField(data.Property);
                }
            }

            static void DrawPositiveNumberField(PropertyData data, float min)
            {
                var property = data.Property;
                switch (property.propertyType)
                {
                    case SerializedPropertyType.Integer:
                        property.intValue = (int)Mathf.Max(EditorGUILayout.IntField(data.Label, property.intValue), min);
                        break;
                    case SerializedPropertyType.Float:
                        property.floatValue = Mathf.Max(EditorGUILayout.FloatField(data.Label, property.floatValue), min);
                        break;
                }
            }
        }

        private void DrawSpectrumView()
        {
            float width = EditorGUIUtility.currentViewWidth - 18f - 4f;
            Rect rect = GUILayoutUtility.GetRect(width, SpectrumViewHeight + SpectrumViewLabelHeight * 2);
            Rect viewRect = new Rect(rect.x - 16f, rect.y + SpectrumViewLabelHeight, width, SpectrumViewHeight);
            if (Event.current.type == EventType.Repaint)
            {
                GUI.skin.window.Draw(viewRect, false, false, false, false);
            }

            viewRect = DrawReferenceView(width, viewRect);
            DrawBands(width, viewRect, out float selectedBandX, out float previousBandOfSelectedX);
            DrawGradientRect(viewRect, selectedBandX, previousBandOfSelectedX);

            void DrawBands(float width, Rect viewRect, out float selectedBandX, out float previousBandOfSelectedX)
            {
                Event evt = Event.current;
                selectedBandX = viewRect.x;
                previousBandOfSelectedX = viewRect.x;
                float lastBandX = viewRect.x;
                for (int i = 0; i < _bandsList.count; i++)
                {
                    var elementProp = _bandsProp.Property.GetArrayElementAtIndex(i);
                    var freqProp = elementProp.FindPropertyRelative(nameof(Band.Frequency));

                    float x = width * Mathf.InverseLerp(MinFrequencyLogValue, MaxFrequencyLogValue, Mathf.Log10(freqProp.floatValue));
                    Rect lineRect = new Rect(viewRect.x + x, viewRect.y, 2f, SpectrumViewHeight);

                    Color color = BroEditorUtility.EditorSetting.GetSpectrumColor(i);
                    EditorGUI.DrawRect(lineRect, _bandsList.index == i ? color.SetAlpha(1f) : color);

                    Rect handleRect = new Rect(lineRect) { size = BandHandleSize };
                    handleRect.x -= BandHandleSize.x * 0.5f;
                    handleRect.y -= SpectrumViewLabelHeight;

                    EditorGUIUtility.AddCursorRect(handleRect, MouseCursor.SlideArrow);
                    DrawAmp(new Rect(lastBandX +1f, viewRect.yMax, lineRect.x - lastBandX - 1f, 0f), i);

                    int controlID = GUIUtility.GetControlID(FocusType.Passive);
                    switch (evt.GetTypeForControl(controlID))
                    {
                        case EventType.MouseDown:
                            if (handleRect.Contains(evt.mousePosition) && evt.button == 0)
                            {
                                GUIUtility.hotControl = controlID;
                                _bandsList.index = i;
                                EditorGUIUtility.SetWantsMouseJumping(1);
                                evt.Use();
                            }
                            break;

                        case EventType.MouseUp:
                            if (GUIUtility.hotControl == controlID && evt.button == 0)
                            {
                                GUIUtility.hotControl = 0;
                                EditorGUIUtility.SetWantsMouseJumping(0);
                                evt.Use();
                            }
                            break;

                        case EventType.MouseDrag:
                            if (GUIUtility.hotControl == controlID)
                            {
                                float normalized = (x + evt.delta.x) / width;
                                float log = Mathf.Lerp(MinFrequencyLogValue, MaxFrequencyLogValue, normalized);
                                float freq = Mathf.Pow(10, log);
                                if (evt.control)
                                {
                                    freq = Mathf.Ceil(freq);
                                }
                                freqProp.floatValue = Mathf.Clamp(freq, GetLastFrequency(i), GetMaxFrequencyOfThisRange(i));
                                evt.Use();
                            }
                            break;
                    }

                    if (_bandsList.index == i && evt.type == EventType.Repaint)
                    {
                        Rect indicatorRect = new Rect(handleRect.center.x, handleRect.y + 3f, 13f, 13f);
                        indicatorRect.x -= Mathf.Floor(indicatorRect.width * 0.5f);
                        GUI.DrawTexture(indicatorRect, _indicator.image);
                    }

                    if(i < _bandsList.index)
                    {
                        previousBandOfSelectedX = lineRect.x;
                    }

                    if(i == _bandsList.index)
                    {
                        selectedBandX = lineRect.x;
                    }
                    lastBandX = lineRect.x;
                }
            }
        }

        private void DrawAmp(Rect rect, int index)
        {
            if(_bands == null || index >= _bands.Count || !Application.isPlaying)
            {
                return;
            }
            float height = Mathf.Min(_bands[index].Amplitube * 10f * SpectrumViewHeight, SpectrumViewHeight);
            EditorGUI.DrawRect(rect.GrowUp(height), BroEditorUtility.EditorSetting.GetSpectrumColor(index));
        }

        private void DrawGradientRect(Rect viewRect ,float x, float xMax)
        {
            if(_bandsList.index >= 0)
            {
                Rect rect = new Rect(viewRect) { x = x + 1f, xMax = xMax };
                Color color = BroEditorUtility.EditorSetting.GetSpectrumColor(_bandsList.index);
                AudioCurveRendering.DrawGradientRect(rect, color, color.SetAlpha(0f), 0.5f, false);
            }
        }

        private Rect DrawReferenceView(float width, Rect viewRect)
        {
            Color refColor = Color.gray;
            refColor.a = 0.2f;
            _referenceFrequencies ??= GetReferenceLogFrequencies();
            for (int i = 0; i < _referenceFrequencies.Count; i++)
            {
                float x = width * Mathf.InverseLerp(MinFrequencyLogValue, MaxFrequencyLogValue, _referenceFrequencies[i].logFreq);
                Rect lineRect = new Rect(viewRect.x + x, viewRect.y + 1f, 1f, SpectrumViewHeight - 2f);
                EditorGUI.DrawRect(lineRect, refColor);

                if(HasLabel(_referenceFrequencies[i].freq, out string label))
                {
                    Rect labelRect = new Rect(viewRect.x + x - (SpectrumViewLabelWidth * 0.5f) , viewRect.y + SpectrumViewHeight, SpectrumViewLabelWidth, SpectrumViewLabelHeight);
                    EditorGUI.LabelField(labelRect, label, EditorStyles.centeredGreyMiniLabel);
                }
            }

            for (int i = 1; i < AmplitubeScaleCount; i++)
            {
                float y = SpectrumViewHeight / AmplitubeScaleCount * i;
                Rect lineRect = new Rect(viewRect.x, viewRect.y + y, width, 1f);
                EditorGUI.DrawRect(lineRect, refColor);
            }

            return viewRect;
        }

        float GetLastFrequency(int index)
        {
            if (index > 0)
            {
                var last = _bandsList.serializedProperty.GetArrayElementAtIndex(index - 1);
                return last.FindPropertyRelative(nameof(Band.Frequency)).floatValue;
            }
            return MinFrequency;
        }

        float GetMaxFrequencyOfThisRange(int index)
        {
            if (index < _bandsList.count - 1)
            {
                var next = _bandsList.serializedProperty.GetArrayElementAtIndex(index + 1);
                return next.FindPropertyRelative(nameof(Band.Frequency)).floatValue - 1f;
            }
            return MaxFrequency;
        }

        private List<(float, float)> GetReferenceLogFrequencies()
        {
            List<(float, float)> result = new List<(float, float)>();
            float log = 10;
            float refFreq = MinFrequency + log;
            while (refFreq < MaxFrequency)
            {
                result.Add((refFreq, Mathf.Log10(refFreq)));

                refFreq += log;
                if (refFreq / log >= 10)
                {
                    log *= 10;
                }
            }
            return result;
        }

        private bool HasLabel(float freq, out string label)
        {
            label = string.Empty;
            foreach (var value in _referenceLabels)
            {
                if(Mathf.Approximately(value, freq))
                {
                    label = freq >= 1000 ? $"{(freq / 1000)}k" : freq.ToString();
                    return true;
                }
            }
            return false;
        }

        private void OnUpdateSpectrum(IReadOnlyList<Band> list)
        {
            Repaint();
        }
    } 
}