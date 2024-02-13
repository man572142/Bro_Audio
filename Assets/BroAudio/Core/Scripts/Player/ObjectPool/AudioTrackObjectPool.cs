using Ami.Extension;
using UnityEngine.Audio;
using static Ami.BroAudio.Tools.BroLog;

namespace Ami.BroAudio.Runtime
{
	public class AudioTrackObjectPool : ObjectPool<AudioMixerGroup>
	{
		private AudioMixerGroup[] _audioMixerGroups = null;
		private int _usedTrackCount = 0;
		public AudioTrackObjectPool(AudioMixerGroup[] audioMixerGroups) : base(null, audioMixerGroups.Length)
		{
			_audioMixerGroups = audioMixerGroups;
		}

		protected override AudioMixerGroup CreateObject()
		{
			if (_usedTrackCount >= _audioMixerGroups.Length)
			{
				LogWarning("You have reached the limit of BroAudio tracks count, which is way beyond the MaxRealVoices count. " +
					"That means the sound will be inaudible, and also uncontrollable. For more infomation, please check the documentation" );
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