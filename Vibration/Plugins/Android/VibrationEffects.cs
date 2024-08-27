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
    /// <para/><see href="https://developer.android.com/reference/android/os/VibrationEffect">Android Docs</see>
    /// </summary>
    public class VibrationEffect : IDisposable, ISupported
    {
        public const int APIRequirement = 26;
        public const int predefinedAPIRequirement = 29;

        internal static AndroidJavaClass vibrationEffectClass;
        private const string createOneShotMethod = "createOneShot",
            createWaveformMethod = "createWaveform";

        /// <summary>
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

        internal static bool NoSupport
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
        /// Note: Even if the device reports no support for a predefined effect, it may still play a fallback vibration.
        /// </summary>
        public static ReadOnlyDictionary<Predefined, SupportStatus> PredefinedSupport { get; private set; }

        public AndroidJavaObject Effect { get; private set; }
        public bool IsEmpty => Effect == null;

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
        /// <br/>Available from API Level >= 26.
        /// <para/><see href="https://developer.android.com/reference/android/os/VibrationEffect#createOneShot(long,%20int)">Android Docs</see>
        /// </summary>
        /// <param name="milliseconds">Duration of the vibration in milliseconds.</param>
        /// <param name="amplitude">If -1, amplitude is set to the device's default. Otherwise, values between 1-255 will be used.
        /// <br/>Check SupportsAmplitudeControl for availability.</param>
        public VibrationEffect(long milliseconds, int amplitude = Amplitude.Default)
        {
            if (NoSupport) return;
            if (amplitude == Amplitude.None || amplitude < Amplitude.Default)
            {
                Log($"The given {nameof(amplitude)} of {amplitude} will trigger no vibration.", LogLevel.Warning);
                return;
            }

            if (amplitude > Amplitude.Max)
                amplitude = Amplitude.Max;
            else if (amplitude != Amplitude.Max && amplitude != Amplitude.Default && NoAmplitudeSupport)
            { } // check if the amplitude was set to something not supported, this triggers a debug log

            // TODO: determine what to do in a multi-vibrator situation as each vibrator might support different things
            if (SupportsAmplitudeControl)
                Effect = vibrationEffectClass.CallStatic<AndroidJavaObject>(createOneShotMethod, milliseconds, amplitude);
            else
                Effect = vibrationEffectClass.CallStatic<AndroidJavaObject>(createOneShotMethod, milliseconds);
        }

        /// <summary>
        /// Attempts to create a waveform vibration effect, a potentially repeating series of timing and optional amplitude pairs.
        /// <br/>Available from API Level >= 26.
        /// <para/><see href="https://developer.android.com/reference/android/os/VibrationEffect#createWaveform(long[],%20int[],%20int)">Android Docs</see>
        /// </summary>
        /// <param name="pattern">Pattern of durations, with format Off-On-Off-On...</param>
        /// <param name="amplitudes">Amplitudes can be Null (for default) or array of exactly pattern length with values of either -1 (device default) or 0 - 255.
        /// <br/>Values that are less than -1 or equal to 0 will not cause vibrations.
        /// <br/>Check SupportsAmplitudeControl for availability.</param>
        /// <param name="repeatIndex">If -1, no repeat. Otherwise, repeat from given nth index in Pattern.</param>
        public VibrationEffect(long[] pattern, int[] amplitudes = null, int repeatIndex = -1)
        {
            if (NoVibrationSupport || NoSupport) return;
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

        /// <summary>
        /// Attempts to create a predefined vibration effect. Predefined effect are a set of common vibration effects that should be identical, regardless of the app they come from.
        /// <br/>This can fallback to a generic pattern if there does not exist a hardware-specific implementation of the effect.
        /// <br/>Available from API Level >= 29.
        /// <para/><see href="https://developer.android.com/reference/android/os/VibrationEffect#createPredefined(int)">Android Docs</see>
        /// </summary>
        /// <param name="predefined">Support for each predefined effect will vary by device.
        /// <br/>Check <see cref="PredefinedSupport"/> for each effect's reported support.</param>
        public VibrationEffect(Predefined predefined)
        {
            Log($"{nameof(VibrationEffect)} called with {nameof(predefined)}: {predefined}");
            if (NoPredefinedSupport) return;

            if (PredefinedSupport[predefined] == SupportStatus.NO)
                Log($"This device reports no support for the given predefined effect of {predefined}, but it will still be tried.", LogLevel.Warning);
            else if (PredefinedSupport[predefined] == SupportStatus.UNKNOWN)
                Log($"This device has the {nameof(SupportStatus)} of {SupportStatus.UNKNOWN} for the {nameof(predefined)} of {predefined}", LogLevel.Warning);

            Effect = vibrationEffectClass.CallStatic<AndroidJavaObject>("createPredefined", (int)predefined);
        }

        internal VibrationEffect(AndroidJavaObject effect)
        {
            if (NoSupport) return;
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
