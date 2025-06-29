using System;
using Ami.BroAudio.Data;

public class ReplayData
{
    private AudioEntity _entity;
    private Action<int> _onClipIndexChanged;
    
    public IBroAudioClip Clip { get; private set; }
    public float MasterVolume { get; private set; }
    public float Pitch { get; private set; }
    
    public ReplayData(AudioEntity entity, Action<int> onClipIndexChanged)
    {
        _entity = entity;
        _onClipIndexChanged = onClipIndexChanged;
    }

    public void NewReplay()
    {
        Clip = _entity.PickNewClip(out int clipIndex);      
        _onClipIndexChanged?.Invoke(clipIndex);
        
        MasterVolume = _entity.GetMasterVolume();
        Pitch = _entity.GetPitch();
    }
}
