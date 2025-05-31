using System;
using Ami.BroAudio.Data;
using UnityEngine;

namespace Ami.BroAudio.Editor
{
    public interface IPropertyClipboardData
    {
        string Type { get; }
    }
    
    [Serializable]
    public struct PlaybackPosData : IPropertyClipboardData
    {
        public float StartPosition;
        public float EndPosition;
        public float Delay;
        [field: SerializeField] public string Type { get; set; }
    }

    [Serializable]
    public struct FadingData : IPropertyClipboardData
    {
        public float FadeIn;
        public float FadeOut;
        [field: SerializeField] public string Type { get; set; }
    }
    
    [Serializable]
    public struct RandomizableData : IPropertyClipboardData
    {
        public const string MasterVolumeType = "MasterVolume";
        public const string Pitch = "Pitch";
        
        public float Main;
        public float RandomRange;
        public int RandomFlag;
        [field: SerializeField] public string Type { get; set; }
    }

    public struct LoopData : IPropertyClipboardData
    {
        public bool Loop;
        public bool SeamlessLoop;
        public float TransitionTime;
        public SeamlessType SeamlessTransitionType;
        public TempoTransition TransitionTempo;
        [field: SerializeField] public string Type { get; set; }
    }

    public static class PropertyClipboardDataFactory
    {
        static PropertyClipboardDataFactory()
        {
        }

        public static PlaybackPosData GetPlaybackPosData(ITransport transport)
        {
            return new PlaybackPosData()
            {
                StartPosition = transport.PlaybackValues[0],
                EndPosition = transport.PlaybackValues[1],
                Delay = transport.PlaybackValues[2],
                Type = nameof(PlaybackPosData),
            };
        }

        public static FadingData GetFadingData(ITransport transport)
        {
            return new FadingData()
            {
                FadeIn = transport.FadingValues[0],
                FadeOut = transport.FadingValues[1],
                Type = nameof(FadingData),
            };
        }

        public static RandomizableData GetMasterVolumeData(float main, float randomRange, int randomFlag)
        {
            return new RandomizableData()
            {
                Main = main,
                RandomRange = randomRange,
                RandomFlag = randomFlag,
                Type = RandomizableData.MasterVolumeType,
            };
        }
        
        public static RandomizableData GetPitchData(float main, float randomRange, int randomFlag)
        {
            return new RandomizableData()
            {
                Main = main,
                RandomRange = randomRange,
                RandomFlag = randomFlag,
                Type = RandomizableData.Pitch,
            };
        }
        
        public static LoopData GetLoopData(bool loop, bool seamlessLoop)
        {
            return new LoopData()
            {
                Loop = loop,
                SeamlessLoop = seamlessLoop,
                Type = nameof(LoopData),
            };
        }
    }
}