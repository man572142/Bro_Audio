using System;
using UnityEngine;
using static MiProduction.Extension.LoopExtension;
using MiProduction.BroAudio.Data;
using MiProduction.BroAudio.EditorSetting;

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

		// 最後一個enum = ALL加1再右移一位
		public static readonly int LastAudioType = ((int)AudioType.All + 1) >> 1;
		public const int IdMultiplier = 100; // 用到1000會超出int上限，若有需要則必須改用long


		public static int ToConstantID(this AudioType audioType)
		{
			if (audioType == AudioType.None)
			{
				return 0;
			}
			else if (audioType == AudioType.All)
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

		public static AudioType ToNext(this AudioType current)
		{
			if(current == 0)
			{
				return current + 1;
			}

			int next = (int)current << 1;
			if(next > LastAudioType)
			{
				return AudioType.All;
			}
			return (AudioType)next;
		}

		public static AudioType GetAudioType(int id)
		{
			AudioType resultType = AudioType.None;
			AudioType nextType = resultType.ToNext();

			While(_ => nextType <= (AudioType)LastAudioType, () =>
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
		/// 每輪迴圈以callback回傳AudioType
		/// </summary>
		public static void LoopAllAudioType(Action<AudioType> loopCallback)
		{
			AudioType currentType = AudioType.None;
			While(_ => currentType <= (AudioType)LastAudioType, () =>
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

		public static bool Validate(string name, int index, BroAudioClip[] clips, int id)
		{
			if (id <= 0)
			{
				LogWarning($"There is a sound missing .please check element {index} in {name}");
				return false;
			}

			foreach (BroAudioClip clipData in clips)
			{
				if (clipData.AudioClip == null)
				{
					LogError($"Audio clip has not been assigned! please check element {index} in {name}.");
					return false;
				}
				float controlLength = (clipData.FadeIn > 0f ? clipData.FadeIn : 0f) + (clipData.FadeOut > 0f ? clipData.FadeOut : 0f) + clipData.StartPosition;
				if (controlLength > clipData.AudioClip.length)
				{
					LogError($"Time control value should not greater than clip's length! please check element {index} in {name}.");
					return false;
				}
			}
			return true;
		}

		public static Type GetAssetType(this AudioType audioType)
		{
			switch (audioType)
			{
				case AudioType.Music:
					return typeof(MusicLibraryAsset);
				case AudioType.UI:
					return typeof(UISoundLibraryAsset);
				case AudioType.Ambience:
					return typeof(AmbienceLibraryAsset);
				case AudioType.SFX:
					return typeof(SfxLibraryAsset);
				case AudioType.VoiceOver:
					return typeof(VoiceOverLibraryAsset);
				default:
					return null;
			}
		}

		public static Color GetAudioTypeColor(AudioType audioType)
		{
			switch (audioType)
			{
				case AudioType.Music:
					return BroAudioGUISetting.DarkBlue;
				case AudioType.UI:
					return BroAudioGUISetting.GrassGreen;
				case AudioType.Ambience:
					return BroAudioGUISetting.LakeGreen;
				case AudioType.SFX:
					return BroAudioGUISetting.SoftRed;
				case AudioType.VoiceOver:
					return BroAudioGUISetting.Bronze;
				default:
					return Color.white;
			}
		}
	}
}
