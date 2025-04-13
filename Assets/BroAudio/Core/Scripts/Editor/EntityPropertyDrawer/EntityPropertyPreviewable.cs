using Ami.BroAudio.Data;
using Ami.Extension;
using UnityEditor;

namespace Ami.BroAudio.Editor
{
    public class EntityPropertyPreviewable : IEditorPreviewable
    {
        private SerializedProperty _clipVolProp;

        private readonly SerializedProperty _entityProp;
        private readonly SerializedProperty _masterVolProp;
        private readonly SerializedProperty _pitchProp;

        public float Volume => _clipVolProp.floatValue * _masterVolProp.floatValue;
        public float Pitch => _pitchProp.floatValue;
        public string CurrentClipPath { get; private set; }

        public EntityPropertyPreviewable(SerializedProperty entityProp)
        {
            _entityProp = entityProp;
            _masterVolProp = entityProp.FindBackingFieldProperty(nameof(AudioEntity.MasterVolume));
            _pitchProp = entityProp.FindBackingFieldProperty(nameof(AudioEntity.Pitch));
        }

        public void StartPreview(string clipPath, out float initialVolume, out float initialPitch)
        {
            CurrentClipPath = clipPath;
            var clipProp = _entityProp.serializedObject.FindProperty(clipPath);
            _clipVolProp = clipProp.FindPropertyRelative(nameof(BroAudioClip.Volume));

            float masterVol = _masterVolProp.floatValue;
            if (HasRandom(RandomFlag.Volume))
            {
                var masterRangeProp = _entityProp.FindBackingFieldProperty(nameof(AudioEntity.VolumeRandomRange));
                masterVol = Utility.GetRandomValue(_masterVolProp.floatValue, masterRangeProp.floatValue);
            }
            initialVolume = _clipVolProp.floatValue * masterVol;

            initialPitch = _pitchProp.floatValue;
            if (HasRandom(RandomFlag.Pitch))
            {
                var pitchRangeProp = _entityProp.FindBackingFieldProperty(nameof(AudioEntity.PitchRandomRange));
                initialPitch = Utility.GetRandomValue(_pitchProp.floatValue, pitchRangeProp.floatValue);
            }
        }

        public void EndPreview()
        {
            _clipVolProp = null;
            CurrentClipPath = null;
        }

        private bool HasRandom(RandomFlag target)
        {
            var property = _entityProp.FindBackingFieldProperty(nameof(AudioEntity.RandomFlags));
            RandomFlag randomFlags = (RandomFlag)property.intValue;
            return randomFlags.Contains(target);
        }
    } 
}