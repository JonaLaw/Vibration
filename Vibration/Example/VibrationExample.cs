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

public class VibrationExample : MonoBehaviour
{
    public Transform contentTransform;
    public GameObject sectionGroupPrefab, sectionPrefab, inputBlockPrefab;

    private static Transform contentTransformStatic;
    private static GameObject sectionGroupPrefabStatic, sectionPrefabStatic, inputBlockPrefabStatic;
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
        sectionPrefabStatic = sectionPrefab;
        inputBlockPrefabStatic = inputBlockPrefab;

        listTransforms = new List<Transform>(15);
    }

    void Start()
    {
        AddDeviceInfo();
        AddBasicVibration();
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
        newGroup.AddSection(new Section(newGroup, $"Can Vibrate: {Vibration.CanVibrate}"));
    }

    private void AddBasicVibration()
    {
        SupportType support = Vibration.CanVibrate ? SupportType.Yes : SupportType.No;
        SectionGroup newGroup = new("Basic Vibraiton");

        newGroup.AddSection(new ButtonSection(newGroup, "Vibrate", delegate { Vibration.Vibrate(); }, support));
        newGroup.AddSection(new ButtonSection(newGroup, "Vibrate Pop", delegate { Vibration.VibratePop(); }, support));
        newGroup.AddSection(new ButtonSection(newGroup, "Vibrate Peek", delegate { Vibration.VibratePeek(); }, support));
        newGroup.AddSection(new ButtonSection(newGroup, "Vibrate Nope", delegate { Vibration.VibrateNope(); }, support));

        if (Application.platform == RuntimePlatform.IPhonePlayer)
            support = SupportType.No;

        InputSection vibrateSection = new(newGroup, "Vibrate Duration", 1);
        vibrateSection.AddInput("Duration (ms)", "Input Duration", InputField.ContentType.IntegerNumber, false);
        vibrateSection.SetupButton(delegate { ButtonVibrateDuration(vibrateSection); }, support);
        newGroup.AddSection(vibrateSection);

        InputSection vibratePatternSection = new(newGroup, "Vibrate Pattern", 1);
        vibratePatternSection.AddInput("Durations (ms) On-Off-On...", "Input Durations", InputField.ContentType.Custom, false, "200, 100, 200");
        vibratePatternSection.SetupButton(delegate { ButtonVibratePattern(vibratePatternSection); }, support);
        newGroup.AddSection(vibratePatternSection);

        newGroup.AddSection(new ButtonSection(newGroup, "Vibration Cancel", delegate { Vibration.VibrateCancel(); }, support));
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
        newGroup.AddSection(new Section(newGroup, $"API Version: {VibrationAndroid.AndroidVersion}"));
        newGroup.AddSection(new Section(newGroup, $"Has Vibrator: {VibrationAndroid.CanVibrate}"));
        newGroup.AddSection(new Section(newGroup, $"Haptic Feedback: {VibrationAndroid.SupportsHapticFeedback}"));
        newGroup.AddSection(new Section(newGroup, $"Vibration Effects: {VibrationAndroid.SupportsVibrationEffect}"));
        newGroup.AddSection(new Section(newGroup, $"Predefined Effects: {VibrationAndroid.SupportsPredefinedEffect}"));
        newGroup.AddSection(new Section(newGroup, $"Amplitude Control: {VibrationAndroid.SupportsAmplitudeControl}"));
        newGroup.AddSection(new Section(newGroup, $"Composition Effects: {VibrationAndroid.SupportsComposition}"));
    }

    private void AddAndroidBasicVibrations()
    {
        SectionGroup newGroup = new("Android Vibration");

        SupportType support;
        if (!VibrationAndroid.CanVibrate)
            support = SupportType.No;
        else if (VibrationAndroid.SupportsAmplitudeControl)
            support = SupportType.Limited;
        else support = SupportType.Yes;

        InputSection vibrateSection = new(newGroup, "Vibrate Custom", 2);
        vibrateSection.AddInput("Duration (ms)", "Input Duration", InputField.ContentType.IntegerNumber, false);
        vibrateSection.AddInput("Amplitude: Empty | -1 (default) / 0 to 255", "Input Amplitude (optional)", InputField.ContentType.IntegerNumber, true);
        vibrateSection.SetupButton(delegate { ButtonAndroidVibrate(vibrateSection); }, support);
        newGroup.AddSection(vibrateSection);

        InputSection vibratePatternSection = new(newGroup, "Vibrate Pattern", 3);
        vibratePatternSection.AddInput("Durations (ms) Off-On-Off...", "Input Durations", InputField.ContentType.Custom, false, "0, 300, 50, 200, 50, 100");
        vibratePatternSection.AddInput("Amplitudes: Empty | -1 (default) / 0 to 255", "Input Amplitudes (optional)", InputField.ContentType.Custom, true, "0, 255, 0, 150, 0, 100");
        vibratePatternSection.AddInput("Repeat Index: Empty | 1 to length -1", "Input Repeat Index (optional)", InputField.ContentType.IntegerNumber, true);
        vibratePatternSection.SetupButton(delegate { ButtonAndroidVibratePattern(vibratePatternSection); }, support);
        newGroup.AddSection(vibratePatternSection);
    }

    private void AddAndroidHaptics()
    {
        SupportType support = VibrationAndroid.SupportsHapticFeedback ? SupportType.Yes : SupportType.No;
        SectionGroup newGroup = new("Android Haptic Feedbacks");
        foreach (var item in VibrationAndroid.HapticFeedbackSupport)
        {
            newGroup.AddSection(new ButtonSection(newGroup, item.Key.ToString(),
                delegate { VibrationAndroid.PerformHapticFeedback(item.Key); }, support)
                );
        }
    }

    private void AddAndroidPredefinedEffects()
    {
        SectionGroup newGroup = new("Android Predefined Effects");
        foreach (var item in VibrationAndroid.PredefinedEffectSupport)
        {
            SupportType support;
            if (item.Value == VibrationAndroid.PredefinedEffectSupports.NO)
                support = SupportType.No;
            else if (item.Value == VibrationAndroid.PredefinedEffectSupports.UNKNOWN)
                support = SupportType.Unknown;
            else support = SupportType.Yes;

            newGroup.AddSection(new ButtonSection(newGroup, item.Key.ToString(), delegate { VibrationAndroid.VibratePredefined(item.Key); }, support));
        }
    }

    private void AddAndroidCompositionEffects()
    {
        SectionGroup newGroup = new("Android Composition Effects");

        InputSection compositionSection = new(newGroup, "Vibrate Composition", 3);
        compositionSection.AddInput("Effects", "Input Effects IDs", InputField.ContentType.Custom, false);
        compositionSection.AddInput("Scales: Empty | -1 (default) / 0 to 1", "Input Scales (optional)", InputField.ContentType.Custom, true);
        compositionSection.AddInput("Delays: Empty | >=0 (ms)", "Input Delays (optional)", InputField.ContentType.Custom, true);
        newGroup.AddSection(compositionSection);

        SupportType support;
        bool limitedSupport = false;
        foreach (var item in VibrationAndroid.CompositionEffectSupport)
        {
            if (item.Value)
            {
                support = SupportType.Yes;
            }
            else
            {
                support = SupportType.No;
                limitedSupport = true;
            }

            newGroup.AddSection(new ButtonSection(newGroup, $"{(int)item.Key}: {item.Key.ToString()}",
                delegate { VibrationAndroid.VibrateComposition(new VibrationAndroid.CompositionEffects[] { item.Key }); },
                support));
        }

        support = VibrationAndroid.SupportsComposition ? SupportType.Yes : SupportType.No;
        if (limitedSupport && support == SupportType.Yes)
        {
            support = SupportType.Limited;
        }
        compositionSection.SetupButton(delegate { ButtonAndroidVibrateComposition(compositionSection); }, support);
    }

    private void ButtonVibrateDuration(InputSection inputSection)
    {
        Debug.Log("Button Vibrate Duration");
        try
        {
            int duration = inputSection.GetInputValueAt(0);
            Vibration.Vibrate(duration);
        }
        catch { }
    }

    private void ButtonVibratePattern(InputSection inputSection)
    {
        Debug.Log("Button Vibrate Pattern");
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
        Debug.Log("Button Android Vibrate");
        try
        {
            int duration = inputSection.GetInputValueAt(0);
            int amplitude = inputSection.GetInputValueAt(1);
            VibrationAndroid.Vibrate(duration, amplitude);
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
            int[] durations = inputSection.GetInputValuesAt<int>(0);
            long[] durationLongs = new long[durations.Length];
            for (int i = 0; i < durations.Length; i++)
                durationLongs[i] = durations[i];

            int[] amplitudes = inputSection.GetInputValuesAt<int>(1);
            int repeatIndex = inputSection.GetInputValueAt(2);
            VibrationAndroid.VibratePattern(durationLongs, amplitudes, repeatIndex);
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
            var compositionEffects = VibrationAndroid.ConvertToEnumArray<VibrationAndroid.CompositionEffects>(effectsIDs);
            float[] scales = inputSection.GetInputValuesAt<float>(1);
            int[] delays = inputSection.GetInputValuesAt<int>(2);
            VibrationAndroid.VibrateComposition(compositionEffects, scales, delays);
        }
        catch (Exception e)
        {
            Debug.Log(e.ToString());
        }
    }

    private class SectionGroup
    {
        public GameObject SectionGroupObject { get; }
        public Transform ListTransform { get; }
        public List<Section> Sections { get; }

        public SectionGroup(string title)
        {
            SectionGroupObject = Instantiate(sectionGroupPrefabStatic, contentTransformStatic);
            SectionGroupObject.GetComponentInChildren<Text>().text = title;
            ListTransform = SectionGroupObject.transform.GetChild(1);
            listTransforms.Add(ListTransform);
            Sections = new List<Section>();
        }

        public void AddSection(Section section)
        {
            Sections.Add(section);
            //section.SectionObject.transform.parent = SectionGroupObject.transform;
        }
    }

    private class Section
    {
        public SectionGroup ParentGroup { get; }
        public GameObject SectionObject { get; private set; }
        public Section(SectionGroup group, string title)
        {
            SectionObject = Instantiate(sectionPrefabStatic, group.ListTransform);
            SectionObject.GetComponentInChildren<Text>().text = title;
        }
    }

    private class ButtonSection : Section
    {
        private static ColorBlock unsupportedColors, unkownColors;
        private static bool initializedColors = false;

        public ButtonSection(SectionGroup group, string title, UnityAction call, SupportType supportType) : base(group, title)
        {
            SetupButton(call, supportType);
        }

        public ButtonSection(SectionGroup group, string title) : base(group, title)
        { }

        public void SetupButton(UnityAction call, SupportType supportType)
        {
            Button button = SectionObject.GetComponentInChildren<Button>(true);
            button.gameObject.SetActive(true);
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
                    buttonText.text = "Limit";
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
        private readonly List<InputField> inputFields;
        private readonly List<bool> inputsCanBeEmpty;
        private readonly List<Text> descriptions;

        public InputSection(SectionGroup group, string title, int numberOfInputs) : base(group, title)
        {
            inputFields = new(numberOfInputs);
            inputsCanBeEmpty = new(numberOfInputs);
            descriptions = new(numberOfInputs);
        }

        public void AddInput(string description, string placeholder, InputField.ContentType inputType, bool inputCanbeEmpty, string input = "")
        {
            GameObject section = Instantiate(inputBlockPrefabStatic, SectionObject.transform);
            Text[] texts = section.GetComponentsInChildren<Text>();
            texts[0].text = description;
            texts[1].text = placeholder;
            InputField inputField = section.GetComponentInChildren<InputField>();
            inputField.text = input;
            // set to custom already, shows a keypad
            if (inputType != InputField.ContentType.Custom)
                inputField.contentType = inputType;
            inputFields.Add(inputField);
            inputsCanBeEmpty.Add(inputCanbeEmpty);
            descriptions.Add(texts[0]);
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
                descriptions[index].color = Color.blue;
                return value;
            }
            catch (Exception e)
            {
                descriptions[index].color = Color.red;
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
                else
                    throw new ArgumentException("Type T must be either int or float.");
                descriptions[index].color = Color.blue;
                return values;
            }
            catch (Exception e)
            {
                descriptions[index].color = Color.red;
                Debug.LogWarning("failed to parse the input values");
                throw e;
            }
        }
    }
}
