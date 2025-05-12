using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PlayTable;

public class PTUI_RadialMenu_Tier : MonoBehaviour {

    public float radius = 1.0f;
    public float buttonDistance = 30;
    public PTUI_RadialMenu_Button buttonPref;
    //public float speed = 1500;
    public float animationTimer = 0.2f;

    public string value;// { get; private set; }

    [Range(0,360)]
    public float arc = 360.0f;
    [Range(-180, 180)]
    public float offset = 0f;
    private RadialSection startFrom;
    private bool updatingContent = false;

    public Text text;

    void UpdateChildrenPosition()
    {
        int i = 0;
        float angleStep = arc / (transform.childCount - 1);
        Vector3 direction;

        if (startFrom == RadialSection.down) direction = -transform.forward;
        else if (startFrom == RadialSection.right) direction = transform.right;
        else if (startFrom == RadialSection.left) direction = -transform.right;
        else direction = transform.forward;

        foreach (Transform t in transform)
        {
            t.position = transform.position + Quaternion.Euler(0, offset + angleStep * i, 0) * direction * radius;
            i++;
        }
    }

    internal void SetValue(string newValue)
    {
        //set value
        value = newValue;

        //grey out
        foreach (Transform child in transform)
        {
            //icon
            if (child.GetSiblingIndex() == 0)
                continue;

            PTUI_RadialMenu_Button currButton = child.GetComponent<PTUI_RadialMenu_Button>();
            if (value == default(string) || value == "")
            {
                currButton.SetHighlight(HighlightStatus.Default);
            }
            else
            {
                if (child.GetComponentInChildren<Text>().text == value)
                {
                    currButton.SetHighlight(HighlightStatus.Highlight);
                }
                else
                {
                    currButton.SetHighlight(HighlightStatus.UnHighlight);
                }
            }
        }

        //UpdateButtonOK
        if (GetComponentInParent<PTUI_RadialMenu>().OnSetValue != null)
        {
            GetComponentInParent<PTUI_RadialMenu>().OnSetValue();
        }
    }

    public IEnumerator UpdateContent(string iconStr, string[] content, RadialSection section, PTLogoColor color, Font fontText, Font fontIcon)
    {
        if (!updatingContent //Make sure only one coroutine is running at the same time
            && (startFrom != section || transform.childCount != content.Length + 1) //Content inconsist
            )
        {
            updatingContent = true;

            //Reset
            Reset();

            if (section != RadialSection.inside && section != RadialSection.outside && section != RadialSection.unknown)
            {
                //Calculate new parameters of radail button group
                float targetArc = buttonDistance * (content.Length + 1);
                arc = 0;
                offset = -targetArc / 2.0f;
                startFrom = section;

                //Instantiate empty buttons for animation
                if (content.Length > 0)
                    Instantiate(buttonPref.gameObject, transform).GetComponent<PTUI_RadialMenu_Button>().Set(color, fontIcon, iconStr);
                foreach (string str in content)
                {
                    PTUI_RadialMenu_Button newButton = Instantiate(buttonPref.gameObject, transform).GetComponent<PTUI_RadialMenu_Button>();
                    newButton.Set(color, fontText, "");
                    newButton.name = "Button " + str;
                }

                //Animation
                float initArc = arc;
                float coveredTimer = 0;
                while (arc < targetArc)
                {
                    yield return new WaitForEndOfFrame();
                    coveredTimer += Time.deltaTime;
                    float frac = coveredTimer / animationTimer;
                    arc = initArc + (targetArc - initArc) * frac;
                    arc = arc < targetArc ? arc : targetArc;
                    UpdateChildrenPosition();
                }

                //Set the buttons to actually value
                if (transform.childCount > 0)
                {
                    transform.GetChild(0).GetComponent<PTUI_RadialMenu_Button>().Set(color, fontIcon, iconStr);
                }
                try
                {
                    for (int i = 0; i < content.Length; i++)
                    {
                        if (transform.childCount > i + 1 && transform.GetChild(i + 1) != null)
                        {
                            //Get currButton
                            Transform currButton = transform.GetChild(i + 1);
                            currButton.GetComponent<PTUI_RadialMenu_Button>().Set(color, fontText, content[i]);
                            Text childText = currButton.GetComponentInChildren<Text>();

                            Collider colliderCurrButton = currButton.GetComponent<Collider>();
                            PTGlobalInput_new.OnTouchEnter += (PTTouch touch, Collider collider) =>
                            {
                                if (collider == colliderCurrButton)
                                {
                                    if (value == default(string) || value == "")
                                    {
                                        SetValue(childText.text);
                                    }
                                }
                            };
                            PTGlobalInput_new.OnShortHoldBegin += (PTTouch touch) =>
                            {
                                if (colliderCurrButton && touch.hits.ContainsKey(colliderCurrButton))
                                {
                                    SetValue(childText.text);
                                }
                            };
                        }
                        else
                        {
                            break;
                        }
                    }
                }
                catch { }
            }

            //Turn off updatingContent
            updatingContent = false;
        }
    }
    public void Reset()
    {
        //Reset
        updatingContent = false;
        SetValue(default(string));
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
    }
}
