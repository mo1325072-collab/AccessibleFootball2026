NVDA Controller Client 2026.3.1, official x64 binary, unmodified.
Downloaded 2026-11-11 from:
https://download.nvaccess.org/releases/stable/nvda_2026.3.1_controllerClient.zip

Installed as nvdaControllerClient64.dll to preserve the application's existing import name.
Upstream library license: LGPL 2.1. Included license, API header and upstream readme accompany the DLL.
The asynchronous ordinary speakText route is unchanged. The official goal announcement uses synchronous speakSsml on a worker thread so game restart waits for completion or user cancellation. NVDA older than 2024.1 or older controller clients keep their NVDA voice and original speakText route with an estimated word-length wait (LastCompletionWaitEstimated=true), not exact completion. Windows SAPI is used only by the existing fallback when NVDA itself is unavailable. A 60-second watchdog cancels a stuck NVDA call to prevent a stalled match; actual Windows playback still needs testing.
