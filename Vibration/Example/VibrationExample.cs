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
using GoodVibrations;
using Vibes.Android;

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
        GoodVibrations.Vibration.Init();

        contentTransformStatic = contentTransform;
        sectionGroupPrefabStatic = sectionGroupPrefab;
        infoSectionStatic = infoSection;
        buttonSectionPrefabStatic = buttonSectionPrefab;
        inputBlockPrefabStatic = inputBlockPrefab;

        listTransforms = new List<Transform>(15);
    }

    void Start()
    {
        VibrationLogging.DebugLogLevel = VibrationLogging.LogLevel.All;
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
        newGroup.AddSection(new Section(newGroup, $"Platform: {Application.platform}"));
        // this one is to check how ofen SystemInfo is accurate
        newGroup.AddSection(new Section(newGroup, $"Supports Vibration: {SystemInfo.supportsVibration}"));
        newGroup.AddSection(new Section(newGroup, $"Can Vibrate: {GoodVibrations.Vibration.CanVibrate}"));
    }

    private void AddUniversalVibration()
    {
        SupportType support = GoodVibrations.Vibration.CanVibrate ? SupportType.Yes : SupportType.No;
        SectionGroup newGroup = new("Universal Vibraitons");

        newGroup.AddSection(new ButtonSection(newGroup, "Vibrate", delegate { GoodVibrations.Vibration.Vibrate(); }, support));
        newGroup.AddSection(new ButtonSection(newGroup, "Vibrate Pop", delegate { GoodVibrations.Vibration.VibratePop(); }, support));
        newGroup.AddSection(new ButtonSection(newGroup, "Vibrate Peek", delegate { GoodVibrations.Vibration.VibratePeek(); }, support));
        newGroup.AddSection(new ButtonSection(newGroup, "Vibrate Nope", delegate { GoodVibrations.Vibration.VibrateNope(); }, support));

        if (Application.platform == RuntimePlatform.IPhonePlayer)
            support = SupportType.No;

        InputSection vibrateSection = new(newGroup, "Vibrate Duration", 1);
        vibrateSection.AddInput("Duration (ms)", "Input Duration", support, InputField.ContentType.IntegerNumber, false);
        vibrateSection.SetupButton(delegate { ButtonVibrateDuration(vibrateSection); }, support);
        newGroup.AddSection(vibrateSection);

        InputSection vibratePatternSection = new(newGroup, "Vibrate Pattern", 1);
        vibratePatternSection.AddInput("Durations (ms) On-Off-On...", "Input Durations", support, InputField.ContentType.Standard, false, "200, 500, 200");
        vibratePatternSection.SetupButton(delegate { ButtonVibratePattern(vibratePatternSection); }, support);
        newGroup.AddSection(vibratePatternSection);

        newGroup.AddSection(new ButtonSection(newGroup, "Vibration Cancel", delegate { GoodVibrations.Vibration.VibrateCancel(); }, support));
    }

    private void AddiOSImpactFeedbackStyles()
    {
        SupportType support = VibrationiOS.CanVibrate ? SupportType.Yes : SupportType.No;
        SectionGroup newGroup = new("iOS Impact Styles");
        foreach (var item in Enum.GetValues(typeof(VibrationiOS.ImpactFeedbackStyle)) as VibrationiOS.ImpactFeedbackStyle[])
        {
            newGroup.AddSection(new ButtonSection(newGroup, item.ToString(),
                delegate { VibrationiOS.VibrateImpact(item); }, support));
        }
    }

    private void AddiOSNotificationFeedbackStyles()
    {
        SupportType support = VibrationiOS.CanVibrate ? SupportType.Yes : SupportType.No;
        SectionGroup newGroup = new("iOS Notification Styles");
        foreach (var item in Enum.GetValues(typeof(VibrationiOS.NotificationFeedbackStyle)) as VibrationiOS.NotificationFeedbackStyle[])
        {
            newGroup.AddSection(new ButtonSection(newGroup, item.ToString(),
                delegate { VibrationiOS.VibrateNotification(item); }, support));
        }
    }

    private void AddAndroidInfo()
    {
        SectionGroup newGroup = new("Android Info");
        newGroup.AddSection(new Section(newGroup, $"API Version: {Vibes.Android.VibrationManager.AndroidVersion}"));
        newGroup.AddSection(new Section(newGroup, $"Has Vibrator: {Vibes.Android.VibrationManager.CanVibrate}"));
        newGroup.AddSection(new Section(newGroup, $"Haptic Feedback: {HapticFeedback.Supported}"));
        newGroup.AddSection(new Section(newGroup, $"Haptic Status: {HapticFeedback.HapticStatus}"));
        newGroup.AddSection(new Section(newGroup, $"Vibration Effects: {VibrationEffect.Supported}"));
        newGroup.AddSection(new Section(newGroup, $"Predefined Effects: {VibrationEffect.SupportsPredefined}"));
        newGroup.AddSection(new Section(newGroup, $"Amplitude Control: {VibrationEffect.SupportsAmplitudeControl}"));
        newGroup.AddSection(new Section(newGroup, $"Composition Effects: {VibrationComposition.Supported}"));
        //newGroup.AddSection(new ButtonSection(newGroup, "Android Log Support", delegate { Vibe.Android.Vibration.LogSupport(); }, SupportType.Yes));
    }

    private void AddAndroidBasicVibrations()
    {
        SectionGroup newGroup = new("Android Vibration");

        SupportType support = Vibes.Android.VibrationManager.CanVibrate ? SupportType.Yes : SupportType.No;
        SupportType amplitudeSupport = VibrationEffect.SupportsAmplitudeControl ? SupportType.Yes : SupportType.No;
        SupportType buttonSupport = SupportType.Yes;
        if (!Vibes.Android.VibrationManager.CanVibrate)
            buttonSupport = SupportType.No;
        else if (!VibrationEffect.SupportsAmplitudeControl)
            buttonSupport = SupportType.Limited;

        InputSection vibrateSection = new(newGroup, "Vibrate Standard", 2);
        vibrateSection.AddInput("Duration (ms)", "Input Duration", support, InputField.ContentType.IntegerNumber, false);
        vibrateSection.AddInput("Amplitude: Empty | -1 (default) / 0 to 255", "Input Amplitude (optional)", amplitudeSupport, InputField.ContentType.IntegerNumber, true);
        vibrateSection.SetupButton(delegate { ButtonAndroidVibrate(vibrateSection); }, buttonSupport);
        newGroup.AddSection(vibrateSection);

        InputSection vibratePatternSection = new(newGroup, "Vibrate Pattern", 3);
        vibratePatternSection.AddInput("Durations (ms) Off-On-Off...", "Input Durations csv", support, InputField.ContentType.Standard, false, "0, 300, 500, 200, 500, 100");
        vibratePatternSection.AddInput("Amplitudes: Empty | -1 (default) / 0 to 255", "Input Amplitudes csv (optional)", amplitudeSupport, InputField.ContentType.Standard, true, "0, 255, 0, 150, 0, 100");
        vibratePatternSection.AddInput("Repeat Index: Empty | >=0", "Input index to repeat from after done (optional)", support, InputField.ContentType.IntegerNumber, true);
        vibratePatternSection.SetupButton(delegate { ButtonAndroidVibratePattern(vibratePatternSection); }, buttonSupport);
        newGroup.AddSection(vibratePatternSection);

        newGroup.AddSection(new ButtonSection(newGroup, "Vibration Cancel", delegate { Vibes.Android.VibrationManager.VibrateCancel(); }, support));

        InputSection vibrateOldSection = new(newGroup, "Vibrate Old", 1);
        vibrateOldSection.AddInput("Duration (ms)", "Input Duration", support, InputField.ContentType.IntegerNumber, false);
        vibrateOldSection.SetupButton(delegate { ButtonAndroidVibrateOld(vibrateOldSection); }, support);
        newGroup.AddSection(vibrateOldSection);

        InputSection vibratePatternOldSection = new(newGroup, "Vibrate Pattern Old", 2);
        vibratePatternOldSection.AddInput("Durations (ms) Off-On-Off...", "Input Durations csv", support, InputField.ContentType.Standard, false, "0, 300, 500, 200, 500, 100");
        vibratePatternOldSection.AddInput("Repeat Index: Empty | >=0", "Input index to repeat from after done (optional)", support, InputField.ContentType.IntegerNumber, true);
        vibratePatternOldSection.SetupButton(delegate { ButtonAndroidVibratePatternOld(vibratePatternOldSection); }, support);
        newGroup.AddSection(vibratePatternOldSection);
    }

    private void AddAndroidHaptics()
    {
        SectionGroup newGroup = new("Android Haptic Feedbacks");
        if (HapticFeedback.HapticStatus== Vibes.Android.VibrationManager.SupportStatus.UNKNOWN)
        {
            newGroup.AddSection(new Section(newGroup, $"Warning: Can't tell if haptics are enabled on your device. API 33+"));
            foreach (var item in HapticFeedback.HapticSupport)
            {
                newGroup.AddSection(new ButtonSection(newGroup, item.Key.ToString(),
                    delegate { HapticFeedback.Vibrate(item.Key); },
                    HapticFeedback.HapticSupport[item.Key] ? SupportType.Unknown : SupportType.No));
            }
        }
        else
        {
            foreach (var item in HapticFeedback.HapticSupport)
            {
                newGroup.AddSection(new ButtonSection(newGroup, item.Key.ToString(),
                    delegate { HapticFeedback.Vibrate(item.Key); },
                    HapticFeedback.HapticSupport[item.Key] ? SupportType.Yes : SupportType.No));
            }
        }
    }

    private void AddAndroidPredefinedEffects()
    {
        SectionGroup newGroup = new("Android Predefined Effects");
        foreach (var item in VibrationEffect.PredefinedSupport)
        {
            SupportType support;
            if (item.Value == Vibes.Android.VibrationManager.SupportStatus.NO)
                support = SupportType.No;
            else if (item.Value == Vibes.Android.VibrationManager.SupportStatus.UNKNOWN)
                support = SupportType.Unknown;
            else support = SupportType.Yes;

            newGroup.AddSection(new ButtonSection(newGroup, item.Key.ToString(), delegate { Vibes.Android.VibrationManager.VibratePredefined(item.Key); }, support));
        }
    }

    private void AddAndroidCompositionEffects()
    {
        SectionGroup newGroup = new("Android Composition");

        SupportType compositionSupport = VibrationComposition.Supported? SupportType.Yes : SupportType.No;

        SupportType primitiveSupport = SupportType.Unknown;
        bool fullSupport = true, noSupport = true;
        foreach (var item in VibrationComposition.PrimitiveSupport)
        {
            if (item.Value)
                noSupport = false;
            else
                fullSupport = false;
        }
        if (fullSupport == noSupport)
            Debug.LogError("Vibe.Android.Vibration.CompositionPrimitiveSupport is empty???");
        else if (fullSupport)
            primitiveSupport = SupportType.Yes;
        else if (noSupport)
            primitiveSupport = SupportType.No;
        else
            primitiveSupport = SupportType.Limited;

        InputSection compositionSection = new(newGroup, "Vibrate Composition", 3);
        compositionSection.AddInput("Primitives: Unsupported IDs will fail", "Input Primitve IDs (from below) csv", primitiveSupport, InputField.ContentType.Standard, false);
        compositionSection.AddInput("Scales: Empty | -1 (default) / 0 to 1", "Input Scales csv (optional)", compositionSupport, InputField.ContentType.Standard, true);
        compositionSection.AddInput("Delays: Empty | >=0 (ms)", "Input Delays csv (optional)", compositionSupport, InputField.ContentType.Standard, true);
        compositionSection.SetupButton(delegate { ButtonAndroidVibrateComposition(compositionSection); }, primitiveSupport);
        newGroup.AddSection(compositionSection);

        newGroup.AddSection(new ButtonSection(newGroup, "Vibration Cancel", delegate { Vibes.Android.VibrationManager.VibrateCancel(); }, compositionSupport));

        foreach (var item in VibrationComposition.PrimitiveSupport)
        {
            primitiveSupport = item.Value ? SupportType.Yes : SupportType.No;
            newGroup.AddSection(new ButtonSection(newGroup, $"{(int)item.Key}: {item.Key}",
                delegate { Vibes.Android.VibrationManager.VibrateComposition(new VibrationComposition.Primitives[] { item.Key }); },
                primitiveSupport));
        }
    }

    private void ButtonVibrateDuration(InputSection inputSection)
    {
        Debug.Log("Button Vibrate Duration");
        try
        {
            int duration = inputSection.GetInputValueAt(0);
            GoodVibrations.Vibration.Vibrate(duration);
        }
        catch { }
    }

    private void ButtonVibratePattern(InputSection inputSection)
    {
        Debug.Log("Button Vibrate Pattern");
        try
        {
            int[] durations = inputSection.GetInputValuesAt<int>(0);
            GoodVibrations.Vibration.VibratePattern(durations);
        }
        catch (Exception e)
        {
            Debug.Log(e.ToString());
        }
    }

    private void ButtonAndroidVibrate(InputSection inputSection)
    {
        Debug.Log("Button Android Vibrate");
        try
        {
            int duration = inputSection.GetInputValueAt(0);
            int amplitude = inputSection.GetInputValueAt(1);
            Vibes.Android.VibrationManager.Vibrate(duration, amplitude);
        }
        catch (Exception e)
        {
            Debug.Log(e.ToString());
        }
    }

    private void ButtonAndroidVibratePattern(InputSection inputSection)
    {
        Debug.Log("Button Android Vibrate Pattern");
        try
        {
            long[] durations = inputSection.GetInputValuesAt<long>(0);
            int[] amplitudes = inputSection.GetInputValuesAt<int>(1);
            int repeatIndex = inputSection.GetInputValueAt(2);
            Vibes.Android.VibrationManager.VibratePattern(durations, amplitudes, repeatIndex);
        }
        catch (Exception e)
        {
            Debug.Log(e.ToString());
        }
    }

    private void ButtonAndroidVibrateOld(InputSection inputSection)
    {
        Debug.Log("Button Android Vibrate Old");
        try
        {
            int duration = inputSection.GetInputValueAt(0);
            Vibes.Android.VibrationManager.DefaultVibrator.Vibrate(duration);
        }
        catch (Exception e)
        {
            Debug.Log(e.ToString());
        }
    }

    private void ButtonAndroidVibratePatternOld(InputSection inputSection)
    {
        Debug.Log("Button Android Vibrate Pattern Old");
        try
        {
            long[] durations = inputSection.GetInputValuesAt<long>(0);
            int repeatIndex = inputSection.GetInputValueAt(1);
            Vibes.Android.VibrationManager.DefaultVibrator.Vibrate(durations, repeatIndex);
        }
        catch (Exception e)
        {
            Debug.Log(e.ToString());
        }
    }

    private void ButtonAndroidVibrateComposition(InputSection inputSection)
    {
        Debug.Log("Button Android Vibrate Composition");
        try
        {
            int[] effectsIDs = inputSection.GetInputValuesAt<int>(0);
            var compositionEffects = Vibes.Android.VibrationManager.ConvertToEnumArray<VibrationComposition.Primitives>(effectsIDs);
            float[] scales = inputSection.GetInputValuesAt<float>(1);
            int[] delays = inputSection.GetInputValuesAt<int>(2);
            Vibes.Android.VibrationManager.VibrateComposition(compositionEffects, scales, delays);
        }
        catch (Exception e)
        {
            Debug.Log(e.ToString());
        }
    }

    private class SectionGroup
    {
        public Transform ListTransform { get; }
        public List<Section> Sections { get; }

        public SectionGroup(string title)
        {
            GameObject sectionGroup = Instantiate(sectionGroupPrefabStatic, contentTransformStatic);
            sectionGroup.GetComponentInChildren<Text>().text = title;
            ListTransform = sectionGroup.transform.GetChild(1);
            listTransforms.Add(ListTransform);
            Sections = new List<Section>();
        }

        // this does nothing in the end
        public void AddSection(Section section)
        {
            Sections.Add(section);
        }
    }

    private class Section
    {
        public GameObject SectionObject { get; private set; }

        public Section(SectionGroup group, string title)
        {
            SectionObject = Instantiate(infoSectionStatic, group.ListTransform);
            SectionObject.GetComponentInChildren<Text>().text = title;
        }

        public Section(GameObject prefab, SectionGroup group, string title)
        {
            SectionObject = Instantiate(prefab, group.ListTransform);
            SectionObject.GetComponentInChildren<Text>().text = title;
        }
    }

    private class ButtonSection : Section
    {
        private static ColorBlock unsupportedColors, unkownColors;
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
                    button.colors = unkownColors;
                    buttonText.text = "NA?";
                    break;
                case SupportType.Limited:
                    button.colors = unkownColors;
                    buttonText.text = "Limited";
                    break;
            }
        }

        private static void SetupColorBlocks(Button button)
        {
            unsupportedColors = button.colors;
            unsupportedColors.normalColor = new Color(1, 0.58f, 0.58f);
            unkownColors = button.colors;
            unkownColors.normalColor = new Color(0.98f, 1, 0.5f);
            initializedColors = true;
        }
    }

    private class InputSection : ButtonSection
    {
        private static Color badInput = new(1, 0.78f, 0.78f),
            goodInput = new(0.78f, 1, 0.78f);
        private readonly List<InputField> inputFields;
        private readonly List<bool> inputsCanBeEmpty;
        private readonly List<Image> inputBackgounds;

        public InputSection(SectionGroup group, string title, int numberOfInputs) : base(group, title)
        {
            inputFields = new(numberOfInputs);
            inputsCanBeEmpty = new(numberOfInputs);
            inputBackgounds = new(numberOfInputs);
        }

        public void AddInput(string description, string placeholder, SupportType support, InputField.ContentType inputType, bool inputCanbeEmpty, string input = "")
        {
            GameObject section = Instantiate(inputBlockPrefabStatic, SectionObject.transform);

            Text[] texts = section.GetComponentsInChildren<Text>();
            texts[0].text = description;
            texts[0].color = support switch
            {
                SupportType.No => Color.red,
                SupportType.Limited => Color.yellow,
                SupportType.Unknown => Color.magenta,
                _ => Color.black
            };

            texts[1].text = placeholder;
            InputField inputField = section.GetComponentInChildren<InputField>();
            inputField.text = input;
            inputField.contentType = inputType;
            inputFields.Add(inputField);
            inputsCanBeEmpty.Add(inputCanbeEmpty);
            inputBackgounds.Add(inputField.gameObject.GetComponent<Image>());
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
                inputBackgounds[index].color = goodInput;
                return value;
            }
            catch (Exception e)
            {
                inputBackgounds[index].color = badInput;
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
                inputBackgounds[index].color = goodInput;
                return values;
            }
            catch (Exception e)
            {
                inputBackgounds[index].color = badInput;
                Debug.LogWarning("failed to parse the input values");
                throw e;
            }
        }
    }
}
