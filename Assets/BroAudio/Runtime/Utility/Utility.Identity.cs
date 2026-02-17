using System;
using System.Collections.Generic;
using Ami.BroAudio.Data;
using Ami.Extension;
using static UnityEngine.Debug;

namespace Ami.BroAudio
{
	public static partial class Utility
	{
        public const int LastAudioType = ((int)BroAudioType.All + 1) >> 1;

        [System.Obsolete("IDs are irrelevant", true)]
        public const int IDCapacity = 0x10000000; // 1000 0000 in HEX. 268,435,456 in DEC

        [System.Obsolete("IDs are irrelevant", true)]
        public static int FinalIDLimit => ((BroAudioType)LastAudioType).GetInitialID() + IDCapacity;

        [System.Obsolete("IDs are irrelevant", true)]
        public static int GetInitialID(this BroAudioType audioType)
		{
			if (audioType == BroAudioType.None)
			{
				return 0;
			}
			else if (audioType == BroAudioType.All || (int)audioType < 0)
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

        [System.Obsolete("Type is direct on the entity now", true)]
		public static BroAudioType GetAudioType(int id)
		{
			if(id >= FinalIDLimit)
			{
				return BroAudioType.None;
			}

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

		public static bool IsConcrete(this BroAudioType audioType, bool checkFlags = false)
		{
			if(audioType == BroAudioType.None || audioType == BroAudioType.All)
			{
				return false;
			}
			else if (checkFlags && FlagsExtension.GetFlagsOnCount((int)audioType) > 1)
			{
				return false;
			}
			return true;
		}

        public static void ForeachConcreteAudioType(Action<BroAudioType> loopCallback)
		{
            BroAudioType currentType = BroAudioType.None;
            while(currentType <= (BroAudioType)LastAudioType)
			{
				if(currentType.IsConcrete())
				{
                    loopCallback?.Invoke(currentType);
                }
                
                currentType = currentType.ToNext();
            }
        }

        public static void ForeachConcreteAudioType<T>(T processor) where T : IAudioTypeIterable
        {
            BroAudioType currentType = BroAudioType.None;
            while (currentType <= (BroAudioType)LastAudioType)
            {
                if (currentType.IsConcrete())
                {
                    processor.OnEachAudioType(currentType);
                }

                currentType = currentType.ToNext();
            }
        }

        public static bool Validate(string name, IReadOnlyList<IBroAudioClip> clips)
		{
			if(clips == null || clips.Count == 0)
			{
				LogWarning(LogTitle + $"{name.ToWhiteBold()} has no audio clips, please assign or delete the entity.");
				return false;
			}

			for(int i = 0; i < clips.Count;i++)
			{
				if (!clips[i].IsValid())
				{
                    LogError(LogTitle + $"Audio clip has not been assigned! please check {name.ToWhiteBold()} in Library Manager.");
                    return false;
				}
			}
			return true;
		}
	}
}
