using System.Runtime.InteropServices;
using System.Text;
using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo("SettingsLifecycleHarness")]
[assembly: InternalsVisibleTo("PlayerJourneyHarness")]
[assembly: InternalsVisibleTo("AccessibleFootball2026.Tests")]
namespace AccessibleFootball2026.Audio;
internal static class MciPlayback
{
    [DllImport("winmm.dll", CharSet = CharSet.Auto)]
    private static extern int mciSendString(string command, StringBuilder? buffer, int size, IntPtr callback);
    [DllImport("winmm.dll", CharSet = CharSet.Unicode, EntryPoint = "mciGetErrorStringW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetErrorString(int error, StringBuilder message, int size);
    internal static string DescribeError(int error)
    {
        if (error == -1) return "Windows multimedia library unavailable";
        var text = new StringBuilder(256);
        try { if (GetErrorString(error, text, text.Capacity)) return text.ToString(); }
        catch (DllNotFoundException) { }
        catch (EntryPointNotFoundException) { }
        return "MCI error " + error;
    }
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
