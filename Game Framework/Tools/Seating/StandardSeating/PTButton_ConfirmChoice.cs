using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PTButton_ConfirmChoice : PTGameButton
{
    public GameObject glow;

    protected override IEnumerator OnTouchBeginCoroutine()
    {
        CatanDialogBox dialogBox = transform.GetComponentInParent<CatanDialogBox>();
        dialogBox.ConfirmChoice();
        if (glow != null)
        {
            glow.SetActive(false);
        }
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
