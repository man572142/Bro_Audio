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
        //StartCoroutine(Test());
    }

	public void PlayMusicA()
	{
		if(_music1 > 0)
		{
            BroAudio.PlayMusic(_music1, Transition.Immediate);
		}
	}

    public void PlayMusicB()
    {
        if (_music2 > 0)
        {
            BroAudio.PlayMusic(_music2, Transition.CrossFade);
        }
    }

    public void PlayUI()
	{
        if(_uiClick > 0)
		{
            BroAudio.Play(_uiClick);
		}
	}

    public void PlayUICancel()
	{
        if (_uiCancel > 0)
        {
            BroAudio.Play(_uiCancel);
        }
    }

    public void PlayVO()
	{
        if (_voiceOver > 0)
        {
            BroAudio.Play(_voiceOver);
        }
    }

    private IEnumerator Test()
	{
        Debug.Log("Test");
        // 開始播放
        //BroAudio.PlaySound((int)Cat.Meow);
        // 5秒後
        yield return new WaitForSeconds(5f);
        
        // 在1秒內慢慢將音量降至50%
        BroAudio.SetVolume(0.5f,1f ,BroAudioType.UI);
        yield return new WaitForSeconds(1f);

        // 持續播放5秒
        yield return new WaitForSeconds(5f);
        // 結束播放
        BroAudio.Stop(BroAudioType.UI);
	}        
}
