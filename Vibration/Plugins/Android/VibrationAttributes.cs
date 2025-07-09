using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using UnityEngine;
using static Vibes.Logging;
using static Vibes.Android.VibrationManager;

namespace Vibes.Android
{
    /// <summary>
    /// Encapsulates a collection of attributes describing information about a <see cref="VibrationEffect"/>.
    /// <para/><inheritdoc cref="APIRequirement"/>
    /// <para/><see href="https://developer.android.com/reference/android/os/VibrationAttributes">Android Docs</see>
    /// </summary>
    public class VibrationAttributes : IDisposable
    {
        /// <summary>Available from <see cref="AndroidVersion">API Level</see> 30 and onwards.</summary>
        public const int APIRequirement = 30;
        public static bool Supported { get; private set; }

        internal static bool NotSupported
        {
            get
            {
                if (!Supported)
                    Log($"The {nameof(AndroidVersion)} is less than 33 which doesn't support vibration attributes", LogLevel.Warning);
                return !Supported;
            }
        }
        
        private static AndroidJavaClass vibrationAttributeClass, vibrationAttributesBuilderClass;

        /// <summary>
        /// Descriptors for Vibrations.
        /// <br/>Check the property <see cref="AttributesSupport"/> for the device's support of each one, or check <see cref="AttributesAPISupport"/> for API level support.
        /// <para/><see href="https://developer.android.com/reference/android/os/VibrationAttributes#constants_1">Android Docs</see>
        /// </summary>
        public enum Attributes
        {
            /// <summary>Usage value to use for accessibility vibrations.</summary>
            USAGE_ACCESSIBILITY = 66,
            /// <summary>Usage value to use for alarm vibrations.</summary>
            USAGE_ALARM = 17,
            /// <summary>Vibration usage class value to use when the vibration is initiated to catch user's attention.</summary>
            USAGE_CLASS_ALARM = 1,
            /// <summary>Vibration usage class value to use when the vibration is initiated as a response to user's actions.</summary>
            USAGE_CLASS_FEEDBACK = 2,
            /// <summary>Mask for vibration usage class value.</summary>
            USAGE_CLASS_MASK = 15,
            /// <summary>Vibration usage class value to use when the vibration is part of media, such as music, movie, soundtrack, game or animations.</summary>
            USAGE_CLASS_MEDIA = 3,
            /// <summary>Vibration usage class value to use when the vibration usage class is unknown.</summary>
            USAGE_CLASS_UNKNOWN = 0,
            /// <summary>Usage value to use for vibrations which mean a request to enter/end a communication with the user, such as a voice prompt.</summary>
            USAGE_COMMUNICATION_REQUEST = 65,
            /// <summary>Usage value to use for vibrations which provide a feedback for hardware component interaction, such as a fingerprint sensor.</summary>
            USAGE_HARDWARE_FEEDBACK = 50,
            /// <summary>Usage value to use for media vibrations, such as music, movie, soundtrack, animations, games, or any interactive media that isn't for touch feedback specifically.</summary>
            USAGE_MEDIA = 19,
            /// <summary>Usage value to use for notification vibrations.</summary>
            USAGE_NOTIFICATION = 49,
            /// <summary>Usage value to use for vibrations which emulate physical hardware reactions, such as edge squeeze.</summary>
            USAGE_PHYSICAL_EMULATION = 34,
            /// <summary>Usage value to use for ringtone vibrations.</summary>
            USAGE_RINGTONE = 33,
            /// <summary>Usage value to use for touch vibrations.</summary>
            USAGE_TOUCH = 18,
            /// <summary>Usage value to use when usage is unknown.</summary>
            USAGE_UNKNOWN = 0,
        }

        /// <summary>
        /// Most of these ignore user settings and only privileged apps can do that.
        /// <para/><see href="https://android.googlesource.com/platform/frameworks/base/+/master/core/java/android/os/VibrationAttributes.java">Android Docs</see>
        /// </summary>
        public enum Flags
        {
            NONE = 0,
            /// <summary>Privileged. Requesting effect to be played even under limited interruptions.</summary>
            BYPASS_INTERRUPTION_POLICY = 1,
            /// <summary>Privileged. Requesting effect to be played even when user settings are disabling it.</summary>
            BYPASS_USER_VIBRATION_INTENSITY_OFF = 1 << 1,
            /// <summary>Flag requesting vibration effect to be played with fresh user settings values.
            /// Intended to be used on scenarios where the user settings might have changed recently, and needs to be applied to this vibration.
            /// Can increase the latency for the overall request.</summary>
            INVALIDATE_SETTINGS_CACHE = 1 << 2,
            /// <summary>Flag requesting that this vibration effect be pipelined with other vibration effects from the same package that also carry this flag.</summary>
            PIPELINED_EFFECT = 1 << 3,
            /// <summary>Privileged. Requesting effect to be played without applying the user intensity setting to scale the vibration.
            /// If you need to bypass the user setting disabling vibrations then this also needs the flag <see cref="BYPASS_USER_VIBRATION_INTENSITY_OFF"/> to be set.</summary>
            BYPASS_USER_VIBRATION_INTENSITY_SCALE = 1 << 4
        }

        /// <summary>
        /// The API support level of each <see cref="Attributes"/>. For reference only.
        /// <para/>Use <see cref="AttributesSupport"/> for to check for device support.
        /// </summary>
        public static ReadOnlyDictionary<Attributes, int> AttributesAPISupport { get; private set; }

        /// <summary>
        /// The support status of each <see cref="Attributes"/>. Determined by if the device <see cref="CanVibrate">can vibrate</see>,
        /// and by comparing the devices <see cref="AndroidVersion">API Level</see> with the <see cref="AttributesAPISupport"/> table.
        /// </summary>
        public static ReadOnlyDictionary<Attributes, bool> AttributesSupport { get; private set; }

        private static readonly Dictionary<Attributes, int> attributesAPISupport = new()
        {
            { Attributes.USAGE_ACCESSIBILITY, 33 },
            { Attributes.USAGE_ALARM, 30 },
            { Attributes.USAGE_CLASS_ALARM, 30 },
            { Attributes.USAGE_CLASS_FEEDBACK, 30 },
            { Attributes.USAGE_CLASS_MASK, 30 },
            { Attributes.USAGE_CLASS_MEDIA, 33 },
            { Attributes.USAGE_CLASS_UNKNOWN, 30 },
            { Attributes.USAGE_COMMUNICATION_REQUEST, 30 },
            { Attributes.USAGE_HARDWARE_FEEDBACK, 30 },
            { Attributes.USAGE_MEDIA, 33 },
            { Attributes.USAGE_NOTIFICATION, 30 },
            { Attributes.USAGE_PHYSICAL_EMULATION, 30 },
            { Attributes.USAGE_RINGTONE, 30 },
            { Attributes.USAGE_TOUCH, 30 },
            // { Attributes.USAGE_UNKNOWN, 30 } // shares the same constant value (0) as USAGE_CLASS_UNKNOWN
        };

        internal AndroidJavaObject Attribute { get; private set; }
        public bool IsEmpty => Attribute == null;

        private bool NoAttribute
        {
            get
            {
                if (IsEmpty)
                    Log($"The {nameof(Attribute)} is empty.", LogLevel.Error);
                return IsEmpty;
            }
        }

        internal static void Init()
        {
            Supported = AndroidVersion >= APIRequirement && CanVibrate;
            if (!Supported)
            {
                AttributesSupport = new(CreateDefaultSupportDictionary<Attributes, bool>(false));
                return;
            }

            AttributesSupport = new(CreateSupportDictionary(attributesAPISupport));

            vibrationAttributeClass = new AndroidJavaClass("android.os.VibrationAttributes");
            vibrationAttributesBuilderClass = new AndroidJavaClass("android.os.VibrationAttributes.Builder");
        }

        /// <summary>
        /// Creates a new <see cref="VibrationAttributes"/> object with the given <see cref="Attributes"/>.
        /// </summary>
        public VibrationAttributes(Attributes attribute)
        {
            if (NotSupported) return;
            if (AttributesSupport[attribute] == false)
            {
                Log($"There is no support for the given {nameof(attribute)}: {attribute}", LogLevel.Error);
                return;
            }

            if (AndroidVersion >= 33)
            {
                // https://developer.android.com/reference/android/os/VibrationAttributes#createForUsage(int)
                Attribute = vibrationAttributeClass.CallStatic<AndroidJavaObject>("createForUsage", (int)attribute);
            }
            else
            {
                using Builder builder = new();
                builder.SetUsage(attribute);
                builder.SetVibrationAttribute(this);
            }
        }

        private VibrationAttributes(AndroidJavaObject vibrationAttribute)
        {
            Attribute = vibrationAttribute;
        }

        /// <summary>
        /// Return a combination of the set <see cref="Flags"/>.
        /// <para/><see href="https://developer.android.com/reference/android/os/VibrationAttributes#getFlags()">Android Docs</see>
        /// </summary>
        /// <returns>A combined (bit) mask of all <see cref="Flags"/>. Value is either 0 or a combination of <see cref="Flags"/>.</returns>
        public int GetFlags()
        {
            if (NotSupported || NoAttribute) return 0;
            return Attribute.Call<int>("getFlags");
        }

        /// <summary>
        /// Return the vibration usage <see cref="Attributes"/>.
        /// <para/><see href="https://developer.android.com/reference/android/os/VibrationAttributes#getUsage()">Android Docs</see>
        /// </summary>
        public Attributes GetUsage()
        {
            if (NotSupported || NoAttribute) return 0;
            return (Attributes)Attribute.Call<int>("getUsage");
        }

        /// <summary>
        /// Return the vibration usage class <see cref="Attributes"/>.
        /// <para/><see href="https://developer.android.com/reference/android/os/VibrationAttributes#getUsageClass()">Android Docs</see>
        /// </summary>
        public Attributes GetUsageClass()
        {
            if (NotSupported || NoAttribute) return 0;
            return (Attributes)Attribute.Call<int>("getUsageClass");
        }

        public void Dispose()
        {
            Attribute?.Dispose();
            Attribute = null;
        }

        /// <summary>
        /// Builder class for <see cref="VibrationAttributes"/> objects. By default, the <see cref="Attributes"/> is set to <see cref="Attributes.USAGE_UNKNOWN"/>.
        /// <para/><see href="https://developer.android.com/reference/android/os/VibrationAttributes.Builder">Android Docs</see>
        /// </summary>
        public class Builder : IDisposable
        {
            private AndroidJavaObject builderObject;

            public bool IsEmpty => builderObject == null;

            private bool NoBuilderObject
            {
                get
                {
                    if (builderObject == null)
                    {
                        Log($"The {nameof(builderObject)} is null.", LogLevel.Error);
                        return true;
                    }
                    return false;
                }
            }

            /// <summary>
            /// Constructs a new <see cref="Builder"/> with the defaults of <see cref="Attributes.USAGE_UNKNOWN"/>.
            /// <para/><see href="https://developer.android.com/reference/android/os/VibrationAttributes.Builder#Builder()">Android Docs</see>
            /// </summary>
            public Builder()
            {
                if (NotSupported) return;
                builderObject = vibrationAttributesBuilderClass.Call<AndroidJavaObject>("Builder");
            }

            /// <summary>
            /// Constructs a new <see cref="Builder"/> from a given <see cref="VibrationAttributes"/>.
            /// <para/><see href="https://developer.android.com/reference/android/os/VibrationAttributes.Builder#Builder(android.os.VibrationAttributes)">Android Docs</see>
            /// </summary>
            public Builder(VibrationAttributes vibrationAttribute)
            {
                if (NotSupported) return;
                if (vibrationAttribute == null || vibrationAttribute.IsEmpty)
                {
                    Log($"The given {nameof(vibrationAttribute)} is null or it's empty", LogLevel.Error);
                    return;
                }
                builderObject = vibrationAttributesBuilderClass.Call<AndroidJavaObject>("Builder", vibrationAttribute);
            }

            /// <summary>
            /// Combines all of the <see cref="Attributes"/> that have been set and returns a new <see cref="VibrationAttributes"/> object.
            /// <para/><see href="https://developer.android.com/reference/android/os/VibrationAttributes.Builder#build()">Android Docs</see>
            /// </summary>
            public VibrationAttributes Build()
            {
                AndroidJavaObject attribute = BuildJavaObject();
                if (attribute == null) return null;
                return new VibrationAttributes(attribute);
            }

            internal bool SetVibrationAttribute(VibrationAttributes vibrationAttributes)
            {
                if (vibrationAttributes == null)
                {
                    Log($"The given {nameof(vibrationAttributes)} is null.", LogLevel.Error);
                    return false;
                }

                AndroidJavaObject attribute = BuildJavaObject();
                if (attribute == null) return false;
                vibrationAttributes.Attribute = attribute;
                return true;
            }

            private AndroidJavaObject BuildJavaObject()
            {
                if (NotSupported || NoBuilderObject) return null;
                return builderObject.Call<AndroidJavaObject>("build");
            }

            /// <summary>
            /// Sets only the <see cref="Flags"/> specified in the bitmask, leaving the other supported <see cref="Flags"/> unchanged in the <see cref="Builder"/>.
            /// <para/><see href="https://developer.android.com/reference/android/os/VibrationAttributes.Builder#setFlags(int,%20int)">Android Docs</see>
            /// </summary>
            /// <param name="bitmask">Bit range that should be changed, use <see cref="int.MaxValue"/> if you want to reset all flags.</param>
            /// <returns>True if could set them, false if there was an error.</returns>
            public bool SetFlags(Flags[] flags, int bitmask)
            {
                if (NotSupported || NoBuilderObject) return false;

                int flagCombo = 0;
                foreach (Flags f in flags)
                    flagCombo |= (int)f;
                // TODO: Check if this works as expected
                builderObject.Call("setFlags", flagCombo, bitmask);
                return true;
            }

            /// <summary>
            /// Sets the <see cref="Attributes"/> describing the type of the corresponding <see cref="VibrationEffect"/>.
            /// <para/><see href="https://developer.android.com/reference/android/os/VibrationAttributes.Builder#setFlags(int,%20int)">Android Docs</see>
            /// </summary>
            /// <returns>True if could set, false if there was an error.</returns>
            public bool SetUsage(Attributes attribute)
            {
                if (NotSupported || NoBuilderObject) return false;
                if (AttributesSupport[attribute] == false)
                {
                    Log($"There is no support for the given {nameof(attribute)}: {attribute}", LogLevel.Error);
                    return false;
                }

                builderObject.Call("setUsage", (int)attribute);
                return true;
            }

            public void Dispose()
            {
                builderObject?.Dispose();
                builderObject = null;
            }
        }
    }
}
