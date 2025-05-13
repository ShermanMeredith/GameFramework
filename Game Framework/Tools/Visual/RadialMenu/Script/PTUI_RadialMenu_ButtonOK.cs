using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayTable;

public enum HighlightStatus { Default, Highlight, UnHighlight }

public class PTUI_RadialMenu_ButtonOK : MonoBehaviour {
    private Vector3 initLocalScale;
    private const float scaleupRate = 1.1f;
    private Collider myCollider;

    private void Awake()
    {
        initLocalScale = transform.localScale;
        myCollider = GetComponent<Collider>();
        PTGlobalInput.OnTouchEnd += (PTTouch touch) =>
        {
            if (myCollider && touch.hits.ContainsKey(myCollider))
            {
                SetHighlight(HighlightStatus.UnHighlight);
            }
        };
        PTGlobalInput.OnTouchInside += (PTTouch touch, Collider collider) =>
        {
            if (collider == myCollider)
            {
                SetHighlight(HighlightStatus.Highlight);
            }
        };
        PTGlobalInput.OnTouchExit += (PTTouch touch, Collider collider) =>
        {
            if (collider == myCollider)
            {
                SetHighlight(HighlightStatus.UnHighlight);
            }
        };
        PTGlobalInput.OnTouchEnter += (PTTouch touch, Collider collider) =>
        {
            if (collider == myCollider)
            {
                SetHighlight(HighlightStatus.Highlight);
            }
        };
    }

    public void SetHighlight(HighlightStatus status)
    {
        switch (status)
        {
            case HighlightStatus.Default:
                transform.localScale = initLocalScale;
                if ((Behaviour)GetComponent("Halo"))
                {
                    ((Behaviour)GetComponent("Halo")).enabled = false;
                }
                break;
            case HighlightStatus.Highlight:
                transform.localScale = initLocalScale * scaleupRate;
                if ((Behaviour)GetComponent("Halo"))
                {
                    ((Behaviour)GetComponent("Halo")).enabled = true;
                }
                break;
            case HighlightStatus.UnHighlight:
                transform.localScale = initLocalScale;
                if ((Behaviour)GetComponent("Halo"))
                {
                    ((Behaviour)GetComponent("Halo")).enabled = false;
                }
                break;
        }
    }
}
