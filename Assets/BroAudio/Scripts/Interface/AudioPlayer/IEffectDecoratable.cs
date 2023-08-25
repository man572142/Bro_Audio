namespace Ami.BroAudio
{
    public interface IEffectDecoratable
    {
#if !UNITY_WEBGL
#if UNITY_2020_2_OR_NEWER
        internal IPlayerEffect AsInvader();
#else
        IPlayerEffect AsInvader();
#endif
#endif
    }
}