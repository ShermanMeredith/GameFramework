using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayTable;

public class PTButton_Play : PTGameButton_new
{
    protected override IEnumerator OnClickCoroutine()
    {
        CatanStateManager.Instance.SetState(CatanStateManager.State.Game);
        yield return null;
    }

    protected override IEnumerator OnLongHoldClickCoroutine()
    {
        yield return null;
    }

    protected override IEnumerator OnShortHoldClickCoroutine()
    {
        yield return null;
    }
}
