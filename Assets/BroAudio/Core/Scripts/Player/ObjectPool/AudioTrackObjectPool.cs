using Ami.Extension;
using UnityEngine.Audio;
using static UnityEngine.Debug;

namespace Ami.BroAudio.Runtime
{
	public class AudioTrackObjectPool : ObjectPool<AudioMixerGroup>
	{
		private AudioMixerGroup[] _audioMixerGroups = null;
		private int _usedTrackCount = 0;
		private readonly bool _isDominator = false;
		public AudioTrackObjectPool(AudioMixerGroup[] audioMixerGroups, bool isDominator = false) : base(null, audioMixerGroups.Length)
		{
			_audioMixerGroups = audioMixerGroups;
			_isDominator = isDominator;
		}

		protected override AudioMixerGroup CreateObject()
		{
			if (_usedTrackCount >= _audioMixerGroups.Length)
			{
				if(_isDominator)
				{
                    LogWarning(Utility.LogTitle + "You have used up all the [Dominator] tracks. If you need more tracks, please click the [Add Dominator Track] button in Tool/BroAudio/Preference.");
                }
				else
				{
                    LogWarning(Utility.LogTitle + "You have reached the limit of BroAudio tracks count, which is way beyond the MaxRealVoices count. " +
                    "That means the sound will be inaudible, and also uncontrollable. For more infomation, please check the documentation");
                }
				return null;
			}

			AudioMixerGroup audioMixerGroup = _audioMixerGroups[_usedTrackCount];
			_usedTrackCount++;
			return audioMixerGroup;
		}

		protected override void DestroyObject(AudioMixerGroup track)
		{
			// max pool size equals to track count. there is no track will be destroy.
		}
	}
}