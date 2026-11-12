using System.Threading;
using System.Threading.Tasks;

namespace AccessibleFootball2026.Audio;

/// <summary>One pending screen-reader update. Recorded commentary owns the channel first.</summary>
public sealed class PriorityScreenReader : IScreenReader, IDisposable
{
    private readonly IScreenReader _inner;
    private readonly Func<bool> _commentaryBusy;
    private readonly object _sync = new();
    private readonly object _speechGate;
    private (string Text, TaskCompletionSource<bool> Done)? _pending;
    private bool _pumping, _disposed;
    private int _speaking;
    private CancellationTokenSource? _currentSpeech;
    public bool IsAvailable => _inner.IsAvailable;
    public bool IsSpeaking => Volatile.Read(ref _speaking) != 0;
    public bool HasPendingSpeech { get { lock (_sync) return _pending != null || IsSpeaking; } }
    public PriorityScreenReader(IScreenReader inner, Func<bool> commentaryBusy, object? speechGate = null)
    { _inner = inner; _commentaryBusy = commentaryBusy; _speechGate = speechGate ?? new object(); }
    public void Speak(string text, bool interrupt = true) => _ = SpeakUntilFinishedAsync(text, interrupt);
    public Task SpeakUntilFinishedAsync(string text, bool interrupt = true)
    {
        if (string.IsNullOrWhiteSpace(text)) return Task.CompletedTask;
        var done = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        lock (_sync)
        {
            if (_disposed) return Task.CompletedTask;
            _pending?.Done.TrySetResult(false); // Superseded updates never become a spoken backlog.
            _pending = (text, done);
            if (_pumping) return done.Task;
            _pumping = true;
        }
        _ = PumpAsync();
        return done.Task;
    }
    private async Task PumpAsync()
    {
        while (true)
        {
            while (_commentaryBusy() && !_disposed) await Task.Delay(50);
            (string Text, TaskCompletionSource<bool> Done)? next;
            lock (_speechGate)
            {
                // Starting a recording and claiming the reader use the same gate: no check/start race.
                if (_commentaryBusy()) continue;
                lock (_sync)
                {
                    next = _pending; _pending = null;
                    if (next == null || _disposed) { _pumping = false; return; }
                    _currentSpeech = new CancellationTokenSource();
                    Volatile.Write(ref _speaking, 1);
                }
            }
            try
            {
                await _inner.SpeakUntilFinishedAsync(next.Value.Text, true).WaitAsync(_currentSpeech!.Token);
                next.Value.Done.TrySetResult(true);
            }
            catch { next.Value.Done.TrySetResult(false); }
            finally { lock (_sync) { _currentSpeech?.Dispose(); _currentSpeech = null; Volatile.Write(ref _speaking, 0); } }
        }
    }
    // A goal or penalty may preempt a reader as well as the recorded commentary.
    public void CancelSpeech()
    {
        lock (_sync) { _pending?.Done.TrySetResult(false); _pending = null; _currentSpeech?.Cancel(); }
        _inner.CancelSpeech();
    }
    public void Dispose() { _disposed = true; CancelSpeech(); }
}
