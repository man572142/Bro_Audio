using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Ami.Extension;
using System;
using System.Text;

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

		[SerializeField] Text _apiText = null;
#pragma warning disable 414
        [SerializeField] Text _sideNoteText = null;
		[SerializeField] bool _isWebGLSupported = true;
#pragma warning restore 414

        [Header("Please use [SetText] in the context menu")]
		[SerializeField] MethodText[] _methodTexts = null;
		

		private void Start()
		{
			if(!_apiText)
			{
				_apiText = GetComponent<Text>();
			}
			_apiText.supportRichText = true;
#if UNITY_WEBGL
			if(_sideNoteText && !_isWebGLSupported)
			{
				_sideNoteText.gameObject.SetActive(true);
				_sideNoteText.color = new Color(1, 0.3f, 0.3f);
				_sideNoteText.text = "This feature is not supported in WebGL";
			}
#endif
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
				if (data.Parameters != null && data.Parameters.Length > 0)
				{
					for (int i = 0; i < data.Parameters.Length; i++)
					{
						string para = data.Parameters[i];
						if (i > 0)
						{
							builder.Append(", ");
						}
						builder.Append(para.SetSize(20));
					}
				}
				builder.Append(")");
			}

			_apiText.text = builder.ToString();
#if UNITY_EDITOR
            if (!Application.isPlaying)
			{
				UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(_apiText);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
			}
#endif
        }

    } 
}
