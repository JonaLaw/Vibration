using System;
using System.Collections.ObjectModel;
using UnityEngine;
using static Vibes.Logging;
using static Vibes.Android.VibrationManager;

namespace Vibes.Android
{
    /// <summary>
    /// A combination of <see cref="Primitives"/> that are combined to be playable as a single <see cref="VibrationEffect"/>.
    /// <para/><inheritdoc cref="APIRequirement"/>
    /// <para/><see href="https://developer.android.com/reference/android/os/VibrationEffect.Composition">Android Docs</see>
    /// </summary>
    public class VibrationComposition : IDisposable
    {
        /// <summary>Available from <see cref="AndroidVersion">API Level</see> 30 and onwards.</summary>
        public const int APIRequirement = 30;
        private const string addPrimitiveMethod = "addPrimitive";

        public static bool Supported { get; private set; }

        internal static bool NotSupported
        {
            get
            {
                if (!Supported)
                    Log("This device has no support for Vibration Effect Composition", LogLevel.Error);
                return !Supported;
            }
        }

        /// <summary>
        /// Haptics used to create the <see cref="VibrationEffect"/>. Check <see cref="PrimitiveSupport"/> for the support of each Primitive.
        /// <para/><see href="https://developer.android.com/reference/android/os/VibrationEffect.Composition#constants_1">Android Docs</see>
        /// </summary>
        public enum Primitives
        {
            /// <summary>This effect should produce a sharp, crisp click sensation.</summary>
            CLICK = 1,
            /// <summary>This very short low frequency effect should produce a light crisp sensation intended to be used repetitively for dynamic feedback.</summary>
            LOW_TICK = 8,
            /// <summary>A haptic effect that simulates quick downwards movement with gravity.</summary>
            QUICK_FALL = 6,
            /// <summary>A haptic effect that simulates quick upward movement against gravity.</summary>
            QUICK_RISE = 4,
            /// <summary>A haptic effect that simulates slow upward movement against gravity.</summary>
            SLOW_RISE = 5,
            /// <summary>A haptic effect that simulates spinning momentum.</summary>
            SPIN = 3,
            /// <summary>A haptic effect that simulates downwards movement with gravity. Often followed by extra energy of hitting and reverberation to augment physicality.</summary>
            THUD = 2,
            /// <summary>This very short effect should produce a light crisp sensation intended to be used repetitively for dynamic feedback.</summary>
            TICK = 7
        }

        /// <summary>
        /// Support for each primitive as determined by API support level and reported device support.
        /// </summary>
        public static ReadOnlyDictionary<Primitives, bool> PrimitiveSupport { get; private set; }

        private AndroidJavaObject composition;
        public bool IsEmpty => composition == null;

        private bool NoComposition
        {
            get
            {
                if (IsEmpty)
                    Log($"The {nameof(composition)} is empty.", LogLevel.Error);
                return IsEmpty;
            }
        }

        internal static void Init()
        {
            Supported = AndroidVersion >= APIRequirement && CanVibrate;
            if (!Supported)
            {
                PrimitiveSupport = new(CreateDefaultSupportDictionary<Primitives, bool>(false));
                return;
            }

            // Get the support of each Primitive from the device itself. Primitive support depends on API level and device manufacturer support.
            // https://developer.android.com/reference/android/os/Vibrator#arePrimitivesSupported(int[])
            PrimitiveSupport = new(GetSupportDictionary<Primitives, bool, bool>("arePrimitivesSupported"));
        }

        // https://developer.android.com/reference/android/os/VibrationEffect#startComposition()
        public VibrationComposition()
        {
            if (NotSupported) return;
            // if (VibrationEffect.vibrationEffectClass == null) TODO: add null checks?
            composition = VibrationEffect.vibrationEffectClass.CallStatic<AndroidJavaObject>("startComposition");
        }

        /// <summary>
        /// Attempts to create a <see cref="VibrationComposition"/> and compose it into a <see cref="VibrationEffect"/> using the given inputs.
        /// <para/><see href="https://developer.android.com/reference/android/os/VibrationEffect.Composition#addPrimitive(int,%20float,%20int)">Related Android Docs</see>
        /// </summary>
        /// <param name="primitives">The primitives you want to add to the composition.
        /// <para/> Warning: If a primitive is not supported the entire composition fails. Check <see cref="PrimitiveSupport"/> for support.</param>
        /// <param name="scales">The scales to apply to the intensity of the primitive.
        /// Either null for all device default, or an array that can have values between 0f and 1f inclusive or <see cref="float.NaN"/> for default.</param>
        /// <param name="delays">The amounts of time (ms) to wait before playing the next primitive. Either null or values 0 or greater.</param>
        /// <returns>VibrationEffect if successful in composition, null if failure.</returns>
        public static VibrationEffect CreateEffect(Primitives[] primitives, float[] scales = null, int[] delays = null)
        {
            if (NotSupported) return null;
            if (primitives == null || primitives.Length == 0)
            {
                Log($"The given {nameof(primitives)} is either null or its length is 0", LogLevel.Error);
                return null;
            }
            if (scales != null && primitives.Length != scales.Length)
            {
                Log($"The length of {nameof(scales)} \'{scales.Length}\' does not equal the length of {nameof(primitives)} \'{primitives.Length}\'", LogLevel.Error);
                return null;
            }
            if (delays != null && primitives.Length != delays.Length)
            {
                Log($"The length of {nameof(delays)} \'{delays.Length}\' does not equal the length of {nameof(primitives)} \'{primitives.Length}\'", LogLevel.Error);
                return null;
            }

            using VibrationComposition composition = new();

            if (scales == null)
            {
                for (int i = 0; i < primitives.Length; i++)
                    if (composition.AddPrimitive(primitives[i]) == false) return null;
            }
            else if (delays == null)
            {
                for (int i = 0; i < primitives.Length; i++)
                    if (composition.AddPrimitive(primitives[i], scales[i]) == false) return null;
            }
            else
            {
                for (int i = 0; i < primitives.Length; i++)
                    if (composition.AddPrimitive(primitives[i], scales[i], delays[i]) == false) return null;
            }

            return composition.Compose();
        }

        /// <summary>
        /// Add a primitive effect to the end of the current composition.
        /// <para/><see href="https://developer.android.com/reference/android/os/VibrationEffect.Composition#addPrimitive(int,%20float,%20int)">Android Docs</see>
        /// </summary>
        /// <param name="primitive">If the primitive is not supported it will not be added and false will be returned. Check <see cref="PrimitiveSupport"/> for support.</param>
        /// <param name="scale">The scale to apply to the intensity of the primitive. Value can be between 0f and 1f inclusive, or <see cref="float.NaN"/> for device default.
        /// Value will be clamped between 0 and 1 if not default.</param>
        /// <param name="delay">The amount of time in milliseconds to wait before playing this primitive, starting when the previous primitive finished.
        /// Value can be 0 or greater. Less than 0 will be clamped.</param>
        /// <returns>True if successful in adding to composition, false if failure</returns>
        public bool AddPrimitive(Primitives primitive, float scale = float.NaN, int delay = 0)
        {
            if (NotSupported || NoComposition) return false;
            if (PrimitiveSupport[primitive] == false)
            {
                Log($"The given {nameof(primitive)} of {primitive} is reported as not supported by this device.", LogLevel.Error);
                // TODO: determine what to do in a multi-vibrator situation as each vibrator might support different things
                return false;
            }

            if (float.IsNaN(scale)) // use default scale, can't tell what it might be
            {
                composition.Call<AndroidJavaObject>(addPrimitiveMethod, (int)primitive);
                return true;
            }

            scale = Math.Clamp(scale, 0, 1);

            if (delay <= 0) // no delay
                composition.Call<AndroidJavaObject>(addPrimitiveMethod, (int)primitive, scale);
            else
                composition.Call<AndroidJavaObject>(addPrimitiveMethod, (int)primitive, scale, delay);

            return true;
        }

        /// <summary>
        /// Compose all of the added primitives together into a single <see cref="VibrationEffect"/>.
        /// <br/>The <see cref="VibrationComposition"/> can still be used and edited afterwards without changing previous composed effects.
        /// <para/><see href="https://developer.android.com/reference/android/os/VibrationEffect.Composition#compose()">Android Docs</see>
        /// </summary>
        public VibrationEffect Compose()
        {
            if (NotSupported || NoComposition) return null;
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
