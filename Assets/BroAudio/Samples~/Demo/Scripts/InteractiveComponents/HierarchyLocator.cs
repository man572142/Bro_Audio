using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

#if UNITY_EDITOR
namespace Ami.BroAudio.Demo
{
	public class HierarchyLocator : InteractiveComponent
	{
		[SerializeField] GameObject _target = null;

		protected override bool ListenToInteractiveZone() => false;

		private void Update()
		{
			if(!PauseMenu.Instance.IsOpen && InteractiveZone.IsInZone && Input.GetKeyDown(KeyCode.Tab))
			{
				Selection.activeObject = _target;
				EditorGUIUtility.PingObject(_target);
			}
		}
	}
} 
#endif