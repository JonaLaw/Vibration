using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

public static class VibrationAndroid
{
    // https://developer.android.com/reference/android/view/HapticFeedbackConstants
    public enum HapticFeedbacks
    {
        NO_HAPTICS = -1, // 34
        LONG_PRESS = 0, // 3 api support
        VIRTUAL_KEY = 1, // 5
        KEYBOARD_TAP = 3, // 8
        CLOCK_TICK = 4, // 21
        CONTEXT_CLICK = 6, // 23
        KEYBOARD_RELEASE = 7, // 27
        VIRTUAL_KEY_RELEASE = 8, // 27
        TEXT_HANDLE_MOVE = 9, // 27
        GESTURE_START = 12, // 30
        GESTURE_END = 13, // 30
        CONFIRM = 16, // 30
        REJECT = 17, // 30
        TOGGLE_ON = 21, // 34
        TOGGLE_OFF = 22, // 34
        GESTURE_THRESHOLD_ACTIVATE = 23, // 34
        GESTURE_THRESHOLD_DEACTIVATE = 24, // 34
        DRAG_START = 25, // 34
        SEGMENT_TICK = 26, // 34
        SEGMENT_FREQUENT_TICK = 27, // 34
    }

    public enum HapticFeedbackFlags
    {
        NONE = 0, // this ID is not used by android
        IGNORE_GLOBAL_SETTING = 2, // 3, depricated in 33
        IGNORE_VIEW_SETTING = 1, // 3
    }

    // https://developer.android.com/reference/android/os/VibrationEffect#constants_1
    public enum PredefinedEffects
    {
        CLICK = 0,
        DOUBLE_CLICK = 1,
        HEAVY_CLICK = 5,
        TICK = 2
    }

    // https://developer.android.com/reference/android/os/Vibrator#constants_1
    public enum PredefinedEffectSupports
    {
        UNKNOWN = 0,
        YES = 1,
        NO = 2
    }

    // https://developer.android.com/reference/android/os/VibrationEffect.Composition#constants_1
    public enum CompositionEffects
    {
        CLICK = 1, // 30 api support
        LOW_TICK = 8, // 31
        QUICK_FALL = 6, // 30
        QUICK_RISE = 4, // 30
        SLOW_RISE = 5, // 30
        SPIN = 3, // 31
        THUD = 2, // 31
        TICK = 7 // 30
    }

    public readonly struct Amplitude
    {
        public const int None = 0;
        public const int Default = -1; // (int)PredefinedEffect.DEFAULT_AMPLITUDE
        public const int Max = 255;
    }

    public static bool EnableLogging { get; set; }
    public static bool PauseLogging { get; set; }
    public static int AndroidVersion { get; private set; }
    public static bool CanVibrate { get; private set; }
    public static bool SupportsHapticFeedback { get; private set; }
    public static bool SupportsVibrationEffect { get; private set; }
    public static bool SupportsPredefinedEffect { get; private set; }
    public static bool SupportsAmplitudeControl { get; private set; }
    public static bool SupportsComposition { get; private set; }
    public static ReadOnlyDictionary<HapticFeedbacks, bool> HapticFeedbackSupport{ get; private set; }
    public static ReadOnlyDictionary<PredefinedEffects, PredefinedEffectSupports> PredefinedEffectSupport { get; private set; }
    public static ReadOnlyDictionary<CompositionEffects, bool> CompositionEffectSupport { get; private set; }

    private static bool NoVibrationSupport
    {
        get
        {
            if (!CanVibrate)
                Log(logNoSupport + logVibration);
            return !CanVibrate;
        }
    }

    private static bool NoHapticFeedback
    {
        get
        {
            if (!SupportsHapticFeedback)
                Log(logNoSupport + logHaptics);
            return !SupportsHapticFeedback;
        }
    }

    private static bool NoVibrationEffectSupport
    {
        get
        {
            if (!SupportsVibrationEffect)
                Log(logNoSupport + logVibrationEffect);
            return !SupportsVibrationEffect;
        }
    }

    private static bool NoPredefinedEffectSupport
    {
        get
        {
            if (!SupportsPredefinedEffect)
                Log(logNoSupport + logPredefinedEffect);
            return !SupportsPredefinedEffect;
        }
    }

    private static bool NoAmplitudeControlSupport
    {
        get
        {
            if (!SupportsAmplitudeControl)
                Log(logNoSupport + logAmplitudeControl);
            return !SupportsAmplitudeControl;
        }
    }

    private static bool NoVibrationCompositionSupport
    {
        get
        {
            if (!SupportsComposition)
                Log(logNoSupport + logVibrationComposition);
            return !SupportsComposition;
        }
    }

    private static readonly Dictionary<HapticFeedbacks, int> hapticFeedbackAPISupport = new()
    {
        { HapticFeedbacks.NO_HAPTICS, 34 },
        { HapticFeedbacks.LONG_PRESS, 3 },
        { HapticFeedbacks.VIRTUAL_KEY, 5 },
        { HapticFeedbacks.KEYBOARD_TAP, 8 },
        { HapticFeedbacks.CLOCK_TICK, 21 },
        { HapticFeedbacks.CONTEXT_CLICK, 23 },
        { HapticFeedbacks.KEYBOARD_RELEASE, 27 },
        { HapticFeedbacks.VIRTUAL_KEY_RELEASE, 27 },
        { HapticFeedbacks.TEXT_HANDLE_MOVE, 27 },
        { HapticFeedbacks.GESTURE_START, 30 },
        { HapticFeedbacks.GESTURE_END, 30 },
        { HapticFeedbacks.CONFIRM, 30 },
        { HapticFeedbacks.REJECT, 30 },
        { HapticFeedbacks.TOGGLE_ON, 34 },
        { HapticFeedbacks.TOGGLE_OFF, 34 },
        { HapticFeedbacks.GESTURE_THRESHOLD_ACTIVATE, 34 },
        { HapticFeedbacks.GESTURE_THRESHOLD_DEACTIVATE, 34 },
        { HapticFeedbacks.DRAG_START, 34 },
        { HapticFeedbacks.SEGMENT_TICK, 34 },
        { HapticFeedbacks.SEGMENT_FREQUENT_TICK, 34 },
    };

    private const string logNoSupport = "This device has no support for ",
        logVibration = "Vibration",
        logHaptics = "Haptics Effects",
        logVibrationEffect = "Vibration Effects",
        logPredefinedEffect = "Predefined Effects",
        logAmplitudeControl = "Amplitude Control",
        logVibrationComposition = "Vibration Effect Composition";

    private const string vibrateMethod = "vibrate", hapticFeedbackMethod = "performHapticFeedback";
    private static AndroidJavaObject hapticFeedback;
    private static AndroidJavaObject vibrator;
    private static AndroidJavaClass vibrationEffectClass;

    private static bool initialized = false;

    public static void Init()
    {
        if (initialized) return;

        if (Application.platform != RuntimePlatform.Android)
        {
            Log("Application platform is not Android.");
            CompleteInitialization();
            return;
        }

        // Get Api Level
        using AndroidJavaClass androidVersionClass = new("android.os.Build$VERSION");
        AndroidVersion = androidVersionClass.GetStatic<int>("SDK_INT");

        // Get UnityPlayer and CurrentActivity
        using AndroidJavaClass unityPlayer = new("com.unity3d.player.UnityPlayer");
        using AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
        
        if (currentActivity == null)
        {
            Debug.LogError("Unable to get the current Android activity for vibration usage.");
            CompleteInitialization();
            return;
        }

        vibrator = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator");
        CanVibrate = vibrator.Call<bool>("hasVibrator");
        if (!CanVibrate)
        {
            CompleteInitialization();
            return;
        }

        SupportsHapticFeedback = AndroidVersion >= 3;
        SupportsVibrationEffect = AndroidVersion >= 26;
        SupportsPredefinedEffect = AndroidVersion >= 29;
        SupportsComposition = AndroidVersion >= 30;

        if (SupportsHapticFeedback)
        {
            hapticFeedback = new AndroidJavaObject("AndroidPlugin", currentActivity);
        }
        Dictionary<HapticFeedbacks, bool> hapticsSupport = new(hapticFeedbackAPISupport.Count);
        foreach (var item in hapticFeedbackAPISupport)
        {
            hapticsSupport.Add(item.Key, item.Value <= AndroidVersion);
        }
        HapticFeedbackSupport = new(hapticsSupport);

        if (!SupportsVibrationEffect)
        {
            CompleteInitialization();
            return;
        }

        SupportsAmplitudeControl = vibrator.Call<bool>("hasAmplitudeControl");
        vibrationEffectClass = new AndroidJavaClass("android.os.VibrationEffect");

        if (AndroidVersion >= 30)
        {
            // https://developer.android.com/reference/android/os/Vibrator#areEffectsSupported(int[])
            PredefinedEffectSupport = new(GetSupportDictionary<PredefinedEffects, PredefinedEffectSupports, int>("areEffectsSupported"));
        }
        else
        {
            // since we can't check if certain predefined effects are supported, mark them all as unknown
            PredefinedEffects[] effectsArray = Enum.GetValues(typeof(PredefinedEffects)) as PredefinedEffects[];
            Dictionary<PredefinedEffects, PredefinedEffectSupports> effectsSupport = new(effectsArray.Length);
            foreach (PredefinedEffects effect in effectsArray)
            {
                effectsSupport.Add(effect, PredefinedEffectSupports.UNKNOWN);
            }
            PredefinedEffectSupport = new(effectsSupport);
        }

        if (SupportsComposition)
        {
            // https://developer.android.com/reference/android/os/Vibrator#arePrimitivesSupported(int[])
            CompositionEffectSupport = new(GetSupportDictionary<CompositionEffects, bool, bool>("arePrimitivesSupported"));
        }

        CompleteInitialization();
    }

    public static bool PerformHapticFeedback(HapticFeedbacks haptic, HapticFeedbackFlags flag = HapticFeedbackFlags.NONE, bool cancel = false)
    {
        if (NoHapticFeedback) return false;
        if (cancel) VibrateCancel();

        if (hapticFeedbackAPISupport[haptic] > AndroidVersion)
        {
            Log($"{logNoSupport}the {logHaptics} of {nameof(haptic)}");
            return false;
        }

        if (flag == HapticFeedbackFlags.IGNORE_GLOBAL_SETTING && AndroidVersion >= 33)
        {
            Log($"{logNoSupport}the {logHaptics} flag of {nameof(flag)}" +
                $", it will be set to the default of {nameof(HapticFeedbackFlags.NONE)}");
            flag = HapticFeedbackFlags.NONE;
        }

        if (flag == HapticFeedbackFlags.NONE)
            hapticFeedback.Call(hapticFeedbackMethod, (int)haptic);
        else
            hapticFeedback.Call(hapticFeedbackMethod, (int)haptic, (int)flag);
        return true;
    }

    /// <summary>
    /// Vibrate for Milliseconds, with Amplitude (if available).
    /// </summary>
    /// <param name="milliseconds">Duration of the vibration in milliseconds.</param>
    /// <param name="amplitude">If -1, amplitude is set to the device defaultAmplitude. Otherwise, values between 1-255 will be used. If < defaultAmplitude (-1), nothing will happen.</param>
    /// <param name="cancel">If true, CancelAndroid() will be called automatically.</param>
    public static bool Vibrate(long milliseconds, int amplitude = Amplitude.Default, bool cancel = false)
    {
        if (NoVibrationSupport) return false;
        if (cancel) VibrateCancel();

        if (!SupportsVibrationEffect)
        {
            vibrator.Call(vibrateMethod, milliseconds);
            return true;
        }

        using VibrationEffect effect = new(milliseconds, amplitude);
        if (effect == null) return false;
        VibrateEffect(effect);
        return true;
    }

    /// <summary>
    /// Vibrate Pattern with durations of Off and On, with Amplitudes (if available).
    /// </summary>
    /// <param name="pattern">Pattern of durations, with format Off-On-Off-On...</param>
    /// <param name="amplitudes">Amplitudes can be Null (for default) or array of exactly Pattern array length with values between -1 (default) - 255. Values that are < -1 and 0 will not cause vibrations.</param>
    /// <param name="repeatIndex">If -1, no repeat. Otherwise, repeat from given nth index in Pattern.</param>
    /// <param name="cancel">If true, CancelAndroid() will be called automatically.</param>
    public static bool VibratePattern(long[] pattern, int[] amplitudes = null, int repeatIndex = -1, bool cancel = false)
    {
        if (cancel) VibrateCancel();
        if (NoVibrationSupport) return false;
        if (ValidatePattern(pattern, amplitudes, repeatIndex) == false) return false;

        if (!SupportsVibrationEffect)
        {
            vibrator.Call(vibrateMethod, pattern, repeatIndex);
            return true;
        }

        using VibrationEffect effect = new(pattern, amplitudes, repeatIndex);
        if (effect == null) return false;
        VibrateEffect(effect);
        return true;
    }

    /// <summary>
    /// Vibrate predefined effect (described in Vibration.PredefinedEffect). Available from Api Level >= 29.
    /// </summary>
    /// <param name="cancel">If true, CancelAndroid() will be called automatically.</param>
    public static bool VibratePredefined(PredefinedEffects predefinedEffect, bool cancel = false)
    {
        if (cancel) VibrateCancel();
        using VibrationEffect effect = new(predefinedEffect);
        if (effect == null) return false;
        VibrateEffect(effect);
        return true;
    }

    public static bool VibrateComposition(CompositionEffects[] compositionEffects, float[] scales = null, int[] delays = null, bool cancel = false)
    {
        if (cancel) VibrateCancel();
        using VibrationEffect effect = new(compositionEffects, scales, delays);
        if (effect == null) return false;
        VibrateEffect(effect);
        return true;
    }

    public static bool VibrateEffect(VibrationEffect effect, bool cancel = false)
    {
        if (NoVibrationSupport) return false;
        if (cancel) VibrateCancel();
        if (effect == null || effect.Effect == null)
        {
            Log("The given vibration effect is null or the encapsulated effect is null");
            return false;
        }
        vibrator.Call(vibrateMethod, effect.Effect);
        return true;
    }

    public static bool VibrateCancel()
    {
        if (NoVibrationSupport) return false;
        vibrator.Call("cancel");
        return true;
    }

    public static TEnum[] ConvertToEnumArray<TEnum>(int[] values) where TEnum : Enum
    {
        TEnum[] enumArray = new TEnum[values.Length];

        for (int i = 0; i < values.Length; i++)
        {
            if (Enum.IsDefined(typeof(TEnum), values[i]))
            {
                enumArray[i] = (TEnum)Enum.ToObject(typeof(TEnum), values[i]);
            }
            else
            {
                throw new ArgumentException($"Value {values[i]} is not defined in enum type {typeof(TEnum)}.");
            }
        }

        return enumArray;
    }

    public static void LogSupport()
    {
        Debug.Log(
            $"Android Version: {AndroidVersion}\n" +
            $"Vibration Support: {CanVibrate}\n" +
            $"Haptic Feedback Support {SupportsHapticFeedback}\n" +
            $"Vibration Effect Support: {SupportsVibrationEffect}\n" +
            $"Predefined Effect Support: {SupportsPredefinedEffect}\n" +
            $"Amplitude Control Support: {SupportsAmplitudeControl}\n" +
            $"Vibration Composition Support: {SupportsComposition}");

        PrintSupportDictionary(HapticFeedbackSupport);
        PrintSupportDictionary(PredefinedEffectSupport);
        PrintSupportDictionary(CompositionEffectSupport);
    }

    private static void PrintSupportDictionary<TKey, TValue>(ReadOnlyDictionary<TKey, TValue> support)
    {
        var lines = support.Select(kvp => kvp.Key.ToString() + ": " + kvp.Value.ToString());
        Debug.Log(string.Join(Environment.NewLine, lines));
    }

    private static void CompleteInitialization()
    {
        initialized = true;
        HapticFeedbackSupport ??= new(GetDefaultSupportDictionary<HapticFeedbacks, bool>(false));
        PredefinedEffectSupport ??= new(GetDefaultSupportDictionary<PredefinedEffects, PredefinedEffectSupports>(PredefinedEffectSupports.NO));
        CompositionEffectSupport ??= new(GetDefaultSupportDictionary<CompositionEffects, bool>(false));
    }

    private static Dictionary<TKey, TValue> GetDefaultSupportDictionary<TKey, TValue>(TValue value) where TKey : Enum
    {
        Array keys = Enum.GetValues(typeof(TKey));
        Dictionary<TKey, TValue> support = new(keys.Length);
        foreach (TKey key in keys)
        {
            support.Add(key, value);
        }
        return support;
    }

    private static Dictionary<TKey, TValue> GetSupportDictionary<TKey, TValue, TReturn>(string methodName) where TKey : Enum
    { 
        // create arrays of each effect and their corresponding IDs
        Array effectsArray = Enum.GetValues(typeof(TKey));
        TKey[] effects = effectsArray as TKey[];
        int[] ids = effectsArray.Cast<int>().ToArray();
        // get support for each effect
        TValue[] supportResult;
        if (typeof(TValue) == typeof(TReturn))
        {
            supportResult = vibrator.Call<TValue[]>(methodName, ids);
        }
        else
        {
            TReturn[] result = vibrator.Call<TReturn[]>(methodName, ids);
            supportResult = result.Select(x => (TValue)Enum.ToObject(typeof(TValue), x)).ToArray();
        }
        Dictionary<TKey, TValue> support = new(effects.Length);
        for (int i = 0; i < effects.Length; i++)
        {
            support.Add(effects[i], supportResult[i]);
        }
        return support;
    }

    private static bool ValidatePattern(long[] pattern, int[] amplitudes, int repeatIndex)
    {
        if (pattern.Length == 0 || (amplitudes != null && amplitudes.Length != pattern.Length))
        {
            Log($"The length of pattern \'{pattern.Length}\' is 0, or does not equal the length of amplitudes \'{amplitudes.Length}\'.");
            return false;
        }
        if (repeatIndex < -1 || repeatIndex >= pattern.Length)
        {
            Log($"The repeatIndex of \'{repeatIndex}\' is not valid for the length of pattern \'{pattern.Length}\'.");
            return false;
        }
        return true;
    }

    private static void Log(string log)
    {
        if (PauseLogging || !EnableLogging) return;
        Debug.LogWarning(log);
    }

    public readonly struct CompositionPrimitive
    {
        public CompositionEffects CompositionEffect { get; }
        public int CompositionEffectID { get => (int)CompositionEffect; }
        public float Scale { get; }
        public int Delay { get; }

        public CompositionPrimitive(CompositionEffects compositionEffect, float scale = -1, int delay = 0)
        {
            if (scale > 1) scale = 1;
            if (delay < 0) delay = 0;

            CompositionEffect = compositionEffect;
            Scale = scale;
            Delay = delay;
        }
    }

    public class VibrationComposition : IDisposable
    {
        private const string addPrimitiveMethod = "addPrimitive";
        public AndroidJavaObject Composition { get; private set; }

        public VibrationComposition()
        {
            if (NoVibrationCompositionSupport) return;
            Composition = vibrationEffectClass.CallStatic<AndroidJavaObject>("startComposition");
        }

        public bool AddPrimitive(CompositionPrimitive compositionPrimitive)
        {
            if (Composition == null)
            {
                Log($"The composition is null");
                return false;
            }
            if (CompositionEffectSupport[compositionPrimitive.CompositionEffect] == false)
            {
                Log($"The composition primitive of {nameof(compositionPrimitive.CompositionEffect)} is not supported by this device.");
                return false;
            }

            // https://developer.android.com/reference/android/os/VibrationEffect.Composition#public-methods_1
            if (compositionPrimitive.Scale < 0) // use default scale
                Composition.Call<AndroidJavaObject>(addPrimitiveMethod, compositionPrimitive.CompositionEffectID);
            else if (compositionPrimitive.Delay == 0) // no delay
                Composition.Call<AndroidJavaObject>(addPrimitiveMethod, compositionPrimitive.CompositionEffectID, compositionPrimitive.Scale);
            else
                Composition.Call<AndroidJavaObject>(addPrimitiveMethod, compositionPrimitive.CompositionEffectID, compositionPrimitive.Scale, compositionPrimitive.Delay);

            return true;
        }

        public void Dispose()
        {
            Composition.Dispose();
            Composition = null;
        }
    }

    public class VibrationEffect : IDisposable
    {
        private const string createOneShotMethod = "createOneShot",
            createWaveformMethod = "createWaveform";

        public AndroidJavaObject Effect { get; private set; }

        public VibrationEffect(long milliseconds, int amplitude = Amplitude.Default)
        {
            if (NoVibrationEffectSupport) return;
            if (amplitude == Amplitude.None || amplitude < Amplitude.Default)
            {
                Log($"The amplitude of {amplitude} will trigger no vibration.");
                return;
            }

            if (amplitude > Amplitude.Max)
            {
                amplitude = Amplitude.Max;
            }
            else if (amplitude != Amplitude.Max && NoAmplitudeControlSupport) {} // can trigger a debug log
            Effect = vibrationEffectClass.CallStatic<AndroidJavaObject>(createOneShotMethod, milliseconds, amplitude);
        }

        public VibrationEffect(long[] pattern, int[] amplitudes = null, int repeatIndex = -1)
        {
            if (NoVibrationEffectSupport) return;
            if (ValidatePattern(pattern, amplitudes, repeatIndex) == false) return;
            if (amplitudes != null && NoAmplitudeControlSupport)
            {
                amplitudes = null;
            }
            if (amplitudes == null)
            {
                Effect = vibrationEffectClass.CallStatic<AndroidJavaObject>(createWaveformMethod, pattern, repeatIndex);
                return;
            }
            // validate the amplitude values
            for (int i = 0; i < amplitudes.Length; i++)
            {
                int amplitude = amplitudes[i];
                amplitudes[i] = amplitude switch
                {
                    < Amplitude.Default => Amplitude.None,
                    > Amplitude.Max => Amplitude.Max,
                    _ => amplitude
                };
            }
            Effect = vibrationEffectClass.CallStatic<AndroidJavaObject>(createWaveformMethod, pattern, amplitudes, repeatIndex);
        }

        public VibrationEffect(PredefinedEffects predefinedEffect)
        {
            if (NoPredefinedEffectSupport) return;
            if (PredefinedEffectSupport[predefinedEffect] == PredefinedEffectSupports.NO)
            {
                Log($"{logNoSupport}the {logPredefinedEffect} of {nameof(predefinedEffect)}");
                return;
            }
            if (PredefinedEffectSupport[predefinedEffect] == PredefinedEffectSupports.UNKNOWN)
            {
                Log($"This device has unknown support for the {logPredefinedEffect} of {nameof(predefinedEffect)}");
            }
            Effect = vibrationEffectClass.CallStatic<AndroidJavaObject>("createPredefined", (int)predefinedEffect);
        }

        public VibrationEffect(VibrationComposition vibrationComposition)
        {
            if (NoVibrationCompositionSupport) return;
            if (vibrationComposition == null || vibrationComposition.Composition == null)
            {
                Log("The vibration composition or the encapulated composition is null");
                return;
            }
            ComposeComposition(vibrationComposition);
        }

        public VibrationEffect(CompositionEffects[] compositionEffects, float[] scales = null, int[] delays = null)
        {
            if (NoVibrationCompositionSupport) return;

            if (compositionEffects.Length == 0)
            {
                Log($"The length of compositionEffects \'{compositionEffects.Length}\' is 0");
                return;
            }
            if (scales != null && compositionEffects.Length != scales.Length)
            {
                Log($"The length of compositionEffects \'{compositionEffects.Length}\' does not equal the length of scales \'{scales.Length}\'");
                return;
            }
            if (delays != null && compositionEffects.Length != delays.Length)
            {
                Log($"The length of compositionEffects \'{compositionEffects.Length}\' does not equal the length of delays \'{delays.Length}\'");
                return;
            }

            using VibrationComposition composition = new();
            for (int i = 0; i < compositionEffects.Length; i++)
            {
                CompositionPrimitive compositionPrimitive;
                if (scales == null)
                    compositionPrimitive = new(compositionEffects[i]);
                else if (delays == null)
                    compositionPrimitive = new(compositionEffects[i], scales[i]);
                else
                    compositionPrimitive = new(compositionEffects[i], scales[i], delays[i]);

                if (!composition.AddPrimitive(compositionPrimitive)) return;
            }

            ComposeComposition(composition);
        }

        public void Vibrate()
        {
            VibrateEffect(this);
        }

        public void Dispose()
        {
            Effect.Dispose();
            Effect = null;
        }

        private void ComposeComposition(VibrationComposition composition)
        {
            Effect = composition.Composition.Call<AndroidJavaObject>("compose");
        }
    }
}
