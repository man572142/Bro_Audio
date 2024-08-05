using UnityEngine;

namespace Ami.BroAudio
{
    public interface IAudioPlayerContent
    {
        /// <inheritdoc cref="AudioSource.GetOutputData(float[], int)"/>
        void GetOutputData(float[] samples, int channels);

        /// <inheritdoc cref="AudioSource.GetSpectrumData(float[], int, FFTWindow)"/>
        void GetSpectrumData(float[] samples, int channels, FFTWindow window);

        /// <inheritdoc cref="AudioSource.GetSpatializerFloat(int, out float)"/>
        bool GetSpatializerFloat(int index, out float value);

        /// <inheritdoc cref="AudioSource.GetAmbisonicDecoderFloat(int, out float)"/>
        bool GetAmbisonicDecoderFloat(int index, out float value);

        // TODO: need to reset all property when recycling
        AudioSource GetAudioSource();
    }
}