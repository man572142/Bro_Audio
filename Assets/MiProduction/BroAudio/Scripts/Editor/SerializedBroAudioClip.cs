using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using MiProduction.Extension;
using System;
using System.IO;

namespace MiProduction.BroAudio.Asset.Core
{
    public class SerializedBroAudioClip : BroAudioClip, IChangesTrackable
    {
        private SerializedProperty _clipProp;

        public int ChangedID { get; set; }


        public SerializedBroAudioClip(SerializedProperty clipProp)
        {
            _clipProp = clipProp;
            SetTransportValue();
        }

        public static void ResetAllSerializedProperties(SerializedProperty property)
        {
            property.FindPropertyRelative(nameof(OriginAudioClip)).objectReferenceValue = null;
            property.FindPropertyRelative(nameof(EditedAudioClip)).objectReferenceValue = null;
            property.FindPropertyRelative(nameof(Volume)).floatValue = 1f;
            property.FindPropertyRelative(nameof(StartPosition)).floatValue = 0f;
            property.FindPropertyRelative(nameof(EndPosition)).floatValue = 0f;
            property.FindPropertyRelative(nameof(FadeIn)).floatValue = 0f;
            property.FindPropertyRelative(nameof(FadeOut)).floatValue = 0f;

            property.FindPropertyRelative(nameof(Weight)).intValue = 0;
        }

        public static bool IsDifferent(SerializedBroAudioClip x, SerializedBroAudioClip y)
        {
            Debug.Log($"x start:{x.StartPosition} y start:{y.StartPosition}");
            return
                x.StartPosition != y.StartPosition ||
                x.EndPosition != y.EndPosition ||
                x.FadeIn != y.FadeIn ||
                x.FadeOut != y.FadeOut;
        }

        // Only set values while Constructing and CommittingChanges
        public void SetTransportValue()
        {
            StartPosition = _clipProp.FindPropertyRelative(nameof(StartPosition)).floatValue;
            EndPosition = _clipProp.FindPropertyRelative(nameof(EndPosition)).floatValue;
            FadeIn = _clipProp.FindPropertyRelative(nameof(FadeIn)).floatValue;
            FadeOut = _clipProp.FindPropertyRelative(nameof(FadeOut)).floatValue;
        }

        public bool NeedsToBeTrimmed() => StartPosition != 0f || EndPosition != 0f;
        

        public void CommitChanges()
        {
            // Create New Audio Clip
            AudioClip clip = _clipProp.FindPropertyRelative(nameof(OriginAudioClip)).objectReferenceValue as AudioClip;
            if(NeedsToBeTrimmed())
            {
                string clipName = $"~{clip.name}_s{StartPosition}_e{EndPosition}";
                clip = clip.Trim(StartPosition, EndPosition, clipName);
            }

            SavWav.Save(Utility.EditedClipsPath, clip);
            _clipProp.FindPropertyRelative(nameof(EditedAudioClip)).objectReferenceValue = clip;
            SetTransportValue();
        }

        public bool IsDirty()
        {
            return IsDifferent(this, new SerializedBroAudioClip(_clipProp));
        }

        public void DiscardChanges()
        {
            _clipProp.FindPropertyRelative(nameof(StartPosition)).floatValue = StartPosition;
            _clipProp.FindPropertyRelative(nameof(EndPosition)).floatValue = EndPosition;
            _clipProp.FindPropertyRelative(nameof(FadeIn)).floatValue = FadeIn;
            _clipProp.FindPropertyRelative(nameof(FadeOut)).floatValue = FadeOut;
        }

        
    }

}