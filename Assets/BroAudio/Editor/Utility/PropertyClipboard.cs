using UnityEditor;
using UnityEngine;
using System;

namespace Ami.BroAudio.Editor
{
    public static class PropertyClipboard
    {
        private interface IClipboardHandler
        {
            void CopyToClipboard();
            void PasteFromClipboard();
        }
        
        private class Data<TTarget, TValue> : IClipboardHandler where TValue : IPropertyClipboardData
        {
            private readonly TTarget _target;
            private readonly TValue _value;
            private readonly Action<TTarget, TValue> _onPaste;

            public Data(TTarget target, TValue value, Action<TTarget, TValue> onPaste)
            {
                _target = target;
                _value = value;
                _onPaste = onPaste;
            }

            public void CopyToClipboard()
            {
                EditorGUIUtility.systemCopyBuffer = JsonUtility.ToJson(_value);
            }

            public void PasteFromClipboard()
            {
                _onPaste?.Invoke(_target, JsonUtility.FromJson<TValue>(EditorGUIUtility.systemCopyBuffer));
            }

            public bool CanPaste()
            {
                try
                {
                    var copied = JsonUtility.FromJson<TValue>(EditorGUIUtility.systemCopyBuffer);
                    return copied.Type == _value.Type;
                }
                catch (ArgumentException)
                {
                    return false;
                }
            }
        }
        
        public static void HandleClipboardContextMenu<TTarget, TValue>(Rect rect, TTarget target, TValue value, Action<TTarget, TValue> onPaste)
            where TValue : IPropertyClipboardData
        {
            var evt = Event.current;
            if (evt.type != EventType.ContextClick || !rect.Contains(evt.mousePosition))
            {
                return;
            }

            var data = new Data<TTarget,TValue>(target, value, onPaste);
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Copy"), false, OnCopyValues, data);
            if (data.CanPaste())
            {
                menu.AddItem(new GUIContent("Paste"), false, OnPasteValues, data);
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Paste"));
            }

            menu.ShowAsContext();
        }

        private static void OnCopyValues(object userData)
        {
            if (userData is IClipboardHandler data)
            {
                data.CopyToClipboard();
            }
        }

        private static void OnPasteValues(object userData)
        {
            if (userData is IClipboardHandler data)
            {
                data.PasteFromClipboard();
            }
        }
    }
}
