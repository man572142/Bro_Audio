using MiProduction.Extension;
using UnityEngine.Audio;

namespace MiProduction.BroAudio.Runtime
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
				Utility.LogWarning("You have reached the limit of audio voices count. The sound may not be audible until another voices become available!");
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