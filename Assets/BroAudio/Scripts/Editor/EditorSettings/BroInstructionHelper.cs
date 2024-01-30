using System.Collections;
using System.Collections.Generic;
using Ami.BroAudio.Editor.Setting;
using UnityEngine;
using static Ami.BroAudio.Tools.BroName;
using static Ami.BroAudio.Tools.BroLog;

namespace Ami.BroAudio.Editor
{
	public class BroInstructionHelper
	{
		public const string InstructionMissingText = "BroAudio's instruction file is missing. " +
			"Please reimport the " + InstructionFileName + ".asset file from the package to any Resources/Editor folder";
		public const string MissingText = "??????????";

		private BroInstruction _instruction = null;

		public string GetText(Instruction instruction)
		{
            if (!_instruction)
            {
                _instruction = Resources.Load<BroInstruction>(EditorResourcePath + InstructionFileName);

                if (!_instruction)
                {
                    LogWarning(InstructionMissingText);
                }
            }

            if (_instruction && _instruction.Dictionary.TryGetValue(instruction, out string text))
			{
				return text;
			}

			return MissingText;
		}
	}
}