using UnityEngine.Audio;
using UnityEngine;
using Ami.Extension;
using System.Collections.Generic;
using System;

namespace Ami.BroAudio.Runtime
{
	public class AudioPlayerObjectPool : ObjectPool<AudioPlayer>
	{
		private Transform _parent = null;
		private List<AudioPlayer> _currentPlayers = new List<AudioPlayer>();
		private IAudioMixer _mixer = null;
        private Action<AudioPlayer> _cachedRecycleDelegate;

		public AudioPlayerObjectPool(AudioPlayer baseObject, Transform parent, int maxInternalPoolSize, IAudioMixer mixer) : base(baseObject, maxInternalPoolSize)
		{
			_parent = parent;
			_mixer = mixer;
            _cachedRecycleDelegate = Recycle;
        }

		public override AudioPlayer Extract()
		{
			AudioPlayer player = base.Extract();
			player.gameObject.SetActive(true);
#if !UNITY_WEBGL
			player.SetMixerData(_mixer);
#endif
			_currentPlayers.Add(player);
			return player;
		}

		public override void Recycle(AudioPlayer player)
		{
#if !UNITY_WEBGL
            _mixer.ReturnTrack(player.TrackType, player.AudioTrack); 
#endif
            RemoveFromCurrent(player);
            player.gameObject.SetActive(false);
            base.Recycle(player);
        }

		protected override AudioPlayer CreateObject()
		{
			AudioPlayer newPlayer = GameObject.Instantiate(BaseObject, _parent);
			newPlayer.OnRecycle += _cachedRecycleDelegate;
			return newPlayer;
		}

		protected override void DestroyObject(AudioPlayer player)
		{
            player.OnRecycle -= _cachedRecycleDelegate;
            GameObject.Destroy(player.gameObject);
		}

		private void RemoveFromCurrent(AudioPlayer player)
		{
			for(int i = _currentPlayers.Count - 1; i >=0; i--)
			{
				if(_currentPlayers[i] == player)
				{
					_currentPlayers.RemoveAt(i);
				}
			}
		}

		public IReadOnlyList<AudioPlayer> GetCurrentAudioPlayers()
		{
			return _currentPlayers;
        }
	}
}