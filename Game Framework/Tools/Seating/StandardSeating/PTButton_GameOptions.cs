using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayTable;

public class PTButton_GameOptions : PTGameButton_new
{
    [SerializeField] CatanSettings options;
    
    protected override IEnumerator OnClickCoroutine()
    {
        if(CatanSettings.Instance.IsOpen == false)
        {
            options.ToggleSettings();
        }
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
