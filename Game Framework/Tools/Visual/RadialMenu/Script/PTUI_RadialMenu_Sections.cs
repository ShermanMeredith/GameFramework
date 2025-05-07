using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using PlayTable;

public enum RadialSection { unknown = -1, up, right, down, left, inside, outside }

public class PTUI_RadialMenu_Sections : MonoBehaviour {
    [Tooltip("Up, Right, Down, Left")]
    public PTUI_RadialMenu_Section[] content;
    private const float timerAnimation = 0.2f;
    public float timerHoldToSwitchInside = 0.3f;
    public PTUI_RadialMenu radialMenu;

    public RadialSection section { get; private set; }
    public delegate void SectionDelegate(RadialSection scetion);
    public SectionDelegate OnSectionSwitched;

    private KeyValuePair<RadialSection, DateTime> section_LastTouched = new KeyValuePair<RadialSection, DateTime>(RadialSection.unknown, DateTime.Now);//section, entertime

    /// <summary>
    /// Toggles the visibility. 
    /// </summary>
    /// <param name="visibility">Target visibility.</param>
    /// <returns></returns>
    public IEnumerator Toggle(bool visibility)
    {            
        Image maskImage = GetComponent<Image>();
        Switch(RadialSection.unknown);
        //yield return new WaitUntil(() => maskImage.fillAmount == 1 || maskImage.fillAmount == 0);
        float initFillAmount = maskImage.fillAmount;
        float targetFillAmount = visibility ? 1 : 0;
        float coveredTime = 0;
        while (maskImage.fillAmount != targetFillAmount)
        {
            yield return new WaitForEndOfFrame();
            coveredTime += Time.deltaTime;
            float fraction = coveredTime / timerAnimation;
            fraction = fraction < 1 ? fraction : 1;
            maskImage.fillAmount = initFillAmount + (targetFillAmount - initFillAmount) * fraction;
        }
    }

    /// <summary>
    /// Raw input with the information where the touch is on.
    /// </summary>
    /// <param name="newSection">The section touch is currently on.</param>
    internal void Input(RadialSection newSection)
    {
        if (newSection != section_LastTouched.Key)
        {
            section_LastTouched = new KeyValuePair<RadialSection, DateTime>(newSection, DateTime.Now);
        }

        float coveredtime = (float)((DateTime.Now - section_LastTouched.Value).Milliseconds) / 1000.0f;
        bool holdEnoughTime = coveredtime > timerHoldToSwitchInside;
        bool validNewSection = section != newSection;
        bool validTo = holdEnoughTime && newSection != RadialSection.outside;
        bool validFrom = section == RadialSection.unknown || section == RadialSection.inside;
        bool validSwitch = validNewSection && (validTo || validFrom);

        if (validSwitch)
        {
            Switch(newSection);
        }
    }

    /// <summary>
    /// Actulally switch section.
    /// </summary>
    /// <param name="newSection">The new section going to be set.</param>
    void Switch(RadialSection newSection)
    {
        //Do nothing if new section is the as same as the current one.
        if (section == newSection)
        {
            return;
        }

        //Change section value
        section = newSection;

        //Trigger OnSefctionEnter event
        if (OnSectionSwitched != null)
        {
            OnSectionSwitched(section);
        }

        //UpdateHighlight
        if (section == RadialSection.unknown || section == RadialSection.inside)
        {
            foreach (PTUI_RadialMenu_Section section in content)
            {
                section.GetComponentInChildren<Button>().interactable = true;
            }
        }
        else if (section != RadialSection.outside)
        {
            foreach (PTUI_RadialMenu_Section section in content)
            {
                section.GetComponentInChildren<Button>().interactable = false;
            }
            //print(section);
            content[(int)section].GetComponentInChildren<Button>().interactable = true;
        }
    }

    /// <summary>
    /// Get PTUI_RadialMenu_Section object by RadialSection enum
    /// </summary>
    /// <param name="section"></param>
    /// <returns></returns>
    public PTUI_RadialMenu_Section GetSection(RadialSection section)
    {
        try
        {
            return content[(int)section];
        }
        catch
        {
            return null;
        }
    }
}
