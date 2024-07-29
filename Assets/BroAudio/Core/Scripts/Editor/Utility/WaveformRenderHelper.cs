using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Ami.Extension;
using Ami.Extension.Reflection;
using System;

namespace Ami.BroAudio.Editor
{
    public class WaveformRenderHelper
    {
        private delegate void DoRenderPreview(bool setMaterial, AudioClip clip, AudioImporter audioImporter, Rect wantedRect, float scaleFactor);

        private UnityEditor.Editor _editor = null;
        private Type _clipInspectorClass = null;
        private DoRenderPreview _doRenderPreview = null;

        public void RenderClipWaveform(Rect rect,AudioClip clip)
        {
            _clipInspectorClass ??= ClassReflectionHelper.GetUnityEditorClass("AudioClipInspector");
            string assetPath = AssetDatabase.GetAssetPath(clip);
            AudioImporter importer = AssetImporter.GetAtPath(assetPath) as AudioImporter;
            UnityEditor.Editor.CreateCachedEditor(clip, _clipInspectorClass, ref _editor);

            if(_doRenderPreview == null)
            {
                var method = _clipInspectorClass.GetMethod("DoRenderPreview", ReflectionExtension.PrivateFlag);
                _doRenderPreview = (DoRenderPreview)method.CreateDelegate(typeof(DoRenderPreview), _editor);
            }

            _doRenderPreview?.Invoke(true, clip, importer, rect, 1f);
        }
    }
}
