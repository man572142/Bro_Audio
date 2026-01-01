using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FrameRateLimiter : MonoBehaviour
{
    [SerializeField]  private int _frameRate = 60;
    void Awake()
    {
        Application.targetFrameRate = _frameRate;
    }
}
