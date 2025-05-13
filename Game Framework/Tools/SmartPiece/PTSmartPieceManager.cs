using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using PlayTable;

namespace PlayTable
{
    public class ScannedSmartPiece
    {
        public string uid;
        public ScanOrigin origin;
        public PTSmartpieceObject data;
        public int timesScanned;
    }

    public class ScanOrigin
    {
        public PTTouch touch;
        public PTScanPoint point;
        public ScanOrigin(PTTouch originTouch)
        {
            touch = originTouch;
        }
        public ScanOrigin(PTScanPoint originZone)
        {
            point = originZone;
        }
        public override bool Equals(object obj)
        {
            return (touch == ((ScanOrigin)obj).touch || point == ((ScanOrigin)obj).point);
        }
        public override int GetHashCode()
        {
            if(touch != null)
            {
                return touch.GetHashCode();
            }
            if (point != null)
            {
                return point.GetHashCode();
            }
            else return -1;
        }
        public static bool operator ==(ScanOrigin a, ScanOrigin b)
        {
            if ((object)a == null)
                return (object)b == null;

            return a.Equals(b);
        }
        public static bool operator !=(ScanOrigin a, ScanOrigin b)
        {
            return !(a == b);
        }
    }
    
    public enum SmartPieceType { unknown, figure, card, die };

    public delegate void PTDelegateScannedSmartPiece(ScannedSmartPiece spData);

    public class PTSmartPieceManager : MonoBehaviour
    {
        public enum ScanMode
        {
            Off,
            Precision,
            Wide
        }

        //-------------------------------------------------------------------------------------------------------------
        // DELEGATES
        //-------------------------------------------------------------------------------------------------------------
        // called when an RFID is detected, after its SmartPiece data is handled
        public PTDelegateScannedSmartPiece OnSpScanned;
        // called when the touch containing the SP ends
        public PTDelegateScannedSmartPiece OnSpUp;

        //-------------------------------------------------------------------------------------------------------------
        // FIELDS
        //-------------------------------------------------------------------------------------------------------------
        const int ANT_SIZE_X = 171;   //size of pixels of antenna in x direction (width)
        const int ANT_SIZE_Y = 181;   //size of pixels of antenna in y direction (height)

        public static PTSmartPieceManager Instance { get; private set; }
        private string smartPieceFileName = @"/SmartPieceData.json";
        private float scanFrequency = 0.25f;

        public ScanMode scanMode { get; private set; }
        public PTScanPoint[] scanPoints;
        // saved/loaded from local storage. For local query before checking the server.
        private Dictionary<string, PTSmartpieceObject> uidToSmartPieceData;

        // For tracking scanned smart pieces
        public Dictionary<ScanOrigin, List<ScannedSmartPiece>> originToScannedPieces = new Dictionary<ScanOrigin, List<ScannedSmartPiece>>();
        [SerializeField]
        private GameObject spMarkerPrefab;  // will not spawn markers if null
        public Dictionary<PTTouch, GameObject> spMarkers = new Dictionary<PTTouch, GameObject>();

        //-------------------------------------------------------------------------------------------------------------
        // GETTERS
        //-------------------------------------------------------------------------------------------------------------
        public static string GetSpValue(ScannedSmartPiece sp)
        {
            string spValue = null;
            if (sp.data != null && sp.data.rfids != null)
            {
                foreach (PTSmartpieceRFID rfid in sp.data.rfids)
                {
                    if (rfid.uid == sp.uid)
                    {
                        if (rfid.value != null)
                        {
                            spValue = rfid.value;
                        }
                        break;
                    }
                }
                if (spValue == null)
                {
                    if (sp.data.short_name != null && sp.data.short_name != "")
                    {
                        spValue = sp.data.short_name;
                    }
                    else
                    {
                        spValue = sp.uid;
                    }
                }
            }
            else
            {
                spValue = sp.uid;
            }
            return spValue;
        }

        public List<ScannedSmartPiece> GetSpByUid(string uid)
        {
            List<ScannedSmartPiece> spList = new List<ScannedSmartPiece>();
            foreach (KeyValuePair<ScanOrigin, List<ScannedSmartPiece>> scannedSmartPieces in originToScannedPieces)
            {
                foreach (ScannedSmartPiece sp in scannedSmartPieces.Value)
                {
                    if (sp.uid == uid)
                    {
                        spList.Add(sp);
                    }
                }
            }
            if (spList.Count > 0)
            {
                return spList;
            }
            else
            {
                return null;
            }
        }

        public List<ScannedSmartPiece> GetSpByTouch(PTTouch touch)
        {
            foreach (KeyValuePair<ScanOrigin, List<ScannedSmartPiece>> smartPieces in originToScannedPieces)
            {
                if (smartPieces.Key.touch == touch)
                {
                    return smartPieces.Value;
                }
            }
            return null;
        }

        public List<ScannedSmartPiece> GetSpByScanPoint(PTScanPoint point)
        {
            foreach (KeyValuePair<ScanOrigin, List<ScannedSmartPiece>> smartPieces in originToScannedPieces)
            {
                if (smartPieces.Key.point == point)
                {
                    return smartPieces.Value;
                }
            }
            return null;
        }

        //-------------------------------------------------------------------------------------------------------------
        // INIT
        //-------------------------------------------------------------------------------------------------------------
        private void Awake()
        {
            // manage singleton
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(this);
                return;
            }
            
            //define callbacks
            PTPlatform.OnResultGetSmartpiece += OnResultGetSmartpiece;
            PTGlobalInput.OnTouchBegin += ScanTouch;
            PTGlobalInput.OnTouchEnd += OnTouchEndReceiver;

            InitializeSmartPieceData();

            if (PTService.Instance == null)
            {
                PTService ptService = gameObject.AddComponent<PTService>();
            }
            scanPoints = FindObjectsOfType<PTScanPoint>();
        }

        private void Start()
        {
            PTService.Instance.GpioInitialize();
            PTService.Instance.ResetNfc();

            StartCoroutine(HeartBeatScan());
        }

        private void InitializeSmartPieceData()
        {
            string smartPieceDataPath = Application.persistentDataPath + smartPieceFileName;
            if (File.Exists(smartPieceDataPath))
            {
                Debug.Log("Loading local SmartPiece database");
                string jsonRaw = File.ReadAllText(smartPieceDataPath);
                Debug.Log(jsonRaw);
                uidToSmartPieceData = JsonUtility.FromJson<Dictionary<string, PTSmartpieceObject>>(jsonRaw);
            }
            else
            {
                Debug.Log("No local SmartPiece database");
                uidToSmartPieceData = new Dictionary<string, PTSmartpieceObject>();
            }
        }

        //-------------------------------------------------------------------------------------------------------------
        // EVENT HANDLERS
        //-------------------------------------------------------------------------------------------------------------
        private void OnResultGetSmartpiece(PTResult<PTSmartpieceObject> result)
        {
            Debug.Log("OnResultGetSmartpiece: " + result.ToString());
            
            if (result.succeeded)
            {
                // get uid from query parameter
                string uid = result.queryParameter.Substring(0, result.queryParameter.IndexOf("?"));

                foreach (PTSmartpieceObject spData in result.content)
                {
                    // Register the smart piece data
                    RegisterSmartPieceObject(spData);
                    // Check to see if touch to scan is still active
                    List<ScannedSmartPiece> smartPieces = GetSpByUid(uid);
                    if (smartPieces != null)
                    {
                        foreach (ScannedSmartPiece sp in smartPieces)
                        {
                            sp.data = spData;
                            SetSpMarker(sp);
                        }
                    }
                }
            }
        }

        private void OnSmartPieceScanned(ScannedSmartPiece sp)
        {
            if (GetSpByUid(sp.uid) == null)
            {
                // smartpiece scanned does not exist in game yet
                if (originToScannedPieces.ContainsKey(sp.origin))
                {
                    // smartpiece scanned by touch already tracking another smartpiece
                    originToScannedPieces[sp.origin].Add(sp);
                }
                else
                {
                    // completely new touch + smartpiece
                    originToScannedPieces.Add(sp.origin, new List<ScannedSmartPiece>() { sp });
                }
                SetSpMarker(sp);
            }
            else
            {
                // smartpiece scanned already exists in game
                if (originToScannedPieces.ContainsKey(sp.origin))
                {
                    bool isTrackedByScannedTouch = false;
                    foreach (ScannedSmartPiece existingSmartPiece in originToScannedPieces[sp.origin])
                    {
                        if (existingSmartPiece.uid == sp.uid)
                        {
                            // scanned smartpiece is already tracked via existingSmartPiece
                            isTrackedByScannedTouch = true;
                            existingSmartPiece.timesScanned++;
                            sp = existingSmartPiece;  // overwrite the object created from this scan
                            SetSpMarker(sp);    // make sure the marker reflects most recent scan
                            break;
                        }
                    }
                    if (isTrackedByScannedTouch == false)
                    {
                        // smartpiece scanned is being tracked by a different touch
                        originToScannedPieces[sp.origin].Add(sp);
                    }
                }
                else
                {
                    // smartpiece scanned is being tracked by a touch not yet registered
                    originToScannedPieces.Add(sp.origin, new List<ScannedSmartPiece>() { sp });
                }
            }
            if (OnSpScanned != null)
            {
                OnSpScanned(sp);
            }
        }

        private void OnTouchEndReceiver(PTTouch touch)
        {
            foreach (KeyValuePair<ScanOrigin, List<ScannedSmartPiece>> scannedPieces in originToScannedPieces)
            {
                if (scannedPieces.Key.touch != null && scannedPieces.Key.touch == touch)
                {
                    foreach (ScannedSmartPiece sp in scannedPieces.Value)
                    {
                        // Trigger SmartPiece UP Event
                        if (OnSpUp != null)
                        {
                            OnSpUp(sp);
                        }
                    }
                    originToScannedPieces.Remove(scannedPieces.Key);
                    break;
                }
            }
            if (spMarkers.ContainsKey(touch))
            {
                Destroy(spMarkers[touch]);
                spMarkers.Remove(touch);
            }
        }

        void OnApplicationPause(bool pauseStatus)
        {
            foreach (PTTouch touch in PTInputManager.touches)
            {
                PTGlobalInput.OnTouchEnd(touch);
            }
        }

        //-------------------------------------------------------------------------------------------------------------
        // FUNCTIONS
        //-------------------------------------------------------------------------------------------------------------
        public void SetScanMode(int newScanMode)
        {
            SetScanMode((ScanMode)newScanMode);
        }

        public void SetScanMode(ScanMode newScanMode)
        {
            Debug.Log("Setting scanning mode from " + scanMode + " to " + (ScanMode)newScanMode);
            scanPoints = FindObjectsOfType<PTScanPoint>();
            scanMode = newScanMode;
        }

        private IEnumerator HeartBeatScan()
        {
            while (true)
            {
                float timeUntilNextScan = scanFrequency;
                float scanInterval = scanFrequency / (PTInputManager.touches.Count + scanPoints.Length + 1);
                if (scanMode != ScanMode.Off && PTInputManager.touches.Count > 0)
                {
                    // copy the list in case new touches are registered mid-scan
                    List<PTTouch> touches = new List<PTTouch>(PTInputManager.touches);
                    // scan each touch for a potential SmartPiece
                    foreach (PTTouch scanningTouch in touches)
                    {
                        // check if touch has ended since beginning this heartbeatscan
                        if (PTInputManager.touches.Contains(scanningTouch))
                        {
                            ScanTouch(scanningTouch);
                            yield return new WaitForSeconds(scanInterval);
                            timeUntilNextScan -= scanInterval;
                        }
                    }
                }
                foreach (PTScanPoint scanPoint in scanPoints)
                {
                    if (scanPoint.isActiveScanning)
                    {
                        ScanPoint(scanPoint);
                        yield return new WaitForSeconds(scanInterval);
                        timeUntilNextScan -= scanInterval;
                    }
                }
                // wait before scanning again
                yield return new WaitForSeconds(timeUntilNextScan);
            }
        }

        private void ScanPoint(PTScanPoint point)
        {
            Vector2 screenPosition = Camera.main.WorldToScreenPoint(point.transform.position);
            string scannedUid = PTService.Instance.Scan((int)screenPosition.x, Screen.height - (int)screenPosition.y);

            if (scannedUid != null && scannedUid != "null" && scannedUid != "")
            {
                AddScannedSmartPiece(new ScanOrigin(point), scannedUid);
            }
        }

        private void ScanTouch(PTTouch touch)
        {
            // scan touch location for SmartPiece
            int xCoordinate = (int)touch.position.x;
            int yCoordinate = Screen.height - (int)touch.position.y;
            switch (scanMode)
            {
                case ScanMode.Precision:
                    PrecisionScan(xCoordinate, yCoordinate, touch);
                    break;

                case ScanMode.Wide:
                    WideScan(xCoordinate, yCoordinate, touch);
                    break;
            }
        }

        private void PrecisionScan(int x, int y, PTTouch touch)
        {
            // uncomment for smart dice debugging in editor
            if (Application.isEditor)
            {
                if (Input.GetMouseButton(0))
                {
                    AddScannedSmartPiece(new ScanOrigin(PTInputManager.touches[0]), "04AB31F2FA6384");
                }
                if (Input.GetMouseButton(1))
                {
                    if (PTInputManager.touches.Count > 1)
                    {
                        AddScannedSmartPiece(new ScanOrigin(PTInputManager.touches[1]), "04A91AF2FA6384");
                    }
                    else
                    {
                        AddScannedSmartPiece(new ScanOrigin(PTInputManager.touches[0]), "04A91AF2FA6384");
                    }
                }
            }
            else
            {
                string scannedUid = PTService.Instance.Scan(x, y);

                if (scannedUid != null && scannedUid != "null" && scannedUid != "")
                {
                    AddScannedSmartPiece(new ScanOrigin(touch), scannedUid);
                }
            }
        }

        private void WideScan(int x, int y, PTTouch touch)
        {
            List<string> scannedUids = null;
            
            for (int yOffset = -1; yOffset < 2; ++yOffset)
            {
                for (int xOffset = -1; xOffset < 2; ++xOffset)
                {
                    int xToScan = (xOffset * ANT_SIZE_X / 2) + x;
                    int yToScan = (yOffset * ANT_SIZE_Y / 2) + y;

                    string result = PTService.Instance.Scan(xToScan, yToScan);
                    if (result != "null")
                    {
                        if (scannedUids == null)
                        {
                            scannedUids = new List<string>() { result };
                        }
                        else
                        {
                            if (scannedUids.Contains(result) == false)
                            {
                                scannedUids.Add(result);
                            }
                        }
                    }
                }
            }

            if (scannedUids != null)
            {
                foreach (string uid in scannedUids)
                {
                    AddScannedSmartPiece(new ScanOrigin(touch), uid);
                }
            }
        }

        public void AddScannedSmartPiece(ScanOrigin scanOrigin, string scannedUid)
        {
            // convert to lower case and remove leading zeros. This is necessary because
            //  values from server do not include leading zeros, but scanned smartpieces do.
            string uid = "0x" + scannedUid.ToLower().TrimStart(new char[] { '0' });
            // create scanned smartpiece object
            ScannedSmartPiece smartPiece = new ScannedSmartPiece()
            {
                uid = uid,
                origin = scanOrigin,
                timesScanned = 1
            };
            // check local data for uid
            if (uidToSmartPieceData.ContainsKey(uid))
            {
                smartPiece.data = uidToSmartPieceData[uid];
            }
            if (smartPiece.data == null)
            {
                // only fetch data once per uid
                if(GetSpByUid(uid) == null)
                {
                    // fetch data from server
                    PTPlatform.GetSmartpiece(uid);
                }
            }
            OnSmartPieceScanned(smartPiece);
        }

        public void SetSpMarker(ScannedSmartPiece sp)
        {
            if (sp.origin.point != null)
            {
                // delegate spMarker to scan zone
                sp.origin.point.OnScanned(sp);
            }
            else if (sp.origin.touch != null && spMarkerPrefab != null)
            {
                // create / update spMarker
                if (spMarkers.ContainsKey(sp.origin.touch) == false)
                {
                    // SmartPiece Marker does not exist yet. Create one
                    GameObject marker = Instantiate(spMarkerPrefab, Vector3.Lerp(sp.origin.touch.hitPoint, new Vector3(0,10,0), 1f/10f), Quaternion.Euler(90, 0, 0));
                    marker.GetComponent<PTSmartPieceMarker>().Init(sp);
                    sp.origin.touch.AddFollower(marker.GetComponent<Collider>(), Vector3.up);
                    spMarkers.Add(sp.origin.touch, marker);
                }
                else
                {
                    // SmartPiece Marker exists. Update existing SpMarker
                    PTSmartPieceMarker spMarker = spMarkers[sp.origin.touch].GetComponent<PTSmartPieceMarker>();
                    spMarker.GetComponent<PTSmartPieceMarker>().Init(sp);
                }
            }
        }

        private void RegisterSmartPieceObject(PTSmartpieceObject spData)
        {
            if (spData != null && spData.rfids != null)
            {
                foreach (PTSmartpieceRFID rfid in spData.rfids)
                {
                    if (rfid.uid != null && rfid.uid != "")
                    {
                        if (uidToSmartPieceData.ContainsKey(rfid.uid) == false)
                        {
                            Debug.Log("Adding " + rfid.uid + " to dictionary");
                            uidToSmartPieceData.Add(rfid.uid, spData);
                        }
                    }
                }
            }
            string jsonRaw = JsonUtility.ToJson(uidToSmartPieceData);
            print("Writing to file: " + jsonRaw);
            string smartPieceDataPath = Application.persistentDataPath + smartPieceFileName;
            File.WriteAllText(smartPieceDataPath, jsonRaw);
        }
    }
}