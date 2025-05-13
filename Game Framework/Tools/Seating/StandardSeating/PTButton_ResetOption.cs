using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PTButton_ResetOption : PTGameButton_new
{
    protected override IEnumerator OnLongHoldClickCoroutine()
    {
        yield return null;
    }

    protected override IEnumerator OnShortHoldClickCoroutine()
    {
        yield return null;
    }

    protected override IEnumerator OnTouchBeginCoroutine()
    {
        PTSeatOptions options = GetComponentInParent<PTSeatOptions>();
        options.ResetChoice();
        options.StartCoroutine(options.ShowOptionsCoroutine());
        yield return null;
    }
}
