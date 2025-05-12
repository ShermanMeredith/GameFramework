using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayTable;
using UnityEngine.UI;
using System;

/// <summary>
/// The final output of the radial menu
/// </summary>
[Serializable]
public class RadialSentence
{
    public RadialSection section = RadialSection.unknown;
    public string value0 = default(string);
    public string value1 = default(string);

    public override string ToString()
    {
        return JsonUtility.ToJson(this);
    }
}

public class PTUI_RadialMenu : MonoBehaviour {
    public bool isComplete { get { return sections.GetComponent<Image>().fillAmount == 1; } }
    public bool isInvisible { get { return sections.GetComponent<Image>().fillAmount == 0; } }
    public Vector3 positionOffset = new Vector3(0, 0.51f, 0);
    public PTUI_RadialMenu_Sections sections;
    public Transform radialBorderInside;
    public Transform radialBorderOutside;
    public PTUI_RadialMenu_Tier tier0;
    public PTUI_RadialMenu_Tier tier1;
    public PTUI_RadialMenu_ButtonOK buttonOK;
    public bool isOnButtonOK { get; private set; }

    public delegate void VoidDelegate();
    public VoidDelegate OnSubmit;
    public VoidDelegate OnSetValue;
    public VoidDelegate OnSectionSwitched;
    public VoidDelegate OnAppear;
    public VoidDelegate OnDisappear;

    private Quaternion initRotation;
    private const float radiusInner = 1.6f;
    private const float radiusMiddle = 2.6f;
    private const float radiusOutter = 3.5f;


    public RadialSentence sentence { get {
            RadialSentence ret = new RadialSentence();
            ret.section = sections.section;
            ret.value0 = tier0.value;
            ret.value1 = tier1.value;
            return ret;
        }
    }
    public bool isSentenceCompleted
    {
        get
        {
            PTUI_RadialMenu_Section currSection = sections.GetSection(sentence.section);
            bool validTier0 = currSection != null && (currSection.content0.Length == 0 || sentence.value0 != default(string) && sentence.value0 != "");
            bool validTier1 = currSection != null && (currSection.content1.Length == 0 || sentence.value1 != default(string) && sentence.value1 != "");

            return sentence.section != RadialSection.unknown
                && sentence.section != RadialSection.inside
                && sentence.section != RadialSection.outside
                && validTier0
                && validTier1;
        }
    }

    private void Awake()
    {
        initRotation = transform.rotation;
        isOnButtonOK = false;

        //ButtonOK
        Collider colliderButtonOK = buttonOK.GetComponent<Collider>();
        PTGlobalInput_new.OnTouchEnd += (PTTouch touch) =>
        {
            if (colliderButtonOK && touch.hits.ContainsKey(colliderButtonOK))
            {
                if (OnSubmit != null)
                {
                    OnSubmit();
                }
                isOnButtonOK = false;
                Reset();
                StartCoroutine(sections.Toggle(false));
            }
        };
        PTGlobalInput_new.OnTouchInside += (PTTouch touch, Collider collider) =>
        {
            if (collider == colliderButtonOK)
            {
                isOnButtonOK = true;
            }
        };
        PTGlobalInput_new.OnTouchExit += (PTTouch touch, Collider collider) =>
        {
            if (collider == colliderButtonOK)
            {
                isOnButtonOK = false;

            }
        };

        //Misc
        OnSetValue += () =>
        {
            buttonOK.GetComponentInChildren<Image>().enabled = isSentenceCompleted;
            buttonOK.GetComponentInChildren<Collider>().enabled = buttonOK.GetComponentInChildren<Image>().enabled;
            float radius = radiusOutter;
            if (sentence.value1 == "" || sentence.value1 == default(string))
            {
                radius = radiusMiddle;
            }
            if (sentence.value0 == "" || sentence.value0 == default(string))
            {
                radius = radiusInner;
            }
            buttonOK.transform.position = transform.position + Quaternion.Euler(0, 90 * (int)sentence.section, 0) * transform.forward * radius;
            buttonOK.transform.rotation = Quaternion.Euler(0, 90 * (int)sentence.section, 0);
        };
        sections.OnSectionSwitched += (RadialSection section) =>
        {
            if (OnSectionSwitched != null)
            {
                OnSectionSwitched();
            }

            if (section == RadialSection.inside)
            {
                tier0.Reset();
                tier1.Reset();
            }
            else
            {
                PTUI_RadialMenu_Section currSection = sections.GetSection(section);
                if (currSection != null)
                {
                    StartCoroutine(tier0.UpdateContent(currSection.iconStr0, currSection.content0, section, currSection.color, currSection.fontText0, currSection.fontIcon0));
                    StartCoroutine(tier1.UpdateContent(currSection.iconStr1, currSection.content1, section, currSection.color, currSection.fontText1, currSection.fontIcon1));
                }
            }
        };
    }
    private void LateUpdate()
    {
        transform.position = transform.parent.position + positionOffset;
        transform.rotation = initRotation;
    }
    public void Reset()
    {
        StartCoroutine(sections.Toggle(false));
        buttonOK.GetComponentInChildren<Image>().enabled = false;
        buttonOK.GetComponentInChildren<Collider>().enabled = false;
        tier0.Reset();
        tier1.Reset();

        if (OnDisappear != null)
        {
            OnDisappear();
        }
    }
    public void Appear()
    {
        StartCoroutine(sections.Toggle(true));
        if(OnAppear != null)
        {
            OnAppear();
        }
    }

    public void SelecetSection(PTTouch touch)
    {
        if (sections.GetComponent<Image>().fillAmount < 1)
        {
            return;
        }

        Vector2 screenPosition = Camera.main.WorldToScreenPoint(transform.position);
        Vector2 screenPosition_BorderInside = Camera.main.WorldToScreenPoint(radialBorderInside.position);
        Vector2 screenPosition_BorderOutside = Camera.main.WorldToScreenPoint(radialBorderOutside.position);
        Vector2 diffTouch = new Vector2(touch.position.x, touch.position.y) - screenPosition;
        Vector2 diffMin = screenPosition_BorderInside - screenPosition;
        Vector2 diffMax = screenPosition_BorderOutside - screenPosition;

        RadialSection section = RadialSection.unknown;

        float angle = PTUtility.Angle(screenPosition, touch.position) + 45;
        angle = angle > 360 ? angle - 360 : angle;
        int direction = (int)(angle / 90);

        if (diffTouch.sqrMagnitude < diffMin.sqrMagnitude)
        {
            section = RadialSection.inside;
        }
        else if (diffTouch.sqrMagnitude > diffMax.sqrMagnitude)
        {
            section = RadialSection.outside;
        }
        else
        {
            section = (RadialSection)direction;
        }

        sections.Input(section);
    }
}
