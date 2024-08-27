#if UNITY_WEBGL
using System.Runtime.InteropServices;
#endif
using UnityEngine;
using static Vibes.Logging;

namespace Vibes.WebGL
{
    public static class VibrationManager
    {
#if UNITY_WEBGL
        [DllImport("__Internal")]
        private static extern bool _HasVibrator();

        [DllImport("__Internal")]
        private static extern void _Vibrate(int milliseconds);

        [DllImport("__Internal")]
        private static extern void _VibratePattern(int[] pattern);

        [DllImport("__Internal")]
        private static extern void _VibrateCancel();
#endif

        public static bool CanVibrate { get; private set; }
        
        private static bool NoVibrationSupport
        {
            get
            {
                if (!CanVibrate)
                    Log("This device has no support for Vibration", LogLevel.Warning);
                return !CanVibrate;
            }
        }

        private static bool initialized = false;

        public static void Init()
        {
            if (initialized) return;

            if (Application.platform != RuntimePlatform.WebGLPlayer)
            {
                initialized = true;
#if !(UNITY_WEBGL || UNITY_EDITOR)
                Log($"The application's platform is not WebGL", LogLevel.Error);
#endif
                return;
            }
#if UNITY_WEBGL
            CanVibrate = _HasVibrator();
#endif
            initialized = true;
        }

        public static bool Vibrate(int milliseconds, bool cancel = false)
        {
            Log($"{nameof(Vibrate)} called with {nameof(milliseconds)}: {milliseconds}");
            if (cancel && VibrateCancel() == false) return false;
            if (NoVibrationSupport) return false;
#if UNITY_WEBGL
            _Vibrate(milliseconds);
            return true;
#else
            return false;
#endif
        }

        /// https://developer.mozilla.org/en-US/docs/Web/API/Vibration_API#vibration_patterns
        /// <param name="pattern">Alternating periods in milliseconds in which the device vibration is On-Off-On...</param>
        public static bool VibratePattern(int[] pattern, bool cancel = false)
        {
            if (LoggingAllEnabled)
            {
                Log($"{nameof(Vibrate)} called with {nameof(pattern)}: [{string.Join(", ", pattern)}]\n" +
                    $"{nameof(cancel)}: {cancel}");
            }
            if (cancel && VibrateCancel() == false) return false;
            if (NoVibrationSupport) return false;
#if UNITY_WEBGL
            _VibratePattern(pattern);
            return true;
#else
            return false;
#endif
        }

        /// https://developer.mozilla.org/en-US/docs/Web/API/Vibration_API#canceling_existing_vibrations
        public static bool VibrateCancel()
        {
            Log($"{nameof(VibrateCancel)} called");
            if (NoVibrationSupport) return false;
#if UNITY_WEBGL
            _VibrateCancel();
            return true;
#else
            return false;
#endif
        }
    }
}
