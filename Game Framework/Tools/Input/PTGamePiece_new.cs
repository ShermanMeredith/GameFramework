using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayTable;

/// <summary>
/// PTGamePiece is an object in scene that is draggable and droppable into zones. Examples include: Cards, Tokens...
/// </summary>
public class PTGamePiece_new : PTLocalInput_new
{
    #region fields
    [SerializeField] private bool isDraggable = false;
    public bool IsDraggable 
    { 
        get { return isDraggable; } 
        set { isDraggable = value; } 
    }
    public bool isCenteredOnDrag = true;
    [SerializeField]
    private bool defaultDragReject;
    [SerializeField]
    private float draggedScaleOffset = 0.2f, hoverScaleOffset = -0.1f, draggedPositionOffset = 2f;
    private Vector3 startingScale;
    private Vector3 hoverScale;

    private PTZone_new hoverZone = null;

    [SerializeField]
    private float touchFeedbackScale = 1.2f;
    private Coroutine touchFeedbackCoroutine;
    private Vector3 pretouchLocalScale;
    private bool isTouched;
    #endregion

    #region properties
    #endregion

    #region delegates
    public PTDelegateZone OnHoverEnter;
    public PTDelegateZone OnHover;
    public PTDelegateZone OnHoverExit;
    public PTDelegateVoid OnDragReject;
    public PTDelegateZone OnZoneAddReject;
    #endregion

    protected override void Awake()
    {
        base.Awake();
        dragWorldPositionOffset = new Vector3(0, draggedPositionOffset, 0);
        hoverScale = transform.GetWorldScale() * (draggedScaleOffset - hoverScaleOffset);
        dragWorldScaleOffset = transform.GetWorldScale() * draggedScaleOffset;

        // Inputs
        OnTouchBegin += (touch) =>
        {
            startingScale = transform.GetWorldScale();
            hoverScale = transform.GetWorldScale() * (draggedScaleOffset + hoverScaleOffset);
            dragWorldScaleOffset = transform.GetWorldScale() * draggedScaleOffset;
            if (!isTouched)
            {
                touchFeedbackCoroutine = StartCoroutine(TouchFeedbackCoroutine());
            }
        };
        OnDrag += (PTTouch touch) =>
        {
            // check if this object is being dragged
            PTTouchFollower follower = touch.FindFollowerBy(GetComponent<Collider>());
            if (follower != null)
            {
                List<PTZone_new> hitZones = follower.hitZones;
                PTZone_new hitZone = null;
                foreach (PTZone_new zone in hitZones)
                {
                    if (zone.Accepts(transform))
                    {
                        hitZone = zone;
                        break;
                    }
                }
                // you are hovering over a hitzone
                if (hitZone != null)
                {
                    // hover enter from no zone
                    if(hoverZone == null)
                    {
                        hoverZone = hitZone;

                        if (OnHoverEnter != null)
                        {
                            OnHoverEnter(hoverZone);
                        }
                        if(hoverZone.OnHoverEnter != null)
                        {
                            hoverZone.OnHoverEnter(follower);
                        }
                    }
                    // hover enter from another zone that is not this hitzone
                    else if (hoverZone != null && hoverZone != hitZone)
                    {
                        //exit the previous zone
                        if (hoverZone.OnHoverExit != null)
                        {
                            hoverZone.OnHoverExit(follower);
                        }
                        // enter this zone
                        hoverZone = hitZone;
                        if (hoverZone.OnHoverEnter != null) 
                        {
                            hoverZone.OnHoverEnter(follower);
                        }
                    }
                    // hover continue
                    if (OnHover != null)
                    {
                        OnHover(hitZone);
                    }
                    if(hoverZone != null && hoverZone.OnHover != null)
                    {
                        hoverZone.OnHover(follower);
                    }
                }
                // you are not hovering over a hitzone
                else
                {
                    if (OnHoverExit != null) 
                    {
                        OnHoverExit(hitZone);
                    }
                    // exit the previous hoverzone
                    if(hoverZone != null && hoverZone.OnHoverExit != null)
                    {
                        hoverZone.OnHoverExit(follower);
                    }
                    // hoverzone is now null
                    hoverZone = hitZone;
                }
            }
            // Dragged when not able to be dragged
            else
            {
                if (OnDragReject != null)
                {
                    OnDragReject();
                }
            }
        };

        OnHoverEnter += (hitZone) =>
        {
            transform.SetWorldScale(startingScale + hoverScale, PT.DEFAULT_TIMER);
        };

        OnHoverExit += (hitZone) =>
        {
            transform.SetWorldScale(startingScale + dragWorldScaleOffset, PT.DEFAULT_TIMER);
        };

        OnDragReject += () =>
        {
            if (defaultDragReject)
            {
                // fire masteraudio event
                StartCoroutine(transform.WiggleCoroutine());
            }
        };

        OnZoneAddReject += (PTZone_new zone) =>
        {
            SendBack();
        };

        OnDragBegin += (touch) =>
        {
            if (touchFeedbackCoroutine != null)
            {
                StopCoroutine(touchFeedbackCoroutine);
                transform.localScale = pretouchLocalScale;
                isTouched = false;
            }
        };
    }

    public IEnumerator TouchFeedbackCoroutine()
    {
        isTouched = true;
        pretouchLocalScale = transform.localScale;
        yield return transform.SetLocalScaleCoroutine(pretouchLocalScale * touchFeedbackScale, PT.DEFAULT_TIMER / 8);
        yield return transform.SetLocalScaleCoroutine(pretouchLocalScale, PT.DEFAULT_TIMER / 2);
        isTouched = false;
    }

    public void SetScaleOffset(float offset)
    {
        draggedScaleOffset = offset;
        dragWorldScaleOffset = transform.GetWorldScale() * draggedScaleOffset;
    }

    public void SetPositionOffset(float offset)
    {
        draggedPositionOffset = offset;
        dragWorldPositionOffset = new Vector3(0, draggedPositionOffset, 0);
    }

    public void SetHoverOffset(float offset)
    {
        hoverScaleOffset = offset;
        hoverScale = transform.GetWorldScale() * (draggedScaleOffset - hoverScaleOffset);
    }
}