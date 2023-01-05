using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct BroAudioClip
{
    public AudioClip AudioClip;
    [Range(0f, 1f)] public float Volume;
    public float Delay;
    public float StartPosition;
    public float EndPosition;
    public float FadeIn;
    public float FadeOut;

    public int Weight;

    public bool IsNull() => AudioClip == null;

    public void ClearData()
	{
        AudioClip = null;
        this = default;
	}
}
