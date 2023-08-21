using Ami.BroAudio;
using UnityEngine;

public class Example : MonoBehaviour
{
    [SerializeField] AudioID _music = default;

    void Start()
    {
        BroAudio.Play(_music);
    }
}