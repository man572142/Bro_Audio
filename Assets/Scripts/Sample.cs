using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Sample : MonoBehaviour
{
    [SerializeField] Transition _transitionA = Transition.Immediate;
    [SerializeField] Transition _transitionB = Transition.Immediate;

    [SerializeField] float _crossFadeTime = -1f;

    public void PlayA()
    {
        SoundManager.Instance.PlayMusic(Music.MusicA,_transitionA);
    }
    
    public void PlayB()
    {
        SoundManager.Instance.PlayMusic(Music.MusicB, _transitionB);
    }
}
