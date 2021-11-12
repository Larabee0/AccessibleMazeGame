using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using GameObjectMaze;

public class ColourSettingsControllerV2 : MonoBehaviour
{

    // events
    public ColourChangedDelegate OnNewColours;

    public UIDocument document;
    private VisualElement rootVisualElement;

    private TextField textField;
    private Slider redSlider;
    private Slider greenSlider;
    private Slider blueSlider;
    private Slider transparencySlider;

    private VisualElement SampleDisplay;

    private RadioButton applyNone;
    private RadioButton applyText;
    private RadioButton applyBackground1;
    private RadioButton applyBackground2;
    private RadioButton applyBackground3;
    private RadioButton applyBackground4;
    private RadioButton applyBackground5;
    private RadioButton applyBackground6;
    private RadioButton applyBackground7;
    private RadioButton applyBackground8;

    private RadioButton sampleMode;
    private RadioButton overrideMode;

    private RadioButton MiniMapToggle;
    private RadioButton MiniMapPathToggle;
    private RadioButton MiniMapOff;
    private RadioButton MiniMapPathoff;

    public bool MiniMapEnabled { get { return MiniMapToggle.value; } set { MiniMapOff.value = !(MiniMapToggle.value = value); } }
    public bool MiniMapPathEnabled { get { return MiniMapPathToggle.value; } set{ MiniMapPathoff.value = !(MiniMapPathToggle.value = value); } }

    private Button ExitButton;
    private Button ResetButton;

    private Color CurrentSampleColour = new Color(0.5f, 0.5f, 0.5f, 1f);

    public Color textCurrent;
    public Color background1Current;
    public Color background2Current;
    public Color background3Current;
    public Color background4Current;
    public Color background5Current;
    public Color background6Current;
    public Color background7Current;
    public Color background8Current;

    public List<VisualElement> textColourUpdateGroup = new List<VisualElement>();
    public List<VisualElement> textColourUpdateGroupBackground = new List<VisualElement>();
    public List<VisualElement> textColourUpdateGroupBorder = new List<VisualElement>();

    public List<VisualElement> background1ColourUpdateGroup = new List<VisualElement>();
    public List<VisualElement> background2ColourUpdateGroup = new List<VisualElement>();
    public List<VisualElement> background3ColourUpdateGroup = new List<VisualElement>();
    public List<VisualElement> background4ColourUpdateGroup = new List<VisualElement>();

    public List<VisualElement> background5ColourUpdateGroup = new List<VisualElement>();
    public List<VisualElement> background6ColourUpdateGroup = new List<VisualElement>();
    public List<VisualElement> background7ColourUpdateGroup = new List<VisualElement>();
    public List<VisualElement> background8ColourUpdateGroup = new List<VisualElement>();
    private void GetCurrentColours()
    {
        textCurrent = UIColours.textCurrent;
        background1Current = UIColours.background1Current;
        background2Current = UIColours.background2Current;
        background3Current = UIColours.background3Current;
        background4Current = UIColours.background4Current;
        background5Current = UIColours.background5Current;
        background6Current = UIColours.background6Current;
        background7Current = UIColours.background7Current;
        background8Current = UIColours.background8Current;
    }


    private void GetDefaultColours()
    {
        textCurrent = UIColours.textDefault;
        background1Current = UIColours.background1Default;
        background2Current = UIColours.background2Default;
        background3Current = UIColours.background3Default;
        background4Current = UIColours.background4Default;
        background5Current = UIColours.background5Current;
        background6Current = UIColours.background6Current;
        background7Current = UIColours.background7Current;
        background8Current = UIColours.background8Current;
    }
    private void Awake()
    {
        document = GetComponent<UIDocument>();
    }
    private void OnEnable()
    {
        rootVisualElement = document.rootVisualElement;
        GetCurrentColours();

        rootVisualElement = document.rootVisualElement;
        redSlider = rootVisualElement.Q<Slider>("RedSlider");
        greenSlider = rootVisualElement.Q<Slider>("GreenSlider");
        blueSlider = rootVisualElement.Q<Slider>("BlueSlider");
        transparencySlider = rootVisualElement.Q<Slider>("TransparencySlider");

        textField = rootVisualElement.Q<TextField>("HexCode");
        textField.Q<Label>(null, className: "unity-label").style.unityTextAlign = TextAnchor.MiddleCenter;        
        textField.RegisterValueChangedCallback(ev => HexToRGBA(ev.newValue));

        textColourUpdateGroup.Add(textField.Q<VisualElement>("unity-text-input"));

        SampleDisplay = rootVisualElement.Q<VisualElement>("ColourSampleDisplay");

        applyNone = rootVisualElement.Q<RadioButton>("ApplyNone");
        applyText = rootVisualElement.Q<RadioButton>("ApplyText");
        applyBackground1 = rootVisualElement.Q<RadioButton>("ApplyBackground1");
        applyBackground2 = rootVisualElement.Q<RadioButton>("ApplyBackground2");
        applyBackground3 = rootVisualElement.Q<RadioButton>("ApplyBackground3");
        applyBackground4 = rootVisualElement.Q<RadioButton>("ApplyBackground4");
        applyBackground5 = rootVisualElement.Q<RadioButton>("ApplyBackground5");
        applyBackground6 = rootVisualElement.Q<RadioButton>("ApplyBackground6");
        applyBackground7 = rootVisualElement.Q<RadioButton>("ApplyBackground7");
        applyBackground8 = rootVisualElement.Q<RadioButton>("ApplyBackground8");
        sampleMode = rootVisualElement.Q<RadioButton>("SampleMode");
        overrideMode = rootVisualElement.Q<RadioButton>("OverrideMode");
        MiniMapToggle = rootVisualElement.Q<RadioButton>("MinMapOnOff");
        MiniMapPathToggle = rootVisualElement.Q<RadioButton>("MinMapPathOnOff");
        MiniMapOff = rootVisualElement.Q<RadioButton>("MiniMapOff");
        MiniMapPathoff = rootVisualElement.Q<RadioButton>("MiniMapPathOff");

        ExitButton = rootVisualElement.Q<Button>("ExitMenu");
        ResetButton = rootVisualElement.Q<Button>("ResetColours");

        ExitButton.RegisterCallback<ClickEvent>(ev => this.gameObject.SetActive(false));
        ResetButton.RegisterCallback<ClickEvent>(ev => ResetToDefaultColours());

        applyNone.RegisterValueChangedCallback(ev => ApplyColourModeChanged());
        applyText.RegisterValueChangedCallback(ev => ApplyColourModeChanged());
        applyBackground1.RegisterValueChangedCallback(ev => ApplyColourModeChanged());
        applyBackground2.RegisterValueChangedCallback(ev => ApplyColourModeChanged());
        applyBackground3.RegisterValueChangedCallback(ev => ApplyColourModeChanged());
        applyBackground4.RegisterValueChangedCallback(ev => ApplyColourModeChanged());
        applyBackground5.RegisterValueChangedCallback(ev => ApplyColourModeChanged());
        applyBackground6.RegisterValueChangedCallback(ev => ApplyColourModeChanged());
        applyBackground7.RegisterValueChangedCallback(ev => ApplyColourModeChanged());
        applyBackground8.RegisterValueChangedCallback(ev => ApplyColourModeChanged());

        redSlider.RegisterValueChangedCallback(ev => RedValueChanged(ev.newValue));
        greenSlider.RegisterValueChangedCallback(ev => GreenValueChanged(ev.newValue));
        blueSlider.RegisterValueChangedCallback(ev => BlueValueChanged(ev.newValue));
        transparencySlider.RegisterValueChangedCallback(ev => TransparencyValueChanged(ev.newValue));


        textColourUpdateGroup.AddRange(QueryRootForList("unity-text-element"));
        textColourUpdateGroupBackground.AddRange(QueryRootForList("unity-radio-button__checkmark"));
        textColourUpdateGroupBorder.AddRange(QueryRootForList("unity-radio-button__checkmark-background"));

        background1ColourUpdateGroup.AddRange(QueryRootForList("background-1"));
        background1ColourUpdateGroup.AddRange(QueryRootForList("unity-base-slider__tracker"));

        background2ColourUpdateGroup.AddRange(QueryRootForList("background-2"));
        background2ColourUpdateGroup.AddRange(QueryRootForList("unity-radio-button__checkmark-background"));

        background3ColourUpdateGroup.AddRange(QueryRootForList("background-3"));
        background3ColourUpdateGroup.AddRange(QueryRootForList("unity-base-slider__dragger"));

        background4ColourUpdateGroup.AddRange(QueryRootForList("background-4"));


        background5ColourUpdateGroup.AddRange(QueryRootForList("background-5"));

        background6ColourUpdateGroup.AddRange(QueryRootForList("background-6"));

        background7ColourUpdateGroup.AddRange(QueryRootForList("background-7"));

        background8ColourUpdateGroup.AddRange(QueryRootForList("background-8"));

        SetSlidersToColour();
        UpdateSampleColourNoUpdate();

        UpdateTextColours(textCurrent);
        UpdateBackground1Colours(background1Current);
        UpdateBackground2Colours(background2Current);
        UpdateBackground3Colours(background3Current);
        UpdateBackground4Colours(background4Current);
        UpdateBackground5Colours(background5Current);
        UpdateBackground6Colours(background6Current);
        UpdateBackground7Colours(background7Current);
        UpdateBackground8Colours(background8Current);

        GameController.instance.GetMinimapSettings();
    }

    private List<VisualElement> QueryRootForList(string className)
    {
        return rootVisualElement.Query<VisualElement>(null, className: className).ToList();
    }

    public void ResetToDefaultColours()
    {
        GetDefaultColours();
        sampleMode.value = true;
        overrideMode.value = false;
        applyNone.value = true;
        applyText.value = false;
        applyBackground1.value = false;
        applyBackground2.value = false;
        applyBackground3.value = false;
        applyBackground4.value = false;
        applyBackground5.value = false;
        applyBackground6.value = false;
        applyBackground7.value = false;
        applyBackground8.value = false;

        UpdateTextColours(textCurrent);
        UpdateBackground1Colours(background1Current);
        UpdateBackground2Colours(background2Current);
        UpdateBackground3Colours(background3Current);
        UpdateBackground4Colours(background4Current);
        UpdateBackground5Colours(background5Current);
        UpdateBackground6Colours(background6Current);
        UpdateBackground7Colours(background7Current);
        UpdateBackground8Colours(background8Current);
    }

    private void SetSlidersToColour()
    {
        redSlider.value = CurrentSampleColour.r;
        greenSlider.value = CurrentSampleColour.g;
        blueSlider.value = CurrentSampleColour.b;
        transparencySlider.value = CurrentSampleColour.a;
    }

    private void RedValueChanged(float inValue)
    {
        CurrentSampleColour.r = inValue;
        UpdateSampleColour();
    }

    private void GreenValueChanged(float inValue)
    {
        CurrentSampleColour.g = inValue;
        UpdateSampleColour();
    }

    private void BlueValueChanged(float inValue)
    {
        CurrentSampleColour.b = inValue;
        UpdateSampleColour();
    }

    private void TransparencyValueChanged(float inValue)
    {
        CurrentSampleColour.a = inValue;
        UpdateSampleColour();
    }

    private void UpdateSampleColourNoUpdate()
    {
        StyleColor styleColor = SampleDisplay.style.backgroundColor;
        styleColor.value = CurrentSampleColour;
        SampleDisplay.style.backgroundColor = styleColor;
        UpdateHexField(CurrentSampleColour);
    }

    private void UpdateSampleColour()
    {

        StyleColor styleColor = SampleDisplay.style.backgroundColor;
        styleColor.value = CurrentSampleColour;
        SampleDisplay.style.backgroundColor = styleColor;
        UpdateHexField(CurrentSampleColour);
        UpdateColours();
    }

    private void UpdateSampleColour(Color SetColour)
    {
        if (sampleMode.value)
        {
            StyleColor styleColor = SampleDisplay.style.backgroundColor;
            styleColor.value = CurrentSampleColour = SetColour;
            SampleDisplay.style.backgroundColor = styleColor;
        }
        else if (overrideMode.value)
        {
            CurrentSampleColour = SampleDisplay.style.backgroundColor.value;
            UpdateColours();
        }
        SetSlidersToColour();
    }

    private void ApplyColourModeChanged()
    {
        if (applyNone.value)
        {
            return;
        }
        if (applyText.value)
        {
            UpdateSampleColour(textCurrent);
        }
        if (applyBackground1.value)
        {
            UpdateSampleColour(background1Current);
        }
        if (applyBackground2.value)
        {
            UpdateSampleColour(background2Current);
        }
        if (applyBackground3.value)
        {
            UpdateSampleColour(background3Current);
        }
        if (applyBackground4.value)
        {
            UpdateSampleColour(background4Current);
        }
        if (applyBackground5.value)
        {
            UpdateSampleColour(background5Current);
        }
        if (applyBackground6.value)
        {
            UpdateSampleColour(background6Current);
        }
        if (applyBackground7.value)
        {
            UpdateSampleColour(background7Current);
        }
        if (applyBackground8.value)
        {
            UpdateSampleColour(background8Current);
        }
    }

    private void UpdateColours()
    {
        if (applyNone.value)
        {
            return;
        }
        if (applyText.value)
        {
            textCurrent = CurrentSampleColour;
            UpdateTextColours(textCurrent);
        }
        if (applyBackground1.value)
        {
            background1Current = CurrentSampleColour;
            UpdateBackground1Colours(background1Current);
        }
        if (applyBackground2.value)
        {
            background2Current = CurrentSampleColour;
            UpdateBackground2Colours(background2Current);
        }
        if (applyBackground3.value)
        {
            background3Current = CurrentSampleColour;
            UpdateBackground3Colours(background3Current);
        }
        if (applyBackground4.value)
        {
            background4Current = CurrentSampleColour;
            UpdateBackground4Colours(background4Current);
        }
        if (applyBackground5.value)
        {
            background5Current = CurrentSampleColour;
            UpdateBackground5Colours(background5Current);
        }
        if (applyBackground6.value)
        {
            background6Current = CurrentSampleColour;
            UpdateBackground6Colours(background6Current);
        }
        if (applyBackground7.value)
        {
            background7Current = CurrentSampleColour;
            UpdateBackground7Colours(background7Current);
        }
        if (applyBackground8.value)
        {
            background8Current = CurrentSampleColour;
            UpdateBackground8Colours(background8Current);
        }
    }

    private void UpdateTextColours(Color newColour)
    {
        for (int i = 0; i < textColourUpdateGroup.Count; i++)
        {
            StyleColor styleColor = textColourUpdateGroup[i].style.color;
            styleColor.value = newColour;
            textColourUpdateGroup[i].style.color = styleColor;
        }
        for (int i = 0; i < textColourUpdateGroupBackground.Count; i++)
        {
            StyleColor styleColor = textColourUpdateGroupBackground[i].style.backgroundColor;
            styleColor.value = newColour;
            textColourUpdateGroupBackground[i].style.backgroundColor = styleColor;
        }
        for (int i = 0; i <textColourUpdateGroupBorder.Count; i++)
        {
            StyleColor styleColor = textColourUpdateGroupBorder[i].style.color;
            styleColor.value = newColour;
            textColourUpdateGroupBorder[i].style.borderRightColor = styleColor;
            textColourUpdateGroupBorder[i].style.borderLeftColor = styleColor;
            textColourUpdateGroupBorder[i].style.borderBottomColor = styleColor;
            textColourUpdateGroupBorder[i].style.borderTopColor = styleColor;
        }
    }

    private void UpdateBackground1Colours(Color newColour)
    {
        for (int i = 0; i < background1ColourUpdateGroup.Count; i++)
        {
            StyleColor styleColor = background1ColourUpdateGroup[i].style.backgroundColor;
            styleColor.value = newColour;
            background1ColourUpdateGroup[i].style.backgroundColor = styleColor;
        }
    }

    private void UpdateBackground2Colours(Color newColour)
    {
        for (int i = 0; i < background2ColourUpdateGroup.Count; i++)
        {
            StyleColor styleColor = background2ColourUpdateGroup[i].style.backgroundColor;
            styleColor.value = newColour;
            background2ColourUpdateGroup[i].style.backgroundColor = styleColor;
        }
    }

    private void UpdateBackground3Colours(Color newColour)
    {
        for (int i = 0; i < background3ColourUpdateGroup.Count; i++)
        {
            StyleColor styleColor = background3ColourUpdateGroup[i].style.backgroundColor;
            styleColor.value = newColour;
            background3ColourUpdateGroup[i].style.backgroundColor = styleColor;
        }
    }

    private void UpdateBackground4Colours(Color newColour)
    {
        for (int i = 0; i < background4ColourUpdateGroup.Count; i++)
        {
            StyleColor styleColor = background4ColourUpdateGroup[i].style.backgroundColor;
            styleColor.value = newColour;
            background4ColourUpdateGroup[i].style.backgroundColor = styleColor;
        }
    }

    private void UpdateBackground5Colours(Color newColour)
    {
        for (int i = 0; i < background5ColourUpdateGroup.Count; i++)
        {
            StyleColor styleColor = background5ColourUpdateGroup[i].style.backgroundColor;
            styleColor.value = newColour;
            background5ColourUpdateGroup[i].style.backgroundColor = styleColor;
        }
    }

    private void UpdateBackground6Colours(Color newColour)
    {
        for (int i = 0; i < background6ColourUpdateGroup.Count; i++)
        {
            StyleColor styleColor = background6ColourUpdateGroup[i].style.backgroundColor;
            styleColor.value = newColour;
            background6ColourUpdateGroup[i].style.backgroundColor = styleColor;
        }
    }

    private void UpdateBackground7Colours(Color newColour)
    {
        for (int i = 0; i < background7ColourUpdateGroup.Count; i++)
        {
            StyleColor styleColor = background7ColourUpdateGroup[i].style.backgroundColor;
            styleColor.value = newColour;
            background7ColourUpdateGroup[i].style.backgroundColor = styleColor;
        }
    }

    private void UpdateBackground8Colours(Color newColour)
    {
        for (int i = 0; i < background8ColourUpdateGroup.Count; i++)
        {
            StyleColor styleColor = background8ColourUpdateGroup[i].style.backgroundColor;
            styleColor.value = newColour;
            background8ColourUpdateGroup[i].style.backgroundColor = styleColor;
        }
    }

    private void HexToRGBA(string value)
    {
        if (value[0] != '#')
        {
            value = "#" + value;
        }
        if (ColorUtility.TryParseHtmlString(value, out Color newColour))
        {
            CurrentSampleColour = newColour;
            UpdateSampleColour();
        }
    }

    private void UpdateHexField(Color colour)
    {
        textField.value = "#" + ColorUtility.ToHtmlStringRGBA(colour);
    }

    private void OnDisable()
    {
        UIColours.textCurrent = textCurrent;
        UIColours.background1Current = background1Current;
        UIColours.background2Current = background2Current;
        UIColours.background3Current = background3Current;
        UIColours.background4Current = background4Current;

        UIColours.background5Current = background5Current;
        UIColours.background6Current = background6Current;
        UIColours.background7Current = background7Current;
        UIColours.background8Current = background8Current;

        OnNewColours?.Invoke(new ColourChangedEventArgs
        {
            textCurrent = UIColours.textCurrent,
            background1Current = UIColours.background1Current,
            background2Current = UIColours.background2Current,
            background3Current = UIColours.background3Current,
            background4Current = UIColours.background4Current,
            background5Current = UIColours.background5Current,
            background6Current = UIColours.background6Current,
            background7Current = UIColours.background7Current,
            background8Current = UIColours.background8Current
        });
    }
}
