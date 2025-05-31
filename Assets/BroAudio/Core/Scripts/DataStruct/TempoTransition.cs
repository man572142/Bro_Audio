#if UNITY_EDITOR
namespace Ami.BroAudio.Data
{
    [System.Serializable]
    public struct TempoTransition
    {
        public float BPM;
        public int Beats;
    }
}
#endif