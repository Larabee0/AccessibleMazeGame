using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class MainUIControllerA : MonoBehaviour
{
    public ColourSettingsController coloursApplicator;
    public VisualTreeAsset shipElementPrefab;

    public UIDocument document;
    private VisualElement rootVisualElement;

    private VisualElement shipActionContainer;
    private VisualElement shipButtonContainer;
    private ShipUIElement selected;

    private Button attackButton;
    private Button moveButton;
    private Button deselectButton;
    private Button evadeButton;

    public List<VisualElement> textColourUpdateGroup = new List<VisualElement>();
    public List<VisualElement> textColourUpdateGroupImageTint = new List<VisualElement>();

    public List<VisualElement> background1ColourUpdateGroup = new List<VisualElement>();

    public List<VisualElement> background2ColourUpdateGroup = new List<VisualElement>();

    public List<VisualElement> background3ColourUpdateGroup = new List<VisualElement>();
    public List<VisualElement> background3ColourUpdateGroupImageTint = new List<VisualElement>();

    public List<VisualElement> background4ColourUpdateGroup = new List<VisualElement>();

    private ScrollView outliner;

    public List<ShipUIElement> outLinerElements = new List<ShipUIElement>();

    public Dictionary<TemplateContainer, ShipUIElement> outlinerElementsDict = new Dictionary<TemplateContainer, ShipUIElement>();

    private void Awake()
    {
        ShipUIElement.ElementPrefab = shipElementPrefab;
        coloursApplicator.OnNewColours += ColourChanged_OnChangedEvent;

        rootVisualElement = document.rootVisualElement;
    }


    // Start is called before the first frame update
    private void OnEnable()
    {
        outliner = rootVisualElement.Q<ScrollView>("OutlinerList");
        shipActionContainer = rootVisualElement.Q<VisualElement>("ShipContainer");
        shipButtonContainer = rootVisualElement.Q<VisualElement>("ButtonContainer");

        attackButton = shipButtonContainer.Q<Button>("Attack");
        moveButton = shipButtonContainer.Q<Button>("Move");
        deselectButton = shipButtonContainer.Q<Button>("Deselect");
        evadeButton = shipButtonContainer.Q<Button>("Evade");

        attackButton.RegisterCallback<ClickEvent>(ev => ClickEvent_AttackButton());
        moveButton.RegisterCallback<ClickEvent>(ev => ClickEvent_MoveButton());
        deselectButton.RegisterCallback<ClickEvent>(ev => ClickEvent_DeselectButton());
        evadeButton.RegisterCallback<ClickEvent>(ev => ClickEvent_EvadeButton());

        textColourUpdateGroup.AddRange(QueryRootForList("unity-text-element"));

        background1ColourUpdateGroup.AddRange(QueryRootForList("background-1"));

        background2ColourUpdateGroup.AddRange(QueryRootForList("background-2"));
        background2ColourUpdateGroup.AddRange(QueryRootForList("unity-scroller"));

        background3ColourUpdateGroup.AddRange(QueryRootForList("background-3"));
        background3ColourUpdateGroup.AddRange(QueryRootForList("unity-base-slider__dragger"));
        background3ColourUpdateGroupImageTint.AddRange(QueryRootForList("unity-repeat-button"));

        background4ColourUpdateGroup.AddRange(QueryRootForList("background-4"));

        for (int i = 0; i < 30; i++)
        {
            AddElementToOutLinter(i, 100, "Ship " + i.ToString());
        }
    }

    private List<VisualElement> QueryRootForList(string className)
    {
        return rootVisualElement.Query<VisualElement>(null, className: className).ToList();
    }

    // Update is called once per frame
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Debug.Log("Escape Key Pressed");
            if (!coloursApplicator.gameObject.activeInHierarchy)
            {
                coloursApplicator.gameObject.SetActive(true);
            }
            else
            {
                coloursApplicator.gameObject.SetActive(false);
            }
        }
    }

    private void ClickEvent_Outliner(TemplateContainer sender)
    {
        if(selected != null)
        {
            return;
        }
        selected = outlinerElementsDict[sender];
        shipActionContainer.Add(sender);
    }

    private void ClickEvent_AttackButton()
    {
        if(selected == null)
        {
            return;
        }
        selected.Health = 50f;
        Debug.Log("Attack button registered|Ship ID: " + selected.ID);
    }

    private void ClickEvent_MoveButton()
    {
        if (selected == null)
        {
            return;
        }
        Debug.Log("Move button registered|Ship ID: " + selected.ID);
    }

    private void ClickEvent_DeselectButton()
    {
        if (selected == null)
        {
            return;
        }
        outliner.Add(selected.root);
        selected = null;
    }

    private void ClickEvent_EvadeButton()
    {
        if (selected == null)
        {
            return;
        }
        Debug.Log("Evade button registered|Ship ID: " + selected.ID);
    }

    public void AddElementToOutLinter(int ID, float health, string name)
    {
        ShipUIElement shipElement = ShipUIElement.Create(outliner, ID, name, health);
        outLinerElements.Add(shipElement);

        TemplateContainer root = shipElement.root;
        outlinerElementsDict.Add(root, shipElement);
        root.name = ID.ToString();
        root.RegisterCallback<ClickEvent>(ev => ClickEvent_Outliner(root));
        textColourUpdateGroup.AddRange(root.Query<VisualElement>(null, className: "unity-text-element").ToList());
        textColourUpdateGroupImageTint.Add(root.Q<VisualElement>("Icon"));

        background2ColourUpdateGroup.AddRange(root.Query<VisualElement>(null, className: "background-2").ToList());

        background3ColourUpdateGroup.AddRange(rootVisualElement.Query<VisualElement>(null, className: "unity-progress-bar__progress").ToList());

        background4ColourUpdateGroup.AddRange(rootVisualElement.Query<VisualElement>(null, className: "background-4").ToList());
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

public class ShipUIElement
{
    public static VisualTreeAsset ElementPrefab;

    public TemplateContainer root;
    public ProgressBar healthBar;
    public VisualElement icon;
    public Label name;
    public int ID;

    public string NameText { 
        set
        {
            name.text = value;
        }
        get
        {
            return name.text;
        }
    }

    public float Health
    {
        set
        {
            healthBar.value = value;
        }
    }

    public static ShipUIElement Create()
    {
        TemplateContainer newElement = ElementPrefab.Instantiate();
        ShipUIElement element = new ShipUIElement
        {
            root = newElement,
            healthBar = newElement.Q<ProgressBar>("healthBar"),
            name = newElement.Q<Label>("NameField"),
            icon = newElement.Q<VisualElement>("Icon"),
        };

        return element;
    }

    public static ShipUIElement Create(int ID)
    {
        TemplateContainer newElement = ElementPrefab.Instantiate();
        ShipUIElement element = new ShipUIElement
        {
            root = newElement,
            healthBar = newElement.Q<ProgressBar>("healthBar"),
            name = newElement.Q<Label>("NameField"),
            icon = newElement.Q<VisualElement>("Icon"),
            ID = ID,
        };

        return element;
    }

    public static ShipUIElement Create(VisualElement parent, int ID)
    {
        TemplateContainer newElement = ElementPrefab.Instantiate();
        parent.Add(newElement);
        ShipUIElement element = new ShipUIElement
        {
            root = newElement,
            healthBar = newElement.Q<ProgressBar>("healthBar"),
            name = newElement.Q<Label>("NameField"),
            icon = newElement.Q<VisualElement>("Icon"),
            ID = ID,
        };
        return element;
    }

    public static ShipUIElement Create(VisualElement parent, int ID, string name, float health = 100f)
    {
        TemplateContainer newElement = ElementPrefab.Instantiate();
        parent.Add(newElement);
        ShipUIElement element = new ShipUIElement
        {
            root = newElement,
            healthBar = newElement.Q<ProgressBar>("healthBar"),
            name = newElement.Q<Label>("NameField"),
            icon = newElement.Q<VisualElement>("Icon"),
            ID = ID,
        };
        element.NameText = name;
        element.Health = health;
        return element;
    }
}