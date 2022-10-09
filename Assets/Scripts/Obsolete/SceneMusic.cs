using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MiProduction.BroAudio.SceneMusic
{
    [System.Serializable]
    public struct SceneMusic
    {
        public Music Music;
        [SerializeField] private List<string> _scenes;

        public List<string> Scenes
        {
            get
            {
                if(_scenes == null)
                    _scenes = new List<string>();
                return _scenes;
            }

        }
    }

}


