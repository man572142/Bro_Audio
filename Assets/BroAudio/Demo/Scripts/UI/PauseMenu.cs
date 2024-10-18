using System;
using System.Collections;
using System.Collections.Generic;
using Ami.Extension;
using UnityEditor;
using UnityEngine;

namespace Ami.BroAudio.Demo
{
	public interface IPauseMenu
	{
		bool IsOpen { get; }
	}

	public class PauseMenu : MonoBehaviour, IPauseMenu
	{
		public static IPauseMenu Instance = null;

		[SerializeField] GameObject _ui = null;
		[SerializeField] float _fadeTime = default;
#pragma warning disable 414
        [SerializeField, Frequency] float _othersLowPasFreq = default;
#pragma warning restore 414
#if UNITY_EDITOR
        [SerializeField] GameObject _hierarchyLocateTarget = null;
#endif
		public bool IsOpen { get; private set; }

		void Start()
		{
			_ui.gameObject.SetActive(false);
			Instance = this;
		}

		private void OnDestroy()
		{
			Instance = null;
		}

		// Update is called once per frame
		void Update()
		{
			if(Input.GetKeyDown(KeyCode.Backspace))
			{
				IsOpen = !IsOpen;
				ChangeOpenState();
			}

#if UNITY_EDITOR
			if(IsOpen && Input.GetKeyDown(KeyCode.Tab))
			{
				Selection.activeObject = _hierarchyLocateTarget;
				EditorGUIUtility.PingObject(_hierarchyLocateTarget);
			}
#endif
		}

		private void ChangeOpenState()
		{
			_ui.gameObject.SetActive(IsOpen);

			if(IsOpen)
			{
#if !UNITY_WEBGL
				BroAudio.SetEffect(Effect.LowPass(_othersLowPasFreq, _fadeTime)); 
#endif
			}
			else
			{
#if !UNITY_WEBGL
				BroAudio.SetEffect(Effect.LowPass(Effect.Defaults.LowPass, _fadeTime)); 
#endif
			}
            Cursor.visible = IsOpen;
        }
	} 
}
