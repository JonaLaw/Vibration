using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using static GoodVibrations.VibrationLogging;
using static AndroidVibration.Vibration;

namespace AndroidVibration
{
    /// <summary>
    /// A static class that controls all haptics occurring on the app's Android view.
    /// <para/><see href="https://developer.android.com/develop/ui/views/haptics/haptic-feedback">Android Docs Discussion</see>
    /// </summary>
    public static class HapticFeedback
    {
        public const int APIRequirement = 3;
        private const string hapticFeedbackMethod = "performHapticFeedback";
        private static AndroidJavaObject mUnityPlayer;

        public static bool Supported { get; private set; }

        private static bool NoSupport
        {
            get
            {
                if (!Supported)
                    Log("This device has no support for Haptics Effects", LogLevel.Warning);
                return !Supported;
            }
        }

        /// <summary>
        /// Constants to be used to perform haptic feedback effects.
        /// Check the property <see cref="HapticSupport">AttributesSupport</see> for the support of each Haptic.
        /// <para/><see href="https://developer.android.com/reference/android/view/HapticFeedbackConstants">Android Docs</see>
        /// </summary>
        public enum Haptic
        {
            NO_HAPTICS = -1, // 34
            LONG_PRESS = 0, // 3 API support
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

        /// <summary>
        /// Optional flags for <see cref="Haptic">Haptics</see> to override certain Android restrictions. Support for IGNORE_GLOBAL_SETTING is not availible after API 32.
        /// <para/><see href="https://developer.android.com/reference/android/view/HapticFeedbackConstants">Android Docs</see>
        /// </summary>
        public enum Flag
        {
            NONE = 0, // this ID is not used by android
            IGNORE_GLOBAL_SETTING = 2, // 3, depricated in 33
            IGNORE_VIEW_SETTING = 1, // 3
        }

        /// <summary>
        /// The support status of each <see cref="Haptic">Haptic</see> as determined by API support and device vibration support.
        /// Warning: A device's own support varies and there's no way to determine if full support exists.
        /// </summary>
        public static ReadOnlyDictionary<Haptic, bool> HapticSupport { get; private set; }

        /// <summary>
        /// What the user has currently set vibration to in their device settings.
        /// It can possibly change mid app usage. You can try to update this value by calling CheckHapticFeedbackChange().
        /// Warning: From API 33+ we can't access this setting, and as a result it'll be marked as UNKNOWN from 33+. 
        /// It's best to treat it as disabled if you want to manage vibrations yourself.
        /// </summary>
        public static SupportStatus HapticStatus { get; private set; } = SupportStatus.NO;

        private static readonly Dictionary<Haptic, int> hapticFeedbackAPISupport = new()
        {
            { Haptic.NO_HAPTICS, 34 },
            { Haptic.LONG_PRESS, 3 },
            { Haptic.VIRTUAL_KEY, 5 },
            { Haptic.KEYBOARD_TAP, 8 },
            { Haptic.CLOCK_TICK, 21 },
            { Haptic.CONTEXT_CLICK, 23 },
            { Haptic.KEYBOARD_RELEASE, 27 },
            { Haptic.VIRTUAL_KEY_RELEASE, 27 },
            { Haptic.TEXT_HANDLE_MOVE, 27 },
            { Haptic.GESTURE_START, 30 },
            { Haptic.GESTURE_END, 30 },
            { Haptic.CONFIRM, 30 },
            { Haptic.REJECT, 30 },
            { Haptic.TOGGLE_ON, 34 },
            { Haptic.TOGGLE_OFF, 34 },
            { Haptic.GESTURE_THRESHOLD_ACTIVATE, 34 },
            { Haptic.GESTURE_THRESHOLD_DEACTIVATE, 34 },
            { Haptic.DRAG_START, 34 },
            { Haptic.SEGMENT_TICK, 34 },
            { Haptic.SEGMENT_FREQUENT_TICK, 34 },
        };

        internal static void Init()
        {
            Supported = AndroidVersion >= APIRequirement && CanVibrate;
            if (!Supported)
            {
                HapticSupport = new(CreateDefaultSupportDictionary<Haptic, bool>(false));
                return;
            }
            CheckHapticFeedbackChange();
            mUnityPlayer = currentActivity.Get<AndroidJavaObject>("mUnityPlayer");
            HapticSupport = new(CreateSupportDictionary(hapticFeedbackAPISupport));
            //hapticFeedback = new AndroidJavaObject("com.vibration.hapticfeedbacklibrary.AndroidPlugin", currentActivity);
        }

        /// <summary>
        /// Trys to check the current haptic feedback user setting and sets the HapticFeedbackStatus to it.
        /// <para/><see href="https://developer.android.com/reference/android/provider/Settings.System#HAPTIC_FEEDBACK_ENABLED">Android Docs</see>
        /// </summary>
        /// <returns>What HapticFeedbackStatus has been set to.</returns>
        public static SupportStatus CheckHapticFeedbackChange()
        {
            SupportStatus oldHapticStatus = HapticStatus;
            if (NoSupport) return HapticStatus; // by default it's set to: No
            if (AndroidVersion >= 33)
            {
                // According to the Android docs: "User settings are applied automatically by the service and should not be applied by individual apps."
                // So we can't tell if haptics are enabled and we can't change it, best to treat it as disabled if you want to manage things yourself
                HapticStatus = SupportStatus.UNKNOWN;
                Log($"The Android API version is {AndroidVersion} which doesn't allow for apps to control or see haptic feedback settings. " +
                    $"From API 33+ {nameof(HapticStatus)} will always report as {nameof(SupportStatus.UNKNOWN)}.", LogLevel.Warning);
            }
            else
            {
                using AndroidJavaClass settings = new("android.provider.Settings");
                using AndroidJavaObject contentResolver = currentActivity.Call<AndroidJavaObject>("getContentResolver");
                int result = settings.CallStatic<int>("getInt", contentResolver, "haptic_feedback_enabled", 0);
                HapticStatus = (result == 1 ? SupportStatus.YES : SupportStatus.NO);
                Log($"{nameof(HapticStatus)} has been set from {oldHapticStatus} to {HapticStatus}");
            }
            return HapticStatus;
        }

        /// <summary>
        /// Gets or sets the view's haptic feedback setting. Setting this to false will disable all haptic feedback calls on the view, including default ones.
        /// Will not override a user's system settings.
        /// <para/><see href="https://developer.android.com/reference/android/view/View#attr_android:hapticFeedbackEnabled">Android Docs</see>
        /// </summary>
        public static bool HapticFeedbackEnabled
        {
            get
            {
                if (NoSupport) return false;
                return mUnityPlayer.Get<bool>("hapticFeedbackEnabled");
            }
            set
            {
                if (NoSupport) return;
                mUnityPlayer.Set("hapticFeedbackEnabled", value);
            }
        }

        /// <summary>
        /// Attempts to vibrate the given haptic feedback, with an optional flag.
        /// <para/><see href="https://developer.android.com/reference/android/view/View#performHapticFeedback(int)">Android Docs</see>
        /// </summary>
        /// <param name="haptic">Note that each haptic feedback has an API level requirement and you can check support using the property HapticSupport.</param>
        /// <param name="flag">An optional flag to override certain Android focusing restrictions. Support for IGNORE_GLOBAL_SETTING is not availible after API 32.</param>
        /// <param name="cancel">Do you want to cancel any current vibrations taking place before this effect is played?</param>
        /// <returns>Whether the haptic feedback could be played.</returns>
        public static bool Vibrate(Haptic haptic, Flag flag = Flag.NONE, bool cancel = false)
        {
            Log($"{nameof(Vibrate)} called with {nameof(haptic)}: {haptic}, {nameof(flag)}: {flag}, and {nameof(cancel)}: {cancel}");

            if (cancel && VibrateCancel() == false) return false;
            if (!Supported)
            {
                Log("This device has no support for Haptics Effects", LogLevel.Warning);
                return false;
            }

            if (!HapticSupport[haptic])
            {
                Log($"This device has no support for the given {nameof(haptic)}: {haptic}", LogLevel.Error);
                return false;
            }

            if (flag == Flag.IGNORE_GLOBAL_SETTING && AndroidVersion >= 33)
            {
                Log($"This device has no support for the given {nameof(Flag)} of {flag} as its API is 33+," +
                    $" it will be set to the default of {Flag.NONE}", LogLevel.Warning);
                flag = Flag.NONE;
            }

            bool result;
            if (flag == Flag.NONE)
                result = mUnityPlayer.Call<bool>(hapticFeedbackMethod, (int)haptic);
            else
                result = mUnityPlayer.Call<bool>(hapticFeedbackMethod, (int)haptic, (int)flag);
            Log($"{nameof(Vibrate)} finished with the result of: {result}");
            return result;
        }
    }
}
