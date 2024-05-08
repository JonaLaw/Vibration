using System.Runtime.InteropServices;
using UnityEngine;

public static class VibrationWebGL
{
    [DllImport("__Internal")]
    private static extern bool _HasVibrator();

    [DllImport("__Internal")]
    private static extern void _Vibrate(int milliseconds);

    [DllImport("__Internal")]
    private static extern void _VibratePattern(int[] pattern);

    [DllImport("__Internal")]
    private static extern void _VibrateCancel();

    public static bool CanVibrate { get; private set; }
    private static bool initialized = false;

    public static void Init()
    {
        if(initialized) return;

        if (Application.platform != RuntimePlatform.WebGLPlayer)
        {
            initialized = true;
            return;
        }

        CanVibrate = _HasVibrator();
        initialized = true;
    }

    public static void Vibrate(int milliseconds)
    {
        if (!CanVibrate) return;
        _Vibrate(milliseconds);
    }

    /// https://developer.mozilla.org/en-US/docs/Web/API/Vibration_API#vibration_patterns
    /// <param name="pattern">Alternating periods in milliseconds in which the device vibration is On-Off-On...</param>
    public static void VibratePattern(int[] pattern)
    {
        if (!CanVibrate) return;
        _VibratePattern(pattern);
    }

    /// https://developer.mozilla.org/en-US/docs/Web/API/Vibration_API#canceling_existing_vibrations
    public static void VibrateCancel()
    {
        if (!CanVibrate) return;
        _VibrateCancel();
    }
}
