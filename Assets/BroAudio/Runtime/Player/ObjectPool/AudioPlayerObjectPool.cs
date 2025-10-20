using UnityEngine;
using Ami.Extension;
using System.Collections.Generic;

namespace Ami.BroAudio.Runtime
{
    public class AudioPlayerObjectPool : ObjectPool<AudioPlayer>
    {
        private readonly Transform _parent = null;
        private readonly List<AudioPlayer> _currentPlayers = new List<AudioPlayer>();

        public AudioPlayerObjectPool(AudioPlayer baseObject, Transform parent, int maxInternalPoolSize) : base(baseObject, maxInternalPoolSize)
        {
            _parent = parent;
        }

        public override AudioPlayer Extract()
        {
            AudioPlayer player = base.Extract();
            player.gameObject.SetActive(true);
            _currentPlayers.Add(player);
            return player;
        }

        public override void Recycle(AudioPlayer player)
        {
            RemoveFromCurrent(player);
            player.gameObject.SetActive(false);
            base.Recycle(player);
        }

        protected override AudioPlayer CreateObject()
        {
            AudioPlayer newPlayer = GameObject.Instantiate(BaseObject, _parent);
            return newPlayer;
        }

        protected override void DestroyObject(AudioPlayer player)
        {
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