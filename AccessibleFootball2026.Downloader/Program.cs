using System;
using System.Windows.Forms;
using AccessibleFootball2026.Audio;

namespace AccessibleFootball2026.Downloader;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        var screenReader = new NvdaScreenReader();
        var form = new DownloaderForm(screenReader);
        Application.Run(form);
    }
}
