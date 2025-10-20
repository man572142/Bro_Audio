using System;
using UnityEngine;
using UnityEngine.UI;

namespace Ami.BroAudio.Demo
{
    public class SongNameUI : MonoBehaviour
    {
        [SerializeField] Text _title = null;

        void Start()
        {
            BroAudio.OnBGMChanged += OnBGMChanged;
        }

        private void OnDestroy()
        {
            BroAudio.OnBGMChanged -= OnBGMChanged;
        }

        private void OnBGMChanged(IAudioPlayer player)
        {
            if(player == null)
            {
                return;
            }

            if(!player.IsPlaying)
            {
                player.OnStart(SetClipName);
            }
            else
            {
                SetClipName(player);
            }
            
        }

        private void SetClipName(IAudioPlayer player)
        {
            _title.text = player.AudioSource.clip.name;
        }
    }
}