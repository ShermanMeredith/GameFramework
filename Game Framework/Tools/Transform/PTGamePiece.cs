using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayTable;

/// <summary>
/// PTGamePiece is an object in scene that is draggable and droppable into zones. Examples include: Cards, Tokens...
/// </summary>
public class PTGamePiece : PTLocalInput
{
    #region fields
    [SerializeField]
    private bool defaultDragReject;
    [SerializeField]
    private float draggedScaleOffset = 0.2f, hoverScaleOffset = -0.1f, draggedPositionOffset = 2f;
    private Vector3 startingScale;
    private Vector3 hoverScale;

    private PTZone hoverZone = null;
    #endregion

    #region properties
    #endregion

    #region delegates
    PTDelegateVoid OnHoverEnter;
    PTDelegateVoid OnHover;
    PTDelegateVoid OnHoverExit;
    PTDelegateVoid OnDragReject;
    PTDelegateZone OnZoneAddReject;
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
        };
        OnDrag += (PTTouch touch) =>
        {
            // check if this object is being dragged
            PTTouchFollower follower = touch.FindFollowerBy(GetComponent<Collider>());
            if (follower != null)
            {
                List<PTZone> hitZones = follower.hitZones;
                PTZone hitZone = null;
                foreach (PTZone zone in hitZones)
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

                        OnHoverEnter?.Invoke();
                        hoverZone.OnHoverEnter?.Invoke(follower);
                    }
                    // hover enter from another zone that is not this hitzone
                    else if (hoverZone != null && hoverZone != hitZone)
                    {
                        //exit the previous zone
                        hoverZone.OnHoverExit?.Invoke(follower);
                        // enter this zone
                        hoverZone = hitZone;
                        hoverZone.OnHoverEnter?.Invoke(follower);
                    }
                    // hover continue
                    OnHover?.Invoke();
                    hoverZone.OnHover?.Invoke(follower);
                }
                // you are not hovering over a hitzone
                else
                {
                    OnHoverExit?.Invoke();
                    // exit the previous hoverzone
                    hoverZone?.OnHoverExit?.Invoke(follower);
                    // hoverzone is now null
                    hoverZone = hitZone;
                }
            }
            // Dragged when not able to be dragged
            else
            {
                OnDragReject?.Invoke();
            }
        };

        OnHoverEnter += () =>
        {
            transform.SetWorldScale(startingScale + hoverScale, PT.DEFAULT_TIMER);
        };

        OnHoverExit += () =>
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

        OnZoneAddReject += (PTZone zone) =>
        {
            SendBack();
        };
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