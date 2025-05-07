using System;
using System.Collections;
using PlayTable;
using UnityEngine;

public class PTSeatSelector : MonoBehaviour {

    public PTGamePiece buttonRemove;

    private PTGamePiece localInput;
    private bool isBeingDragged;
    private PTSeat lastSeat;
    public PTSeat seat { get { return GetComponentInParent<PTSeat>(); } }
    public PTLayoutZone playerPoolZone {  get { return FindObjectOfType<PTPlayerPool>().GetComponent<PTLayoutZone>(); } }

    private void Awake()
    {
        localInput = GetComponent<PTGamePiece>();

        localInput.OnDragBegin += HandlerOnDragBegin;
        localInput.OnDropped += HandlerOnDropped;
        localInput.OnShortHoldBegin += HandlerOnShortHoldBein;
        buttonRemove.OnTouched += HandlerButtonRemoveOnTouched;
    }

    private void HandlerButtonRemoveOnTouched(PTTouch touch)
    {
        if (seat)
        {
            seat.UpdateLabel(true);
        }

        Destroy(gameObject);
    }
    private void HandlerOnShortHoldBein(PTTouch touch)
    {
        if (isBeingDragged || touch.followers.Count > 0)
        {
            return;
        }

        buttonRemove.gameObject.SetActive(!buttonRemove.gameObject.activeSelf);
    }
    private void HandlerOnDragBegin(PTTouch touch)
    {
        isBeingDragged = true;

        lastSeat = seat;

        transform.SetParent(null);

        if (buttonRemove.gameObject.activeSelf)
        {
            buttonRemove.gameObject.SetActive(false);
        }

        if (lastSeat)
        {
            lastSeat.UpdateLabel();
        }
    }
    private void HandlerOnDropped(PTTouchFollower follower)
    {
        PTSeat targetSeat = null;

        foreach (Collider collider in follower.touch.hits.Keys)
        {
            if (collider.GetComponent<PTSeat>())
            {
                targetSeat = collider.GetComponent<PTSeat>();
                break;
            }
        }

        if (targetSeat != null)
        {
            //on seat
            if (targetSeat != lastSeat)
            {
                if (lastSeat != null)
                {
                    //exchange seat
                    //Debug.Log("Exchange seat");
                    if (targetSeat.playerSitting)
                    {
                        lastSeat.LetSit(targetSeat.playerSitting.transform);
                    }
                }
                else
                {
                    //move the sitting player to pool
                    playerPoolZone.Add(targetSeat.playerSitting, 0, PT.DEFAULT_TIMER);
                }
            }

            targetSeat.LetSit(transform);
            playerPoolZone.Arrange();
            isBeingDragged = false;
        }
        else
        {
            StartCoroutine(ReturnToPlayerPoolCoroutine());
        }
    }

    private IEnumerator ReturnToPlayerPoolCoroutine()
    {
        yield return playerPoolZone.AddCoroutine(this, 0, PT.DEFAULT_TIMER);
        yield return playerPoolZone.ArrangeCoroutine(PT.DEFAULT_TIMER);
        isBeingDragged = false;
    }
}
