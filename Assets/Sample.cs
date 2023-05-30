using System.Collections;
using UnityEngine;
using MiProduction.BroAudio;
using UnityEngine.Audio;
using UnityEngine.UI;

public class Sample : MonoBehaviour
{
    [SerializeField] AudioID _music1;
    [SerializeField] AudioID _music2;
    [SerializeField] AudioID _uiClick;
    [SerializeField] AudioID _uiCancel;
    [SerializeField] AudioID _voiceOver;
    [SerializeField] AudioMixer _broMixer = null;

    [SerializeField] Text _log = null;

    void Start()
    {
    }

	public void PlayMusicA()
	{
        BroAudio.PlayMusic(_music1, Transition.Immediate);
    }

    public void PlayMusicB()
    {
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

    private IEnumerator Test()
	{
        _broMixer.SetFloat("Track1_Send", -25f);
        _broMixer.SetFloat("Track2_Send", -80f);
        
        

        if(_broMixer.GetFloat("Track2_Send", out float level))
		{
            

            while (level < 0f)
			{
                level += Time.deltaTime * 5f;
                _broMixer.SetFloat("Track2_Send",level);
                if (_log && _broMixer.GetFloat("Track2_Send", out float logLevel))
                {
                    _log.text = logLevel.ToString();
                }

                yield return null;
			}
		}

        _broMixer.SetFloat("Track1_Send", 0f);
    }        
}
