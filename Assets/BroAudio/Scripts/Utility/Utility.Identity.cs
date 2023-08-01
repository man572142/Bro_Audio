using System;
using static MiProduction.Extension.LoopExtension;
using static MiProduction.BroAudio.BroLog;
using MiProduction.BroAudio.Data;

namespace MiProduction.BroAudio
{
	public static partial class Utility
	{
		public enum ValidationErrorCode
		{
			NoError,
			IsNullOrEmpty,
			StartWithNumber,
			ContainsInvalidWord,
		}

		// �̫�@��enum = ALL�[1�A�k���@��
		public static readonly int LastAudioType = ((int)BroAudioType.All + 1) >> 1;
		public const int IdMultiplier = 100; // �Ψ�1000�|�W�Xint�W���A�Y���ݭn�h�������long

		public const BroAudioType PersistentType = BroAudioType.Music | BroAudioType.Ambience;
		public const BroAudioType OneShotType = BroAudioType.SFX | BroAudioType.UI | BroAudioType.VoiceOver;


		public static int ToConstantID(this BroAudioType audioType)
		{
			if (audioType == BroAudioType.None)
			{
				return 0;
			}
			else if (audioType == BroAudioType.All)
			{
				return int.MaxValue;
			}

			// Faster than Math.Log2 ()
			int result = 1;
			int type = (int)audioType;

			While(_ => (type >> 1) > 0, () => 
			{
				type = type >> 1;
				result *= IdMultiplier;

				return Statement.Continue;
			});
			return result;
		}

		public static BroAudioType ToNext(this BroAudioType current)
		{
			if(current == 0)
			{
				return current + 1;
			}

			int next = (int)current << 1;
			if(next > LastAudioType)
			{
				return BroAudioType.All;
			}
			return (BroAudioType)next;
		}

		public static BroAudioType GetAudioType(int id)
		{
			BroAudioType resultType = BroAudioType.None;
			BroAudioType nextType = resultType.ToNext();
			// ���^�@��While�H��֮į�}�P 
			While(_ => nextType <= (BroAudioType)LastAudioType, () =>
			{
				if (id >= resultType.ToConstantID() && id < nextType.ToConstantID())
				{
					return Statement.Break;
				}
				resultType = nextType;
				nextType = nextType.ToNext();

				return Statement.Continue;
			});
			return resultType;
		}
		
		/// <summary>
		/// �C���j��Hcallback�^��AudioType
		/// </summary>
		public static void ForeachAudioType(Action<BroAudioType> loopCallback)
		{
			BroAudioType currentType = BroAudioType.None;
			While(_ => currentType <= (BroAudioType)LastAudioType, () =>
			{
				loopCallback?.Invoke(currentType);
				currentType = currentType.ToNext();
				return Statement.Continue;
			});
		}

		public static bool IsInvalidName(string name,out ValidationErrorCode errorCode)
		{
			if (String.IsNullOrWhiteSpace(name))
			{
				errorCode = ValidationErrorCode.IsNullOrEmpty;
				return true;
			}

			if(Char.IsNumber(name[0]))
			{
				errorCode = ValidationErrorCode.StartWithNumber;
				return true;
			}

			foreach (char word in name)
			{
				if (!Char.IsNumber(word) && word != '_' && !IsEnglishLetter(word))
				{
					errorCode = ValidationErrorCode.ContainsInvalidWord;
					return true;
				}
			}
			errorCode = ValidationErrorCode.NoError;
			return false;
		}

		public static bool IsEnglishLetter(char word)
		{
			return (word >= 65 && word <= 90) || (word >= 97 && word <= 122);
		}

		public static bool Validate(string name, BroAudioClip[] clips, int id)
		{
			if (id <= 0)
			{
				LogWarning($"There is a missing or unassigned AudioID.");
				return false;
			}

			if(clips == null || clips.Length == 0)
			{
				LogError($"{name} has no audio clips, please assign or delete the library.");
				return false;
			}

			for(int i = 0; i < clips.Length;i++)
			{
				var clipData = clips[i];
				if (clipData.AudioClip == null)
				{
					LogError($"Audio clip has not been assigned! please check {name} in Library Manager.");
					return false;
				}
				float controlLength = (clipData.FadeIn > 0f ? clipData.FadeIn : 0f) + (clipData.FadeOut > 0f ? clipData.FadeOut : 0f) + clipData.StartPosition;
				if (controlLength > clipData.AudioClip.length)
				{
					LogError($"Time control value should not greater than clip's length! please check clips element:{i} in {name}.");
					return false;
				}
			}
			return true;
		}

		public static Type GetAssetType(this BroAudioType audioType)
		{
			switch (audioType)
			{
				case BroAudioType.Music:
					return typeof(MusicLibraryAsset);
				case BroAudioType.UI:
					return typeof(UISoundLibraryAsset);
				case BroAudioType.Ambience:
					return typeof(AmbienceLibraryAsset);
				case BroAudioType.SFX:
					return typeof(SfxLibraryAsset);
				case BroAudioType.VoiceOver:
					return typeof(VoiceOverLibraryAsset);
				default:
					return null;
			}
		}
	}
}
