using System.Collections;
using UnityEngine;
using MiProduction.BroAudio;

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
        var player = BroAudio.Play(_music1)
            .AsMusic().SetTransition(Transition.CrossFade,3f).SetVolume(0.5f)
            .GetPlaybackControl();

        yield return new WaitForSeconds(5f);

        Debug.Log("B Default");
        BroAudio.Play(_music2)
            .AsMusic().SetTransition(Transition.Default, StopMode.Mute, 3f);

        yield return new WaitForSeconds(5f);

		Debug.Log("VoiceOver");
		BroAudio.Play(_voiceOver).AsExclusive().LowPassOthers();

		yield return new WaitForSeconds(3f);
		Debug.Log("VoiceOver");
		BroAudio.Play(_voiceOver).AsExclusive().DuckOthers(0.3f);

		yield return new WaitForSeconds(3f);
		Debug.Log("VoiceOver");
		BroAudio.Play(_voiceOver).AsExclusive().HighPassOthers();


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
