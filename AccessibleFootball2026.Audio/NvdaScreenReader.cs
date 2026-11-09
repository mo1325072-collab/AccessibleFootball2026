using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace AccessibleFootball2026.Audio;

/// <summary>
/// جسر الاتصال الفوري بقارئ الشاشة NVDA عبر nvdaControllerClient64
/// بزمن استجابة 0ms وبدون أي لاج، مع دعم احتياطي تلقائي لـ Windows SAPI
/// وبدون أي رموز كونسول أو صفارات مذر بورد مزعجة
/// </summary>
public class NvdaScreenReader : IScreenReader
{
    private bool _nvdaAvailable = false;
    private static object? _sapiVoice = null;
    private static bool _sapiAttempted = false;

    [DllImport("nvdaControllerClient64.dll", CharSet = CharSet.Unicode, EntryPoint = "nvdaController_testIfRunning")]
    private static extern int nvdaController_testIfRunning64();

    [DllImport("nvdaControllerClient64.dll", CharSet = CharSet.Unicode, EntryPoint = "nvdaController_speakText")]
    private static extern int nvdaController_speakText64([MarshalAs(UnmanagedType.LPWStr)] string text);

    [DllImport("nvdaControllerClient64.dll", CharSet = CharSet.Unicode, EntryPoint = "nvdaController_cancelSpeech")]
    private static extern int nvdaController_cancelSpeech64();

    public bool IsAvailable => _nvdaAvailable;

    public NvdaScreenReader()
    {
        CheckNvda();
    }

    private void CheckNvda()
    {
        try
        {
            _nvdaAvailable = (nvdaController_testIfRunning64() == 0);
        }
        catch
        {
            _nvdaAvailable = false;
        }
    }

    public void Speak(string text, bool interrupt = true)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        if (!_nvdaAvailable)
        {
            CheckNvda();
        }

        if (_nvdaAvailable)
        {
            try
            {
                if (interrupt)
                {
                    nvdaController_cancelSpeech64();
                }
                nvdaController_speakText64(text);
                return;
            }
            catch
            {
                _nvdaAvailable = false;
            }
        }

        // صوت Windows SAPI الاحتياطي الناطق
        SpeakSapi(text, interrupt);
    }

    public void CancelSpeech()
    {
        if (_nvdaAvailable)
        {
            try
            {
                nvdaController_cancelSpeech64();
            }
            catch { }
        }

        if (_sapiVoice != null)
        {
            try
            {
                _sapiVoice.GetType().InvokeMember("Speak",
                    BindingFlags.InvokeMethod,
                    null, _sapiVoice, new object[] { "", 2 }); // 2 = SVSFPurgeBeforeSpeak
            }
            catch { }
        }
    }

    private static void SpeakSapi(string text, bool interrupt)
    {
        try
        {
            if (!_sapiAttempted)
            {
                _sapiAttempted = true;
                var type = Type.GetTypeFromProgID("SAPI.SpVoice");
                if (type != null)
                {
                    _sapiVoice = Activator.CreateInstance(type);
                }
            }

            if (_sapiVoice != null)
            {
                int flags = 1 | (interrupt ? 2 : 0); // 1 = SVSFlagsAsync, 2 = SVSFPurgeBeforeSpeak
                _sapiVoice.GetType().InvokeMember("Speak",
                    BindingFlags.InvokeMethod,
                    null, _sapiVoice, new object[] { text, flags });
            }
        }
        catch { }
    }
}
