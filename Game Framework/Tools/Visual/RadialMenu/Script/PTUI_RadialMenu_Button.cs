using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PlayTable;
using UnityEngine.EventSystems;

public class PTUI_RadialMenu_Button : MonoBehaviour {
    private Vector3 initLocalScale;
    private const float scaleupRate = 1.1f;

    private void Awake()
    {
        initLocalScale = transform.localScale;
    }

    public void Set(PTLogoColor color, Font font, string value)
    {
        GetComponentInChildren<Image>().color = PT.GetOfficialColor(color);
        GetComponentInChildren<Text>().text = value;
        GetComponentInChildren<Text>().font = font;
    }

    public void SetHighlight(HighlightStatus status)
    {
        switch (status)
        {
            case HighlightStatus.Default:
                transform.localScale = initLocalScale;
                if (GetComponentInChildren<Button>())
                {
                    GetComponentInChildren<Button>().interactable = true;
                }
                if ((Behaviour)GetComponent("Halo"))
                {
                    ((Behaviour)GetComponent("Halo")).enabled = false;
                }
                break;
            case HighlightStatus.Highlight:
                transform.localScale = initLocalScale * scaleupRate;
                if (GetComponentInChildren<Button>())
                {
                    GetComponentInChildren<Button>().interactable = true;
                }
                if ((Behaviour)GetComponent("Halo"))
                {
                    ((Behaviour)GetComponent("Halo")).enabled = true;
                }
                break;
            case HighlightStatus.UnHighlight:
                transform.localScale = initLocalScale;
                if (GetComponentInChildren<Button>())
                {
                    GetComponentInChildren<Button>().interactable = false;
                }
                if ((Behaviour)GetComponent("Halo"))
                {
                    ((Behaviour)GetComponent("Halo")).enabled = false;
                }
                break;
        }
    }
}

