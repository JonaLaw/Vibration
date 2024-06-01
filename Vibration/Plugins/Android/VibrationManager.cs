using UnityEngine;
using static GoodVibrations.VibrationLogging;
using static AndroidVibration.Vibration;

namespace AndroidVibration
{
    /// <summary>
    /// Provides access to all <see cref="Vibrator">Vibrators</see> from the device, as well as the ability to run them in a synchronized fashion.
    /// <para/><see href="https://developer.android.com/reference/android/os/VibratorManager">Android Docs</see>
    /// </summary>
    public static class VibratorManager
    {
        public const int APIRequirement = 31;

        public static bool Supported { get; private set; }

        private static AndroidJavaObject managerObject;

        internal static bool NotSupported
        {
            get
            {
                if (!Supported)
                    Log($"{nameof(VibratorManager)} is not supported below API version 31.", LogLevel.Error);
                return !Supported;
            }
        }

        internal static void Init()
        {
            Supported = AndroidVersion >= APIRequirement && CanVibrate;
            if (!Supported) return;
            managerObject = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator_manager");
        }

        /// <summary>
        /// Turn all the vibrators off.
        /// <para/><see href="https://developer.android.com/reference/android/os/VibratorManager#cancel()">Android Docs</see>
        /// </summary>
        /// <returns>True if could complete call, false if not.</returns>
        public static bool Cancel()
        {
            if (NotSupported) return false;
            managerObject.Call("cancel");
            return true;
        }

        /// <summary>
        /// Returns the default Vibrator for the device.
        /// <para/><see href="https://developer.android.com/reference/android/os/VibratorManager#getDefaultVibrator()">Android Docs</see>
        /// </summary>
        /// <returns>The default Vibrator if could complete call, null if not.</returns>
        public static Vibrator GetDefaultVibrator()
        {
            if (NotSupported) return null;
            return new Vibrator(managerObject.Call<AndroidJavaObject>("getDefaultVibrator"));
        }

        /// <summary>
        /// Retrieve a single vibrator by id.
        /// <para/><see href="https://developer.android.com/reference/android/os/VibratorManager#getVibrator(int)">Android Docs</see>
        /// </summary>
        /// <returns>Corresponding Vibrator if could complete call, null if not.</returns>
        public static Vibrator GetVibrator(int id)
        {
            if (NotSupported) return null;
            return new Vibrator(managerObject.Call<AndroidJavaObject>("getVibrator", id));
        }

        /// <summary>
        /// List all available vibrator ids, returning a possible empty list.
        /// <para/><see href="https://developer.android.com/reference/android/os/VibratorManager#getVibratorIds()">Android Docs</see>
        /// </summary>
        /// <returns>An array of Vibrator ids if could complete call, null if not.</returns>
        public static int[] GetVibratorIds()
        {
            if (NotSupported) return null;
            return managerObject.Call<int[]>("getVibratorIds");
        }

        /// <summary>
        /// Vibrate with a given combination of effects.<br/>
        /// The app should be in foreground for the vibration to happen.
        /// Background apps should specify a ringtone, notification or alarm usage in order to vibrate.
        /// <para/><see href="https://developer.android.com/reference/android/os/VibratorManager#vibrate(android.os.CombinedVibration)">Android Docs</see>
        /// </summary>
        /// <param name="combinedVibration">A combination of VibrationEffects to be played on one or more vibrators.</param>
        /// <param name="attributes"></param>
        /// <returns>True if could complete call, false if not.</returns>
        public static bool Vibrate(CombinedVibration combinedVibration, VibrationAttributes attributes = null)
        {
            if (NotSupported) return false;
            if (combinedVibration == null || combinedVibration.IsEmpty)
            {
                Log($"The given {nameof(combinedVibration)} is null or it's empty.", LogLevel.Error);
                return false;
            }
            if (attributes != null && attributes.IsEmpty)
            {
                Log($"The given {nameof(attributes)} is empty.", LogLevel.Warning);
                attributes = null;
            }

            if (attributes == null)
                managerObject.Call("vibrate", combinedVibration.Combination);
            else
                managerObject.Call("vibrate", combinedVibration.Combination, attributes.Attribute);
            return true;
        }

        internal static void Dispose()
        {
            managerObject?.Dispose();
            managerObject = null;
        }
    }
}
