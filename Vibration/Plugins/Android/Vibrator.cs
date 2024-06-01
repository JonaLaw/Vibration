using System;
using System.Linq;
using UnityEngine;
using static GoodVibrations.VibrationLogging;
using static AndroidVibration.Vibration;

namespace AndroidVibration
{
    /// <summary>
    /// A class that operates a designated vibrator on the device. 
    /// </summary>
    public class Vibrator : ISupported //, IDisposable
    {
        // the api requirement for this is 1
        public static bool Supported => CanVibrate;

        public readonly int id;
        internal AndroidJavaObject VibratorObject { get; private set; }
        public bool IsEmpty => VibratorObject == null;

        internal Vibrator(AndroidJavaObject vibratorObject)
        {
            VibratorObject = vibratorObject;
            id = AndroidVersion < 31 ? 0 : vibratorObject.Call<int>("getId");
        }

        /// <summary>
        /// Query whether the vibrator natively supports the given effects.
        /// </summary>
        /// <returns>Null if no vibration can happen, or if the method is not supported, or if the input was null</returns>
        public SupportStatus[] AreEffectsSupported(VibrationEffect.Predefined[] predefinedEffects)
        {
            if (NoVibrationSupport) return null;
            if (AndroidVersion < 30)
            {
                Log($"Support for the method {nameof(AreEffectsSupported)} begins with API version 30.");
                return null;
            }
            if (predefinedEffects == null)
            {
                Log($"The given {nameof(predefinedEffects)} was null.", LogLevel.Error);
                return null;
            }
            int[] ids = predefinedEffects.Select(x => (int)x).ToArray();
            int[] result = VibratorObject.Call<int[]>("areEffectsSupported", ids);
            return result.Select(x => (SupportStatus)x).ToArray();
        }

        /// <summary>
        /// Query whether the vibrator supports the given primitives.
        /// </summary>
        /// <returns>Null if no vibration can happen, or if the method is not supported, or if the input was null</returns>
        public bool[] ArePrimitivesSupported(VibrationComposition.Primitives[] primitives)
        {
            if (NoVibrationSupport) return null;
            if (AndroidVersion < 30)
            {
                Log($"Support for the method {nameof(ArePrimitivesSupported)} begins with API version 30.");
                return null;
            }
            if (primitives == null)
            {
                Log($"The given {nameof(primitives)} was null.", LogLevel.Error);
                return null;
            }
            int[] ids = primitives.Select(x => (int)x).ToArray();
            return VibratorObject.Call<bool[]>("arePrimitivesSupported", ids);
        }

        /// <summary>
        /// Turn the vibrator off.
        /// </summary>
        /// <returns>Whether cancel could be called, not if it was successful.</returns>
        public bool Cancel()
        {
            if (NoVibrationSupport) return false;
            VibratorObject.Call("cancel");
            return true;
        }

        /// <summary>
        /// Query the estimated durations of the given primitives.
        /// </summary>
        /// <returns>The value at a given index will contain the duration in milliseconds of the effect at the same index in the querying array.
        /// The duration will be positive for primitives that are supported and zero for the unsupported ones, in correspondence with ArePrimitivesSupported().</returns>
        public int[] GetPrimitiveDurations(VibrationComposition.Primitives[] primitives)
        {
            if (NoVibrationSupport) return null;
            if (AndroidVersion < 31)
            {
                Log($"Support for the method {nameof(GetPrimitiveDurations)} begins with API version 31.");
                return null;
            }
            if (primitives == null)
            {
                Log($"The given {nameof(primitives)} was null.", LogLevel.Error);
                return null;
            }
            int[] ids = primitives.Select(x => (int)x).ToArray();
            return VibratorObject.Call<int[]>("getPrimitiveDurations", ids);
        }

        /// <summary>
        /// Gets the Q factor of the vibrator.
        /// </summary>
        /// <returns>The Q factor of the vibrator, or NaN if the method isn't supported, it's unknown, not applicable, 
        /// or if this vibrator is a composite of multiple physical devices with different Q factors.</returns>
        public float GetQFactor()
        {
            if (NoVibrationSupport) return float.NaN;
            if (AndroidVersion < 34)
            {
                Log($"Support for the method {nameof(GetQFactor)} begins with API version 34.");
                return float.NaN;
            }
            return VibratorObject.Call<float>("getQFactor");
        }

        /// <summary>
        /// Gets the resonant frequency of the vibrator, if applicable.
        /// </summary>
        /// <returns>The resonant frequency of the vibrator, or NaN if the method isn't supported, if it's unknown, 
        /// not applicable, or if this vibrator is a composite of multiple physical devices with different frequencies.</returns>
        public float GetResonantFrequency()
        {
            if (NoVibrationSupport) return float.NaN;
            if (AndroidVersion < 34)
            {
                Log($"Support for the method {nameof(GetResonantFrequency)} begins with API version 34.");
                return float.NaN;
            }
            return VibratorObject.Call<float>("getResonantFrequency");
        }

        /// <summary>
        /// Check whether the vibrator has amplitude control.
        /// </summary>
        /// <returns>True if the device reports that it has amplitude support, false if the device can't report its support or if it lacks support.</returns>
        public bool HasAmplitudeControl()
        {
            if (NoVibrationSupport) return false;
            if (AndroidVersion < 26)
            {
                Log($"Support for the method {nameof(HasAmplitudeControl)} begins with API version 26.");
                return false;
            }
            return VibratorObject.Call<bool>("hasAmplitudeControl");
        }

        /// <summary>
        /// Check whether the hardware has a vibrator.
        /// </summary>
        public bool HasVibrator()
        {
            if (AndroidVersion < 11) return false;
            return VibratorObject.Call<bool>("hasVibrator");
        }

        /// <summary>
        /// Vibrate constantly for the specified period of time. The app should be in the foreground for the vibration to happen.
        /// </summary>
        /// <returns>Whether the vibration could be played, not if it was successful in playing.</returns>
        [Obsolete("This method was deprecated in API level 26. Use Vibrate(VibrationEffect) instead.", false)]
        public bool Vibrate(long milliseconds)
        {
            if (NoVibrationSupport) return false;
            VibratorObject.Call("vibrate", milliseconds);
            return true;
        }

        /// <summary>
        /// Vibrate with a given effect. The app should be in the foreground for the vibration to happen.
        /// Background apps should specify a ringtone, notification or alarm usage in order to vibrate.
        /// </summary>
        /// <returns>Whether the vibration could be played, not if it was successful in playing.</returns>
        public bool Vibrate(VibrationEffect effect, VibrationAttributes attributes)
        {
            if (VibrationEffect.NoSupport) return false;
            if (VibrationAttributes.NoSupport)
            {
                return Vibrate(effect);
            }
            if (attributes == null || attributes.IsEmpty)
            {
                Log($"The given {nameof(attributes)} is null or it's empty", LogLevel.Error);
                return Vibrate(effect);
            }

            if (effect == null || effect.IsEmpty)
            {
                Log($"The given {nameof(effect)} is null or it's empty", LogLevel.Error);
                return false;
            }

            VibratorObject.Call("vibrate", effect.Effect, attributes.Attribute);
            return true;
        }

        /// <summary>
        /// Vibrate with a given pattern.
        /// </summary>
        /// <param name="pattern">Pattern of durations (ms), with format Off-On-Off-On...</param>
        /// <param name="repeatIndex">If -1, no repeat. Otherwise, repeat from given nth index in Pattern.</param>
        /// <returns>Whether the vibration could be played, not if it was successful in playing.</returns>
        [Obsolete("This method was deprecated in API level 26. Use Vibrate(VibrationEffect) instead.", false)]
        public bool Vibrate(long[] pattern, int repeatIndex = -1)
        {
            if (NoVibrationSupport) return false;
            if (ValidatePattern(pattern, null, repeatIndex) == false) return false;
            VibratorObject.Call("vibrate", pattern, repeatIndex);
            return true;
        }

        /// <summary>
        /// Vibrate with a given effect. The app should be in the foreground for the vibration to happen.
        /// <returns>Whether the vibration could be played, not if it was successful in playing.</returns>
        public bool Vibrate(VibrationEffect effect)
        {
            if (NoVibrationSupport || VibrationEffect.NoSupport) return false;
            if (effect == null || effect.IsEmpty)
            {
                Log($"The given {nameof(effect)} is null or it's empty", LogLevel.Error);
                return false;
            }
            VibratorObject.Call("vibrate", effect.Effect);
            return true;
        }

        // should not be public
        internal void Dispose()
        {
            VibratorObject?.Dispose();
            VibratorObject = null;
        }
    }
}
