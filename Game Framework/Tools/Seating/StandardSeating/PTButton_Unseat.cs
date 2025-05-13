using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PTButton_Unseat : PTGameButton
{
    protected override IEnumerator OnTouchBeginCoroutine()
    {
        GetComponentInParent<PTSeatingSeat>().ResetSeat();
        GetComponentInParent<PTSeatingSeat>().GetComponentInChildren<PTSeatingIcon>().SendToLineUp();
        yield return null;
    }

    protected override IEnumerator OnLongHoldClickCoroutine()
    {
        //
        yield return null;
    }

    protected override IEnumerator OnShortHoldClickCoroutine()
    {
        //
        yield return null;
    }
}
