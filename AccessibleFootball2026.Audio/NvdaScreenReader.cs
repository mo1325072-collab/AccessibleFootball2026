using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace AccessibleFootball2026.Audio;

/// <summary>
/// جسر الاتصال الفوري بقارئ الشاشة NVDA عبر nvdaControllerClient64
/// يحافظ على صوت NVDA المختار، مع توافق تقديري للنسخ القديمة وSAPI عند غياب NVDA فقط.
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

    [DllImport("nvdaControllerClient64.dll", CharSet=CharSet.Unicode, EntryPoint="nvdaController_speakSsml")]
    private static extern int SpeakSsmlNative(string ssml,int symbolLevel,int priority,[MarshalAs(UnmanagedType.U1)] bool asynchronous);

    public bool IsAvailable => _nvdaAvailable;
    public bool LastCompletionWaitEstimated { get; private set; }

    public async System.Threading.Tasks.Task SpeakUntilFinishedAsync(string text,bool interrupt=true)
    {
        if(string.IsNullOrWhiteSpace(text))return;
        CheckNvda();
        LastCompletionWaitEstimated=false;
        if(_nvdaAvailable)
        {
            try
            {
                if(interrupt)nvdaController_cancelSpeech64();
                string escaped=System.Security.SecurityElement.Escape(text)??"";
                var spoken=System.Threading.Tasks.Task.Run(()=>SpeakSsmlNative("<speak>"+escaped+"</speak>",-1,0,false));
                try
                {
                    int result=await spoken.WaitAsync(TimeSpan.FromSeconds(60));
                    if(result==0 || result==1223)return; // completed, or canceled by the user
                }
                catch(TimeoutException){CancelSpeech();return;} // avoid an old NVDA synchronous-cancel race hanging the match
            }
            catch(DllNotFoundException){}
            catch(EntryPointNotFoundException){}
            // Preserve the user's NVDA voice/routing on older APIs. This compatibility wait is an estimate, not completion evidence.
            LastCompletionWaitEstimated=true;
            Speak(text,interrupt);
            await System.Threading.Tasks.Task.Delay(TimeSpan.FromSeconds(Math.Max(2,text.Split(' ',StringSplitOptions.RemoveEmptyEntries).Length*.65)));
            return;
        }
        // Existing SAPI route only when NVDA itself is unavailable, never to replace a working NVDA voice.
        await System.Threading.Tasks.Task.Run(()=>SpeakSapi(text,interrupt,synchronous:true));
    }

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

    private static void SpeakSapi(string text, bool interrupt, bool synchronous=false)
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
                int flags = (synchronous ? 0 : 1) | (interrupt ? 2 : 0); // 1 = SVSFlagsAsync, 2 = SVSFPurgeBeforeSpeak
                _sapiVoice.GetType().InvokeMember("Speak",
                    BindingFlags.InvokeMethod,
                    null, _sapiVoice, new object[] { text, flags });
            }
        }
        catch { }
    }
}
