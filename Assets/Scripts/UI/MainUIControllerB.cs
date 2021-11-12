using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class MainUIControllerB : MonoBehaviour
{
    public ColourSettingsController coloursApplicator;
    public UIDocument document;
    private VisualElement rootVisualElement;

    private Button SettingsButton;

    public List<VisualElement> textColourUpdateGroup = new List<VisualElement>();
    public List<VisualElement> textColourUpdateGroupImageTint = new List<VisualElement>();

    public List<VisualElement> background1ColourUpdateGroup = new List<VisualElement>();

    public List<VisualElement> background2ColourUpdateGroup = new List<VisualElement>();

    public List<VisualElement> background3ColourUpdateGroup = new List<VisualElement>();
    public List<VisualElement> background3ColourUpdateGroupImageTint = new List<VisualElement>();

    public List<VisualElement> background4ColourUpdateGroup = new List<VisualElement>();

    private void Awake()
    {
        coloursApplicator.OnNewColours += ColourChanged_OnChangedEvent;

        rootVisualElement = document.rootVisualElement;
    }

    private void OnEnable()
    {
        SettingsButton = rootVisualElement.Q<Button>("SettingsButton");

        SettingsButton.RegisterCallback<ClickEvent>(ev => ClickEvent_OpenColourMenu());

        textColourUpdateGroup.AddRange(QueryRootForList("unity-text-element"));

        background1ColourUpdateGroup.AddRange(QueryRootForList("background-1"));

        background2ColourUpdateGroup.AddRange(QueryRootForList("background-2"));
        background2ColourUpdateGroup.AddRange(QueryRootForList("unity-scroller"));

        background3ColourUpdateGroup.AddRange(QueryRootForList("background-3"));
        background3ColourUpdateGroup.AddRange(QueryRootForList("unity-base-slider__dragger"));
        background3ColourUpdateGroupImageTint.AddRange(QueryRootForList("unity-repeat-button"));

        background4ColourUpdateGroup.AddRange(QueryRootForList("background-4"));

    }

    private void ClickEvent_OpenColourMenu()
    {
        Debug.Log("clickevent");
        if (!coloursApplicator.gameObject.activeInHierarchy)
        {
            coloursApplicator.gameObject.SetActive(true);
        }
    }

    private List<VisualElement> QueryRootForList(string className)
    {
        return rootVisualElement.Query<VisualElement>(null, className: className).ToList();
    }


    private void ColourChanged_OnChangedEvent(ColourChangedEventArgs e)
    {
        UpdateTextColours(e.textCurrent);
        UpdateBackground1Colours(e.background1Current);
        UpdateBackground2Colours(e.background2Current);
        UpdateBackground3Colours(e.background3Current);
        UpdateBackground4Colours(e.background4Current);
    }

    private void UpdateTextColours(Color newColour)
    {
        for (int i = 0; i < textColourUpdateGroup.Count; i++)
        {
            StyleColor styleColor = textColourUpdateGroup[i].style.color;
            styleColor.value = newColour;
            textColourUpdateGroup[i].style.color = styleColor;
        }
        for (int i = 0; i < textColourUpdateGroupImageTint.Count; i++)
        {
            StyleColor styleColor = textColourUpdateGroupImageTint[i].style.unityBackgroundImageTintColor;
            styleColor.value = newColour;
            textColourUpdateGroupImageTint[i].style.unityBackgroundImageTintColor = styleColor;
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

        for (int i = 0; i < background3ColourUpdateGroupImageTint.Count; i++)
        {
            StyleColor styleColor = background3ColourUpdateGroupImageTint[i].style.unityBackgroundImageTintColor;
            styleColor.value = newColour;
            background3ColourUpdateGroupImageTint[i].style.unityBackgroundImageTintColor = styleColor;
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
}
