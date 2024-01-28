using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Ami.Extension;
using System;
using System.Text;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace Ami.BroAudio.Demo
{
	public class APIText : MonoBehaviour
	{
		[Serializable]
		public struct MethodText
		{
			public string Method;
			public string[] Parameters;
		}

		public const string ClassColor = "4EC9B0";
		public const string MethodColor = "DCDCAA";

		[SerializeField] Text _component = null;

#if UNITY_EDITOR
		[Header("Please use [SetText] in the context menu")]
		[SerializeField] MethodText[] _methodTexts = null;
#endif

		private void Start()
		{
			if(!_component)
			{
				_component = GetComponent<Text>();
			}
			_component.supportRichText = true;
		}
		
		[ContextMenu("SetText")]
		public void SetAPI()
		{
			StringBuilder builder = new StringBuilder("BroAudio".SetColor(ClassColor));
			foreach (var data in _methodTexts)
			{
				builder.Append(".");
				builder.Append(data.Method.SetColor(MethodColor));
				builder.Append("(");
				if(data.Parameters != null && data.Parameters.Length > 0)
				{
					for (int i = 0; i < data.Parameters.Length;i++)
					{
						string para = data.Parameters[i];
						if(i > 0)
						{
							builder.Append(", ");
						}
						builder.Append(para.SetSize(20));
					}
				}
				builder.Append(")");
			}

			_component.text = builder.ToString();

			if(!Application.isPlaying)
			{
				UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(_component);
				EditorSceneManager.MarkSceneDirty(gameObject.scene);
			}
		}
	} 
}