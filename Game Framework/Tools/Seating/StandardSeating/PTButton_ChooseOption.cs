using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PTButton_ChooseOption : PTGameButton_new
{
    protected override IEnumerator OnTouchBeginCoroutine()
    {
        PTSeatOptions seatOptions = transform.GetComponentInParent<PTSeatOptions>();
        seatOptions.ChooseOption(seatOptions.GetOptionIndex(gameObject));
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
