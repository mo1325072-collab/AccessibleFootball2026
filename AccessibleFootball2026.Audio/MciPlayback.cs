using System.Runtime.InteropServices;
using System.Text;
using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo("SettingsLifecycleHarness")]
namespace AccessibleFootball2026.Audio;
internal static class MciPlayback
{
    [DllImport("winmm.dll", CharSet = CharSet.Auto)]
    private static extern int mciSendString(string command, StringBuilder? buffer, int size, IntPtr callback);
    internal static Func<string, StringBuilder?, int>? TestCommand;
    internal static int SendString(string command, StringBuilder? buffer, int size, IntPtr callback)
    {
        if (TestCommand is { } test) return test(command, buffer);
        try { return mciSendString(command, buffer, size, callback); }
        catch (DllNotFoundException) { return -1; }
        catch (EntryPointNotFoundException) { return -1; }
        catch (BadImageFormatException) { return -1; }
    }
}
