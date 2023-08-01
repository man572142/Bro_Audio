using UnityEngine.Audio;
using UnityEngine;
using MiProduction.Extension;
using System.Collections.Generic;

namespace MiProduction.BroAudio.Runtime
{
	public class AudioPlayerObjectPool : ObjectPool<AudioPlayer>
	{
		private ObjectPool<AudioMixerGroup> _audioTrackPool = null;
		private Transform _parent = null;
		private List<AudioPlayer> _inUsePlayers = null;

		public AudioPlayerObjectPool(AudioPlayer baseObject, Transform parent, int maxInternalPoolSize,AudioMixerGroup[] audioMixerGroups) : base(baseObject, maxInternalPoolSize)
		{
			_audioTrackPool = new AudioTrackObjectPool(audioMixerGroups);
			_parent = parent;
		}

		public override AudioPlayer Extract()
		{
			AudioPlayer player = base.Extract();
			player.SetMixer(SoundManager.Instance.Mixer);
			player.AudioTrack = _audioTrackPool.Extract();

			_inUsePlayers ??= new List<AudioPlayer>();
			_inUsePlayers.Add(player);
			return player;
		}

		public override void Recycle(AudioPlayer player)
		{
			RemoveFromInUse(player);
			_audioTrackPool.Recycle(player.AudioTrack);
			player.AudioTrack = null;
			base.Recycle(player);
		}

		protected override AudioPlayer CreateObject()
		{
			AudioPlayer newPlayer = GameObject.Instantiate(BaseObject, _parent);
			newPlayer.OnRecycle += Recycle;
			return newPlayer;
		}

		protected override void DestroyObject(AudioPlayer instance)
		{
			GameObject.Destroy(instance.gameObject);
		}

		private void RemoveFromInUse(AudioPlayer player)
		{
			for(int i = _inUsePlayers.Count - 1; i >=0; i--)
			{
				if(_inUsePlayers[i] == player)
				{
					_inUsePlayers.RemoveAt(i);
				}
			}
		}

		public IEnumerable<AudioPlayer> GetInUseAudioPlayers()
		{
			if(_inUsePlayers != null)
			{
				foreach (var player in _inUsePlayers)
				{
					yield return player;
				}
			}	
		}
	}
}