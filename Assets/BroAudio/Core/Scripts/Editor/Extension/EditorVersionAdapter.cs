using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Ami.Extension
{
    public static class EditorVersionAdapter
    {
        public static GUIStyle LinkLabel
        {
            get
            {
#if UNITY_2019_2_OR_NEWER
                
                return EditorStyles.linkLabel;
#else
                GUIStyle linkLabel = new GUIStyle(EditorStyles.label);
                linkLabel.normal.textColor = new Color(0.48f,0.675f,0.953f);
                return linkLabel;
#endif
            }
        }

        // In both newer and older versions, it's better to cache the result since this method isn't very efficient.
        public static bool HasOpenEditorWindow<T>() where T : UnityEditor.EditorWindow
        {
#if UNITY_2019_3_OR_NEWER
            return EditorWindow.HasOpenInstances<T>();
#else
            UnityEngine.Object[] wins = Resources.FindObjectsOfTypeAll(typeof(T));
            return wins != null && wins.Length > 0;
#endif
        }
    }
}