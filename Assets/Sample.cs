using System.Collections;
using UnityEngine;
using MiProduction.BroAudio;
using MiProduction.Extension;
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
		Debug.Log("Play _music1");
        BroAudio.Play(_music1).AsBGM();
        yield return new WaitForSeconds(2f);

		Debug.Log("Set effect lowPass");
		var effect = new EffectParameter(EffectType.LowPass);
		effect.FadeTime = 2f;
		effect.FadingEase = Ease.OutCubic;
		effect.Value = 800f;
		var xorUI = BroAudioType.All ^ BroAudioType.UI;
		BroAudio.SetEffect(effect, xorUI).ForSeconds(15f);
		yield return new WaitForSeconds(2f);

		Debug.Log("Play _voiceOver as invader");
		BroAudio.Play(_voiceOver)
			.AsInvader().QuietOthers(0.3f,BroAdvice.FadeTime_Smooth);

		yield return new WaitForSeconds(3f);

		Debug.Log("Play _music2");
		BroAudio.Play(_music2)
			.AsBGM().SetTransition(Transition.Default, StopMode.Mute, 2f);

		//Debug.Log("Set effect none");
		//effect.Type = EffectType.None;
		//BroAudio.SetEffect(effect);

		//BroAudio.Play(_voiceOver).WithEffect().LowPassOthers();
		//yield return new WaitForSeconds(6.826f);

		//Debug.Log("DUCK");
		//BroAudio.Play(_voiceOver).WithEffect().QuietOthers(0.3f);
		//yield return new WaitForSeconds(1f);

		//Debug.Log("DUCK");
		//BroAudio.Play(_voiceOver).WithEffect().QuietOthers(0.2f);
		//yield return new WaitForSeconds(1f);

		//Debug.Log("DUCK");
		//BroAudio.Play(_voiceOver).WithEffect().QuietOthers(0.1f);
		//yield return new WaitForSeconds(1f);

		//Debug.Log("A CrossFade");
		//BroAudio.Play(_music1).AsMusic().SetTransition(Transition.CrossFade);
		//yield return new WaitForSeconds(3.27f);

		//BroAudio.Play(_voiceOver).WithEffect().HighPassOthers();
		//yield return new WaitForSeconds(7.75f);

		//BroAudio.Play(_voiceOver).WithEffect().LowPassOthers();
		//yield return new WaitForSeconds(6.826f);

		//Debug.Log("DUCK");
		//BroAudio.Play(_voiceOver).WithEffect().QuietOthers(0.3f);

		//Debug.Log("B Default");
		//BroAudio.Play(_music2)
		//	.AsMusic().SetTransition(Transition.Default);
		//yield return new WaitForSeconds(3.27f);

		//BroAudio.Play(_voiceOver).WithEffect().HighPassOthers();
		//yield return new WaitForSeconds(6.826f);

		//Debug.Log("DUCK");
		//BroAudio.Play(_voiceOver).WithEffect().QuietOthers(0.3f);
		//yield return new WaitForSeconds(3.27f);

		//BroAudio.Play(_voiceOver).WithEffect().HighPassOthers();

		//yield return new WaitForSeconds(5f);

		//Debug.Log("A OnlyFadeOut");
		//BroAudio.Play(_music1).AsMusic().SetTransition(Transition.OnlyFadeOut);

		//yield return new WaitForSeconds(8f);

		//Debug.Log("B OnlyFadeIn");
		//BroAudio.Play(_music2)
		//	.AsMusic().SetTransition(Transition.OnlyFadeIn, StopMode.Pause, 3f);

		//yield return new WaitForSeconds(8f);

		//Debug.Log("A Immediate");
		//BroAudio.Play(_music1).AsMusic().SetTransition(Transition.Immediate).SetVolume(0.5f);
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
        BroAudio.Play(_uiClick);
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
