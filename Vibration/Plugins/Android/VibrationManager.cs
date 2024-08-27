// Thanks to ruzrobert for inspiration! https://gist.github.com/ruzrobert/d98220a3b7f71ccc90403e041967c46b

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;
using static Vibes.Logging;

namespace Vibes.Android
{
    // https://developer.android.com/reference/android/os/Vibrator#constants_1
    public enum SupportStatus
    {
        UNKNOWN = 0,
        YES = 1,
        NO = 2
    }

    public static class VibrationManager
    {
        /// <summary>
        /// a.k.a. API version
        /// </summary>
        public static int AndroidVersion { get; private set; }
        public static bool CanVibrate { get; private set; }
        public static Vibrator DefaultVibrator { get; private set; }
        public static ReadOnlyCollection<Vibrator> Vibrators { get; private set; }

        internal static bool NoVibrationSupport
        {
            get
            {
                if (!CanVibrate)
                    Log("This device has no support for Vibration", LogLevel.Error);
                return !CanVibrate;
            }
        }

        internal static AndroidJavaObject currentActivity;

        private const string vibrateMethod = "vibrate";
        private static bool initialized = false;

        public static void Init()
        {
            if (initialized) return;

            if (Application.platform != RuntimePlatform.Android)
            {
#if !(UNITY_ANDROID || UNITY_EDITOR)
                Log("The application's platform is not Android.", LogLevel.Error);
#endif
                CompleteInitialization();
                return;
            }

            using AndroidJavaClass androidVersionClass = new("android.os.Build$VERSION");
            // TODO: catch unity device simulator
            AndroidVersion = androidVersionClass.GetStatic<int>("SDK_INT");

            using AndroidJavaClass unityPlayer = new("com.unity3d.player.UnityPlayer");
            currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

            if (currentActivity != null)
            {
                Log("Unable to get the current Android activity for vibration usage.", LogLevel.Error);
                CompleteInitialization();
                return;
            }

            if (AndroidVersion >= 31)
            {
                VibratorManager.Init();
                DefaultVibrator = VibratorManager.GetDefaultVibrator();
            }
            else
            {
                DefaultVibrator = new Vibrator(currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator"));
            }

            CanVibrate = DefaultVibrator.HasVibrator();
            if (CanVibrate == false)
            {
                // time to dispose of everything that we've determined can't be used
                currentActivity?.Dispose();
                currentActivity = null;
                VibratorManager.Dispose();
                DefaultVibrator?.Dispose();
                DefaultVibrator = null;
                CompleteInitialization();
                return;
            }

            if (AndroidVersion >= 31)
            {
                // there is api support for multiple vibrators, but i haven't seen any discussion about real world use of this
                int[] vibratorIDs = VibratorManager.GetVibratorIds();
                Vibrator[] vibrators = new Vibrator[vibratorIDs.Length];
                for (int i = 0; i < vibratorIDs.Length; i++)
                {
                    vibrators[i] = DefaultVibrator.id == vibratorIDs[i] ?
                        DefaultVibrator : VibratorManager.GetVibrator(vibratorIDs[i]);
                }
                Vibrators = Array.AsReadOnly(vibrators);
            }

            CompleteInitialization();
        }

        /// <summary>
        /// Attempts to Vibrate for the given milliseconds, with amplitude (if available).
        /// </summary>
        /// <param name="milliseconds">Duration of the vibration in milliseconds.</param>
        /// <param name="amplitude">If -1, amplitude is set to the device's default. Otherwise, values between 1-255 will be used. Check SupportsAmplitudeControl for availability.</param>
        /// <param name="cancel">Do you want to cancel any current vibrations taking place before this effect is played?</param>
        /// <returns>Whether the vibration could be played, not if it was successful in playing.</returns>
        public static bool Vibrate(long milliseconds, int amplitude = VibrationEffect.Amplitude.Default, bool cancel = false)
        {
            Log($"{nameof(Vibrate)} called with {nameof(milliseconds)}: {milliseconds}, {nameof(amplitude)}: {amplitude}, {nameof(cancel)}: {cancel}");
            
            if (cancel && VibrateCancel() == false) return false;

            if (!VibrationEffect.Supported)
#pragma warning disable CS0618 // Type or member is obsolete
                return DefaultVibrator.Vibrate(milliseconds);
#pragma warning restore CS0618 // Type or member is obsolete

            using VibrationEffect effect = new(milliseconds, amplitude);
            return DefaultVibrator.Vibrate(effect);
        }

        /// <summary>
        /// Attempts to vibrate the given pattern, with amplitudes (if available), and an optional repeat setting.
        /// </summary>
        /// <param name="pattern">Pattern of durations, with format Off-On-Off-On...</param>
        /// <param name="amplitudes">Amplitudes can be Null (for default) or array of exactly Pattern length with values of 
        /// either -1 (device default) or 0 - 255. Values that are < -1 and 0 will not cause vibrations. Check SupportsAmplitudeControl for availability.</param>
        /// <param name="repeatIndex">If -1, no repeat. Otherwise, repeat from given nth index in Pattern.</param>
        /// <param name="cancel">Do you want to cancel any current vibrations taking place before this effect is played?</param>
        /// <returns>Whether the vibration pattern could be played, not if it was successful in playing.</returns>
        public static bool VibratePattern(long[] pattern, int[] amplitudes = null, int repeatIndex = -1, bool cancel = false)
        {
            if (LoggingAllEnabled) // don't want to call string.join if avoidable
            {
                Log($"{nameof(VibratePattern)} called with {nameof(pattern)}: [{string.Join(", ", pattern)}]\n" +
                    $"{nameof(amplitudes)}: [{string.Join(", ", amplitudes)}]\n" +
                    $"{nameof(repeatIndex)}: {repeatIndex}, {nameof(cancel)}: {cancel}");
            }

            if (cancel && VibrateCancel() == false) return false;

            if (!VibrationEffect.Supported)
#pragma warning disable CS0618 // Type or member is obsolete
                return DefaultVibrator.Vibrate(pattern, repeatIndex);
#pragma warning restore CS0618 // Type or member is obsolete

            using VibrationEffect effect = new(pattern, amplitudes, repeatIndex);
            if (effect.IsEmpty) return false;
            return DefaultVibrator.Vibrate(effect);
        }

        /// <summary>
        /// Attempts to create the Predefined Effect and vibrate it. Available from API Level >= 29.
        /// </summary>
        /// <param name="predefined">Support for each predefined effect will vary by device.</param>
        /// <param name="cancel">Do you want to cancel any current vibrations taking place before this effect is played?</param>
        /// <returns>Whether the vibration effect could be played, not if it was successful in playing.</returns>
        public static bool VibratePredefined(VibrationEffect.Predefined predefined, bool cancel = false)
        {
            Log($"{nameof(VibratePredefined)} called with {nameof(predefined)}: {predefined}");
            if (cancel && VibrateCancel() == false) return false;
            using VibrationEffect effect = new(predefined);
            return DefaultVibrator.Vibrate(effect);
        }

        /// <summary>
        /// Attempts to create the Composition Effect and vibrate it. Available from API Level >= 30.
        /// </summary>
        /// <param name="primitives">The primitives you want to add to the composition. If a primitive is not supported the entire compostion fails.</param>
        /// <param name="scales">The scales to apply to the intensity of the primitive. Either null or -1s for default, or between 0f and 1f inclusive.</param>
        /// <param name="delays">The amounts of time (ms) to wait before playing the next primitive. Either null or values 0 or greater.</param>
        /// <param name="cancel">Do you want to cancel any current vibrations taking place before this effect is played?</param>
        /// <returns>Whether the vibration effect could be played, not if it was successful in playing.</returns>
        public static bool VibrateComposition(VibrationComposition.Primitives[] primitives, float[] scales = null, int[] delays = null, bool cancel = false)
        {
            if (LoggingAllEnabled) // don't want to call string.join if avoidable
            {
                Log($"{nameof(VibrateComposition)} called with {nameof(primitives)}: [{string.Join(", ", primitives)}]\n" +
                    $"{nameof(scales)}: [{string.Join(", ", scales)}]\n" +
                    $"{nameof(delays)}: [{string.Join(", ", delays)}]\n" +
                    $"{nameof(cancel)}: {cancel}");
            }

            if (cancel && VibrateCancel() == false) return false;
            using VibrationEffect effect = new(primitives, scales, delays);
            return DefaultVibrator.Vibrate(effect);
        }

        /// <summary>
        /// Attempts to vibrate the given effect.
        /// </summary>
        /// <param name="effect">The effect you want to vibrate.</param>
        /// <param name="cancel">Do you want to cancel any current vibrations taking place before this effect is played?</param>
        /// <returns>Whether the vibration effect could be played, not if it was successful in playing.</returns>
        public static bool VibrateEffect(VibrationEffect effect, VibrationAttributes attribute = null, bool cancel = false)
        {
            Log($"{nameof(VibrateEffect)} called with {nameof(cancel)}: {cancel}");
            
            if (cancel && VibrateCancel() == false) return false;

            if (attribute != null)
                return DefaultVibrator.Vibrate(effect, attribute);
            else
                return DefaultVibrator.Vibrate(effect);
        }

        public static bool VibrateCombinedEffect(CombinedVibration combinedVibration, VibrationAttributes attributes = null, bool cancel = false)
        {
            Log($"{nameof(VibrateCombinedEffect)} called with {nameof(cancel)}: {cancel}");
            if (cancel && VibrateCancel() == false) return false;
            VibratorManager.Vibrate(combinedVibration, attributes);
            return true;
        }

        /// <summary>
        /// Cancel the playback of any current vibration taking place on the device.
        /// </summary>
        /// <returns>Whether the cancel could be done, not if it was successful.</returns>
        public static bool VibrateCancel()
        {
            Log($"{nameof(VibrateCancel)} called");
            if (NoVibrationSupport) return false;
            DefaultVibrator.Cancel();
            return true;
        }

        public static TEnum[] ConvertToEnumArray<TEnum>(int[] values) where TEnum : Enum
        {
            TEnum[] enumArray = new TEnum[values.Length];

            for (int i = 0; i < values.Length; i++)
            {
                if (Enum.IsDefined(typeof(TEnum), values[i]))
                    enumArray[i] = (TEnum)Enum.ToObject(typeof(TEnum), values[i]);
                else
                    throw new ArgumentException($"Value {values[i]} is not defined in enum type {typeof(TEnum)}.");
            }

            return enumArray;
        }

        public static void LogSupportToDebug()
        {
            Debug.Log(
                $"{nameof(AndroidVersion)}: {AndroidVersion}\n" +
                $"{nameof(CanVibrate)}: {CanVibrate}\n" +
                $"{nameof(HapticFeedback)} : {HapticFeedback.Supported}\n" +
                $"{nameof(HapticFeedback.HapticStatus)} {HapticFeedback.HapticStatus}\n" +
                $"{nameof(VibrationEffect)} : {VibrationEffect.Supported}\n" +
                $"{nameof(VibrationEffect.SupportsPredefined)} : {VibrationEffect.SupportsPredefined}\n" +
                $"{nameof(VibrationEffect.SupportsAmplitudeControl)}: {VibrationEffect.SupportsAmplitudeControl}\n" +
                $"{nameof(VibrationComposition)}: {VibrationComposition.Supported}\n" +
                $"{nameof(VibratorManager)}: {VibratorManager.Supported}\n" +
                $"{nameof(VibrationAttributes)}: {VibrationAttributes.Supported}\n" +
                $"{nameof(CombinedVibration)}: {CombinedVibration.Supported}");

            Debug.Log($"Printing contents of {nameof(HapticFeedback.HapticSupport)}");
            PrintSupportDictionary(HapticFeedback.HapticSupport);
            Debug.Log($"Printing contents of {nameof(VibrationEffect.PredefinedSupport)}");
            PrintSupportDictionary(VibrationEffect.PredefinedSupport);
            Debug.Log($"Printing contents of {nameof(VibrationComposition.PrimitiveSupport)}");
            PrintSupportDictionary(VibrationComposition.PrimitiveSupport);
        }

        internal static Dictionary<TKey, TValue> CreateDefaultSupportDictionary<TKey, TValue>(TValue value) where TKey : Enum
        {
            Array keys = Enum.GetValues(typeof(TKey));
            Dictionary<TKey, TValue> support = new(keys.Length);
            foreach (TKey key in keys)
                support.Add(key, value);
            return support;
        }

        internal static Dictionary<TKey, bool> CreateSupportDictionary<TKey>(Dictionary<TKey, int> apiSupport) where TKey : Enum
        {
            Dictionary<TKey, bool> support = new(apiSupport.Count);
            foreach (var item in apiSupport)
                support.Add(item.Key, item.Value <= AndroidVersion);
            return support;
        }

        /// <summary>
        /// Creates a Dictionary of <TKey, TValue> by calling methodName with TKey and parsing it's TReturn into TValue
        /// </summary>
        /// <typeparam name="TKey">The Enum that will be converted into integers and used for methodName.</typeparam>
        /// <typeparam name="TValue">The desired dictionary's value.</typeparam>
        /// <typeparam name="TReturn">The type of value the Android Java methodName you are calling will return as an array.</typeparam>
        /// <param name="methodName">The Android Java method you want to call.</param>
        internal static Dictionary<TKey, TValue> GetSupportDictionary<TKey, TValue, TReturn>(string methodName) where TKey : Enum
        {
            // create arrays of each effect and their corresponding IDs
            Array effectsArray = Enum.GetValues(typeof(TKey));
            TKey[] effects = effectsArray as TKey[];
            int[] ids = effectsArray.Cast<int>().ToArray();

            // get support for each effect
            TValue[] supportResult;
            if (typeof(TValue) == typeof(TReturn))
            {
                supportResult = DefaultVibrator.VibratorObject.Call<TValue[]>(methodName, ids);
            }
            else
            {
                TReturn[] result = DefaultVibrator.VibratorObject.Call<TReturn[]>(methodName, ids);
                supportResult = result.Select(x => (TValue)Enum.ToObject(typeof(TValue), x)).ToArray();
            }

            Dictionary<TKey, TValue> support = new(effects.Length);
            for (int i = 0; i < effects.Length; i++)
                support.Add(effects[i], supportResult[i]);
            return support;
        }

        internal static bool ValidatePattern(long[] pattern, int[] amplitudes, int repeatIndex)
        {
            if (pattern.Length == 0 || (amplitudes != null && amplitudes.Length != pattern.Length))
            {
                Log($"The length of {nameof(pattern)} \'{pattern.Length}\' is 0, or does not equal the length of {nameof(amplitudes)} \'{amplitudes.Length}\'.", LogLevel.Error);
                return false;
            }
            if (repeatIndex < -1 || repeatIndex >= pattern.Length)
            {
                Log($"The {nameof(repeatIndex)} of \'{repeatIndex}\' is not valid for the length of {nameof(pattern)} \'{pattern.Length}\'.", LogLevel.Error);
                return false;
            }
            return true;
        }

        private static void PrintSupportDictionary<TKey, TValue>(ReadOnlyDictionary<TKey, TValue> support)
        {
            var lines = support.Select(kvp => kvp.Key.ToString() + ": " + kvp.Value.ToString());
            Debug.Log(string.Join(Environment.NewLine, lines));
        }

        /// <summary>
        /// Marks the class as initialized and sets up the support dictionaries to their default values if they haven't been assigned yet.
        /// </summary>
        private static void CompleteInitialization()
        {
            initialized = true;
            HapticFeedback.Init();
            VibrationEffect.Init();
            VibrationComposition.Init();
            VibrationAttributes.Init();
            CombinedVibration.Init();

            if (DefaultVibrator == null)
                Vibrators = Array.AsReadOnly(new Vibrator[] { });
            else
                Vibrators ??= Array.AsReadOnly(new Vibrator[] { DefaultVibrator });
        }
    }

    public interface ISupported
    {
        public static bool Supported { get; }
        public bool IsEmpty { get; }
    }
}
