using System;
using System.Collections.ObjectModel;
using UnityEngine;
using static Vibes.Logging;
using static Vibes.Android.VibrationManager;

namespace Vibes.Android
{
    /// <summary>
    /// A combination of <see cref="Primitives"/> that are combined to be playable as a single <see cref="VibrationEffect"/>.
    /// <para/><see href="https://developer.android.com/reference/android/os/VibrationEffect.Composition">Android Docs</see>
    /// </summary>
    public class VibrationComposition : IDisposable, ISupported
    {
        public const int APIRequirement = 30;
        private const string addPrimitiveMethod = "addPrimitive";

        public static bool Supported { get; private set; }

        internal static bool NoSupport
        {
            get
            {
                if (!Supported)
                    Log("This device has no support for Vibration Effect Composition", LogLevel.Error);
                return !Supported;
            }
        }

        /// <summary>
        /// Haptics used to create the Vibration Effect. Check <see cref="PrimitiveSupport"/> for the support of each Primitive.
        /// <para/><see href="https://developer.android.com/reference/android/os/VibrationEffect.Composition#constants_1">Android Docs</see>
        /// </summary>
        public enum Primitives
        {
            CLICK = 1, // 30 API support
            LOW_TICK = 8, // 31
            QUICK_FALL = 6, // 30
            QUICK_RISE = 4, // 30
            SLOW_RISE = 5, // 30
            SPIN = 3, // 31
            THUD = 2, // 31
            TICK = 7 // 30
        }

        /// <summary>
        /// Support for each primitive as determined by API support level and reported device support.
        /// </summary>
        public static ReadOnlyDictionary<Primitives, bool> PrimitiveSupport { get; private set; }

        private AndroidJavaObject composition;
        public bool IsEmpty => composition == null;

        internal static void Init()
        {
            Supported = AndroidVersion >= APIRequirement && CanVibrate;
            if (!Supported)
            {
                PrimitiveSupport = new(CreateDefaultSupportDictionary<Primitives, bool>(false));
                return;
            }

            // https://developer.android.com/reference/android/os/Vibrator#arePrimitivesSupported(int[])
            PrimitiveSupport = new(GetSupportDictionary<Primitives, bool, bool>("arePrimitivesSupported"));
        }

        // https://developer.android.com/reference/android/os/VibrationEffect#startComposition()
        public VibrationComposition()
        {
            if (NoSupport) return;
            composition = VibrationEffect.vibrationEffectClass.CallStatic<AndroidJavaObject>("startComposition");
        }

        /// <summary>
        /// Attempts to create and compose the composition effect using the given inputs.
        /// </summary>
        /// <param name="primitives">The primitives you want to add to the composition. If a primitive is not supported the entire compostion fails.</param>
        /// <param name="scales">The scales to apply to the intensity of the primitive. Either null or -1s for default, or between 0f and 1f inclusive.</param>
        /// <param name="delays">The amounts of time (ms) to wait before playing the next primitive. Either null or values 0 or greater.</param>
        public static VibrationEffect CreateEffect(Primitives[] primitives, float[] scales = null, int[] delays = null)
        {
            if (NoSupport) return null;
            if (primitives.Length == 0)
            {
                Log($"The length of {nameof(primitives)} \'{primitives.Length}\' is 0", LogLevel.Error);
                return null;
            }
            if (scales != null && primitives.Length != scales.Length)
            {
                Log($"The length of {nameof(primitives)} \'{primitives.Length}\' does not equal the length of {nameof(scales)} \'{scales.Length}\'", LogLevel.Error);
                return null;
            }
            if (delays != null && primitives.Length != delays.Length)
            {
                Log($"The length of {nameof(primitives)} \'{primitives.Length}\' does not equal the length of {nameof(delays)} \'{delays.Length}\'", LogLevel.Error);
                return null;
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

                if (result == false) return null;
            }

            return composition.Compose();
        }

        /// <summary>
        /// Add a primitive effect to the end of the current composition.
        /// <para/><see href="https://developer.android.com/reference/android/os/VibrationEffect.Composition#addPrimitive(int,%20float,%20int)">Android Docs</see>
        /// </summary>
        /// <param name="scale">The scale to apply to the intensity of the primitive. Value is between 0f and 1f inclusive, or -1 for device default.
        /// Greater than 1 will be clamped, while less than 0 will be ignored.</param>
        /// <param name="delay">The amount of time in milliseconds to wait before playing this primitive, starting when the previous primitive finished.
        /// Value is 0 or greater. Less than 0 will be clamped.</param>
        /// <returns>TODO</returns>
        public bool AddPrimitive(Primitives primitive, float scale = -1, int delay = 0)
        {
            if (NoSupport) return false;
            if (IsEmpty)
            {
                Log($"The {nameof(composition)} is empty.", LogLevel.Error);
                return false;
            }
            if (Enum.IsDefined(typeof(Primitives), primitive) == false)
            {
                Log($"The given {nameof(primitive)} of {primitive} is not valid.", LogLevel.Error);
                return false;
            }
            if (PrimitiveSupport[primitive] == false)
            {
                Log($"The given {nameof(primitive)} of {primitive} is reported as not supported by this device.", LogLevel.Error);
                // TODO: determine what to do in a multi-vibrator situation as each vibrator might support different things
                return false;
            }

            // scale can be -1 (default) or 0 to 1, can't find what the default value ends up being, anything less that 0 will be treated as -1
            if (scale > 1) scale = 1;
            if (delay < 0) delay = 0;

            // https://developer.android.com/reference/android/os/VibrationEffect.Composition#public-methods_1
            if (scale < 0) // use default scale, don't know what it might be
                composition.Call<AndroidJavaObject>(addPrimitiveMethod, (int)primitive);
            else if (delay <= 0) // no delay
                composition.Call<AndroidJavaObject>(addPrimitiveMethod, (int)primitive, scale);
            else
                composition.Call<AndroidJavaObject>(addPrimitiveMethod, (int)primitive, scale, delay);

            return true;
        }

        /// <summary>
        /// Compose all of the added primitives together into a single VibrationEffect.
        /// <para/><see href="https://developer.android.com/reference/android/os/VibrationEffect.Composition#compose()">Android Docs</see>
        /// </summary>
        public VibrationEffect Compose()
        {
            if (NoSupport) return null;
            if (IsEmpty)
            {
                Log($"The {nameof(composition)} is empty.", LogLevel.Error);
                return null;
            }
            AndroidJavaObject comp = composition.Call<AndroidJavaObject>("compose");
            return new VibrationEffect(comp);
        }

        public void Dispose()
        {
            composition?.Dispose();
            composition = null;
        }
    }
}
