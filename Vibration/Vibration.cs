////////////////////////////////////////////////////////////////////////////////
//
// @author Benoît Freslon @benoitfreslon
// https://github.com/BenoitFreslon/Vibration
// https://benoitfreslon.com
//
////////////////////////////////////////////////////////////////////////////////

using System;
using UnityEngine;

public static class Vibration
{
#if UNITY_ANDROID
    [Obsolete("This variable is always null now.", true)]
    public static AndroidJavaClass unityPlayer, vibrationEffect;
    [Obsolete("This variable is always null now.", true)]
    public static AndroidJavaObject currentActivity, vibrator, context;
#endif

    public static bool EnableLogging { get; set; }
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
    public static void Init ()
    {
        if (initialized) return;
        CanVibrate = SystemInfo.supportsVibration;

#if UNITY_EDITOR
        EnableLogging = true;
        VibrationiOS.Init();
        VibrationAndroid.Init();
        VibrationAndroid.EnableLogging = true;
        VibrationWebGL.Init();
        CanVibrate = false;
#elif UNITY_IOS
        VibrationiOS.Init();
        CanVibrate = VibrationiOS.CanVibrate;
#elif UNITY_ANDROID
        VibrationAndroid.Init();
        CanVibrate = VibrationAndroid.CanVibrate;
#elif UNITY_WEBGL
        VibrationWebGL.Init();
        CanVibrate = VibrationWebGL.CanVibrate;
#endif

        initialized = true;
    }

    /// <summary>
    /// Default Long-ish Vibration
    /// </summary>
    public static void Vibrate()
    {
        Log("vibrate called");
        if (NoVibrator) return;
#if UNITY_ANDROID || UNITY_IOS
        Handheld.Vibrate();
#elif UNITY_WEBGL
        VibrationWebGL.Vibrate(200);
#endif
    }

    ///<summary>
    /// Tiny pop vibration
    ///</summary>
    public static void VibratePop ()
    {
        Log("vibrate pop called");
        if (NoVibrator) return;
#if UNITY_IOS
        VibrationiOS.VibratePop();
#elif UNITY_ANDROID
        // disable logging temporarily so we can try different vibrations quickly
        VibrationAndroid.PauseLogging = true;
        if (VibrationAndroid.VibratePredefined(VibrationAndroid.PredefinedEffects.TICK))
            return;
        else if (VibrationAndroid.PerformHapticFeedback(VibrationAndroid.HapticFeedbacks.KEYBOARD_TAP))
            return;
        else
            VibrationAndroid.Vibrate(50);
        VibrationAndroid.PauseLogging = false;
#elif UNITY_WEBGL
        VibrationWebGL.Vibrate(50);
#endif
    }

    ///<summary>
    /// Small peek vibration
    ///</summary>
    public static void VibratePeek ()
    {
        Log("vibrate peek called");
        if (NoVibrator) return;
#if UNITY_IOS
        VibrationiOS.VibratePeek();
#elif UNITY_ANDROID
        VibrationAndroid.PauseLogging = true;
        if (VibrationAndroid.VibratePredefined(VibrationAndroid.PredefinedEffects.CLICK))
            return;
        else if (VibrationAndroid.PerformHapticFeedback(VibrationAndroid.HapticFeedbacks.CONFIRM))
            return;
        else
            VibrationAndroid.Vibrate(100);
        VibrationAndroid.PauseLogging = false;
#elif UNITY_WEBGL
        VibrationWebGL.Vibrate(100);
#endif
    }

    ///<summary>
    /// 3 small vibrations
    ///</summary>
    public static void VibrateNope ()
{
        Log("vibrate nope called");
        if (NoVibrator) return;
#if UNITY_IOS
        VibrationiOS.VibrateNope();
#elif UNITY_ANDROID
        if (VibrationAndroid.SupportsComposition &&
            VibrationAndroid.CompositionEffectSupport[VibrationAndroid.CompositionEffects.LOW_TICK])
        {
            VibrationAndroid.CompositionEffects[] compEffects =
            {
                VibrationAndroid.CompositionEffects.LOW_TICK,
                VibrationAndroid.CompositionEffects.LOW_TICK,
                VibrationAndroid.CompositionEffects.LOW_TICK
            };

            VibrationAndroid.VibrateComposition(compEffects);
        }
        else
        {
            VibrationAndroid.VibratePattern(new long[] { 0, 50, 100, 50, 100, 50 });
        }
#elif UNITY_WEBGL
        int[] milliseconds = { 50, 100, 50, 100, 50 };
        VibrationWebGL.VibratePattern(milliseconds);
#endif
    }

    /// <summary>
    /// Vibrate for a given duration
    /// </summary>
    /// <param name="duration">Alternating periods in milliseconds in which the device vibration is On-Off-On...</param>
    public static void Vibrate(int duration)
    {
        Log($"vibrate duration called: {duration}");
        if (NoVibrator) return;
#if UNITY_IOS
        Log("iOS does not support vibration durations");
#elif UNITY_ANDROID
        VibrationAndroid.Vibrate(duration);
#elif UNITY_WEBGL
        VibrationWebGL.Vibrate(duration);
#endif
    }

    /// <summary>
    /// Vibrate a pattern of On-Off durations
    /// </summary>
    /// <param name="pattern">Alternating periods in milliseconds in which the device vibration is On-Off-On...</param>
    public static void VibratePattern(int[] pattern)
    {
        Log("vibrate pattern called");
        if (NoVibrator) return;
#if UNITY_IOS
        Log("iOS does not support vibration patterns");
#elif UNITY_ANDROID
        // android takes longs and its first element is an off duration
        long[] longs = new long[pattern.Length + 1];
        longs[0] = 0;
        for (int i = 0; i < pattern.Length; i++)
            longs[i + 1] = pattern[i];
        VibrationAndroid.VibratePattern(longs);
#elif UNITY_WEBGL
        VibrationWebGL.VibratePattern(pattern);
#endif
    }

    /// <summary>
    /// Cancel any current vibrations
    /// </summary>
    public static void VibrateCancel()
    {
        Log("vibrate cancel called");
        if (NoVibrator) return;
#if UNITY_IOS
        Log("iOS does not support vibration Canceling");
#elif UNITY_ANDROID
        VibrationAndroid.VibrateCancel();
#elif UNITY_WEBGL
        VibrationWebGL.VibrateCancel();
#endif
    }

    [Obsolete("This method is obsolete. Call VibrationiOS.VibrateImpact() instead.")]
    public static void VibrateIOS(ImpactFeedbackStyle style)
    {
#if UNITY_IOS
        VibrationiOS.VibrateImpact((VibrationiOS.ImpactFeedbackStyle)style);
#endif
    }

    [Obsolete("This method is obsolete. Call VibrationiOS.VibrateNotification() instead.")]
    public static void VibrateIOS(NotificationFeedbackStyle style)
    {
#if UNITY_IOS
        VibrationiOS.VibrateNotification((VibrationiOS.NotificationFeedbackStyle)style);
#endif
    }

    [Obsolete("This method is obsolete. Call VibrationiOS.VibrateSelectionChanged() instead.")]
    public static void VibrateIOS_SelectionChanged()
    {
#if UNITY_IOS
        VibrationiOS.VibrateSelectionChanged();
#endif
    }

#if UNITY_ANDROID
    [Obsolete("This method is obsolete. Call VibrationAndroid.Vibrate() instead.")]
    public static void VibrateAndroid ( long milliseconds )
    {
        VibrationAndroid.Vibrate(milliseconds);
    }

    [Obsolete("This method is obsolete. Call VibrationAndroid.VibratePattern() instead.")]
    public static void VibrateAndroid ( long[] pattern, int repeat )
    {
        VibrationAndroid.VibratePattern(pattern, repeatIndex : repeat);
    }
#endif

    [Obsolete("This method is obsolete. Call VibrationAndroid.CancelVibration() instead.")]
    public static void CancelAndroid ()
    {
        VibrationAndroid.VibrateCancel();
    }

    [Obsolete("This method is obsolete. Use the property CanVibrate instead.")]
    public static bool HasVibrator ()
    {
        return CanVibrate;
    }

    [Obsolete("This property is obsolete. Use the property VibrationAndroid.AndroidVersion instead.")]
    public static int AndroidVersion
    {
        get => VibrationAndroid.AndroidVersion;
    }

    private static void Log(string msg)
    {
        if (!EnableLogging) return;
        Debug.LogWarning(msg);
    }
}

[Obsolete("This enum is obsolete. Use the enum VibrationiOS.ImpactFeedbackStyle instead.")]
public enum ImpactFeedbackStyle
{
    Heavy,
    Medium,
    Light,
    Rigid,
    Soft
}

[Obsolete("This enum is obsolete. Use the enum VibrationiOS.NotificationFeedbackStyle instead.")]
public enum NotificationFeedbackStyle
{
    Error,
    Success,
    Warning
}
