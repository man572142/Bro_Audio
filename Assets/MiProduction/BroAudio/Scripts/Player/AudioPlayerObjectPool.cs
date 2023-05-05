using UnityEngine.Audio;
using UnityEngine;
using MiProduction.Extension;

namespace MiProduction.BroAudio.Core
{
	public class AudioPlayerObjectPool : ObjectPool<AudioPlayer>
	{
		private ObjectPool<AudioMixerGroup> _audioTrackPool = null;
		private Transform _parent = null;

		public AudioPlayerObjectPool(AudioPlayer baseObject, Transform parent, int maxInternalPoolSize,AudioMixerGroup[] audioMixerGroups) : base(baseObject, maxInternalPoolSize)
		{
			_audioTrackPool = new AudioTrackObjectPool(audioMixerGroups);
			_parent = parent;
		}

		public override AudioPlayer Extract()
		{
			AudioPlayer player = base.Extract();
			player.AudioTrack = _audioTrackPool.Extract();
			return player;
		}

		public override void Recycle(AudioPlayer player)
		{
			_audioTrackPool.Recycle(player.AudioTrack);
			base.Recycle(player);
		}

		protected override AudioPlayer CreateObject()
		{
			AudioPlayer newPlayer = GameObject.Instantiate(BaseObject, _parent);
			newPlayer.AudioTrack = _audioTrackPool.Extract();
			newPlayer.OnRecycle += Recycle;
			return newPlayer;
		}

		protected override void DestroyObject(AudioPlayer instance)
		{
			GameObject.Destroy(instance);
		}
	}
}