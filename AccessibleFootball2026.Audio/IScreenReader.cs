namespace AccessibleFootball2026.Audio;

public interface IScreenReader
{
    bool IsAvailable { get; }
    void Speak(string text, bool interrupt = true);
    void CancelSpeech();
}
