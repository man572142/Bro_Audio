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
        private UnityEditor.Editor _editor = null;
        private Type _clipInspectorClass = null;

        public void RenderClipWaveform(Rect rect,AudioClip clip)
        {
            _clipInspectorClass = _clipInspectorClass ?? AudioClassReflectionHelper.GetUnityEditorClass("AudioClipInspector");
            string assetPath = AssetDatabase.GetAssetPath(clip);
            AudioImporter importer = AssetImporter.GetAtPath(assetPath) as AudioImporter;
            UnityEditor.Editor.CreateCachedEditor(clip, _clipInspectorClass, ref _editor);
            
            // todo: should be cached to improve the performance ? but it seem not that inefficient
            ReflectionExtension.ExecuteMethod("DoRenderPreview", new object[] { true, clip, importer, rect, 1f }, _clipInspectorClass, _editor, ReflectionExtension.PrivateFlag);
        }
    }
}
