using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ami.BroAudio;

public class TestDude : MonoBehaviour
{
    [SerializeField] SoundID _sound;
    void Start()
    {
        StartCoroutine(Co());
    }

    private IEnumerator Co()
    {
        yield return new WaitForSeconds(1);

        _sound.Play(Vector3.zero).OnEnd(_ => Debug.Log("OnEnd"));
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Space))
        {
            BroAudio.Stop(_sound);
        }
    }
}
