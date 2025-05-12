using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayTable;

/// <summary>
/// PTDropZone is a zone where added objects are either sent into a child zone or stay where they were dropped
/// </summary>
public class PTDropZone : PTZone_new
{
    private List<PTZone_new> subZones
    {
        get
        {
            List<PTZone_new> childZones = new List<PTZone_new>(GetComponentsInChildren<PTZone_new>());
            childZones.Remove(this);
            return childZones;
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

            foreach (PTZone_new zone in subZones)
            {
                print("trying to drop into " + zone + ". Accepts = " + zone.Accepts(component.transform));
                if (zone.Accepts(component.transform))
                {
                    zone.Add(component.transform);
                    break;
                }
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
