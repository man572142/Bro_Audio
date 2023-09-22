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
        BroVirtual,

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
            switch (instruction)
            {
                case Instruction.HaasEffectTooltip:
                    return _instruction.HaasEffectTooltipText;

                case Instruction.RuntimeSettingFileMissing:
                    return _instruction.SettingFileMissingText;

                case Instruction.TracksAndVoicesNotMatchWarning:
                    return _instruction.TracksAndVoicesNotMatchWarning;

                case Instruction.AddTracksConfirmationDialog:
                    return _instruction.AddTracksConfirmationDialog;

                case Instruction.AssetOutputPathPanelTtile:
                    return _instruction.AssetOutputPathPanelTtile;

                case Instruction.ClipEditorConfirmationDialog:
                    return _instruction.ClipEditorConfirmationDialog;

                case Instruction.Copyright:
                    return _instruction.Copyright;

                case Instruction.AudioVoicesToolTip:
                    return _instruction.AudioVoicesToolTip;

                case Instruction.BroVirtual:
                    return _instruction.BroVirtualToolTip;

                case Instruction.LibraryState_IsNullOrEmpty:
                    return _instruction.LibraryState_IsNullOrEmpty;

                case Instruction.LibraryState_IsDuplicated:
                    return _instruction.LibraryState_IsDuplicated;

                case Instruction.LibraryState_ContainsInvalidWords:
                    return _instruction.LibraryState_ContainsInvalidWords;

                case Instruction.LibraryState_Fine:
                    return _instruction.LibraryState_Fine;

                case Instruction.AssetNaming_IsNullOrEmpty:
                    return _instruction.AssetNaming_IsNullOrEmpty;

                case Instruction.AssetNaming_ContainsWhiteSpace:
                    return _instruction.AssetNaming_ContainsWhiteSpace;

                case Instruction.AssetNaming_IsDuplicated:
                    return _instruction.AssetNaming_IsDuplicated;

                case Instruction.AssetNaming_ContainsInvalidWords:
                    return _instruction.AssetNaming_ContainsInvalidWords;

                case Instruction.AssetNaming_StartWithNumber:
                    return _instruction.AssetNaming_StartWithNumber;
                
                default:
                    return MissingText;
            }
        }
    }
}