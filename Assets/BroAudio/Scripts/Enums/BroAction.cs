using UnityEngine;

namespace Ami.BroAudio
{
	public enum BroAction
	{
        [InspectorName("Play ()")] Play = 0,
		[InspectorName("Play (followTarget)")] PlayFollowTarget,
		[InspectorName("Play (position)")] PlayInPosition,

		[EnumSeparator]
        [InspectorName("Stop ()")] StopById = 100,
		[InspectorName("Stop (fadeTime)")] StopByIdFadeTime,
		[InspectorName("Stop (audioType)")]	StopByType,
		[InspectorName("Stop (audioType , fadeTime)")]StopByTypeFadeTime,

        [EnumSeparator]
        [InspectorName("Pause ()")]	Pause = 200,
		[InspectorName("Pause (fadeTime)")]	PauseFadeTime,

        [EnumSeparator]
        [InspectorName("SetMasterVolume (volume)")] SetVolume = 300,
        [InspectorName("SetVolume (volume)")] SetVolumeById,
        [InspectorName("SetVolume (volume, fadeTime)")] SetVolumeByIdFadeTime,
        [InspectorName("SetVolume (volume, audioType)")] SetVolumeByType,
        [InspectorName("SetVolume (volume, audioType, fadeTime)")]	SetVolumeByTypeFadeTime,

        [EnumSeparator]
        [InspectorName("SetEffect (effectParameter)")]SetEffect = 400,
		[InspectorName("SetEffect (effectParameter, audioType)")] SetEffectByType,
		
	} 
}