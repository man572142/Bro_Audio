namespace Ami.BroAudio
{
    public interface IEffectDecoratable
    {
#if !UNITY_WEBGL
        internal IPlayerEffect AsDominator();
#endif
    }
}