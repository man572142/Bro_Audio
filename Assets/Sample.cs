using System.Collections;
using UnityEngine;
using MiProduction.BroAudio;
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
		BroAudio.SetVolume(0.7f);

        var effect = new EffectParameter(EffectType.LowPass);
		var notUI = BroAudioType.All ^ BroAudioType.UI;
		BroAudio.SetEffect(effect, notUI);

        BroAudio.Play(_music1).AsMusic();
        yield return new WaitForSeconds(20f);

        BroAudio.Play(_music2)
            .AsMusic().SetTransition(Transition.Default, StopMode.Mute, 2f);
		yield return new WaitForSeconds(3f);

		BroAudio.Play(_voiceOver).AsExclusive().LowPassOthers();
		yield return new WaitForSeconds(6.826f);

		Debug.Log("DUCK");
		BroAudio.Play(_voiceOver).AsExclusive().DuckOthers(0.3f);
		yield return new WaitForSeconds(1f);

		Debug.Log("DUCK");
		BroAudio.Play(_voiceOver).AsExclusive().DuckOthers(0.2f);
		yield return new WaitForSeconds(1f);

		Debug.Log("DUCK");
		BroAudio.Play(_voiceOver).AsExclusive().DuckOthers(0.1f);
		yield return new WaitForSeconds(1f);

		Debug.Log("A CrossFade");
		BroAudio.Play(_music1).AsMusic().SetTransition(Transition.CrossFade);
		yield return new WaitForSeconds(3.27f);

		BroAudio.Play(_voiceOver).AsExclusive().HighPassOthers();
		yield return new WaitForSeconds(7.75f);

		BroAudio.Play(_voiceOver).AsExclusive().LowPassOthers();
		yield return new WaitForSeconds(6.826f);

		Debug.Log("DUCK");
		BroAudio.Play(_voiceOver).AsExclusive().DuckOthers(0.3f);

		Debug.Log("B Default");
		BroAudio.Play(_music2)
			.AsMusic().SetTransition(Transition.Default);
		yield return new WaitForSeconds(3.27f);

		BroAudio.Play(_voiceOver).AsExclusive().HighPassOthers();
		yield return new WaitForSeconds(6.826f);

		Debug.Log("DUCK");
		BroAudio.Play(_voiceOver).AsExclusive().DuckOthers(0.3f);
		yield return new WaitForSeconds(3.27f);

		BroAudio.Play(_voiceOver).AsExclusive().HighPassOthers();

		yield return new WaitForSeconds(5f);

		Debug.Log("A OnlyFadeOut");
		BroAudio.Play(_music1).AsMusic().SetTransition(Transition.OnlyFadeOut);

		yield return new WaitForSeconds(8f);

		Debug.Log("B OnlyFadeIn");
		BroAudio.Play(_music2)
			.AsMusic().SetTransition(Transition.OnlyFadeIn, StopMode.Pause, 3f);

		yield return new WaitForSeconds(8f);

		Debug.Log("A Immediate");
		BroAudio.Play(_music1).AsMusic().SetTransition(Transition.Immediate).SetVolume(0.5f);
	}


    public void PlayMusicA()
	{
        Debug.Log("A Default");
        BroAudio.Play(_music1);
    }


	public void PlayMusicB()
    {
        Debug.Log("B CrossFade");
        BroAudio.Play(_music2);
    }

    public void PlayUI()
	{
        BroAudio.Play(_uiClick).AsExclusive().DuckOthers(0.3f, 0.1f);
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
