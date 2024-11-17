using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;
using static Ami.BroAudio.Editor.PlaybackGroupEditor;

namespace Ami.BroAudio.Editor
{
    public static class PlaybackRuleValueDrawer
    {
        private static IEnumerable<(Type, string)> GetUnityDrawers()
        {
            yield return (typeof(RangeAttribute), "RangeDrawer");
            yield return (typeof(MinAttribute), "MinDrawer");
            yield return (typeof(DelayedAttribute), "DelayedDrawer");
            yield return (typeof(MultilineAttribute), "MultilineDrawer");
            yield return (typeof(TextAreaAttribute), "TextAreaDrawer");
            yield return (typeof(ColorUsageAttribute), "ColorUsageDrawer");
            yield return (typeof(GradientUsageAttribute), "GradientUsageDrawer");
        }

        private static IEnumerable<(Type,Type)> GetBroDrawers()
        {
            yield return (typeof(Volume), typeof(VolumeAttributeDrawer));
            yield return (typeof(Frequency),typeof(FrequencyAttributeDrawer));
            yield return (typeof(Pitch), typeof(PitchAttributeDrawer));
        }

        public static void DrawValue(SerializedProperty property, AttributesContainer attrContainer)
        {
            if (TryGetBroAttributeDrawer(property, attrContainer, out var drawer) ||
                TryGetUnityAttributeDrawer(property, attrContainer, out drawer))
            {
                Rect rect = EditorGUILayout.GetControlRect(false, drawer.GetPropertyHeight(property, GUIContent.none));
                drawer.OnGUI(rect, property, GUIContent.none);
            }
            else
            {
                Rect rect = EditorGUILayout.GetControlRect(false, EditorGUI.GetPropertyHeight(property));
                EditorGUI.PropertyField(rect, property, GUIContent.none);
            }
        }

        private static bool TryGetBroAttributeDrawer(SerializedProperty property, AttributesContainer attrContainer, out PropertyDrawer drawer)
        {
            drawer = null;
            foreach (var (attrType, drawerType) in GetBroDrawers())
            {
                if (attrContainer.TryGet(attrType, out var attribute) &&
                    TryGetDrawer(drawerType, attribute, out drawer))
                {
                    return drawer != null;
                }
            }
            return false;
        }

        private static bool TryGetUnityAttributeDrawer(SerializedProperty property, AttributesContainer attrContainer, out PropertyDrawer drawer)
        {
            drawer = null;
            foreach (var (type, drawerName) in GetUnityDrawers())
            {
                if (attrContainer.TryGet(type, out var attribute) &&
                    TryGetDrawer(drawerName, attribute, out drawer))
                {
                    return drawer != null;
                }
            }
            return false;
        }

        private static bool TryGetDrawer<T>(string drawerName, PropertyAttribute attribute, out T drawer) where T : GUIDrawer
        {
            Type drawerType = typeof(T).Assembly?.GetType($"UnityEditor.{drawerName}");
            return TryGetDrawer(drawerType, attribute, out drawer);
        }

        private static bool TryGetDrawer<T>(Type drawerType, PropertyAttribute attribute, out T drawer) where T : GUIDrawer
        {
            drawer = null;
            var attributeField = drawerType?.GetField("m_Attribute", BindingFlags.NonPublic | BindingFlags.Instance);
            if (drawerType != null && attributeField != null)
            {
                drawer = Activator.CreateInstance(drawerType) as T;
                if (drawer != null)
                {
                    attributeField.SetValue(drawer, attribute);
                    return true;
                }
            }
            return false;
        }
    }
}