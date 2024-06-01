////////////////////////////////////////////////////////////////////////////////
//
// @author Benoît Freslon @benoitfreslon
// https://github.com/BenoitFreslon/Vibration
// https://benoitfreslon.com
//
////////////////////////////////////////////////////////////////////////////////

using System;
using UnityEngine;
using static GoodVibrations.VibrationLogging;
using AndroidVibration;

namespace GoodVibrations
{
    public static class Vibration
    {
#if UNITY_ANDROID
        [Obsolete("This variable is always null now.", true)]
        public static AndroidJavaClass unityPlayer, vibrationEffect;
        [Obsolete("This variable is always null now.", true)]
        public static AndroidJavaObject currentActivity, vibrator, context;
#endif

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
        public static void Init()
        {
            if (initialized) return;
            //CanVibrate = SystemInfo.supportsVibration;

#if UNITY_EDITOR
            DebugLogLevel = LogLevel.All;
            VibrationiOS.Init();
            AndroidVibration.Vibration.Init();
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
        public static bool Vibrate()
        {
            Log($"{nameof(Vibrate)} called");
            if (NoVibrator) return false;
#if UNITY_ANDROID || UNITY_IOS
            Handheld.Vibrate();
            return true;
#elif UNITY_WEBGL
            return VibrationWebGL.Vibrate(200);
#endif
        }

        ///<summary>
        /// Tiny pop vibration
        ///</summary>
        public static bool VibratePop()
        {
            Log($"{nameof(VibratePop)} called");
            if (NoVibrator) return false;
#if UNITY_IOS
            return VibrationiOS.VibratePop();
#elif UNITY_ANDROID
            return AndroidVibration.Vibration.Vibrate(50);
#elif UNITY_WEBGL
            return VibrationWebGL.Vibrate(50);
#endif
        }

        ///<summary>
        /// Small peek vibration
        ///</summary>
        public static bool VibratePeek()
        {
            Log($"{nameof(VibratePeek)} called");
            if (NoVibrator) return false;
#if UNITY_IOS
            return VibrationiOS.VibratePeek();
#elif UNITY_ANDROID
            return AndroidVibration.Vibration.Vibrate(100);
#elif UNITY_WEBGL
            return VibrationWebGL.Vibrate(100);
#endif
        }

        ///<summary>
        /// 3 small vibrations
        ///</summary>
        public static bool VibrateNope()
        {
            Log($"{nameof(VibrateNope)} called");
            if (NoVibrator) return false;
#if UNITY_IOS
            return VibrationiOS.VibrateNope();
#elif UNITY_ANDROID
            return AndroidVibration.Vibration.VibratePattern(new long[] { 0, 50, 100, 50, 100, 50 });
#elif UNITY_WEBGL
            return VibrationWebGL.VibratePattern(new int[] { 50, 100, 50, 100, 50 });
#endif
        }

        /// <summary>
        /// Vibrate for a given duration
        /// </summary>
        /// <param name="duration">Alternating periods in milliseconds in which the device vibration is On-Off-On...</param>
        public static bool Vibrate(int duration)
        {
            Log($"{nameof(Vibrate)} called with a {nameof(duration)}: {duration}");
            if (NoVibrator) return false;
#if UNITY_IOS
            Log("iOS does not support vibration durations", LogLevel.Error);
            return false;
#elif UNITY_ANDROID
            return AndroidVibration.Vibration.Vibrate(duration);
#elif UNITY_WEBGL
            return VibrationWebGL.Vibrate(duration);
#endif
        }

        /// <summary>
        /// Vibrate a pattern of On-Off durations
        /// </summary>
        /// <param name="pattern">Alternating periods in milliseconds in which the device vibration is On-Off-On...</param>
        public static bool VibratePattern(int[] pattern)
        {
            if (LoggingAllEnabled)
                Log($"{nameof(VibratePattern)} called with the given {nameof(pattern)}: [{string.Join(", ", pattern)}]");
            if (NoVibrator) return false;
#if UNITY_IOS
            Log("iOS does not support vibration patterns", LogLevel.Error);
            return false;
#elif UNITY_ANDROID
            // android takes longs and its first element is an off duration
            long[] longs = new long[pattern.Length + 1];
            longs[0] = 0;
            for (int i = 0; i < pattern.Length; i++)
                longs[i + 1] = pattern[i];
            return AndroidVibration.Vibration.VibratePattern(longs);
#elif UNITY_WEBGL
            return VibrationWebGL.VibratePattern(pattern);
#endif
        }

        /// <summary>
        /// Cancel the playback of any current vibration taking place on the device.
        /// </summary>
        /// <returns>Whether the cancel could be done, not if it was successful.</returns>
        public static bool VibrateCancel()
        {
            Log($"{nameof(VibratePop)} called");
            if (NoVibrator) return false;
#if UNITY_IOS
            Log("iOS does not support vibration Canceling", LogLevel.Error);
            return false;
#elif UNITY_ANDROID
            return AndroidVibration.Vibration.VibrateCancel();
#elif UNITY_WEBGL
            return VibrationWebGL.VibrateCancel();
#endif
        }

        [Obsolete("This method is obsolete. Call VibrationiOS.VibrateImpact() instead.")]
        public static void VibrateIOS(ImpactFeedbackStyle style)
        {
            VibrationiOS.VibrateImpact((VibrationiOS.ImpactFeedbackStyle)style);
        }

        [Obsolete("This method is obsolete. Call VibrationiOS.VibrateNotification() instead.")]
        public static void VibrateIOS(NotificationFeedbackStyle style)
        {
            VibrationiOS.VibrateNotification((VibrationiOS.NotificationFeedbackStyle)style);
        }

        [Obsolete("This method is obsolete. Call VibrationiOS.VibrateSelectionChanged() instead.")]
        public static void VibrateIOS_SelectionChanged()
        {
            VibrationiOS.VibrateSelectionChanged();
        }

#if UNITY_ANDROID
        [Obsolete("This method is obsolete. Call VibrationAndroid.Vibrate() instead.")]
        public static void VibrateAndroid(long milliseconds)
        {
            AndroidVibration.Vibration.Vibrate(milliseconds);
        }

        [Obsolete("This method is obsolete. Call VibrationAndroid.VibratePattern() instead.")]
        public static void VibrateAndroid(long[] pattern, int repeat)
        {
            AndroidVibration.Vibration.VibratePattern(pattern, repeatIndex: repeat);
        }
#endif

        [Obsolete("This method is obsolete. Call VibrationAndroid.CancelVibration() instead.")]
        public static void CancelAndroid()
        {
            AndroidVibration.Vibration.VibrateCancel();
        }

        [Obsolete("This method is obsolete. Use the property CanVibrate instead.")]
        public static bool HasVibrator()
        {
            return CanVibrate;
        }

        [Obsolete("This property is obsolete. Use the property VibrationAndroid.AndroidVersion instead.")]
        public static int AndroidVersion
        {
            get => AndroidVibration.Vibration.AndroidVersion;
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
}
