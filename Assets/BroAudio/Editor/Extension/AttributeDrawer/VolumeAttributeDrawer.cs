using UnityEngine;
using UnityEditor;

namespace Ami.BroAudio.Editor
{
	[CustomPropertyDrawer(typeof(Volume))]
	public class VolumeAttributeDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			if(property.propertyType == SerializedPropertyType.Float && attribute is Volume volAttr)
			{
                property.floatValue = BroEditorUtility.DrawVolumeSlider(position, label, property.floatValue, volAttr.CanBoost, property.depth > 1);
            }
			else
			{
				EditorGUI.PropertyField(position, property, label);
			}
		}
	} 
}