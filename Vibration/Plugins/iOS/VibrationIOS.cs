////////////////////////////////////////////////////////////////////////////////
//
// @author Benoît Freslon @benoitfreslon
// https://github.com/BenoitFreslon/Vibration
// https://benoitfreslon.com
//
////////////////////////////////////////////////////////////////////////////////

#if UNITY_IOS
using System.Runtime.InteropServices;
#endif
using UnityEngine;
using static GoodVibrations.VibrationLogging;

namespace GoodVibrations
{
    public static class VibrationiOS
    {
        public enum ImpactFeedbackStyle
        {
            Heavy,
            Medium,
            Light,
            Rigid,
            Soft
        }

        public enum NotificationFeedbackStyle
        {
            Error,
            Success,
            Warning
        }

#if UNITY_IOS
        [DllImport("__Internal")]
        private static extern bool _HasVibrator();

        [DllImport("__Internal")]
        private static extern void _Vibrate();

        [DllImport("__Internal")]
        private static extern void _VibratePop();

        [DllImport("__Internal")]
        private static extern void _VibratePeek();

        [DllImport("__Internal")]
        private static extern void _VibrateNope();

        [DllImport("__Internal")]
        private static extern void _impactOccurred(string style);

        [DllImport("__Internal")]
        private static extern void _notificationOccurred(string style);

        [DllImport("__Internal")]
        private static extern void _selectionChanged();
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

            if (Application.platform != RuntimePlatform.IPhonePlayer)
            {
                initialized = true;
#if !(UNITY_IOS || UNITY_EDITOR)
                Log("The application's platform is not iOS.", LogLevel.Error);
#endif
                return;
            }
#if UNITY_IOS
            CanVibrate = _HasVibrator();
#endif
            initialized = true;
        }

        public static bool Vibrate()
        {
            Log($"{nameof(Vibrate)} called");
            if (NoVibrationSupport) return false;
#if UNITY_IOS
            _Vibrate();
            return true;
#else
            return false;
#endif
        }

        public static bool VibratePop()
        {
            Log($"{nameof(VibratePop)} called");
            if (NoVibrationSupport) return false;
#if UNITY_IOS
            _VibratePop();
            return true;
#else
            return false;
#endif
        }

        public static bool VibratePeek()
        {
            Log($"{nameof(VibratePeek)} called");
            if (NoVibrationSupport) return false;
#if UNITY_IOS
            _VibratePeek();
            return true;
#else
            return false;
#endif
        }

        public static bool VibrateNope()
        {
            Log($"{nameof(VibrateNope)} called");
            if (NoVibrationSupport) return false;
#if UNITY_IOS
            _VibrateNope();
            return true;
#else
            return false;
#endif
        }

        public static bool VibrateImpact(ImpactFeedbackStyle style)
        {
            Log($"{nameof(VibrateImpact)} called with the given {nameof(style)}: {style}");
            if (NoVibrationSupport) return false;
#if UNITY_IOS
            _impactOccurred(nameof(style));
            return true;
#else
            return false;
#endif
        }

        public static bool VibrateNotification(NotificationFeedbackStyle style)
        {
            Log($"{nameof(VibrateNotification)} called with the given {nameof(style)}: {style}");
            if (NoVibrationSupport) return false;
#if UNITY_IOS
            _notificationOccurred(nameof(style));
            return true;
#else
            return false;
#endif
        }

        public static bool VibrateSelectionChanged()
        {
            Log($"{nameof(VibrateSelectionChanged)} called");
            if (NoVibrationSupport) return false;
#if UNITY_IOS
            _selectionChanged();
            return true;
#else
            return false;
#endif
        }
    }
}
