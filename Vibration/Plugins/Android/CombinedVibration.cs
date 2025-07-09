using System;
using UnityEngine;
using static Vibes.Logging;
using static Vibes.Android.VibrationManager;

namespace Vibes.Android
{
    /// <summary>
    /// A combination of <see cref="VibrationEffect"/>s to be performed by one or more <see cref="Vibrator"/>s.
    /// <para/><inheritdoc cref="APIRequirement"/>
    /// <para/><see href="https://developer.android.com/reference/android/os/CombinedVibration">Android Docs</see>
    /// </summary>
    public class CombinedVibration : IDisposable
    {
        /// <summary>Available from <see cref="AndroidVersion">API Level</see> 31 and onwards.</summary>
        public const int APIRequirement = 31;
        public static bool Supported { get; private set; }
        internal static bool NotSupported
        {
            get
            {
                if (!Supported)
                    Log("This device has no support for Vibration Combination Effects", LogLevel.Warning);
                return !Supported;
            }
        }

        private static AndroidJavaClass combinedVibrationClass;

        internal AndroidJavaObject Combination { get; private set; }
        public bool IsEmpty => Combination == null;

        internal static void Init()
        {
            Supported = AndroidVersion >= APIRequirement && CanVibrate;
            if (!Supported) return;
            combinedVibrationClass = new AndroidJavaClass("android.os.CombinedVibration");
        }

        /// <summary>
        /// Create a vibration that plays a single <see cref="VibrationEffect"/> in parallel on all <see cref="Vibrator"/>s.
        /// <para/><see href="https://developer.android.com/reference/android/os/CombinedVibration#createParallel(android.os.VibrationEffect)">Android Docs</see>
        /// </summary>
        public CombinedVibration(VibrationEffect effect)
        {
            if (NotSupported) return;
            if (effect == null || effect.IsEmpty)
            {
                Log($"The given {nameof(effect)} is either null or is empty.", LogLevel.Error);
                return;
            }
            Combination = combinedVibrationClass.Call<AndroidJavaObject>("createParallel", effect.Effect);
        }

        public void Dispose()
        {
            Combination?.Dispose();
            Combination = null;
        }

        /// <summary>
        /// <see cref="ParallelCombination.Combine"/> uses this to generate a <see cref="CombinedVibration"/>
        /// </summary>
        private CombinedVibration(AndroidJavaObject combination)
        {
            if (NotSupported) return;
            if (combination == null)
                Log($"The given {nameof(combination)} is null.", LogLevel.Error);
            Combination = combination;
        }

        /// <summary>
        /// A combination of <see cref="VibrationEffect"/>s that should be played in multiple <see cref="Vibrator"/>s in parallel.
        /// <para/><see href="https://developer.android.com/reference/android/os/CombinedVibration.ParallelCombination">Android Docs</see>
        /// </summary>
        public class ParallelCombination : IDisposable
        {
            private AndroidJavaObject parallel;
            public bool IsEmpty => parallel == null;

            private bool NoParallel
            {
                get
                {
                    if (IsEmpty)
                        Log($"The {nameof(parallel)} is null.", LogLevel.Error);
                    return IsEmpty;
                }
            }

            /// <summary>
            /// Start creating a vibration that plays effects in parallel on one or more <see cref="Vibrator"/>s.
            /// A parallel vibration takes one or more <see cref="VibrationEffect"/>s associated to individual vibrators to be performed at the same time.
            /// <para/><see href="https://developer.android.com/reference/android/os/CombinedVibration#startParallel()">Android Docs</see>
            /// </summary>
            public ParallelCombination()
            {
                if(NotSupported) return;
                parallel = combinedVibrationClass.Call<AndroidJavaObject>("startParallel");
            }

            /// <summary>
            /// Add or replace a one shot <see cref="VibrationEffect"/> to be performed by the specified <see cref="Vibrator"/>.
            /// <para/><see href="https://developer.android.com/reference/android/os/CombinedVibration.ParallelCombination#addVibrator(int,%20android.os.VibrationEffect)">Android Docs</see>
            /// </summary>
            /// <param name="vibrator">The id of the vibrator that should perform this effect.</param>
            /// <param name="effect">The effect this vibrator should play. This value cannot be null.</param>
            /// <returns>True if able to add the effect to the vibrator, false if unable.</returns>
            public bool AddVibrator(Vibrator vibrator, VibrationEffect effect)
            {
                if (NotSupported || NoParallel) return false;
                if (vibrator == null || vibrator.IsEmpty)
                {
                    Log($"The given {nameof(vibrator)} is either null or it's empty.", LogLevel.Error);
                    return false;
                }
                if (effect == null || effect.IsEmpty)
                {
                    Log($"The given {nameof(effect)} is either null or it's empty.", LogLevel.Error);
                    return false;
                }

                parallel.Call("addVibrator", vibrator.id, effect.Effect);
                return true;
            }

            /// <summary>
            /// Combine all of the added effects into a CombinedVibration. The ParallelCombination object is still valid after this call.
            /// <para/><see href="https://developer.android.com/reference/android/os/CombinedVibration.ParallelCombination#combine()">Android Docs</see>
            /// </summary>
            /// <returns>The CombinedVibration resulting from combining the added effects to be played in parallel.</returns>
            public CombinedVibration Combine()
            {
                if (NoParallel) return null;
                return new CombinedVibration(parallel.Call<AndroidJavaObject>("combine"));
            }

            public void Dispose()
            {
                parallel?.Dispose();
                parallel = null;
            }
        }
    }
}
