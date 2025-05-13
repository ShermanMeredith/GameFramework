using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using PlayTable;

public class PTDiceManager : MonoBehaviour
{
    // fields
    public static PTDiceManager Instance { get; private set; }
    const float VELOCITY_THRESHOLD = 3.3f;
    const float OFFSET = 1f;
    [SerializeField]
    private bool autoInit, spawnsGrid, disableOnRoll;
    [SerializeField]
    private int numberOfDice = 2, dragHeight;
    [SerializeField]
    private GameObject diePrefab, draggablePrefab, extraDiePrefab;

    [SerializeField, TagSelector]
    private List<string> ignoreCollision;

    public static bool IsDragging { get; set; }

    // properties
    public bool IsInitialized { get { return Dice != null; } }
    public bool HasStartedRolling { get; private set; }
    public int NumberOfDice {  get { return numberOfDice; } }
    public PTDie[] Dice { get; private set; }
    public GameObject[] DiceDraggables { get; private set; }
    public GameObject DiePrefab { set { diePrefab = value; } }
    public GameObject ExtraDiePrefab {  set { extraDiePrefab = value; } }

    public int DiceSum
    {
        get
        {
            int sum = 0;
            foreach (PTDie die in Dice)
            {
                sum += die.RollValue;
            }
            return sum;
        }
    }

    public int[] DiceValues
    {
        get
        {
            int[] ret = new int[Dice.Length];
            for(int i = 0; i < Dice.Length; ++i)
            {
                ret[i] = Dice[i].RollValue;
            }
            return ret;
        }
    }

    public bool IsFinishedRolling
    {
        get
        {
            bool finished = true;
            foreach (PTDie die in Dice)
            {
                if (die.IsRolling)
                {
                    finished = false;
                }
            }
            return finished;
        }
    }

    // functions
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        if (autoInit) { Initialize(); }
        transform.parent.gameObject.SetActive(false);
        Application.targetFrameRate = 60;
    }

    public void Initialize()
    {
        transform.parent.gameObject.SetActive(true);
        SpawnDice();
        SpawnDiceDraggables();
        SetIgnoreCollisions();
    }

    public void Initialize(int numOfDie, List<Transform> position)
    {
        //Debug.Log("Initialize " + numOfDie + " " + position);
        numberOfDice = numOfDie;
        HasStartedRolling = false;
        IsDragging = false;

        transform.parent.gameObject.SetActive(true);

        SpawnDice(position);
        SpawnDiceDraggables();
        EnableDice(false);
        SetIgnoreCollisions();
    }
    
    /*
    private void Update()
    {
        if(Dice != null && DiceDraggables.Length == Dice.Length && !IsDragging)
        {
            for(int i = 0; i< DiceDraggables.Length; ++i)
            {
                DiceDraggables[i].transform.position = Vector3.Lerp(Dice[i].rigidBody.transform.position, Camera.main.transform.position, 0.15f);
            }
        }
    }
    */

    public void DestroyDice()
    {
        transform.parent.gameObject.SetActive(false);
        if(Dice != null)
        {
            foreach (PTDie die in Dice)
            {
                Destroy(die.gameObject);
            }
            Dice = null;
        }

        if(DiceDraggables != null)
        {
            foreach (GameObject dieDraggable in DiceDraggables)
            {
                Destroy(dieDraggable);
            }
            DiceDraggables = null;
        }
    }

    public IEnumerator SetVisible(bool visible, float timer)
    {
        foreach (PTDie die in Dice)
        {
            //float targetAlpha = visible ? 1 : 0;
            StartCoroutine(die.Fade(visible, timer));
        }
        yield return timer;
    }

    public void AutoRollDice(float power)
    {
        HasStartedRolling = true;
        foreach (PTDie die in Dice)
        {
            die.Roll(power);
        }
    }

    public void AutoRollDice(float power, Vector3 target)
    {
        foreach (PTDie die in Dice)
        {
            die.Roll(power, target);
        }
    }

    public void EnableDice()
    {
        EnableDice(true);
    }

    public void EnableDice(bool isEnabled)
    {
        //Debug.Log("EnableDice " + isEnabled);
        if (DiceDraggables == null)
        {
            return;
        }

        for(int i = 0; i < DiceDraggables.Length; ++i)
        {
            DiceDraggables[i].GetComponent<Collider>().enabled = isEnabled;
            // TODO: Reset dice position on enable
            // DiceDraggables[i].transform.position = Vector3.Lerp(Dice[i].rigidBody.transform.position, Camera.main.GetComponent<CatanCameraController>().GamePosition, 0.15f);
        }
    }

    public void SetDiceKinematic(bool isKinematic)
    {
        foreach (PTDie die in Dice)
        {
            die.rigidBody.isKinematic = isKinematic;
        }
    }

    private void SpawnDice(List<Transform> transforms)
    {
        Debug.LogError("SpawnDice");
        Dice = new PTDie[numberOfDice];

        for (int i = 0; i < numberOfDice; ++i)
        {
            if (i == 0)
            {
                Dice[i] = Instantiate(diePrefab, this.transform).GetComponent<PTDie>();
            }
            else
            {
                Dice[i] = Instantiate(extraDiePrefab, this.transform).GetComponent<PTDie>();
            }

            Dice[i].transform.position = new Vector3(transforms[i].position.x, transforms[i].position.y + 3f, transforms[i].position.z);
            Dice[i].GoBackPosition = Dice[i].transform.position;
        }
        Dice[0].SetAsYellowCatanDie();
    }

    private void SpawnDice()
    {
        //Debug.LogError("SpawnDice");
        Dice = new PTDie[numberOfDice];

        if (spawnsGrid)
        {
            int sqrt = Mathf.CeilToInt(Mathf.Sqrt(numberOfDice));
            int spawned = 0;
            for (int y = 0; y < sqrt; ++y)
            {
                for (int x = 0; x < sqrt; ++x)
                {
                    Dice[y * sqrt + x] = Instantiate(diePrefab, this.transform).GetComponent<PTDie>();
                    Dice[y * sqrt + x].transform.localPosition = new Vector3(x, 0, y);
                    spawned++;
                    Dice[0].SetAsYellowCatanDie();
                    if (spawned >= numberOfDice) { return; }
                }
            }
        }
        else
        {
            for (int i = 0; i < numberOfDice; ++i)
            {
                Dice[i] = Instantiate(diePrefab, transform).GetComponent<PTDie>();
                Dice[i].transform.position = new Vector3(transform.position.x, transform.position.y + 3f, transform.position.z);
                Dice[i].GoBackPosition = Dice[i].transform.position;
            }
        }
    }

    private void SpawnDiceDraggables()
    {
        DiceDraggables = new GameObject[numberOfDice];
        for (int i = 0; i < numberOfDice; ++i)
        {
            DiceDraggables[i] = Instantiate(draggablePrefab, transform);
            GameObject diceDraggable = DiceDraggables[i];

            diceDraggable.GetComponent<PTGamePiece>().OnDragBegin += (PTTouch touch) => 
            {
                if (!IsDragging)
                {
                    DiceDraggedHandler(diceDraggable.transform);
                }
            };
            diceDraggable.GetComponent<PTGamePiece>().OnDropped += (PTTouchFollower follower) => { StartCoroutine(DiceDroppedHandler(follower)); };
            diceDraggable.GetComponent<PTGamePiece>().SetPositionOffset(dragHeight);

            SpringJoint springJoint = DiceDraggables[i].GetComponent<SpringJoint>();
            for (int j = 0; j < numberOfDice - 1; ++j)
            {
                DiceDraggables[i].AddComponent<SpringJoint>(springJoint);
            }
            foreach (SpringJoint joint in DiceDraggables[i].GetComponents<SpringJoint>())
            {
                joint.autoConfigureConnectedAnchor = false;
                joint.connectedAnchor = Vector3.zero;
                joint.spring = springJoint.spring;
                joint.damper = springJoint.damper;
                joint.maxDistance = springJoint.maxDistance;
            }
        }
    }

    public IEnumerator SpringDiceToLocation(Vector3 globalLocation, bool visible, float timer = PT.DEFAULT_TIMER)
    {
        //Debug.Log("SpringDiceToLocation " + globalLocation);
        bool wasKinematic = Dice[0].rigidBody.isKinematic;
        foreach (PTDie die in Dice)
        {
            die.rigidBody.isKinematic = false;
            die.rigidBody.drag = 0;
            SpringJoint spring = die.rigidBody.gameObject.AddComponent<SpringJoint>();
            spring.minDistance = 0;
            spring.maxDistance = 0;
            spring.damper = 10;
            spring.anchor = Vector3.zero;
            spring.autoConfigureConnectedAnchor = false;
            spring.connectedAnchor = globalLocation;
            StartCoroutine(die.Fade(visible, timer));
        }
        yield return new WaitForSeconds(timer);
        foreach (PTDie die in Dice)
        {
            Destroy(die.rigidBody.GetComponent<SpringJoint>());
            die.rigidBody.isKinematic = wasKinematic;
            die.rigidBody.transform.parent = die.transform.parent;
            die.transform.position = die.rigidBody.transform.position;
            die.rigidBody.transform.parent = die.transform;
        }
    }

    public IEnumerator SendDiceToLocation(Vector3 globalLocation, bool visible, float timer = PT.DEFAULT_TIMER)
    {
        //Debug.Log("SendDiceToLocation " + globalLocation);
        HasStartedRolling = false;
        bool wasKinematic = Dice[0].rigidBody.isKinematic;

        for (int i = 0; i < Dice.Length; ++i)
        {
            Dice[i].rigidBody.isKinematic = true;
            //Dice[i].rigidBody.useGravity = false;
            Dice[i].rigidBody.transform.SetWorldPosition(globalLocation + new Vector3(i*OFFSET, 0, 0f), timer);
            Dice[i].GoBackPosition = globalLocation + new Vector3(i * OFFSET, 0, 0f);
            StartCoroutine(Dice[i].Fade(visible, timer));
        }
        yield return new WaitForSeconds(timer);
        foreach (PTDie die in Dice)
        {
            die.rigidBody.isKinematic = wasKinematic;
            die.rigidBody.transform.parent = die.transform.parent;
            die.transform.position = die.rigidBody.transform.position;
            die.rigidBody.transform.parent = die.transform;
        }

        for (int i = 0; i < DiceDraggables.Length; ++i)
        {
            DiceDraggables[i].transform.position = Dice[i].rigidBody.transform.position;
        }
    }

    public IEnumerator SendDiceToLocations(List<Transform> transforms, bool visible, float timer = PT.DEFAULT_TIMER)
    {
        //Debug.Log("SendDiceToLocations");
        HasStartedRolling = false;
        //bool wasKinematic = Dice[0].rigidBody.isKinematic;

        for (int i = 0; i < Dice.Length; ++i)
        {
            Vector3 globalLocation = transforms[i].position;

            Dice[i].rigidBody.isKinematic = true;
            Dice[i].rigidBody.transform.SetWorldPosition(globalLocation + new Vector3(0, 3f, 0f), timer);
            Dice[i].GoBackPosition = globalLocation + new Vector3(0, 3f, 0f);

            StartCoroutine(Dice[i].Fade(visible, timer));
        }
        yield return new WaitForSeconds(timer);
        foreach (PTDie die in Dice)
        {
            //die.rigidBody.isKinematic = wasKinematic;
            die.rigidBody.transform.parent = die.transform.parent;
            die.transform.position = die.rigidBody.transform.position;
            die.rigidBody.transform.parent = die.transform;
        }


        for (int i = 0; i < DiceDraggables.Length; ++i)
        {
            DiceDraggables[i].transform.position = Dice[i].rigidBody.transform.position;
        }
    }

    public IEnumerator SendDiceBackToPosition(bool visible, float timer = PT.DEFAULT_TIMER)
    {
        //Debug.Log("SendDiceBackToPosition");
        HasStartedRolling = false;
        //bool wasKinematic = Dice[0].rigidBody.isKinematic;

        for (int i = 0; i < Dice.Length; ++i)
        {
            Dice[i].rigidBody.isKinematic = true;
            Dice[i].rigidBody.useGravity = false;

            Dice[i].rigidBody.transform.SetWorldPosition(
                    Dice[i].GoBackPosition,
                    timer);

            StartCoroutine(Dice[i].Fade(visible, timer));
        }
        yield return new WaitForSeconds(timer);
        foreach (PTDie die in Dice)
        {
            //die.rigidBody.isKinematic = wasKinematic;
            die.rigidBody.transform.parent = die.transform.parent;
            die.transform.position = die.rigidBody.transform.position;
            die.rigidBody.transform.parent = die.transform;
        }

        for (int i = 0; i < DiceDraggables.Length; ++i)
        {
            DiceDraggables[i].transform.position = Dice[i].rigidBody.transform.position;
        }
    }

    public IEnumerator SetDieFace(int dieIndex, int face, float timer = PT.DEFAULT_TIMER)
    {
        if (Dice.Length >= dieIndex)
        {
            if (face > 0 && face <= Dice[dieIndex].FaceCount)
            {
                yield return Dice[dieIndex].SetCoroutine(face, timer);
            }
        }
        else
        {
            Debug.LogError("Trying to set face of die that does not exist: index = " + dieIndex);
        }
    }

    private void SetIgnoreCollisions()
    {
        //Debug.Log("SetIgnoreCollisions");
        foreach (string tag in ignoreCollision)
        {
            foreach (GameObject piece in GameObject.FindGameObjectsWithTag(tag))
            {
                Debug.LogError(piece);
                if(piece.GetComponent<Collider>() != null)
                {
                    foreach (PTDie die in Dice)
                    {
                        Physics.IgnoreCollision(die.rigidBody.GetComponent<Collider>(), piece.GetComponent<Collider>());
                    }
                }
            }
        }
    }

    private void DiceDraggedHandler(Transform dragging)
    {
        //Debug.Log("DiceDraggedHandler");
        IsDragging = true;

        foreach (PTDie die in Dice)
        {
            die.rigidBody.isKinematic = false;
            die.rigidBody.collisionDetectionMode = CollisionDetectionMode.Continuous;
            die.rigidBody.drag = 1;
        }

        // disable other draggables
        foreach (GameObject diceDraggable in DiceDraggables)
        {
            if (diceDraggable != dragging)
            {
                diceDraggable.GetComponent<Collider>().enabled = false;
            }
        }

        Vector3 sumVector = Vector3.zero;
        foreach(PTDie die in Dice)
        {
            sumVector += die.transform.position;
        }
        dragging.transform.position = sumVector / Dice.Length;

        // set up spring joints on current draggable
        SpringJoint[] joints = dragging.GetComponents<SpringJoint>();
        for (int i = 0; i < joints.Length; ++i)
        {
            joints[i].connectedBody = Dice[i].rigidBody;
        }
    }

    private IEnumerator DiceDroppedHandler(PTTouchFollower follower)
    {
        //Debug.Log("DiceDroppedHandler " + follower.name);
        bool startedRolling = true;

        foreach (PTDie die in Dice)
        {
            if (new Vector3(die.rigidBody.velocity.x, 0, die.rigidBody.velocity.z).magnitude < VELOCITY_THRESHOLD)
            {
                startedRolling = false;
            }
        }

        foreach (GameObject draggable in DiceDraggables)
        {
            draggable.GetComponent<Collider>().enabled = !disableOnRoll;
            foreach (SpringJoint joint in draggable.GetComponents<SpringJoint>())
            {
                joint.connectedBody = null;
            }
        }

        if (startedRolling)
        {
            //Debug.Log("DiceDroppedHandler startedRolling");
            foreach (PTDie die in Dice)
            {
                die.rigidBody.useGravity = true;
                die.rigidBody.drag = 0;
            }
        }
        else
        {
            //Debug.Log("DiceDroppedHandler !startedRolling");
            EnableDice(false);
            yield return SendDiceBackToPosition(true, PT.DEFAULT_TIMER * 3);
            foreach (GameObject draggable in DiceDraggables)
            {
                draggable.GetComponent<Collider>().enabled = true;
            }

            EnableDice(true);
        }

        HasStartedRolling = startedRolling;
        IsDragging = false;
    }
}
