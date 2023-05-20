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
        // �}�l����
        //BroAudio.PlaySound((int)Cat.Meow);
        // 5���
        yield return new WaitForSeconds(5f);
        
        // �b1���C�C�N���q����50%
        BroAudio.SetVolume(0.5f,1f ,BroAudioType.UI);
        yield return new WaitForSeconds(1f);

        // ���򼽩�5��
        yield return new WaitForSeconds(5f);
        // ��������
        BroAudio.Stop(BroAudioType.UI);
	}        
}
