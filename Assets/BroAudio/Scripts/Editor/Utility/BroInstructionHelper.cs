using System.Collections;
using System.Collections.Generic;
using Ami.BroAudio.Editor.Setting;
using UnityEngine;
using static Ami.BroAudio.BroName;

namespace Ami.BroAudio.Editor
{
	public enum Instruction
	{
		HaasEffectTooltip,
		SettingFileMissing,
		TracksAndVoicesNotMatchWarning,
		AddTracksConfirmationDialog,
		AssetOutputPathPanelTtile,
		ClipEditorConfirmationDialog,
		Copyright,
		AudioVoicesToolTip,
	}

	public class BroInstructionHelper
	{
		public const string InstructionMissingText = "BroAudio's instruction file is missing. " +
			"Please reimport the " + InstructionFileName + ".asset file from the package to any Resources/Editor folder";
		public const string MissingText = "??????????";

		private BroInstruction _instruction = null;

		public void Init()
		{
			_instruction = Resources.Load<BroInstruction>("Editor/" + InstructionFileName);
			if (!_instruction)
			{
				BroLog.LogWarning(InstructionMissingText);
			}
		}

		public string GetText(Instruction instruction)
		{
			if (!_instruction)
			{
				return MissingText;
			}

			string instructionText = GetInstruction(instruction);
			return string.IsNullOrWhiteSpace(instructionText) ? MissingText : instructionText;
		}

		private string GetInstruction(Instruction instruction)
		{
			switch (instruction)
			{
				case Instruction.HaasEffectTooltip:
					return _instruction.HaasEffectTooltipText;

				case Instruction.SettingFileMissing:
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
				default:
					return MissingText;
			}
		}
	}

}