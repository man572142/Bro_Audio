using System;
using static Ami.BroAudio.Tools.BroLog;
using Ami.BroAudio.Data;

namespace Ami.BroAudio
{
	public static partial class Utility
	{
        public static readonly int LastAudioType = ((int)BroAudioType.All + 1) >> 1;
		public static readonly int IDCapacity = 0x10000000; // 1000 0000 in HEX. 268,435,456 in DEC

        public static int FinalIDLimit => ((BroAudioType)LastAudioType).GetInitialID() + IDCapacity;

        public static int GetInitialID(this BroAudioType audioType)
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
			int result = 0;
			int type = (int)audioType;

			while(type > 0)
			{
                result += IDCapacity;
                type = type >> 1;
			}
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

			while(nextType <= (BroAudioType)LastAudioType)
			{
				if (id >= resultType.GetInitialID() && id < nextType.GetInitialID())
				{
					break;
				}
				resultType = nextType;
				nextType = nextType.ToNext();
			}
			return resultType;
		}

		public static void ForeachConcreteAudioType(Action<BroAudioType> loopCallback)
		{
            BroAudioType currentType = BroAudioType.None;
            while(currentType <= (BroAudioType)LastAudioType)
			{
				if(currentType != BroAudioType.None && currentType != BroAudioType.All)
				{
                    loopCallback?.Invoke(currentType);
                }
                
                currentType = currentType.ToNext();
            }
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
				LogWarning($"{name} has no audio clips, please assign or delete the library.");
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
	}
}
