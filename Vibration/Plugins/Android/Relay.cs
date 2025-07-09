using static Vibes.Logging;
using static Vibes.Android.VibrationManager;

namespace Vibes.Android
{
    public static class Relay
    {
        /// <summary>
        /// Attempts to choose the correct Vibrate function to use based on device support.
        /// </summary>
        /// <param name="milliseconds">Duration of the vibration in milliseconds.</param>
        /// <param name="amplitude">If -1, amplitude is set to the device's default. Otherwise, values between 1-255 will be used.
        /// <para/>Check <see cref="VibrationEffect.SupportsAmplitudeControl"/> for availability.</param>
        /// <param name="cancel">Do you want to cancel any current vibrations taking place before this vibration is played?</param>
        /// <returns>Whether the vibration could be played, not if it was successful in playing.</returns>
        public static bool Vibrate(long milliseconds, int amplitude = VibrationEffect.Amplitude.Default, bool cancel = false)
        {
            if (NoVibrationSupport) return false;
            if (cancel) VibrateCancel();

            if (!VibrationEffect.Supported)
#pragma warning disable CS0618 // Type or member is obsolete
                return DefaultVibrator.Vibrate(milliseconds);
#pragma warning restore CS0618 // Type or member is obsolete

            using VibrationEffect effect = new(milliseconds, amplitude);
            return DefaultVibrator.Vibrate(effect);
        }

        /// <summary>
        /// Attempts to choose the correct Vibrate pattern function to use based on device support.
        /// </summary>
        /// <param name="pattern">Pattern of durations, with format Off-On-Off-On...</param>
        /// <param name="amplitudes">Amplitudes can be Null (for default) or array of exactly Pattern length with values of 
        /// either -1 (device default) or 0 - 255. Values that are < -1 and 0 will not cause vibrations. Check SupportsAmplitudeControl for availability.</param>
        /// <param name="repeatIndex">If -1, no repeat. Otherwise, repeat from given nth index in Pattern.</param>
        /// <param name="cancel">Do you want to cancel any current vibrations taking place before this effect is played?</param>
        /// <returns>Whether the vibration pattern could be played, not if it was successful in playing.</returns>
        public static bool VibratePattern(long[] pattern, int[] amplitudes = null, int repeatIndex = -1, bool cancel = false)
        {
            if (NoVibrationSupport) return false;
            if (cancel) VibrateCancel();

            if (!VibrationEffect.Supported)
#pragma warning disable CS0618 // Type or member is obsolete
                return DefaultVibrator.Vibrate(pattern, repeatIndex);
#pragma warning restore CS0618 // Type or member is obsolete

            using VibrationEffect effect = new(pattern, amplitudes, repeatIndex);
            return DefaultVibrator.Vibrate(effect);
        }

        /// <summary>
        /// Cancel the playback of any current vibration taking place on the device.
        /// </summary>
        /// <returns>Whether the cancel could be done, not if it was successful.</returns>
        public static bool VibrateCancel()
        {
            DefaultVibrator.Cancel();
            return true;
        }

        /// <summary>
        /// Attempts to create the Predefined Effect and vibrate it. Available from API Level >= 29.
        /// </summary>
        /// <param name="predefined">Support for each predefined effect will vary by device.</param>
        /// <param name="cancel">Do you want to cancel any current vibrations taking place before this effect is played?</param>
        /// <returns>Whether the vibration effect could be played, not if it was successful in playing.</returns>
        public static bool VibratePredefined(VibrationEffect.Predefined predefined, bool cancel = false)
        {
            if (cancel) VibrateCancel();
            using VibrationEffect effect = new(predefined);
            return DefaultVibrator.Vibrate(effect);
        }

        public static bool VibrateComposition(VibrationComposition.Primitives[] primitives, float[] scales = null, int[] delays = null, bool cancel = false)
        {
            if (cancel) VibrateCancel();
            using VibrationEffect effect = VibrationComposition.CreateEffect(primitives, scales, delays);
            return DefaultVibrator.Vibrate(effect);
        }

        public static bool VibrateEffect(VibrationEffect effect, VibrationAttributes attribute = null, bool cancel = false)
        {
            if (cancel) VibrateCancel();

            if (attribute != null)
                return DefaultVibrator.Vibrate(effect, attribute);
            else
                return DefaultVibrator.Vibrate(effect);
        }

        public static bool VibrateCombinedEffect(CombinedVibration combinedVibration, VibrationAttributes attributes = null, bool cancel = false)
        {
            if (cancel) VibrateCancel();
            VibratorManager.Vibrate(combinedVibration, attributes);
            return true;
        }

    }
}
