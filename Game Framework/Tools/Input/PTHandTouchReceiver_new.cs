using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayTable;

[RequireComponent(typeof(PTLayoutZone_new))]
public class PTHandTouchReceiver_new : PTLocalInput_new
{
    private class DragInfo
    {
        public Transform draggedChild { get; private set; }
        public float dragBeginXPos { get; private set; }
        public int prevSortingOrder { get; private set; }
        public DragState dragState { get; private set; }
        public DragInfo(Transform _transform, float xPos, int sortingOrder)
        {
            draggedChild = _transform;
            dragBeginXPos = xPos;
            prevSortingOrder = sortingOrder;
            dragState = DragState.None;
        }
        public void SetDragState(DragState newState)
        {
            dragState = newState;
        }
    }

    private enum DragState { None, Scrolling, Dragging };
    private enum Direction { Up, Down, Any };

    // Set in inspector
    [SerializeField]
    private bool scrollable;
    [SerializeField]
    int dragOutLimit;
    [SerializeField]
    protected float clampLeft, clampRight, startOffset;
    [SerializeField]
    private float deadZone = 0.2f;
    [SerializeField]
    private float fanRate;
    [SerializeField]
    private float angleThreshold = 110f;
    [SerializeField]
    Direction dragOutDirection;
    [SerializeField]
    Transform parentOfDraggedChild;

    // Private fields
    private Dictionary<PTTouch, DragInfo> touchDragInfo = new Dictionary<PTTouch, DragInfo>();
    protected PTLayoutZone_new myZone;
    private int draggingSortingOrder = 5;

    // public fields
    [TagSelector]
    public List<string> draggableTypes = new List<string>();

    // Properties
    protected float Spacing { get { return myZone.dimensionSpacings[0].x; } }
    private List<Collider> CollidersInChildren
    {
        get
        {
            List<Collider> colliders = new List<Collider>(GetComponentsInChildren<Collider>());
            colliders.Remove(GetComponent<Collider>());
            return colliders;
        }
    }
    protected int NumberDragging
    {
        get
        {
            int num = 0;
            foreach(KeyValuePair<PTTouch, DragInfo> activeTouch in touchDragInfo)
            {
                if(activeTouch.Value.dragState == DragState.Dragging)
                {
                    ++num;
                }
            }
            return num;
        }
    }

    public int NumberDraggingByTag(string dragTag)
    {
        int num = 0;
        foreach (KeyValuePair<PTTouch, DragInfo> activeTouch in touchDragInfo)
        {
            if (activeTouch.Value.dragState == DragState.Dragging)
            {
                if(activeTouch.Value.draggedChild.tag == dragTag)
                {
                    ++num;
                }
            }
        }
        return num;
    }

    protected override void Awake()
    {
        base.Awake();
        myZone = GetComponent<PTLayoutZone_new>();
        myZone.OnRemoved += (obj) => myZone.Arrange();
        OnDrag += DragHandler;
        OnDragBegin += DragBeginHandler;
        OnTouchEnd_BeginOnThis += (PTTouch touch) => 
        {
            Transform draggedObj = touchDragInfo[touch].draggedChild;
            if (draggedObj)
            {
                draggedObj.GetComponent<UnityEngine.Rendering.SortingGroup>().sortingOrder = touchDragInfo[touch].prevSortingOrder;
            }
            touchDragInfo.Remove(touch);
        };
    }

    private void DragBeginHandler(PTTouch touch)
    {
        float dragBegin = myZone.content.localPosition.x;
        Transform draggedChild = null;
        int sortingOrder = 0;
        if (draggableTypes.Count > 0)
        {
            foreach (Collider col in CollidersInChildren)
            {
                if (touch.hits.ContainsKey(col) && draggableTypes.Contains(col.tag))
                {
                    draggedChild = col.transform;
                    sortingOrder = draggedChild.GetComponent<UnityEngine.Rendering.SortingGroup>().sortingOrder;
                    break;
                }
            }
        }
        touchDragInfo.Add(touch, new DragInfo(draggedChild, dragBegin, sortingOrder));
    }

    private void DragHandler(PTTouch touch)
    {
        if (draggableTypes.Count > 0 || scrollable)
        {
            if (touchDragInfo[touch].dragState == DragState.Scrolling)
            {
                Scroll(touch);
            }
            else if (touchDragInfo[touch].dragState == DragState.Dragging)
            {
                //
            }
            else if (touchDragInfo[touch].dragState == DragState.None)
            {
                Scroll(touch);
                if (Vector3.Distance(touch.hitPoint, touch.hitPointBegin) > deadZone)
                {
                    if (CanDragOut(touch))
                    {
                        touchDragInfo[touch].SetDragState(DragState.Dragging);
                        touchDragInfo[touch].draggedChild.GetComponent<UnityEngine.Rendering.SortingGroup>().sortingOrder = draggingSortingOrder;
                        if (touchDragInfo[touch].draggedChild.GetComponent<PTLocalInput_new>())
                        {
                            touch.AddFollower(touchDragInfo[touch].draggedChild.GetComponent<PTLocalInput_new>());
                            touchDragInfo[touch].draggedChild.GetComponent<PTLocalInput_new>().OnDragBegin(touch);
                        }
                        else
                        {
                            touch.AddFollower(touchDragInfo[touch].draggedChild.GetComponent<Collider>(), Vector3.zero);
                        }
                        touchDragInfo[touch].draggedChild.SetLocalRotation(Quaternion.Euler(Vector3.zero), PT.DEFAULT_TIMER / 2);
                        touchDragInfo[touch].draggedChild.parent = parentOfDraggedChild;
                    }
                    else
                    {
                        touchDragInfo[touch].SetDragState(DragState.Scrolling);
                    }
                }
            }
        }
    }

    private bool CanDragOut(PTTouch touch)
    {
        bool canDragOut = false;
        if(touchDragInfo[touch].draggedChild != null && (dragOutLimit == 0 || NumberDragging < dragOutLimit))
        {
            Vector2 dragVector = touch.initPosition - touch.position;
            Vector2 localUp = new Vector2(transform.TransformDirection(Vector3.forward).x, transform.TransformDirection(Vector3.forward).z);
            float angle = Vector2.Angle(dragVector, localUp);
            if (angle > angleThreshold)
            {
                if (dragOutDirection == Direction.Up || dragOutDirection == Direction.Any)
                {
                    canDragOut = true;
                }
            }
            else if (angle < 180 - angleThreshold)
            {
                if (dragOutDirection == Direction.Down || dragOutDirection == Direction.Any)
                {
                    canDragOut = true;
                }
            }
        }
        return canDragOut;
    }

    private void Scroll(PTTouch touch)
    {
        Vector2 dragVector = touch.initPosition - touch.position;
        Vector3 slope = transform.TransformDirection(Vector3.right);
        float dragDistance = Vector3.Distance(touch.hitPoint, touch.hitPointBegin);

        if (Vector2.Angle(new Vector2(slope.x, slope.z), dragVector) < 90)
        {
            dragDistance *= -1;
        }

        float newLocalXPos;
        if (Mathf.Abs(slope.x) > Mathf.Abs(slope.z))
        {
            newLocalXPos = slope.x * (touch.hitPoint.x - touch.hitPointBegin.x) + touchDragInfo[touch].dragBeginXPos;
        }
        else
        {
            newLocalXPos = slope.z * (touch.hitPoint.z - touch.hitPointBegin.z) + touchDragInfo[touch].dragBeginXPos;
        }
        newLocalXPos = Mathf.Max(clampLeft, newLocalXPos);
        newLocalXPos = Mathf.Min(clampRight, newLocalXPos);

        myZone.content.localPosition = new Vector3(newLocalXPos, 0, 0);
        
        FanContent();
    }

    public void FanContent()
    {
        if (fanRate > 0)
        {
            foreach (Transform child in myZone.Objects)
            {
                List<PTTouch> touchesDragging = PTGlobalInput_new.FindTouchesDragging(child.GetComponent<Collider>());
                if(touchesDragging.Count == 0)
                {
                    /*
                    float offset = myZone.content.localPosition.x + (child.GetSiblingIndex() * Spacing);
                    float rotAngle = fanRate * offset / Spacing;
                    float circumference = (360 / fanRate) * Spacing;
                    float radius = circumference / (2 * Mathf.PI);

                    float offsetX = radius * Mathf.Sin(rotAngle * Mathf.PI / 180);
                    float offsetZ = -1 * (radius * (1 - Mathf.Cos(rotAngle * Mathf.PI / 180)));

                    child.localPosition = new Vector3(offsetX - myZone.content.localPosition.x, 0, offsetZ - myZone.content.localPosition.z);
                    child.localEulerAngles = new Vector3(child.localEulerAngles.x, rotAngle, child.localEulerAngles.z);
                    */
                    SendToFanPosition(child);
                }
            }
            foreach (KeyValuePair<PTTouch, DragInfo> touchDrag in touchDragInfo)
            {
                if (touchDrag.Value.draggedChild != null && touchDrag.Key.FindFollowerBy(touchDrag.Value.draggedChild.GetComponent<Collider>()) != null)
                {
                    Vector3 touchPos = touchDrag.Key.hitPoint;
                    touchPos.y = touchDrag.Value.draggedChild.position.y;
                    touchDrag.Value.draggedChild.position = touchPos;
                }
            }
        }
    }

    public void SendToFanPosition(Transform obj)
    {
        int siblingIndex = GetActiveSiblingIndex(obj);
        float offset = GetOffset(siblingIndex);

        float rotAngle = fanRate * offset / Spacing;
        float circumference = (360 / fanRate) * Spacing;
        float radius = circumference / (2 * Mathf.PI);

        float offsetX = radius * Mathf.Sin(rotAngle * Mathf.PI / 180);
        float offsetZ = -1 * (radius * (1 - Mathf.Cos(rotAngle * Mathf.PI / 180)));

        obj.SetLocalPosition(new Vector3(offsetX - myZone.content.localPosition.x, 0, offsetZ - myZone.content.localPosition.z), PT.DEFAULT_TIMER);
        obj.SetLocalRotation(Quaternion.Euler(obj.localEulerAngles.x, rotAngle, obj.localEulerAngles.z), PT.DEFAULT_TIMER);

        //obj.gameObject.SetActive(myZone.Count <= 5);
    }

    private int GetActiveSiblingIndex(Transform obj)
    {
        if (myZone.ignoreDisabledChildren)
        {
            int activeSiblingCount = 0;
            for(int i = 0; i < obj.parent.childCount; ++i)
            {
                if (obj.parent.GetChild(i) == obj)
                {
                    return activeSiblingCount;
                }
                if (obj.parent.GetChild(i).gameObject.activeInHierarchy)
                {
                    activeSiblingCount++;
                }
            }
            Debug.LogError("ERROR: Object to send to fan position not found in hand: " + obj);
            return 0;
        }
        else
        {
            return obj.GetSiblingIndex();
        }
    }

    private float GetOffset(int siblingIndex)
    {
        bool symmetric = myZone.dimensionIsSymmetric.Length > 0 && myZone.dimensionIsSymmetric[0].x > 0;
        bool startFromCenter = myZone.dimensionStartFromCenter.Length > 0 && myZone.dimensionStartFromCenter[0];
        float offset = siblingIndex;

        if (startFromCenter)
        {
            if (siblingIndex % 2 == 0)
            {
                offset = offset / 2 * -1;
            }
            else
            {
                offset = (offset + 1) / 2;
            }
            if (symmetric)
            {
                if (myZone.Count % 2 == 0)
                {
                    offset -= 0.5f;
                }
            }
            else
            {
                // add back the x value of the leftmost element
                offset += (myZone.Count + 1) / 2 - 1;
            }
        }

        else if (symmetric)
        {
            float minOffset = (float)(myZone.Count - 1) / 2 * -1;
            offset += minOffset;
        }
        return myZone.content.localPosition.x + (offset * Spacing);
    }
}
