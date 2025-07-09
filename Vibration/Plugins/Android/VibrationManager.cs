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

    /// <summary>
    /// Manages Android Vibration services and contains shared helper functions for them.<see cref="Vibrator"/>s.
    /// </summary>
    public static class VibrationManager
    {
        /// <summary>
        /// AKA API version
        /// </summary>
        public static int AndroidVersion { get; private set; }
        /// <summary>
        /// If the hardware device have a vibrator
        /// </summary>
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

        /// <summary>
        /// Initializes all the Android services needed for Vibration.
        /// </summary>
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
                foreach (int id in vibratorIDs)
                {
                    vibrators[id] = DefaultVibrator.id == id ?
                        DefaultVibrator : VibratorManager.GetVibrator(id);
                }
                Vibrators = Array.AsReadOnly(vibrators);
            }

            CompleteInitialization();
        }

        // TODO
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

        /// <summary>
        /// Creates a default dictionary to use when there you can't get support from the device.
        /// </summary>
        /// <typeparam name="TKey">The Enums you want to turn into a dictionary.</typeparam>
        /// <typeparam name="TValue">The type of value you want for dictionary keys to be.</typeparam>
        /// <param name="value">The default value all keys will be assigned.</param>
        /// <returns>A dictionary with all the Enums assigned the same value.</returns>
        internal static Dictionary<TKey, TValue> CreateDefaultSupportDictionary<TKey, TValue>(TValue value) where TKey : Enum
        {
            Array keys = Enum.GetValues(typeof(TKey));
            Dictionary<TKey, TValue> support = new(keys.Length);
            foreach (TKey key in keys)
                support[key] = value; // some keys share the same constant, thanks android
            return support;
        }

        /// <summary>
        /// Creates a dictionary that reports whether each Enum value is supported by the device by comparing the Enums API support level to the device's reported API level.
        /// </summary>
        internal static Dictionary<TKey, bool> CreateSupportDictionary<TKey>(Dictionary<TKey, int> apiSupport) where TKey : Enum
        {
            Dictionary<TKey, bool> support = new(apiSupport.Count);
            foreach (var item in apiSupport)
                support.Add(item.Key, item.Value <= AndroidVersion);
            return support;
        }
        
        /// <summary>
        /// Creates a Dictionary of &lt;TKey, TValue&gt; by calling methodName on Android with TKey and parsing it's TReturn into TValue.
        /// </summary>
        /// <typeparam name="TKey">The Enum that will be converted into integers and used for methodName.</typeparam>
        /// <typeparam name="TValue">The desired dictionary's value type.</typeparam>
        /// <typeparam name="TReturn">The value type that the Android Java methodName you are calling will return as an array.</typeparam>
        /// <param name="methodName">The Android Java method you want to call.</param>
        internal static Dictionary<TKey, TValue> GetSupportDictionary<TKey, TValue, TReturn>(string methodName) where TKey : Enum
        {
            // create arrays of each effect and their corresponding IDs
            Array effectsArray = Enum.GetValues(typeof(TKey));
            int[] ids = effectsArray.Cast<int>().ToArray();

            // get support for each effect
            TValue[] supportResult = null;
            if (typeof(TValue) == typeof(TReturn))
            {
                supportResult = DefaultVibrator.VibratorObject.Call<TValue[]>(methodName, ids);
            }
            else
            {
                TReturn[] result = DefaultVibrator.VibratorObject.Call<TReturn[]>(methodName, ids);
                if (result != null)
                    supportResult = result.Select(x => (TValue)Enum.ToObject(typeof(TValue), x)).ToArray();
            }

            if (supportResult == null || supportResult.Length != ids.Length)
            {
                Debug.LogError($"The returned support result from the Android device from the " +
                    $"method name of \"{methodName}\" did not match the length of it's input.");

                supportResult = new TValue[effectsArray.Length];
                TValue defaultResult = (TValue)Enum.ToObject(typeof(TValue), 0);
                Array.Fill(supportResult, defaultResult);
            }

            TKey[] effects = effectsArray as TKey[];
            Dictionary<TKey, TValue> support = new(effects.Length);
            for (int i = 0; i < effects.Length; i++)
                support.Add(effects[i], supportResult[i]);
            return support;
        }

        /// <summary>
        /// Makes sure that the given pattern not null or empty, the amplitudes length matches up, and the repeat index is within bounds.
        /// </summary>
        internal static bool ValidatePattern(long[] pattern, int[] amplitudes, int repeatIndex)
        {
            if (pattern == null || pattern.Length == 0)
            {
                Log($"The given {nameof(pattern)} was null or empty.", LogLevel.Error);
                return false;
            }
            if (amplitudes != null && amplitudes.Length != pattern.Length)
            {
                Log($"The length of {nameof(pattern)} \'{pattern.Length}\' does not equal the length of {nameof(amplitudes)} \'{amplitudes.Length}\'.", LogLevel.Error);
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
            {
                DefaultVibrator = new Vibrator(null);
                Vibrators = Array.AsReadOnly(new Vibrator[] { });
            }
            else
            {
                Vibrators ??= Array.AsReadOnly(new Vibrator[] { DefaultVibrator });
            }
        }
    }
}
