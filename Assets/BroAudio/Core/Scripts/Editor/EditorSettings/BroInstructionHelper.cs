using System.Collections;
using System.Collections.Generic;
using Ami.BroAudio.Editor.Setting;
using UnityEngine;
using static Ami.BroAudio.Tools.BroName;

namespace Ami.BroAudio.Editor
{
	public class BroInstructionHelper
	{
		public const string InstructionMissingText = "BroAudio's instruction file is missing. " +
			"Please reimport the " + InstructionFileName + ".asset file from the package to any Resources/Editor folder";
		public const string MissingText = "??????????";

		private BroInstruction _instruction = null;
		private BroInstruction Instruction
		{
			get
			{
				if(_instruction == null)
				{
					_instruction = Resources.Load<BroInstruction>(EditorResourcePath + InstructionFileName);
					if (_instruction == null)
					{
						Debug.LogWarning(Utility.LogTitle + InstructionMissingText);
					}
				}
				return _instruction;
			}
		}

		public Object DemoScene => Instruction?.DemoScene;

		public string GetText(Instruction instruction)
		{
            if (Instruction && Instruction.Dictionary.TryGetValue(instruction, out string text))
			{
				return text;
			}

			return MissingText;
		}
	}
}