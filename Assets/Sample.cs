using System.Collections;
using UnityEngine;
using MiProduction.BroAudio;

public class Sample : MonoBehaviour
{
    void Start()
    {
        StartCoroutine(Test());
    }

    private IEnumerator Test()
	{
        Debug.Log("Test");
        // �}�l����
        //BroAudio.PlaySound((int)Cat.Meow);
        // 5���
        yield return new WaitForSeconds(5f);
        
        // �b1���C�C�N���q����50%
        BroAudio.SetVolume(0.5f,1f ,AudioType.UI);
        yield return new WaitForSeconds(1f);

        // ���򼽩�5��
        yield return new WaitForSeconds(5f);
        // ��������
        BroAudio.Stop(AudioType.UI);
	}        
}
