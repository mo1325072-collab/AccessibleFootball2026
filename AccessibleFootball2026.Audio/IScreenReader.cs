namespace AccessibleFootball2026.Audio;

public interface IScreenReader
{
    bool IsAvailable { get; }
    void Speak(string text, bool interrupt = true);
    void CancelSpeech();
    // Test and third-party readers may override this with their actual completion signal.
    System.Threading.Tasks.Task SpeakUntilFinishedAsync(string text, bool interrupt = true)
    {
        Speak(text,interrupt);
        return System.Threading.Tasks.Task.Delay(System.TimeSpan.FromSeconds(System.Math.Max(2,text.Split(' ',System.StringSplitOptions.RemoveEmptyEntries).Length*.65)));
    }
}
