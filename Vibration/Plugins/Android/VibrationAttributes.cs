using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using UnityEngine;
using static GoodVibrations.VibrationLogging;
using static AndroidVibration.Vibration;

namespace AndroidVibration
{
    /// <summary>
    /// Encapsulates a collection of attributes describing information about a vibration.
    /// <para/><see href="https://developer.android.com/reference/android/os/VibrationAttributes">Android Docs</see>
    /// </summary>
    public class VibrationAttributes : IDisposable, ISupported
    {
        public const int APIRequirement = 30;
        public static bool Supported { get; private set; }

        internal static bool NoSupport
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
        /// Descriptors for Vibrations. Check the property <see cref="AttributesSupport">AttributesSupport</see> for the support of each Attribute.
        /// <para/><see href="https://developer.android.com/reference/android/os/VibrationAttributes#constants_1">Android Docs</see>
        /// </summary>
        public enum Attributes
        {
            USAGE_ACCESSIBILITY = 66,
            USAGE_ALARM = 17,
            USAGE_CLASS_ALARM = 1,
            USAGE_CLASS_FEEDBACK = 2,
            USAGE_CLASS_MASK = 15,
            USAGE_CLASS_MEDIA = 3,
            USAGE_CLASS_UNKNOWN = 0,
            USAGE_COMMUNICATION_REQUEST = 65,
            USAGE_HARDWARE_FEEDBACK = 50,
            USAGE_MEDIA = 19,
            USAGE_NOTIFICATION = 49,
            USAGE_PHYSICAL_EMULATION = 34,
            USAGE_RINGTONE = 33,
            USAGE_TOUCH = 18,
            USAGE_UNKNOWN = 0,
        }

        /// <summary>
        /// Most of these ignore user settings and only privileged apps can do that.
        /// <para/><see href="https://android.googlesource.com/platform/frameworks/base/+/master/core/java/android/os/VibrationAttributes.java">Android Docs</see>
        /// </summary>
        public enum Flags
        {
            NONE = 0,
            BYPASS_INTERRUPTION_POLICY = 1,
            BYPASS_USER_VIBRATION_INTENSITY_OFF = 2,
            INVALIDATE_SETTINGS_CACHE = 4,
            PIPELINED_EFFECT = 8,
            BYPASS_USER_VIBRATION_INTENSITY_SCALE = 16
        }

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
            { Attributes.USAGE_UNKNOWN, 30 }
        };

        internal AndroidJavaObject Attribute { get; private set; }
        public bool IsEmpty => Attribute == null;

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
        /// Creates a new VibrationAttributes object with the given attribute.
        /// </summary>
        public VibrationAttributes(Attributes attribute)
        {
            if (NoSupport) return;
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
        /// Return the flags.
        /// <para/><see href="https://developer.android.com/reference/android/os/VibrationAttributes#getFlags()">Android Docs</see>
        /// </summary>
        /// <returns>A combined (bit) mask of all flags. Value is either 0 or a combination of FLAGs.</returns>
        public int GetFlags()
        {
            if (NoSupport) return 0;
            if (IsEmpty)
            {
                Log($"The {nameof(Attribute)} is empty.", LogLevel.Error);
                return 0;
            }
            return Attribute.Call<int>("getFlags");
        }

        /// <summary>
        /// Return the vibration usage Attribute.
        /// <para/><see href="https://developer.android.com/reference/android/os/VibrationAttributes#getUsage()">Android Docs</see>
        /// </summary>
        public Attributes GetUsage()
        {
            if (NoSupport) return 0;
            if (IsEmpty)
            {
                Log($"The {nameof(Attribute)} is empty.", LogLevel.Error);
                return 0;
            }
            return (Attributes)Attribute.Call<int>("getUsage");
        }

        /// <summary>
        /// Return the vibration usage class Attribute.
        /// <para/><see href="https://developer.android.com/reference/android/os/VibrationAttributes#getUsageClass()">Android Docs</see>
        /// </summary>
        public Attributes GetUsageClass()
        {
            if (NoSupport) return 0;
            if (IsEmpty)
            {
                Log($"The {nameof(Attribute)} is empty.", LogLevel.Error);
                return 0;
            }
            return (Attributes)Attribute.Call<int>("getUsageClass");
        }

        public void Dispose()
        {
            Attribute?.Dispose();
            Attribute = null;
        }

        /// <summary>
        /// Builder class for VibrationAttributes objects. By default, all information is set to UNKNOWN.
        /// <para/><see href="https://developer.android.com/reference/android/os/VibrationAttributes.Builder">Android Docs</see>
        /// </summary>
        public class Builder : IDisposable
        {
            private AndroidJavaObject builderObject;

            public bool IsEmpty => builderObject == null;

            /// <summary>
            /// Constructs a new Builder with the defaults.
            /// <para/><see href="https://developer.android.com/reference/android/os/VibrationAttributes.Builder#Builder()">Android Docs</see>
            /// </summary>
            public Builder()
            {
                if (NoSupport) return;
                builderObject = vibrationAttributesBuilderClass.Call<AndroidJavaObject>("Builder");
            }

            /// <summary>
            /// Constructs a new Builder from a given VibrationAttributes.
            /// <para/><see href="https://developer.android.com/reference/android/os/VibrationAttributes.Builder#Builder(android.os.VibrationAttributes)">Android Docs</see>
            /// </summary>
            public Builder(VibrationAttributes vibrationAttribute)
            {
                if (NoSupport) return;
                if (vibrationAttribute == null || vibrationAttribute.IsEmpty)
                {
                    Log($"The given {nameof(vibrationAttribute)} is null or it's empty", LogLevel.Error);
                    return;
                }
                builderObject = vibrationAttributesBuilderClass.Call<AndroidJavaObject>("Builder", vibrationAttribute);
            }

            /// <summary>
            /// Combines all of the attributes that have been set and returns a new VibrationAttributes object.
            /// <para/><see href="https://developer.android.com/reference/android/os/VibrationAttributes.Builder#build()">Android Docs</see>
            /// </summary>
            public VibrationAttributes Build()
            {
                if (NoSupport) return null;
                if (IsEmpty)
                {
                    Log($"The {nameof(builderObject)} is null.", LogLevel.Error);
                    return null;
                }
                return new VibrationAttributes(builderObject.Call<AndroidJavaObject>("build"));
            }

            /// <summary>
            /// Combines all of the attributes that have been set and returns a new VibrationAttributes object.
            /// <para/><see href="https://developer.android.com/reference/android/os/VibrationAttributes.Builder#build()">Android Docs</see>
            /// </summary>
            internal bool SetVibrationAttribute(VibrationAttributes vibrationAttributes)
            {
                if (NoSupport) return false;
                if (IsEmpty)
                {
                    Log($"The {nameof(builderObject)} is null.", LogLevel.Error);
                    return false;
                }
                if (vibrationAttributes == null)
                {
                    Log($"The given {nameof(vibrationAttributes)} is null.", LogLevel.Error);
                    return false;
                }
                vibrationAttributes.Attribute = builderObject.Call<AndroidJavaObject>("build");
                return true;
            }

            /// <summary>
            /// Replaces the current flags with the given flags.
            /// <para/><see href="https://developer.android.com/reference/android/os/VibrationAttributes.Builder#setFlags(int,%20int)">Android Docs</see>
            /// </summary>
            /// <returns>True if could set the flags, false if there was an error.</returns>
            public bool SetFlags(Flags[] flags)
            {
                if (NoSupport) return false;
                if (IsEmpty)
                {
                    Log($"The {nameof(builderObject)} is null.", LogLevel.Error);
                    return false;
                }

                int flagCombo = 0;
                foreach (Flags f in flags)
                    flagCombo |= (int)f;
                // TODO: check if this does replace all the flags
                builderObject.Call("setFlags", flagCombo, int.MaxValue);
                return true;
            }

            /// <summary>
            /// Sets the attribute describing the type of the corresponding vibration.
            /// <para/><see href="https://developer.android.com/reference/android/os/VibrationAttributes.Builder#setFlags(int,%20int)">Android Docs</see>
            /// </summary>
            /// <returns>True if could set the attribute, false if there was an error.</returns>
            public bool SetUsage(Attributes attribute)
            {
                if (NoSupport) return false;
                if (IsEmpty)
                {
                    Log($"The {nameof(builderObject)} is null.", LogLevel.Error);
                    return false;
                }
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
