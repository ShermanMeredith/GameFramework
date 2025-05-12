using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayTable;

/// <summary>
/// PTContainerZone is a zone that only accepts an object if its container is empty
/// </summary>
public class PTContainerZone_new : PTZone_new
{
    [SerializeField]
    private bool swapEnabled;

    public PTDelegateTransformFromTransform OnSwap;

    protected override void Awake()
    {
        childrenWorldEularAngles = transform.eulerAngles;
        base.Awake();
        capacity = 1;

        if (swapEnabled)
        {
            OnDropped += (PTTouchFollower droppedObj) =>
            {
                if (Count > 0)
                {
                    PTZone_new newZoneForCurrentObject = droppedObj.transform.GetComponentInParent<PTZone_new>();
                    if (newZoneForCurrentObject != null)
                    {
                        Transform currentObject = Get(0);
                        newZoneForCurrentObject.Add(currentObject);
                        if (OnSwap != null) { OnSwap(currentObject, newZoneForCurrentObject.transform); }
                    }
                }
                Add(droppedObj.transform);
            };
        }
    }

    public override bool Accepts(Transform obj)
    {
        if (swapEnabled)
        {
            bool canAccept = false;
            if (acceptedObjectsWhenDropped.Count == 0)
            {
                Debug.LogError("Accepted objects not defined for PTZone in " + name);
            }
            foreach (string acceptable in acceptedObjectsWhenDropped)
            {
                if (obj.tag == acceptable)
                {
                    canAccept = true;
                    break;
                }
            }
            return canAccept;
        }
        else
        {
            return base.Accepts(obj);
        }
    }

    /// <summary>
    /// Add a transform to content. Ignoring Accepts method.
    /// </summary>
    /// <param name="component"></param>
    /// <param name="siblingIndex"></param>
    /// <param name="timer"></param>
    /// <returns></returns>
    public override IEnumerator AddCoroutine(Component component, int siblingIndex, float timer)
    {
        if (component != null)
        {
            Transform fromParent = component.transform.parent;
            //transform 's collider is being dragged
            Collider collider = component.GetComponent<Collider>();
            if (collider && collider.IsBeingDragged())
            {
                yield break;
            }

            bool colliderWasEnabled = (collider && collider.enabled);
            if (collider)
            {
                collider.enabled = false;
            }

            if (fromParent != null)
            {
                PTZone_new zone = fromParent.GetComponentInParent<PTZone_new>();
                if(zone != null && zone.GetComponent<PTContainerZone_new>() != this && zone.OnRemoved != null)
                {
                    fromParent.GetComponentInParent<PTZone_new>().OnRemoved(component.transform);
                }
            }

            if (controlChildrenWorldEularAngles)
            {
                component.transform.SetWorldRotation(childrenWorldEularAngles, timer);
            }
            component.transform.SetParent(content, siblingIndex);
            component.transform.SetLocalPosition(Vector3.zero, timer);
            if (controlChildrenWorldScale)
            {
                component.transform.SetWorldScale(childrenWorldScale, timer);
            }

            yield return new WaitForSeconds(timer);
            if (collider)
            {
                collider.enabled = colliderWasEnabled;
            }

            if (component != null && OnAdded != null)
            {
                OnAdded(component.transform, fromParent);
            }

        }
    }
    public override IEnumerator AddCoroutine(Component component, float timer)
    {
        int siblingIndex = component.transform.parent == content ? component.transform.GetSiblingIndex() : int.MaxValue;
        yield return AddCoroutine(component, siblingIndex, timer);
    }
    public override IEnumerator AddCoroutine(Component component)
    {
        int siblingIndex = component.transform.parent == content ? component.transform.GetSiblingIndex() : int.MaxValue;
        yield return AddCoroutine(component, siblingIndex, PT.DEFAULT_TIMER);
    }
    public override void Add(Component component, int siblingIndex, float timer)
    {
        StartCoroutine(AddCoroutine(component, siblingIndex, timer));
    }
    public override void Add(Component component, float timer)
    {
        //Add to the end of children if trans is not child of content.
        if (component != null)
        {
            int siblingIndex = component.transform.parent == content ? component.transform.GetSiblingIndex() : int.MaxValue;
            Add(component, siblingIndex, timer);
        }
    }
    public override void Add(Component component)
    {
        Add(component, PT.DEFAULT_TIMER);
    }
}
