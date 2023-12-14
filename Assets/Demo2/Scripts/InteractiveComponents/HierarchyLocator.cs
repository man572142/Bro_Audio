using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

#if UNITY_EDITOR
namespace Ami.BroAudio.Demo
{
	[RequireComponent(typeof(InteractiveZone))]
	public class HierarchyLocator : MonoBehaviour
	{
		[SerializeField] InteractiveZone _interactiveZone = null;
		[SerializeField] GameObject _target = null;

		private void Update()
		{
			if(_interactiveZone.IsInZone && Input.GetKeyDown(KeyCode.Tab))
			{
				Selection.activeObject = _target;
				EditorGUIUtility.PingObject(_target);
			}
		}
	}
} 
#endif