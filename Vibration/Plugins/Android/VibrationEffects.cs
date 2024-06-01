using System;
using System.Collections.ObjectModel;
using UnityEngine;
using static GoodVibrations.VibrationLogging;
using static AndroidVibration.Vibration;

namespace AndroidVibration
{
    public class VibrationEffect : IDisposable, ISupported
    {
        public const int APIRequirement = 26;
        public const int predefinedAPIRequirement = 29;

        internal static AndroidJavaClass vibrationEffectClass;
        private const string createOneShotMethod = "createOneShot",
            createWaveformMethod = "createWaveform";

        public enum Predefined
        {
            CLICK = 0,
            DOUBLE_CLICK = 1,
            HEAVY_CLICK = 5,
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
                    Log("This device reports no support for Amplitude Control", LogLevel.Warning);
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
        /// Play an unsupported effect at your own risk.
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
        /// Attempts to create a Vibration Effect with the given inputs. Available from API Level >= 26.
        /// </summary>
        /// <param name="milliseconds">Duration of the vibration in milliseconds.</param>
        /// <param name="amplitude">If -1, amplitude is set to the device's default. Otherwise, values between 1-255 will be used. Check SupportsAmplitudeControl for availability</param>
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
        /// Attempts to create a Vibration Effect with the given inputs. Available from API Level >= 26.
        /// </summary>
        /// <param name="pattern">Pattern of durations, with format Off-On-Off-On...</param>
        /// <param name="amplitudes">Amplitudes can be Null (for default) or array of exactly Pattern length with values of 
        /// either -1 (device default) or 0 - 255. Values that are < -1 and 0 will not cause vibrations. Check SupportsAmplitudeControl for availability.</param>
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
        /// Attempts to create the Predefined Effect. Available from API Level >= 29.
        /// </summary>
        /// <param name="predefined">Support for each predefined effect will vary by device.</param>
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

        /// <summary>
        /// Attempts to Composes the Vibration Composition so that it can be played on demand. Available from API Level >= 30.
        /// </summary>
        /// <param name="vibrationComposition">If the composition is null or empty the Vibration effect will be empty too.</param>
        public VibrationEffect(VibrationComposition vibrationComposition)
        {
            if (VibrationComposition.NoSupport) return;
            if (vibrationComposition == null || vibrationComposition.IsEmpty)
            {
                Log($"The given {nameof(vibrationComposition)} is null or it's empty", LogLevel.Error);
                return;
            }
            ComposeComposition(vibrationComposition);
        }

        /// <summary>
        /// Attempts to create the Composition Effect. Available from API Level >= 30.
        /// </summary>
        /// <param name="primitives">The primitives you want to add to the composition. If a primitive is not supported the entire compostion fails.</param>
        /// <param name="scales">The scales to apply to the intensity of the primitive. Either null or -1s for default, or between 0f and 1f inclusive.</param>
        /// <param name="delays">The amounts of time (ms) to wait before playing the next primitive. Either null or values 0 or greater.</param>
        public VibrationEffect(VibrationComposition.Primitives[] primitives, float[] scales = null, int[] delays = null)
        {
            if (VibrationComposition.NoSupport) return;

            if (primitives.Length == 0)
            {
                Log($"The length of {nameof(primitives)} \'{primitives.Length}\' is 0", LogLevel.Error);
                return;
            }
            if (scales != null && primitives.Length != scales.Length)
            {
                Log($"The length of {nameof(primitives)} \'{primitives.Length}\' does not equal the length of {nameof(scales)} \'{scales.Length}\'", LogLevel.Error);
                return;
            }
            if (delays != null && primitives.Length != delays.Length)
            {
                Log($"The length of {nameof(primitives)} \'{primitives.Length}\' does not equal the length of {nameof(delays)} \'{delays.Length}\'", LogLevel.Error);
                return;
            }

            using VibrationComposition composition = new();
            for (int i = 0; i < primitives.Length; i++)
            {
                bool result;
                
                if (scales == null)
                    result = composition.AddPrimitive(primitives[i]);
                else if (delays == null)
                    result = composition.AddPrimitive(primitives[i], scales[i]);
                else
                    result = composition.AddPrimitive(primitives[i], scales[i], delays[i]);

                if (result == false) return;
            }

            ComposeComposition(composition);
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
