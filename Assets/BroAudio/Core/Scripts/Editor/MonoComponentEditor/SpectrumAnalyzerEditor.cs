using UnityEditorInternal;
using UnityEditor;
using UnityEngine;
using Ami.Extension;
using static Ami.BroAudio.SpectrumAnalyzer;
using static Ami.Extension.EditorScriptingExtension;
using static Ami.Extension.AudioConstant;

namespace Ami.BroAudio.Editor
{
    [CustomEditor(typeof(SpectrumAnalyzer))]
    public class SpectrumAnalyzerEditor : UnityEditor.Editor
    {
        public const int MinResolutionScale = 6;
        public const int MaxResolutionScale = 13;
        public const int BandElementLineCount = 3;

        private SerializedProperty _soundSourceProp, _resolutionProp, _updateRateProp, _scaleProp, _falldownProp,
            _channelProp, _windowProp;

        ReorderableList _bandsList = null;

        private int[] _resolutionValues = new int[MaxResolutionScale - MinResolutionScale + 1];
        private string[] _resolutionLabels = new string[MaxResolutionScale - MinResolutionScale + 1];
        private Rect[] _bandRects = new Rect[BandElementLineCount];
        private float[] _bandRectRatios = { 0.34f, 0.33f, 0.33f };

        private void OnEnable()
        {
            var so = serializedObject;
            _soundSourceProp = so.FindProperty(NameOf.SoundSource);
            _resolutionProp = so.FindProperty(NameOf.ResolutionScale);
            _updateRateProp = so.FindProperty(NameOf.UpdateRate);
            _scaleProp = so.FindProperty(NameOf.Scale);
            _falldownProp = so.FindProperty(NameOf.FalldownSpeed);
            _channelProp = so.FindProperty(NameOf.Channel);
            _windowProp = so.FindProperty(NameOf.WindowType);

            for(int i = 0; i < _resolutionValues.Length; i++)
            {
                int scale = MinResolutionScale + i;
                _resolutionValues[i] = scale;
                _resolutionLabels[i] = $"{1 << scale} samples";
            }

            _bandsList = new ReorderableList(serializedObject, so.FindProperty(NameOf.Bands))
            {
                elementHeight = (EditorGUIUtility.singleLineHeight + 1f) * BandElementLineCount + ReorderableList.Defaults.padding,
                drawHeaderCallback = OnDrawBandListHeader,
                drawElementCallback = OnDrawBandElement,
            };
        }

        private void OnDrawBandListHeader(Rect rect)
        {
            EditorGUI.LabelField(rect, "Bands");
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

            freqProp.floatValue = 
                DrawLogarithmicSlider_Horizontal(_bandRects[1], freqProp.floatValue, MinFrequency, MaxFrequency, true, false);
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

            EditorGUILayout.PropertyField(_soundSourceProp);
            EditorGUILayout.Space();

            _resolutionProp.intValue =
                EditorGUILayout.IntPopup("Resolution", _resolutionProp.intValue, _resolutionLabels, _resolutionValues);

            EditorGUILayout.PropertyField(_updateRateProp);
            EditorGUILayout.PropertyField(_scaleProp);
            EditorGUILayout.PropertyField(_falldownProp);
            EditorGUILayout.PropertyField(_channelProp);
            EditorGUILayout.PropertyField(_windowProp);
            _bandsList.DoLayoutList();

            serializedObject.ApplyModifiedProperties();
        }
    } 
}