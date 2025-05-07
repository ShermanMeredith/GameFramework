using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayTable;
using UnityEngine.UI;

public class PTUI_RadialMenu_Section : MonoBehaviour {
    public PTLogoColor color;

    public Font fontIcon0;
    public string iconStr0;
    public Font fontText0;
    public string[] content0;

    public Font fontIcon1;
    public string iconStr1;
    public Font fontText1;
    public string[] content1;

    private void Start()
    {
        GetComponentInChildren<Image>().color = PT.GetOfficialColor(color);
    }
}
