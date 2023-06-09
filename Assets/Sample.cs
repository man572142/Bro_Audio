using System.Collections;
using UnityEngine;
using MiProduction.BroAudio;
using UnityEngine.Audio;
using UnityEngine.UI;
using System;

public class Sample : MonoBehaviour
{
    [SerializeField] AudioID _music1;
    [SerializeField] AudioID _music2;
    [SerializeField] AudioID _uiClick;
    [SerializeField] AudioID _uiCancel;
    [SerializeField] AudioID _voiceOver;

    void Start()
    {
        StartCoroutine(PlayTest());
    }

    private IEnumerator PlayTest()
    {
        PlayMusicA();
        yield return new WaitForSeconds(2f);

        PlayMusicB();
        yield return new WaitForSeconds(2f);

        PlayMusicA();
        yield return new WaitForSeconds(2f);

        PlayMusicB();
        yield return new WaitForSeconds(2f);

        PlayMusicA();
        yield return new WaitForSeconds(2f);
        PlayMusicB();

        yield return new WaitForSeconds(2f);

        PlayMusicA();
        yield return new WaitForSeconds(2f);

        PlayMusicB();
        yield return new WaitForSeconds(2f);

        PlayMusicA();
        yield return new WaitForSeconds(2f);
    }

	public void PlayMusicA()
	{
        Debug.Log("A Default");
        BroAudio.PlayMusic(_music1, Transition.Default);
    }


	public void PlayMusicB()
    {
        Debug.Log("B CrossFade");
        BroAudio.PlayMusic(_music2, Transition.CrossFade);
    }

    public void PlayUI()
	{
        BroAudio.Play(_uiClick).DuckOthers(0.3f, 0.1f);
    }

    public void PlayUICancel()
	{
        BroAudio.Play(_uiCancel);
    }

    public void PlayVO()
	{
        BroAudio.Play(_voiceOver);
    }
}
