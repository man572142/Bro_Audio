using System.Collections;
using System.Collections.Generic;
using Ami.BroAudio.Editor.Setting;
using UnityEngine;
using static Ami.BroAudio.Tools.BroName;
using static Ami.BroAudio.Tools.BroLog;

namespace Ami.BroAudio.Editor
{
	public enum Instruction
	{
        // Settings
        RuntimeSettingFileMissing,
        EditorSettingFileMissing,
        AssetOutputPathPanelTtile,

		// Settings/Audio
        HaasEffectTooltip,
		TracksAndVoicesNotMatchWarning,
		AddTracksConfirmationDialog,
        AudioVoicesToolTip,
        BroVirtualToolTip,

        // Settings/Info
        Copyright,

		// Clip Editor
        ClipEditorConfirmationDialog,

        // LibraryState
        LibraryState_IsNullOrEmpty,
        LibraryState_IsDuplicated,
        LibraryState_ContainsInvalidWords,
        LibraryState_Fine,

        // Asset Naming
        AssetNaming_IsNullOrEmpty,
        AssetNaming_ContainsWhiteSpace,
        AssetNaming_IsDuplicated,
        AssetNaming_ContainsInvalidWords,
        AssetNaming_StartWithNumber
    }

    public class BroInstructionHelper
	{
		public const string InstructionMissingText = "BroAudio's instruction file is missing. " +
			"Please reimport the " + InstructionFileName + ".asset file from the package to any Resources/Editor folder";
		public const string MissingText = "??????????";

		private BroInstruction _instruction = null;

		public void Init()
		{
            if(!_instruction)
            {
                _instruction = Resources.Load<BroInstruction>("Editor/" + InstructionFileName);
            }
			
			if (!_instruction)
			{
				LogWarning(InstructionMissingText);
			}
		}

		public string GetText(Instruction instruction)
		{
			if (!_instruction)
			{
				return MissingText;
			}

			string instructionText = GetInstruction(instruction);
			return instructionText;
		}

		private string GetInstruction(Instruction instruction)
		{
            if(_instruction.Dictionary.TryGetValue(instruction,out string text))
			{
                return text;
			}
            return string.Empty;
        }
    }
}