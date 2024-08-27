////////////////////////////////////////////////////////////////////////////////
//
// @author Benoît Freslon @benoitfreslon
// https://github.com/BenoitFreslon/Vibration
// https://benoitfreslon.com
//
////////////////////////////////////////////////////////////////////////////////

using System;
using UnityEngine;
using static Vibes.Logging;


#if UNITY_IOS
using Vibes.iOS;
#elif UNITY_ANDROID
using Vibes.Android;
#elif UNITY_WEBGL
using Vibes.WebGL;
#endif

public static class Vibration
{
#if UNITY_ANDROID
    [Obsolete("This variable is always null now.", true)]
    public static AndroidJavaClass unityPlayer, vibrationEffect;
    [Obsolete("This variable is always null now.", true)]
    public static AndroidJavaObject currentActivity, vibrator, context;
#endif

    public static bool CanVibrate { get; private set; }

    private static bool NoVibrator
    {
        get
        {
            if (!CanVibrate)
                Log("This device can't vibrate.");
            return !CanVibrate;
        }
    }

    private static bool initialized = false;

    //[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Init()
    {
        if (initialized) return;
        //CanVibrate = SystemInfo.supportsVibration;

#if UNITY_IOS || UNITY_ANDROID || UNITY_WEBGL
        VibrationManager.Init();
        CanVibrate = VibrationManager.CanVibrate;
#endif
        VibrationManager.Init();
        initialized = true;
    }

    /// <summary>
    /// Default Long-ish Vibration
    /// </summary>
    public static bool Vibrate()
    {
        Log($"{nameof(Vibrate)} called");
        if (NoVibrator) return false;
#if UNITY_ANDROID || UNITY_IOS
        Handheld.Vibrate();
        return true;
#elif UNITY_WEBGL
        return VibrationManager.Vibrate(200);
#else
        return false;
#endif
    }

    ///<summary>
    /// Tiny pop vibration
    ///</summary>
    public static bool VibratePop()
    {
        Log($"{nameof(VibratePop)} called");
        if (NoVibrator) return false;
#if UNITY_IOS
        return VibrationManager.VibratePop();
#elif UNITY_ANDROID || UNITY_WEBGL
        return VibrationManager.Vibrate(50);
#else
        return false;
#endif
    }

    ///<summary>
    /// Small peek vibration
    ///</summary>
    public static bool VibratePeek()
    {
        Log($"{nameof(VibratePeek)} called");
        if (NoVibrator) return false;
#if UNITY_IOS
        return VibrationManager.VibratePeek();
#elif UNITY_ANDROID || UNITY_WEBGL
        return VibrationManager.Vibrate(100);
#else
        return false;
#endif
    }

    ///<summary>
    /// 3 small vibrations
    ///</summary>
    public static bool VibrateNope()
    {
        Log($"{nameof(VibrateNope)} called");
        if (NoVibrator) return false;
#if UNITY_IOS
        return VibrationManager.VibrateNope();
#elif UNITY_ANDROID
        return VibrationManager.VibratePattern(new long[] { 0, 50, 100, 50, 100, 50 });
#elif UNITY_WEBGL
        return VibrationManager.VibratePattern(new int[] { 50, 100, 50, 100, 50 });
#else
        return false;
#endif
    }

    /// <summary>
    /// Vibrate for a given duration
    /// </summary>
    /// <param name="duration">Alternating periods in milliseconds in which the device vibration is On-Off-On...</param>
    public static bool Vibrate(int duration)
    {
        Log($"{nameof(Vibrate)} called with a {nameof(duration)}: {duration}");
        if (NoVibrator) return false;
#if UNITY_IOS
        Log("iOS does not support vibration durations", LogLevel.Error);
        return false;
#elif UNITY_ANDROID || UNITY_WEBGL
        return VibrationManager.Vibrate(duration);
#else
        return false;
#endif
    }

    /// <summary>
    /// Vibrate a pattern of On-Off durations
    /// </summary>
    /// <param name="pattern">Alternating periods in milliseconds in which the device vibration is On-Off-On...</param>
    public static bool VibratePattern(int[] pattern)
    {
        if (LoggingAllEnabled)
            Log($"{nameof(VibratePattern)} called with the given {nameof(pattern)}: [{string.Join(", ", pattern)}]");
        if (NoVibrator) return false;
#if UNITY_IOS
        Log("iOS does not support vibration patterns", LogLevel.Error);
        return false;
#elif UNITY_ANDROID
        // android takes longs and its first element is an off duration
        long[] longs = new long[pattern.Length + 1];
        longs[0] = 0;
        for (int i = 0; i < pattern.Length; i++)
            longs[i + 1] = pattern[i];
        return VibrationManager.VibratePattern(longs);
#elif UNITY_WEBGL
        return VibrationManager.VibratePattern(pattern);
#else
        return false;
#endif
    }

    /// <summary>
    /// Cancel the playback of any current vibration taking place on the device.
    /// </summary>
    /// <returns>Whether the cancel could be done, not if it was successful.</returns>
    public static bool VibrateCancel()
    {
        Log($"{nameof(VibratePop)} called");
        if (NoVibrator) return false;
#if UNITY_IOS
        Log("iOS does not support vibration Canceling", LogLevel.Error);
        return false;
#elif UNITY_ANDROID || UNITY_WEBGL
        return VibrationManager.VibrateCancel();
#else
        return false;
#endif
    }

    [Obsolete("This method is obsolete. Call Vibes.iOS.VibrationManager.VibrateImpact() instead.")]
    public static void VibrateIOS(ImpactFeedbackStyle style)
    {
        Vibes.iOS.VibrationManager.VibrateImpact((Vibes.iOS.ImpactFeedbackStyle)style);
    }

    [Obsolete("This method is obsolete. Call Vibes.iOS.VibrationManager.VibrateNotification() instead.")]
    public static void VibrateIOS(NotificationFeedbackStyle style)
    {
        Vibes.iOS.VibrationManager.VibrateNotification((Vibes.iOS.NotificationFeedbackStyle)style);
    }

    [Obsolete("This method is obsolete. Call Vibes.iOS.VibrationManager.VibrateSelectionChanged() instead.")]
    public static void VibrateIOS_SelectionChanged()
    {
        Vibes.iOS.VibrationManager.VibrateSelectionChanged();
    }

#if UNITY_ANDROID
    [Obsolete("This method is obsolete. Call Vibes.Android.VibrationManager.Vibrate() instead.")]
    public static void VibrateAndroid(long milliseconds)
    {
        VibrationManager.Vibrate(milliseconds);
    }

    [Obsolete("This method is obsolete. Call Vibes.Android.VibrationManager.VibratePattern() instead.")]
    public static void VibrateAndroid(long[] pattern, int repeat)
    {
        VibrationManager.VibratePattern(pattern, repeatIndex: repeat);
    }
#endif

    [Obsolete("This method is obsolete. Call Vibes.Android.VibrationManager.CancelVibration() instead.")]
    public static void CancelAndroid()
    {
        VibrationManager.VibrateCancel();
    }

    [Obsolete("This method is obsolete. Use the property CanVibrate instead.")]
    public static bool HasVibrator()
    {
        return CanVibrate;
    }

    [Obsolete("This property is obsolete. Use the property Vibes.Android.VibrationManager.AndroidVersion instead.")]
    public static int AndroidVersion
    {
        get => VibrationManager.AndroidVersion;
    }
}

[Obsolete("This enum is obsolete. Use the enum Vibes.iOS.ImpactFeedbackStyle instead.")]
public enum ImpactFeedbackStyle
{
    Heavy,
    Medium,
    Light,
    Rigid,
    Soft
}

[Obsolete("This enum is obsolete. Use the enum Vibes.iOS.NotificationFeedbackStyle instead.")]
public enum NotificationFeedbackStyle
{
    Error,
    Success,
    Warning
}
