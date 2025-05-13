using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayTable;

public class PTButton_LineupScroll : PTGameButton
{
    private bool isScrollRightButton = true;
    [SerializeField] private Transform knob;
    [SerializeField] private Transform chevrons;
    [SerializeField] private GameObject aiGlow;
    [SerializeField] private GameObject accountGlow;
    private bool isScrolling = false;

    protected override IEnumerator OnTouchBeginCoroutine()
    {
        if(isScrolling == false)
        {
            PTSeatingManager.Instance.ScrollCharacterLineups(isScrollRightButton);
            isScrollRightButton = !isScrollRightButton;
            isScrolling = true;
            yield return knob.SetLocalPositionCoroutine(new Vector3(-knob.localPosition.x, knob.localPosition.y, knob.localPosition.z), PT.DEFAULT_TIMER * 2);
            isScrolling = false;
            chevrons.localScale = new Vector3(-chevrons.localScale.x, chevrons.localScale.y, chevrons.localScale.z);
            aiGlow.SetActive(isScrollRightButton);
            accountGlow.SetActive(!isScrollRightButton);
        }
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
