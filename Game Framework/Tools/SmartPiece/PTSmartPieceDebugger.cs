using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PlayTable;

public class PTSmartPieceDebugger : MonoBehaviour
{
    [SerializeField]
    Text verified;

    int touches;
    [SerializeField]
    Image overlay;

    // Start is called before the first frame update
    void Start()
    {
        PTGlobalInput.OnTouchBegin += (PTTouch touch) =>
        {
            PTSmartPieceManager.Instance.SetSpMarker(new ScannedSmartPiece() { origin = new ScanOrigin(touch), uid = ("touch")});
        };
        PTGlobalInput.OnTouchEnd += (PTTouch touch) =>
        {
            //touches--;
            //Debug.Log("touch ended" + touches);
        };

        PTGlobalInput.OnShortHoldBegin += (PTTouch touch) =>
        {
            print(touch + "SHORT HOLD BEGIN");
        };

        PTGlobalInput.OnTouchMoveBegin += (PTTouch touch) =>
        {
            print(touch + "DRAG BEGIN");
        };
    }

    // Update is called once per frame
    void Update()
    {
        if(verified != null)
        {
            string s = "";
            foreach (ScannedSmartPiece sp in GetComponent<PTSmartDiceListener>().verifiedAndStationaryRolls)
            {
                s += PTSmartPieceManager.GetSpValue(sp) + "\r\n";
            }
            verified.text = s;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            PTPlatform.GetSmartpiece("10000abcd00004");
        }
    }

    public void ToggleOverlay()
    {
        overlay.gameObject.SetActive(!overlay.gameObject.activeInHierarchy);
    }
}
