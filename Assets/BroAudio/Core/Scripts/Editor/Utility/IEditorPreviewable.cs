namespace Ami.BroAudio.Editor
{
    public interface IEditorPreviewable
    {
        void StartPreview(string clipPath, out float initialVolume, out float initialPitch);
        void EndPreview();
        float Volume { get; }
        float Pitch { get; }
        string CurrentClipPath { get; }
    } 
}