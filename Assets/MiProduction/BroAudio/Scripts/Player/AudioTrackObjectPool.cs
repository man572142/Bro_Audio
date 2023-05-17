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
				Utility.LogError("Audio voices is not enough !");
				return null;
			}

			AudioMixerGroup audioMixerGroup = _audioMixerGroups[_usedTrackCount];
			_usedTrackCount++;
			return audioMixerGroup;
		}

		protected override void DestroyObject(AudioMixerGroup track)
		{
			// TODO: ???
		}
	}
}