using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ami.Extension;

namespace Ami.BroAudio.Editor.Setting
{
#if BroAudio_DevOnly
	[CreateAssetMenu(menuName = nameof(BroAudio) + "/Instruction",fileName = Tools.BroName.InstructionFileName)]
#endif
	public class BroInstruction : ScriptableObject
	{
		public const bool CanEdit = false;

		[System.Serializable]
		public struct InstructionDictionary
		{
			public Instruction Key;

			[ReadOnlyTextArea(!CanEdit)]
			public string Value;
		}

		[SerializeField] private InstructionDictionary[] _dictionary = null;
		private Dictionary<Instruction, string> _actualDict = new Dictionary<Instruction, string>();

		public Dictionary<Instruction, string> Dictionary => _actualDict;


		private void OnEnable()
		{
			if (_dictionary == null || _dictionary.Length == 0)
			{
				return;
			}

			_actualDict.Clear();
			foreach(var content in _dictionary)
			{
				_actualDict.Add(content.Key, content.Value);
			}
		}
	}
}