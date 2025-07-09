using System;
using System.Collections.ObjectModel;
using UnityEngine;
using static Vibes.Logging;
using static Vibes.Android.VibrationManager;

namespace Vibes.Android
{
    /// <summary>
    /// A VibrationEffect describes a haptic effect (not a <see cref="HapticFeedback"/>) to be performed by a <see cref="Vibrator"/>.
    /// These effects may be any number of things, from single shot vibrations to complex waveforms.
    /// <para/><inheritdoc cref="APIRequirement"/>
    /// <para/><see href="https://developer.android.com/reference/android/os/VibrationEffect">Android Docs</see>
    /// </summary>
    public class VibrationEffect : IDisposable
    {
        /// <summary>Available from <see cref="AndroidVersion">API Level</see> 26 and onwards.</summary>
        public const int APIRequirement = 26;
        /// <summary>Available from <see cref="AndroidVersion">API Level</see> 29 and onwards.</summary>
        public const int predefinedAPIRequirement = 29;

        internal static AndroidJavaClass vibrationEffectClass;
        private const string createOneShotMethod = "createOneShot",
            createWaveformMethod = "createWaveform";

        /// <summary>
        /// Common vibration effects that should be identical, regardless of the app they come from. Might be custom tailored to the device hardware to provide a cohesive experience.
        /// <para/>Warning: If a hardware-specific implementation of the effect doesn't exist, these will either fallback to a generic pattern, or produce nothing at all.
        /// <para/><see href="https://developer.android.com/reference/android/os/VibrationEffect#constants_1">Android Docs</see>
        /// </summary>
        public enum Predefined
        {
            ///<summary>A click effect. Use this effect as a baseline, as it's the most common type of click effect.</summary>
            CLICK = 0,
            ///<summary>A double click effect.</summary>
            DOUBLE_CLICK = 1,
            ///<summary>A heavy click effect. This effect is stronger than CLICK.</summary>
            HEAVY_CLICK = 5,
            ///<summary>A tick effect. This effect is less strong compared to CLICK.</summary>
            TICK = 2
        }

        public readonly struct Amplitude
        {
            public const int None = 0;
            public const int Default = -1;
            public const int Max = 255;
        }

        public static bool Supported { get; private set; }

        public static bool SupportsAmplitudeControl { get; private set; }

        public static bool SupportsPredefined { get; private set; }

        internal static bool NotSupported
        {
            get
            {
                if (!Supported)
                    Log("This device has no support for Vibration Effects", LogLevel.Error);
                return !Supported;
            }
        }

        internal static bool NoAmplitudeSupport
        {
            get
            {
                if (!SupportsAmplitudeControl)
                    Log("This device has/reports no support for Amplitude Control", LogLevel.Warning);
                return !SupportsAmplitudeControl;
            }
        }

        internal static bool NoPredefinedSupport
        {
            get
            {
                if (!SupportsPredefined)
                    Log("This device has no support for Predefined Effects", LogLevel.Error);
                return !SupportsPredefined;
            }
        }

        /// <summary>
        /// The device's reported support for each <see cref="Predefined"/> effect.
        /// <para/>Note: If the device reports <see cref="SupportStatus.NO"/> or <see cref="SupportStatus.UNKNOWN"/> for a predefined effect, it may still play a simpler fallback vibration.
        /// <para/><see href="https://developer.android.com/reference/android/os/Vibrator#areEffectsSupported(int[])">Android Docs</see>
        /// </summary>
        public static ReadOnlyDictionary<Predefined, SupportStatus> PredefinedSupport { get; private set; }

        public AndroidJavaObject Effect { get; private set; }
        public bool IsEmpty => Effect == null;

        // TODO
        public VibrationAttributes Attributes { get; set; }

        internal static void Init()
        {
            Supported = AndroidVersion >= APIRequirement && CanVibrate;
            if (!Supported)
            {
                PredefinedSupport = new(CreateDefaultSupportDictionary<Predefined, SupportStatus>(SupportStatus.NO));
                return;
            }

            vibrationEffectClass = new AndroidJavaClass("android.os.VibrationEffect");
            SupportsAmplitudeControl = DefaultVibrator.VibratorObject.Call<bool>("hasAmplitudeControl");

            SupportsPredefined = AndroidVersion >= predefinedAPIRequirement;
            if (!SupportsPredefined)
            {
                PredefinedSupport = new(CreateDefaultSupportDictionary<Predefined, SupportStatus>(SupportStatus.NO));
                return;
            }

            // checking for each predefined support is gated behind an api level, so mark them all as unknown if we can't check
            PredefinedSupport = AndroidVersion >= 31 ?
                new(GetSupportDictionary<Predefined, SupportStatus, int>("areEffectsSupported")) :
                new(CreateDefaultSupportDictionary<Predefined, SupportStatus>(SupportStatus.UNKNOWN));
        }

        /// <summary>
        /// Attempts to create a one shot vibration effect that will vibrate constantly for the specified period of time at the optional specified amplitude, and then stop.
        /// <para/><see href="https://developer.android.com/reference/android/os/VibrationEffect#createOneShot(long,%20int)">Android Docs</see>
        /// </summary>
        /// <param name="milliseconds">Duration of the vibration in milliseconds.</param>
        /// <param name="amplitude">If -1, amplitude is set to the device's default. Otherwise, values between 1-255 will be used. An amplitude of 0 results in no vibration so it will be ignored.
        /// <br/>Check <see cref="SupportsAmplitudeControl"/> for availability.</param>
        public VibrationEffect(long milliseconds, int amplitude = Amplitude.Default)
        {
            if (NotSupported) return;
            if (amplitude == Amplitude.None || amplitude < Amplitude.Default)
            {
                Log($"The given {nameof(amplitude)} of {amplitude} will trigger no vibration.", LogLevel.Warning);
                return;
            }

            if (amplitude == Amplitude.Default)
            { } // do nothing
            else if (amplitude >= Amplitude.Max)
                amplitude = Amplitude.Max;
            else if (NoAmplitudeSupport)
                amplitude = Amplitude.Default;
            // TODO: check if default is equal to max when there's no amp support
            // TODO: determine what to do in a multi-vibrator situation as each vibrator might support different things
            Effect = vibrationEffectClass.CallStatic<AndroidJavaObject>(createOneShotMethod, milliseconds, amplitude);
        }

        /// <summary>
        /// Attempts to create a waveform vibration effect, a potentially repeating series of timing and optional amplitude pairs.
        /// <para/><see href="https://developer.android.com/reference/android/os/VibrationEffect#createWaveform(long[],%20int[],%20int)">Android Docs</see>
        /// </summary>
        /// <param name="pattern">Pattern of durations, with format Off-On-Off-On...</param>
        /// <param name="amplitudes">Amplitudes can be Null (for default) or array of exactly pattern length with values of either -1 (device default) or 0 - 255.
        /// <br/>Check <see cref="SupportsAmplitudeControl"/> for support status.
        /// <para/>Note: Values that are not -1 will be clamped between 0 and 255. An amplitude of 0 results in no vibration.</param>
        /// <param name="repeatIndex">If -1, no repeat. Otherwise, repeat from given nth index in pattern.</param>
        public VibrationEffect(long[] pattern, int[] amplitudes = null, int repeatIndex = -1)
        {
            if (NoVibrationSupport || NotSupported) return;
            if (ValidatePattern(pattern, amplitudes, repeatIndex) == false) return;
            
            if (amplitudes != null && NoAmplitudeSupport)
            {
                // TODO: determine what to do in a multi-vibrator situation as each vibrator might support different things
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
                if (amplitudes[i] == Amplitude.Default) continue;
                amplitudes[i] = Math.Clamp(amplitudes[i], Amplitude.None, Amplitude.Max);
            }
            Effect = vibrationEffectClass.CallStatic<AndroidJavaObject>(createWaveformMethod, pattern, amplitudes, repeatIndex);
        }

        /// <summary>
        /// Attempts to create a predefined vibration effect. Predefined effects are a set of common vibration effects that should be identical on the device, regardless of the app they come from.
        /// <para/>Warning: If a hardware-specific implementation of the effect doesn't exist, these will either fallback to a generic pattern, or produce nothing at all.
        /// <para/><inheritdoc cref="predefinedAPIRequirement"/>
        /// <para/><see href="https://developer.android.com/reference/android/os/VibrationEffect#createPredefined(int)">Android Docs</see>
        /// </summary>
        /// <param name="predefined">Check <see cref="PredefinedSupport"/> for each effect's reported support.</param>
        public VibrationEffect(Predefined predefined)
        {
            if (NoPredefinedSupport) return;

            if (PredefinedSupport[predefined] != SupportStatus.YES)
            {
                Log($"This device reports the {nameof(SupportStatus)} of {PredefinedSupport[predefined]} " +
                    $"for the given {nameof(predefined)} of {predefined}, but it will still be tried.", LogLevel.Warning);
            }

            Effect = vibrationEffectClass.CallStatic<AndroidJavaObject>("createPredefined", (int)predefined);
        }

        internal VibrationEffect(AndroidJavaObject effect)
        {
            if (NotSupported) return;
            if (effect == null)
            {
                Log($"The given {nameof(effect)} is null.", LogLevel.Error);
                return;
            }
            Effect = effect;
        }

        public void Dispose()
        {
            Effect?.Dispose();
            Effect = null;
        }
    }
}
