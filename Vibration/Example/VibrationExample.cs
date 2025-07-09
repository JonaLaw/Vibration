////////////////////////////////////////////////////////////////////////////////
//  
// @author Benoît Freslon @benoitfreslon
// https://github.com/BenoitFreslon/Vibration
// https://benoitfreslon.com
//
////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using iOS = Vibes.iOS;
using Android = Vibes.Android;
using WebGL = Vibes.WebGL;

public class VibrationExample : MonoBehaviour
{
    public Transform contentTransform;
    public GameObject sectionGroupPrefab, infoSection, buttonSectionPrefab, inputBlockPrefab;

    private static Transform contentTransformStatic;
    private static GameObject sectionGroupPrefabStatic, infoSectionStatic, buttonSectionPrefabStatic, inputBlockPrefabStatic;
    private static List<Transform> listTransforms;

    private enum SupportType
    {
        Yes,
        No,
        Unknown,
        Limited
    }

    void Awake()
    {
        Vibration.Init();

        contentTransformStatic = contentTransform;
        sectionGroupPrefabStatic = sectionGroupPrefab;
        infoSectionStatic = infoSection;
        buttonSectionPrefabStatic = buttonSectionPrefab;
        inputBlockPrefabStatic = inputBlockPrefab;

        listTransforms = new List<Transform>(15);
    }

    void Start()
    {
        Vibes.Logging.DebugLogLevel = Vibes.Logging.LogLevel.All;
        AddDeviceInfo();
        AddUniversalVibration();
#if UNITY_IOS || UNITY_EDITOR
        AddiOSImpactFeedbackStyles();
        AddiOSNotificationFeedbackStyles();
#endif
#if UNITY_ANDROID || UNITY_EDITOR
        AddAndroidInfo();
        AddAndroidBasicVibrations();
        AddAndroidHaptics();
        AddAndroidPredefinedEffects();
        AddAndroidCompositionEffects();
#endif
#if UNITY_WEBGL || UNITY_EDITOR
        // all covered in the basic vibration section
#endif

        StartCoroutine(FixTheGroups());
    }

    private IEnumerator FixTheGroups()
    {
        yield return 0;
        // the section groups only update their size on the first frame they are shown, Unity bug?
        // so we need to show each section (shown by default) before collapsing them
        foreach (Transform t in listTransforms)
            t.gameObject.SetActive(false);
    }

    private void AddDeviceInfo()
    {
        SectionGroup newGroup = new("Device Info");
        new Section(newGroup, "Platform:", Application.platform.ToString());
        // this one is to check how often SystemInfo is accurate
        new Section(newGroup, "Supports Vibration:", SystemInfo.supportsVibration.ToString());
        new Section(newGroup, "Can Vibrate:", Vibration.CanVibrate.ToString());
    }

    private void AddUniversalVibration()
    {
        SupportType support = Vibration.CanVibrate ? SupportType.Yes : SupportType.No;
        SectionGroup newGroup = new("Universal Vibrations");

        new ButtonSection(newGroup, "Vibrate Normal", delegate { Vibration.Vibrate(); }, support);
        new ButtonSection(newGroup, "Vibrate Pop", delegate { Vibration.VibratePop(); }, support);
        new ButtonSection(newGroup, "Vibrate Peek", delegate { Vibration.VibratePeek(); }, support);
        new ButtonSection(newGroup, "Vibrate Nope", delegate { Vibration.VibrateNope(); }, support);

        if (Application.platform == RuntimePlatform.IPhonePlayer)
            support = SupportType.No;

        InputSection vibrateSection = new(newGroup, "Vibrate Duration", 1);
        vibrateSection.AddInput("Duration (ms)", "Input Duration", support, InputField.ContentType.IntegerNumber, false);
        vibrateSection.SetupButton(delegate { ButtonVibrateDuration(vibrateSection); }, support);

        InputSection vibratePatternSection = new(newGroup, "Vibrate Pattern", 1);
        vibratePatternSection.AddInput("Durations (ms) On-Off-On...", "Input Durations", support, InputField.ContentType.Standard, false, "200, 500, 200");
        vibratePatternSection.SetupButton(delegate { ButtonVibratePattern(vibratePatternSection); }, support);

        new ButtonSection(newGroup, "Vibration Cancel", delegate { Vibration.VibrateCancel(); }, support);
    }

    private void AddiOSImpactFeedbackStyles()
    {
        SupportType support = iOS.VibrationManager.CanVibrate ? SupportType.Yes : SupportType.No;
        SectionGroup newGroup = new("iOS Impact Styles");

        var impactFeedbackStyles = Enum.GetValues(typeof(iOS.ImpactFeedbackStyle)) as iOS.ImpactFeedbackStyle[];
        foreach (iOS.ImpactFeedbackStyle style in impactFeedbackStyles)
        {
            new ButtonSection(newGroup, style.ToString(),
                delegate { iOS.VibrationManager.VibrateImpact(style); }, support);
        }
    }

    private void AddiOSNotificationFeedbackStyles()
    {
        SupportType support = iOS.VibrationManager.CanVibrate ? SupportType.Yes : SupportType.No;
        SectionGroup newGroup = new("iOS Notification Styles");

        iOS.NotificationFeedbackStyle[] notificationFeedbackStyles = Enum.GetValues(typeof(iOS.NotificationFeedbackStyle)) as iOS.NotificationFeedbackStyle[];
        foreach (iOS.NotificationFeedbackStyle style in notificationFeedbackStyles)
        {
            new ButtonSection(newGroup, style.ToString(),
                delegate { iOS.VibrationManager.VibrateNotification(style); }, support);
        }
    }

    private void AddAndroidInfo()
    {
        SectionGroup newGroup = new("Android Info");
        new Section(newGroup, "API Version:", Android.VibrationManager.AndroidVersion.ToString());
        new Section(newGroup, "Has Vibrator:", Android.VibrationManager.CanVibrate.ToString());
        new Section(newGroup, "Number of Vibrators:", Android.VibrationManager.Vibrators.Count.ToString());
        new Section(newGroup, "Haptic Feedback:", Android.HapticFeedback.Supported.ToString());
        new Section(newGroup, "Haptic Status:", Android.HapticFeedback.HapticStatus.ToString());
        new Section(newGroup, "Vibration Effects:", Android.VibrationEffect.Supported.ToString());
        new Section(newGroup, "Predefined Effects:", Android.VibrationEffect.SupportsPredefined.ToString());
        new Section(newGroup, "Amplitude Control:", Android.VibrationEffect.SupportsAmplitudeControl.ToString());
        new Section(newGroup, "Attributes:", Android.VibrationAttributes.Supported.ToString());
        new Section(newGroup, "Composition Effects:", Android.VibrationComposition.Supported.ToString());
        new Section(newGroup, "Vibrator Manager:", Android.VibratorManager.Supported.ToString());
        new Section(newGroup, "Combined Vibration:", Android.CombinedVibration.Supported.ToString());
    }

    private void AddAndroidBasicVibrations()
    {
        SectionGroup newGroup = new("Android Basic Vibration");

        SupportType vibrationSupport = Android.VibrationManager.CanVibrate ? SupportType.Yes : SupportType.No;
        SupportType amplitudeSupport = Android.VibrationEffect.SupportsAmplitudeControl ? SupportType.Yes : SupportType.No;

        SupportType fullSupport = vibrationSupport;
        if (fullSupport == SupportType.Yes && !Android.VibrationEffect.SupportsAmplitudeControl)
            fullSupport = SupportType.Limited;

        InputSection vibrateSection = new(newGroup, "Vibrate Standard", 2);
        vibrateSection.AddInput("Duration (ms)", "Input Duration", vibrationSupport, InputField.ContentType.IntegerNumber, false);
        vibrateSection.AddInput("Amplitude: Empty | -1 (default) / 0 to 255", "Input Amplitude (optional)", amplitudeSupport, InputField.ContentType.IntegerNumber, true);
        vibrateSection.SetupButton(delegate { ButtonAndroidVibrate(vibrateSection); }, fullSupport);

        InputSection vibratePatternSection = new(newGroup, "Vibrate Pattern", 3);
        vibratePatternSection.AddInput("Durations (ms) Off-On-Off...", "Input Durations CSV", vibrationSupport, InputField.ContentType.Standard, false, "0, 100, 500, 200, 500, 300");
        vibratePatternSection.AddInput("Amplitudes: Empty | -1 (default) / 0 to 255", "Input Amplitudes CSV (optional)", amplitudeSupport, InputField.ContentType.Standard, true, "0, 255, 0, 150, 0, 100");
        vibratePatternSection.AddInput("Repeat Index: Empty | >=0", "Input index to repeat from after done (optional)", vibrationSupport, InputField.ContentType.IntegerNumber, true);
        vibratePatternSection.SetupButton(delegate { ButtonAndroidVibratePattern(vibratePatternSection); }, fullSupport);

        new ButtonSection(newGroup, "Vibration Cancel", delegate { Android.Relay.VibrateCancel(); }, vibrationSupport);

        InputSection vibrateOldSection = new(newGroup, "Deprecated Vibrate", 1);
        vibrateOldSection.AddInput("Duration (ms)", "Input Duration", vibrationSupport, InputField.ContentType.IntegerNumber, false);
        vibrateOldSection.SetupButton(delegate { ButtonAndroidDeprecatedVibrate(vibrateOldSection); }, vibrationSupport);

        InputSection vibratePatternOldSection = new(newGroup, "Deprecated Vibrate Pattern", 2);
        vibratePatternOldSection.AddInput("Durations (ms) Off-On-Off...", "Input Durations CSV", vibrationSupport, InputField.ContentType.Standard, false, "0, 100, 500, 200, 500, 300");
        vibratePatternOldSection.AddInput("Repeat Index: Empty | >=0", "Input index to repeat from after done (optional)", vibrationSupport, InputField.ContentType.IntegerNumber, true);
        vibratePatternOldSection.SetupButton(delegate { ButtonAndroidDeprecatedVibratePattern(vibratePatternOldSection); }, vibrationSupport);
    }

    private void AddAndroidHaptics()
    {
        SectionGroup newGroup = new("Android Haptic Feedbacks");
        if (Android.HapticFeedback.HapticStatus == Android.SupportStatus.UNKNOWN)
        {
            new Section(newGroup, $"Warning: Can't tell if haptics are enabled on your device. API 33+");
            foreach (var item in Android.HapticFeedback.HapticSupport)
            {
                new ButtonSection(newGroup, item.Key.ToString(),
                    delegate { Android.HapticFeedback.Vibrate(item.Key); },
                    Android.HapticFeedback.HapticSupport[item.Key] ? SupportType.Unknown : SupportType.No);
            }
        }
        else
        {
            foreach (var item in Android.HapticFeedback.HapticSupport)
            {
                new ButtonSection(newGroup, item.Key.ToString(),
                    delegate { Android.HapticFeedback.Vibrate(item.Key); },
                    Android.HapticFeedback.HapticSupport[item.Key] ? SupportType.Yes : SupportType.No);
            }
        }
    }

    private void AddAndroidPredefinedEffects()
    {
        SectionGroup newGroup = new("Android Predefined Effects");
        foreach (var item in Android.VibrationEffect.PredefinedSupport)
        {
            SupportType support = item.Value switch
            {
                Android.SupportStatus.YES => SupportType.Yes,
                Android.SupportStatus.NO => SupportType.No,
                Android.SupportStatus.UNKNOWN => SupportType.Unknown,
                _ => throw new NotImplementedException()
            };

            new ButtonSection(newGroup, item.Key.ToString(), delegate { Android.Relay.VibratePredefined(item.Key); }, support);
        }
    }
     
    private void AddAndroidCompositionEffects()
    {
        SectionGroup newGroup = new("Android Composition");

        SupportType compositionSupport = Android.VibrationComposition.Supported? SupportType.Yes : SupportType.No;

        SupportType primitiveSupport = SupportType.Unknown;
        bool fullSupport = true, noSupport = true;

        foreach (var item in Android.VibrationComposition.PrimitiveSupport)
        {
            if (item.Value)
                noSupport = false;
            else
                fullSupport = false;

            if (!noSupport && !fullSupport) break;
        }

        if (fullSupport)
            primitiveSupport = SupportType.Yes;
        else if (noSupport)
            primitiveSupport = SupportType.No;
        else
            primitiveSupport = SupportType.Limited;

        InputSection compositionSection = new(newGroup, "Vibrate Composition", 3);
        compositionSection.AddInput("Primitives: Unsupported IDs will fail", "Input Primitive IDs (below) CSV", primitiveSupport, InputField.ContentType.Standard, false);
        compositionSection.AddInput("Scales: Empty | -1 (default) / 0 to 1", "Input Scales CSV (optional)", compositionSupport, InputField.ContentType.Standard, true);
        compositionSection.AddInput("Delays: Empty | >=0 (ms)", "Input Delays CSV (optional)", compositionSupport, InputField.ContentType.Standard, true);
        compositionSection.SetupButton(delegate { ButtonAndroidVibrateComposition(compositionSection); }, primitiveSupport);

        new ButtonSection(newGroup, "Vibration Cancel", delegate { Android.Relay.VibrateCancel(); }, compositionSupport);

        foreach (var item in Android.VibrationComposition.PrimitiveSupport)
        {
            new ButtonSection(newGroup, $"{(int)item.Key}: {item.Key}",
                delegate { Android.Relay.VibrateComposition(new Android.VibrationComposition.Primitives[] { item.Key }); },
                (item.Value ? SupportType.Yes : SupportType.No));
        }
    }

    private void ButtonVibrateDuration(InputSection inputSection)
    {
        Debug.Log(nameof(ButtonVibrateDuration));
        try
        {
            int duration = inputSection.GetInputValueAt(0);
            Vibration.Vibrate(duration);
        }
        catch (Exception e)
        {
            Debug.Log(e.ToString());
        }
    }

    private void ButtonVibratePattern(InputSection inputSection)
    {
        Debug.Log(nameof(ButtonVibratePattern));
        try
        {
            int[] durations = inputSection.GetInputValuesAt<int>(0);
            Vibration.VibratePattern(durations);
        }
        catch (Exception e)
        {
            Debug.Log(e.ToString());
        }
    }

    private void ButtonAndroidVibrate(InputSection inputSection)
    {
        Debug.Log(nameof(ButtonAndroidVibrate));
        try
        {
            int duration = inputSection.GetInputValueAt(0);
            int amplitude = inputSection.GetInputValueAt(1);
            Android.Relay.Vibrate(duration, amplitude);
        }
        catch (Exception e)
        {
            Debug.Log(e.ToString());
        }
    }

    private void ButtonAndroidVibratePattern(InputSection inputSection)
    {
        Debug.Log(nameof(ButtonAndroidVibratePattern));
        try
        {
            long[] durations = inputSection.GetInputValuesAt<long>(0);
            int[] amplitudes = inputSection.GetInputValuesAt<int>(1);
            int repeatIndex = inputSection.GetInputValueAt(2);
            Android.Relay.VibratePattern(durations, amplitudes, repeatIndex);
        }
        catch (Exception e)
        {
            Debug.Log(e.ToString());
        }
    }

    private void ButtonAndroidDeprecatedVibrate(InputSection inputSection)
    {
        Debug.Log(nameof(ButtonAndroidDeprecatedVibrate));
        try
        {
            int duration = inputSection.GetInputValueAt(0);
#pragma warning disable CS0618 // Type or member is obsolete
            Android.VibrationManager.DefaultVibrator.Vibrate(duration);
#pragma warning restore CS0618 // Type or member is obsolete
        }
        catch (Exception e)
        {
            Debug.Log(e.ToString());
        }
    }

    private void ButtonAndroidDeprecatedVibratePattern(InputSection inputSection)
    {
        Debug.Log(nameof(ButtonAndroidDeprecatedVibratePattern));
        try
        {
            long[] durations = inputSection.GetInputValuesAt<long>(0);
            int repeatIndex = inputSection.GetInputValueAt(1);
#pragma warning disable CS0618 // Type or member is obsolete
            Android.VibrationManager.DefaultVibrator.Vibrate(durations, repeatIndex);
#pragma warning restore CS0618 // Type or member is obsolete
        }
        catch (Exception e)
        {
            Debug.Log(e.ToString());
        }
    }

    private void ButtonAndroidVibrateComposition(InputSection inputSection)
    {
        Debug.Log(nameof(ButtonAndroidVibrateComposition));
        try
        {
            int[] effectsIDs = inputSection.GetInputValuesAt<int>(0);
            var compositionEffects = Android.VibrationManager.ConvertToEnumArray<Android.VibrationComposition.Primitives>(effectsIDs);
            float[] scales = inputSection.GetInputValuesAt<float>(1);
            int[] delays = inputSection.GetInputValuesAt<int>(2);
            Android.Relay.VibrateComposition(compositionEffects, scales, delays);
        }
        catch (Exception e)
        {
            Debug.Log(e.ToString());
        }
    }

    private void ButtonAndroidVibrateEffect()
    {

    }

    private class SectionGroup
    {
        public Transform ListTransform { get; }
        public SectionGroup(string title)
        {
            GameObject sectionGroup = Instantiate(sectionGroupPrefabStatic, contentTransformStatic);
            sectionGroup.GetComponentInChildren<Text>().text = title;
            ListTransform = sectionGroup.transform.GetChild(1);
            listTransforms.Add(ListTransform);
        }
    }

    private class Section
    {
        public GameObject SectionObject { get; private set; }

        public Section(SectionGroup group, string title, string value) : this(group, title)
        {
            SectionObject.transform.GetChild(1).GetComponent<Text>().text = value;
        }

        public Section(SectionGroup group, string title) : this(infoSectionStatic, group, title)
        { }

        public Section(GameObject prefab, SectionGroup group, string title)
        {
            SectionObject = Instantiate(prefab, group.ListTransform);
            SectionObject.GetComponentInChildren<Text>().text = title;
        }
    }

    private class ButtonSection : Section
    {
        private static ColorBlock unsupportedColors, limitedColors, unknownColors;
        private static bool initializedColors = false;

        public ButtonSection(SectionGroup group, string title, UnityAction call, SupportType supportType) : base(buttonSectionPrefabStatic, group, title)
        {
            SetupButton(call, supportType);
        }

        public ButtonSection(SectionGroup group, string title) : base(buttonSectionPrefabStatic, group, title)
        { }

        public void SetupButton(UnityAction call, SupportType supportType)
        {
            Button button = SectionObject.GetComponentInChildren<Button>(true);
            button.onClick.AddListener(call);
            Text buttonText = button.GetComponentInChildren<Text>();

            if (!initializedColors) SetupColorBlocks(button);

            switch (supportType)
            {
                case SupportType.Yes:
                    buttonText.text = "Test";
                    break;
                case SupportType.No:
                    button.colors = unsupportedColors;
                    buttonText.text = "NA";
                    break;
                case SupportType.Unknown:
                    button.colors = unknownColors;
                    buttonText.text = "NA?";
                    break;
                case SupportType.Limited:
                    button.colors = limitedColors;
                    buttonText.text = "Limited";
                    break;
            }
        }

        private static void SetupColorBlocks(Button button)
        {
            Color selected = new(0.6f, 0.6f, 0.6f);
            Color pressed = new(0.4f, 0.4f, 0.4f);

            unsupportedColors = button.colors;
            unsupportedColors.normalColor = new Color(0.75f, 0.18f, 0.18f);
            unsupportedColors.highlightedColor = unsupportedColors.normalColor * selected;
            unsupportedColors.pressedColor = unsupportedColors.normalColor * pressed;

            unknownColors = button.colors;
            unknownColors.normalColor = new Color(0.82f, 0.76f, 0.26f);
            unknownColors.highlightedColor = unknownColors.normalColor * selected;
            unknownColors.pressedColor = unknownColors.normalColor * pressed;

            limitedColors = button.colors;
            limitedColors.normalColor = new Color(0.75f, 0.7f, 0.2f);
            limitedColors.highlightedColor = limitedColors.normalColor * selected;
            limitedColors.pressedColor = limitedColors.normalColor * pressed;

            initializedColors = true;
        }
    }

    private class InputSection : ButtonSection
    {
        private static Color badInput = new(0.4f, 0.25f, 0.25f),
            goodInput = new(0.2f, 0.4f, 0.2f);

        private static Color notSupported = new(1, 0.2f, 0.2f),
            unknownSupport = new(0.75f, 0.18f, 0.73f),
            limitedSupport = new(0.75f, 0.7f, 0.2f);

        private readonly List<InputField> inputFields;
        private readonly List<bool> inputsCanBeEmpty;
        private readonly List<Image> inputBackgrounds;

        public InputSection(SectionGroup group, string title, int numberOfInputs) : base(group, title)
        {
            inputFields = new(numberOfInputs);
            inputsCanBeEmpty = new(numberOfInputs);
            inputBackgrounds = new(numberOfInputs);
        }

        public void AddInput(string description, string placeholder, SupportType support, InputField.ContentType inputType, bool inputCanBeEmpty, string input = "")
        {
            GameObject section = Instantiate(inputBlockPrefabStatic, SectionObject.transform);

            Text[] texts = section.GetComponentsInChildren<Text>();
            texts[0].text = description;
            texts[0].color = support switch
            {
                SupportType.Yes => Color.white,
                SupportType.No => notSupported,
                SupportType.Unknown => unknownSupport,
                SupportType.Limited => limitedSupport,
                _ => throw new NotSupportedException()
            };

            texts[1].text = placeholder;
            InputField inputField = section.GetComponentInChildren<InputField>();
            inputField.text = input;
            inputField.contentType = inputType;
            inputFields.Add(inputField);
            inputsCanBeEmpty.Add(inputCanBeEmpty);
            inputBackgrounds.Add(inputField.gameObject.GetComponent<Image>());
            return;
        }

        public int GetInputValueAt(int index)
        {
            try
            {
                string input = inputFields[index].text;
                int value;
                if (inputsCanBeEmpty[index] && string.IsNullOrEmpty(input))
                    value = -1;
                else
                    value = int.Parse(input);
                inputBackgrounds[index].color = goodInput;
                return value;
            }
            catch (Exception e)
            {
                inputBackgrounds[index].color = badInput;
                Debug.LogWarning("failed to parse the input value");
                throw e;
            }
        }

        public T[] GetInputValuesAt<T>(int index) where T : struct
        {
            try
            {
                string input = inputFields[index].text;
                T[] values;
                if (inputsCanBeEmpty[index] && string.IsNullOrEmpty(input))
                    values = null;
                else if (typeof(T) == typeof(int))
                    values = Array.ConvertAll(input.Split(','), v => (T)(object)int.Parse(v));
                else if (typeof(T) == typeof(float))
                    values = Array.ConvertAll(input.Split(','), v => (T)(object)float.Parse(v));
                else if (typeof(T) == typeof(long))
                    values = Array.ConvertAll(input.Split(','), v => (T)(object)long.Parse(v));
                else
                    throw new ArgumentException("Type T must be either int or float.");
                inputBackgrounds[index].color = goodInput;
                return values;
            }
            catch (Exception e)
            {
                inputBackgrounds[index].color = badInput;
                Debug.LogWarning("failed to parse the input values");
                throw e;
            }
        }
    }
}
