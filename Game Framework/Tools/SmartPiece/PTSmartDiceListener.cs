using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PlayTable;

public class PTSmartDiceListener : MonoBehaviour
{
    public List<ScannedSmartPiece> verifiedAndStationaryRolls = new List<ScannedSmartPiece>();
    public List<ScannedSmartPiece> verifiedRolls = new List<ScannedSmartPiece>();

    // touches that were triggered before the rolling phase started
    public List<PTTouch> invalidTouches = new List<PTTouch>();
    // touches that are stationary
    public List<PTTouch> stationaryTouches = new List<PTTouch>();

    private int verificationThreshold = 3;

    private void Start()
    {
        PTSmartPieceManager.Instance.OnSpScanned += SpDownHandler;
        PTSmartPieceManager.Instance.OnSpUp += SpUpHandler;
        PTGlobalInput_new.OnTouchEnd += (PTTouch touch) =>
        {
            invalidTouches.Remove(touch);
            stationaryTouches.Remove(touch);
        };
        PTGlobalInput_new.OnShortHoldBegin += (PTTouch touch) =>
        {
            foreach (ScannedSmartPiece sp in verifiedRolls)
            {
                if (sp.origin.touch == touch)
                {
                    if (verifiedAndStationaryRolls.Contains(sp) == false)
                    {
                        verifiedAndStationaryRolls.Add(sp);
                        //if (verifiedAndStationaryRolls.Count == 1) { StartCoroutine(ForceScanAllAntennas()); }
                        PTSmartPieceManager.Instance.spMarkers[sp.origin.touch].GetComponent<PTSmartPieceMarker>().SetMarker(PTSmartPieceMarker.MarkerSprite.greenSquare);
                    }
                }
            }
            stationaryTouches.Add(touch);
        };
        PTGlobalInput_new.OnTouchMoveBegin += (PTTouch touch) =>
        {
            foreach (ScannedSmartPiece sp in verifiedAndStationaryRolls)
            {
                if (sp.origin.touch == touch)
                {
                    verifiedAndStationaryRolls.Remove(sp);
                    PTSmartPieceManager.Instance.spMarkers[sp.origin.touch].GetComponent<PTSmartPieceMarker>().SetMarker(PTSmartPieceMarker.MarkerSprite.yellowSquare);
                    break;
                }
            }
            stationaryTouches.Remove(touch);
        };
    }

    public void ClearVerifiedRolls()
    {
        verifiedRolls.Clear();
        verifiedAndStationaryRolls.Clear();
        foreach (PTTouch touch in PTInputManager.touches)
        {
            invalidTouches.Add(touch);
        }
    }

    private void SpDownHandler(ScannedSmartPiece sp)
    {
        if (IsAlreadyVerified(sp.uid) == false)
        {
            // ignore smartpieces that are not D6 and ignore smartpieces attached to invalid touches
            if (sp.data != null && sp.data.short_name == "D6" && sp.origin.touch != null && invalidTouches.Contains(sp.origin.touch) == false)
            {
                if (sp.timesScanned == verificationThreshold)
                {
                    if (IsValidRollValue(sp))
                    {
                        RemovePreviouslyVerifiedRoll(sp.data.rfids);
                        verifiedRolls.Add(sp);
                        if (stationaryTouches.Contains(sp.origin.touch))
                        {
                            verifiedAndStationaryRolls.Add(sp);
                            //if(verifiedAndStationaryRolls.Count == 1) { StartCoroutine(ForceScanAllAntennas()); }
                            if (PTSmartPieceManager.Instance.spMarkers.ContainsKey(sp.origin.touch))
                            {
                                PTSmartPieceManager.Instance.spMarkers[sp.origin.touch].GetComponent<PTSmartPieceMarker>().SetMarker(PTSmartPieceMarker.MarkerSprite.greenSquare);
                            }
                        }
                    }
                }
            }
            else if (sp.origin.touch != null && invalidTouches.Contains(sp.origin.touch) == true)
            {
                PTSmartPieceManager.Instance.spMarkers[sp.origin.touch].GetComponent<PTSmartPieceMarker>().SetMarker(PTSmartPieceMarker.MarkerSprite.pinkSquare);
            }
        }
    }

    private bool IsAlreadyVerified(string uid)
    {
        foreach (ScannedSmartPiece existingVerifiedRoll in verifiedRolls)
        {
            if (existingVerifiedRoll.uid == uid)
            {
                return true;
            }
        }
        return false;
    }

    private void RemovePreviouslyVerifiedRoll(PTSmartpieceRFID[] rfids)
    {
        ScannedSmartPiece spToRemove = null;
        foreach (PTSmartpieceRFID rfid in rfids)
        {
            foreach (ScannedSmartPiece existingVerifiedRoll in verifiedRolls)
            {
                if (rfid.uid == existingVerifiedRoll.uid)
                {
                    // reset times scanned for future verification purposes
                    existingVerifiedRoll.timesScanned = 0;
                    spToRemove = existingVerifiedRoll;
                    break;
                }
            }
            if (spToRemove != null)
            {
                verifiedRolls.Remove(spToRemove);
                verifiedAndStationaryRolls.Remove(spToRemove);
                return;
            }
        }
    }

    private bool IsValidRollValue(ScannedSmartPiece sp)
    {
        string rollValue = PTSmartPieceManager.GetSpValue(sp);
        return (rollValue == "1" || rollValue == "2" || rollValue == "3" || rollValue == "4" || rollValue == "5" || rollValue == "6");
    }

    private void SpUpHandler(ScannedSmartPiece sp)
    {
        bool isOtherTouchTrackingSp = false;
        foreach (KeyValuePair<ScanOrigin, List<ScannedSmartPiece>> scannedPieces in PTSmartPieceManager.Instance.originToScannedPieces)
        {
            if (scannedPieces.Key != sp.origin)
            {
                foreach (ScannedSmartPiece other in scannedPieces.Value)
                {
                    if (other.uid == sp.uid)
                    {
                        if (other.timesScanned > sp.timesScanned)
                        {
                            isOtherTouchTrackingSp = true;
                            break;
                        }
                    }
                }
            }
            if (isOtherTouchTrackingSp)
            {
                break;
            }
        }
        if (isOtherTouchTrackingSp == false)
        {
            verifiedRolls.Remove(sp);
            verifiedAndStationaryRolls.Remove(sp);
        }
    }

    IEnumerator ForceScanAllAntennas()
    {
        while(verifiedAndStationaryRolls.Count < 2)
        {
            yield return new WaitForSeconds(2);
            for(int bank = 0; bank < 6; ++bank)
            {
                if (verifiedAndStationaryRolls.Count < 2 && PTSmartPieceManager.Instance.scanMode == PTSmartPieceManager.ScanMode.Precision)
                {
                    for (int antenna = 0; antenna < 12; ++antenna)
                    {
                        string uid = PTService_new.Instance.Scan(((Bank)bank).ToString(), antenna);
                        if(uid != null && uid != "null")
                        {
                            PTSmartPieceManager.Instance.AddScannedSmartPiece(new ScanOrigin((PTTouch)null), uid);
                        }
                    }
                    yield return new WaitForSeconds(0.1f);
                }
                else
                {
                    break;
                }
            }
        }
    }
}
