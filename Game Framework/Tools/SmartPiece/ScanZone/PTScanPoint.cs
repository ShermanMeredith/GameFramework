using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayTable;

namespace PlayTable
{
    public class PTScanPoint : MonoBehaviour
    {

        public ScannedSmartPiece SmartPiece { get; private set; }
        public bool isActiveScanning;
        [SerializeField] PTSeatingBlueprint megamanBlueprint;
        [SerializeField] PTSeatingSeat mySeat;

        public void OnScanned(ScannedSmartPiece sp)
        {
            if (sp.uid == "0x4088f5a4f5981")
            {
                PTSeatingIcon megaman = PTSeatingManager.Instance.MakeAvatar(megamanBlueprint);
                if (mySeat != null)
                {
                    mySeat.Zone.Add(megaman.transform);
                }
                foreach(PTScanPoint scanPoint in FindObjectsOfType<PTScanPoint>())
                {
                    scanPoint.isActiveScanning = false;
                }
            }
        }

        public void SetActiveScanning(bool isScanning)
        {
            isActiveScanning = isScanning;
        }
    }
}