#if BroAudio_DevOnly
using System;
using UnityEditor;
using UnityEngine;

namespace Ami.BroAudio.Editor
{
    public static class SineWaveGenerator
    {
        [MenuItem("Tools/BroAudio/Generate Sine Wave")]
        public static void GenerateTestTone()
        {
            float duration = 1f;
            float freq = 1000;
            int sampleRate = 48000;
            float[] samples = new float[(int)(sampleRate * duration)];

            for (int i = 0; i < samples.Length; i++)
            {
                double time = (double)i / sampleRate;
                samples[i] = (float)Math.Sin(2 * Math.PI * time * freq);
            }

            string name = $"TestTone_{freq}Hz_0dB";

            string path = EditorUtility.SaveFilePanelInProject("Generate Test Tone", name, "wav", "");
            var audioClip = AudioClip.Create(name, samples.Length, 1, sampleRate, false);
            audioClip.SetData(samples, 0);

            SavWav.Save(path, audioClip);
        }
    } 
}
#endif