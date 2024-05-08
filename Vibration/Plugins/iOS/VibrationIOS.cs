using System.Runtime.InteropServices;
using UnityEngine;

public static class VibrationiOS
{
    public enum ImpactFeedbackStyle
    {
        Heavy,
        Medium,
        Light,
        Rigid,
        Soft
    }

    public enum NotificationFeedbackStyle
    {
        Error,
        Success,
        Warning
    }

    [DllImport("__Internal")]
    private static extern bool _HasVibrator();

    [DllImport("__Internal")]
    private static extern void _Vibrate();

    [DllImport("__Internal")]
    private static extern void _VibratePop();

    [DllImport("__Internal")]
    private static extern void _VibratePeek();

    [DllImport("__Internal")]
    private static extern void _VibrateNope();

    [DllImport("__Internal")]
    private static extern void _impactOccurred(string style);

    [DllImport("__Internal")]
    private static extern void _notificationOccurred(string style);

    [DllImport("__Internal")]
    private static extern void _selectionChanged();

    public static bool CanVibrate { get; private set; }
    private static bool initialized = false;

    public static void Init()
    {
        if (initialized) return;

        if (Application.platform != RuntimePlatform.IPhonePlayer)
        {
            initialized = true;
            return;
        }

        CanVibrate = _HasVibrator();
        initialized = true;
    }

    public static void Vibrate()
    {
        if (!CanVibrate) return;
        _Vibrate();
    }

    public static void VibratePop()
    {
        if (!CanVibrate) return;
        _VibratePop();
    }

    public static void VibratePeek()
    {
        if (!CanVibrate) return;
        _VibratePeek();
    }

    public static void VibrateNope()
    {
        if (!CanVibrate) return;
        _VibrateNope();
    }

    public static void VibrateImpact(ImpactFeedbackStyle style)
    {
        if (!CanVibrate) return;
        _impactOccurred(nameof(style));
    }

    public static void VibrateNotification(NotificationFeedbackStyle style)
    {
        if (!CanVibrate) return;
        _notificationOccurred(nameof(style));
    }

    public static void VibrateSelectionChanged()
    {
        if (!CanVibrate) return;
        _selectionChanged();
    }
}
