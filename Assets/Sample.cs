using System.Collections;
using UnityEngine;
using MiProduction.BroAudio;

public class Sample : MonoBehaviour
{
    [SerializeField] AudioID _soundTrack;
    [SerializeField] AudioID _sfx;
    [SerializeField] AudioID _sfx2;

    void Start()
    {
        StartCoroutine(Test());
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
