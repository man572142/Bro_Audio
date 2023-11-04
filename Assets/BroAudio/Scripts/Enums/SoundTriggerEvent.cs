namespace Ami.BroAudio
{
    public enum UnityMessage
    {
        Awake,
        Start,
        OnEnable,
        OnDisable,
        OnDestroy,

        [EnumSeparator]
        Update,
        FixedUpdate,
        LateUpdate,
        
        [EnumSeparator]
        OnTriggerEnter,
        OnTriggerStay,
        OnTriggerExit,
        OnCollisionEnter,
        OnCollisionStay,
        OnCollisionExit,
        
        [EnumSeparator]
        OnTriggerEnter2D,
        OnTriggerStay2D,
        OnTriggerExit2D,
        OnCollisionEnter2D,
        OnCollisionStay2D,
        OnCollisionExit2D,
    }
}
