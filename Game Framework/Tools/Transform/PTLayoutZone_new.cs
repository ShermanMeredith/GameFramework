using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayTable;

/// <summary>
/// PTLayoutZone is a zone where added objects are laid out into a special formation based on this script's parameters
/// </summary>
public class PTLayoutZone_new : PTZone_new
{
    /// <summary>
    /// Used for arranging children
    /// </summary>
    public Vector3 firstChildLocalPosition = Vector3.zero;
    /// <summary>
    /// The max num of objects in a dimension
    /// </summary>
    public int[] dimensionLimits = new int[] { };
    /// <summary>
    /// The spacing between children
    /// </summary>
    public Vector3[] dimensionSpacings = new Vector3[1];
    /// <summary>
    /// This determines if transforms mirror in each dimension, in terms of x, y and z.
    /// </summary>
    public Vector3[] dimensionIsSymmetric;
    /// <summary>
    /// This determines if transforms start from center
    /// </summary>
    public bool[] dimensionStartFromCenter;

    /// <summary>
    /// Get the target world position in arrangement
    /// </summary>
    /// <param name="siblingIndex"> The given sibling index </param>
    /// <returns></returns>
    public Vector3 TargetWorldPositionOf(int siblingIndex)
    {
        if (siblingIndex < 0 || siblingIndex >= Count)
        {
            return Vector3.zero;
        }
        else
        {
            return transform.TransformPoint(TargetLocalPositionOf(siblingIndex));
        }
    }
    /// <summary>
    /// Get the target local position in arrangement
    /// </summary>
    /// <param name="siblingIndex"> The given sibling index </param>
    /// <returns></returns>
    public Vector3 TargetLocalPositionOf(int siblingIndex)
    {
        siblingIndex = siblingIndex > 0 ? siblingIndex : 0;
        siblingIndex = siblingIndex < Count ? siblingIndex : Count - 1;

        //Get valid count id limits
        int validCountOfLimits = 0;
        foreach (int i in dimensionLimits)
        {
            if (i <= 0)
            {
                break;
            }
            validCountOfLimits++;
        }

        //Get dimension
        int totalDimension = validCountOfLimits < dimensionSpacings.Length ?
            validCountOfLimits + 1 : dimensionSpacings.Length;

        //Adjust sibling index by extra spacing
        int totalCountExtraSpacing = 0;
        for (int i = 0; i <= siblingIndex; ++i)
        {
            PTTransform currTrans = Get(i).GetComponent<PTTransform>();
            if (currTrans)
            {
                totalCountExtraSpacing += currTrans.countExtraSpacing;
            }
        }
        int adjustedSiblingIndex = siblingIndex + totalCountExtraSpacing;

        //Get total offset
        Vector3 offset = Vector3.zero;
        for (int dimension = 0; dimension < totalDimension; dimension++)
        {
            //Get maxLowerDimension
            int capacityLowerDimension = 1;
            for (int j = 0; j < dimension; j++)
            {
                capacityLowerDimension *= dimensionLimits[j];
            }

            //Calculate offset
            int indexInCurrDimension = (adjustedSiblingIndex / capacityLowerDimension) %
                (totalDimension - dimension > 1 ? dimensionLimits[dimension] : System.Int32.MaxValue);

            int currTotalLowerIncludesDimension = CurrTotalLowerIncludes(dimension);
            int capacityLowerExcludesDimension = CapacityLowerExcludes(dimension);
            int totalInTheSameDimension = capacityLowerExcludesDimension == 0 ? currTotalLowerIncludesDimension : currTotalLowerIncludesDimension / capacityLowerExcludesDimension + (currTotalLowerIncludesDimension % capacityLowerExcludesDimension > 0 ? 1 : 0);
            //Debug.Log("capacityLowerExcludesDimension=" + capacityLowerExcludesDimension + " totalInTheSameDimension=" + totalInTheSameDimension);

            //+ currTotalLowerIncludesDimension % capacityLowerExcludesDimension == 0 ? 0 : 1;
            //Debug.Log(" totalInTheSameDimension=" + totalInTheSameDimension);


            /*Debug.Log(name
                + " indexInCurrDimension=" + indexInCurrDimension
                + " totalInTheSameDimension=" + totalInTheSameDimension
                + " CurrTotalLowerIncludes(dimension)" + CurrTotalLowerIncludes(dimension)
                + " CapacityLowerExcludes(dimension)" + CapacityLowerExcludes(dimension)
                );
            */
            Vector3 currSpacing = dimensionSpacings.Length > dimension ? dimensionSpacings[dimension] : Vector3.zero;
            offset += GetIncreaseSpacing(IsSymmetricOnDimension(dimension), currSpacing, indexInCurrDimension, totalInTheSameDimension, dimension);
        }

        //Target local position
        return firstChildLocalPosition + offset;
    }
    public Vector3 IsSymmetricOnDimension(int dimension)
    {
        return dimension < dimensionIsSymmetric.Length ? dimensionIsSymmetric[dimension] : Vector3.zero;
    }
    private Vector3 GetIncreaseSpacing(Vector3 isSymmetric, Vector3 spacing, int indexInCurrDimension, int totalInTheSameDimension, int dimensionIndex)
    {
        return new Vector3(
            GetOffsetValue(isSymmetric.x != 0, spacing.x, indexInCurrDimension, totalInTheSameDimension, dimensionIndex),
            GetOffsetValue(isSymmetric.y != 0, spacing.y, indexInCurrDimension, totalInTheSameDimension, dimensionIndex),
            GetOffsetValue(isSymmetric.z != 0, spacing.z, indexInCurrDimension, totalInTheSameDimension, dimensionIndex));
    }
    private float GetOffsetValue(bool isSymmetric, float spacing, int indexInCurrDimension, int totalInTheSameDimension, int dimensionIndex)
    {
        //  |   | . |   |           4
        //  2   0   1   3

        //  |   |   |               3
        //  2   0   1

        //  |   | . |   |           4
        //  0   1   2   3

        //  |   |   |               3
        //  0   1   2

        float ret = 0;
        if (isSymmetric)
        {
            if (dimensionIndex >= dimensionStartFromCenter.Length || !dimensionStartFromCenter[dimensionIndex])
            {
                //start from one side
                ret = totalInTheSameDimension == 0
                    ? 0 : ((float)indexInCurrDimension - ((float)totalInTheSameDimension) / 2.0f + 0.5f) * spacing;
                /*Debug.Log(
                    "indexInCurrDimension=" + indexInCurrDimension
                    + " totalInTheSameDimension=" + totalInTheSameDimension
                    + " ret=" + ret);
                */
            }
            else
            {
                //start from center
                float positivity = indexInCurrDimension % 2 == 0 ? -1 : 1;
                float factor = totalInTheSameDimension % 2 == 0 ? (indexInCurrDimension / 2 + 0.5f) : (int)((indexInCurrDimension + 1) / 2);
                ret = factor * positivity * spacing;
            }
        }
        else
        {
            ret = spacing * indexInCurrDimension;
        }
        return ret;
    }
    public int CapacityLowerExcludes(int dimension)
    {
        if (dimension <= 0)
        {
            return 0;
        }
        else
        {
            int ret = 1;
            for (int j = 0; j < dimension; j++)
            {
                ret *= j < dimensionLimits.Length ? dimensionLimits[j] : System.Int32.MaxValue;
            }
            return ret;
        }
    }
    public int CapacityLowerIncludes(int dimension)
    {
        if (dimension < 0)
        {
            return 0;
        }
        else
        {
            int ret = 1;
            for (int j = 0; j <= dimension; j++)
            {
                ret *= j < dimensionLimits.Length ? dimensionLimits[j] : System.Int32.MaxValue;
            }
            return ret;
        }

    }
    public int CurrTotalLowerIncludes(int dimension)
    {
        int maxLowerInclude = CapacityLowerIncludes(dimension);

        return Count < maxLowerInclude ? Count : maxLowerInclude;
    }
    /// <summary>
    /// Flip all the children Simultaneously
    /// </summary>
    /// <param name="timer">the entire time ccost for the animation</param>
    /// <param name="faceup">facing target. facing up if true.</param>
    /// <param name="keepTilt">doesn't change rotation aroung world Y</param>
    /// <returns>Set realtime rotation each frame</returns>
    public IEnumerator FlipTogetherCoroutine(float timer, bool faceup, bool keepTilt)
    {
        foreach (Transform child in content)
        {
            child.SetFacing(faceup, keepTilt, timer);
        }

        yield return new WaitForSeconds(timer);
        Arrange(timer);
    }
    /// <summary>
    /// Flip all the children one afer another
    /// </summary>
    /// <param name="timer">the time cost for the animation of each child</param>
    /// <param name="faceup">facing target. facing up if true.</param>
    /// <param name="keepTilt">doesn't change rotation aroung world Y</param>
    /// <returns>Set realtime rotation each frame</returns>
    public IEnumerator FlipOneByOne(float timer, bool faceup, bool keepTilt)
    {
        foreach (Transform child in content)
        {
            yield return child.SetFacingCoroutine(faceup, keepTilt, timer);
        }

        Arrange(timer);
    }
    /// <summary>
    /// Simply starts FlipTogetherCoroutine
    /// </summary>
    /// <param name="faceup">facing target. facing up if true.</param>
    public void FlipTogether(bool faceup)
    {
        FlipTogether(faceup, true, PT.DEFAULT_TIMER);
    }
    /// <summary>
    /// Simply starts FlipTogetherCoroutine
    /// </summary>
    /// <param name="timer">the entire time ccost for the animation</param>
    /// <param name="faceup">facing target. facing up if true.</param>
    /// <param name="keepTilt">doesn't change rotation aroung world Y</param>
    public void FlipTogether(bool faceup, bool keepTilt, float timer)
    {
        StartCoroutine(FlipTogetherCoroutine(timer, faceup, keepTilt));
    }

    /// <summary>
    /// Return if the zone is arranging.
    /// </summary>
    public bool IsArranging { get; private set; }

    #region Unity built-in
    private void OnEnable()
    {
        IsArranging = false;

        //Run the endless ContinuouslyArrange coroutine
        StartCoroutine(ContinuouslyArrange());
    }
    #endregion

    /// <summary>
    /// Arrange every autoArrangeTimer seconds
    /// </summary>
    /// <returns>Set current position in interpolation</returns>
    private IEnumerator ContinuouslyArrange()
    {
        const float waitTime = 3;
        while (true)
        {
            if (autoArrangeTimer > 0)
            {
                //Debug.LogError(name);
                yield return ArrangeCoroutine(PT.DEFAULT_TIMER);
                yield return new WaitForSeconds(autoArrangeTimer);
            }
            else
            {
                //Check if the autoArrangeTimer is back on
                yield return new WaitForSeconds(waitTime);
            }
        }
    }

    /// <summary>
    /// Arrange all children by adding all of them
    /// </summary>
    /// <param name="timer"></param>
    /// <returns></returns>
    public IEnumerator ArrangeCoroutine(float timer)
    {
        if (!IsArranging)
        {
            IsArranging = true;
            Dictionary<Transform, bool> colliderWasEnabled = new Dictionary<Transform, bool>();
            for (int i = 0; i < Count; ++i)
            {
                //animation
                Transform currChild = Get(i);
                Add(currChild, timer);
                if (currChild.GetComponent<Collider>())
                {
                    colliderWasEnabled.Add(currChild, currChild.GetComponent<Collider>().enabled);
                    currChild.GetComponent<Collider>().enabled = false;
                }
                currChild.ToggleVisibility(i < maxVisable || maxVisable < 0, PT.DEFAULT_TIMER);
            }
            yield return new WaitForSeconds(timer);
            for (int i = 0; i < Count; ++i)
            {
                Transform currChild = Get(i);
                if (currChild.GetComponent<Collider>())
                {
                    currChild.GetComponent<Collider>().enabled = colliderWasEnabled.ContainsKey(currChild) ? colliderWasEnabled[currChild] : false;
                }
            }
            IsArranging = false;

            if (OnArranged != null)
            {
                OnArranged();
            }
        }
    }
    public void Arrange(float timer)
    {
        if (isActiveAndEnabled)
        {
            StartCoroutine(ArrangeCoroutine(timer));
        }
    }
    public void Arrange()
    {
        if (isActiveAndEnabled)
        {
            StartCoroutine(ArrangeCoroutine(arrangeAnimationTimer));
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

            if (controlChildrenWorldEularAngles)
            {
                component.transform.SetWorldRotation(childrenWorldEularAngles, timer);
            }
            component.transform.SetParent(content, siblingIndex);

            if (fromParent != null && fromParent.GetComponentInParent<PTZone_new>() != null && fromParent.GetComponentInParent<PTZone_new>().transform != transform && fromParent.GetComponentInParent<PTZone_new>().OnRemoved != null)
            {
                fromParent.GetComponentInParent<PTZone_new>().OnRemoved(component.transform);
            }

            if (GetComponent<PTHandTouchReceiver_new>() != null && GetComponent<PTHandTouchReceiver_new>().isActiveAndEnabled)
            {
                GetComponent<PTHandTouchReceiver_new>().SendToFanPosition(component.transform);
            }
            else
            {
                component.transform.SetLocalPosition(TargetLocalPositionOf(siblingIndex), timer);
            }
            if (controlChildrenWorldScale)
            {
                component.transform.SetWorldScale(childrenWorldScale, timer);
            }

            yield return new WaitForSeconds(timer);

            if (component != null && OnAdded != null)
            {
                OnAdded(component.transform, fromParent);
            }

            //collider.enabled = true;

        }
    }
    public override IEnumerator AddCoroutine(Component component, float timer)
    {
        //Add to the end of children if trans is not child of content.
        if (component != null)
        {
            int siblingIndex = int.MaxValue;
            if (component.transform.parent == content)
            {
                int enabledSiblingCount = 0;
                for (int i = 0; i < component.transform.parent.childCount; ++i)
                {
                    if (component.transform.parent.GetChild(i) == component.transform)
                    {
                        siblingIndex = enabledSiblingCount;
                        break;
                    }
                    if (component.transform.parent.GetChild(i).gameObject.activeInHierarchy)
                    {
                        ++enabledSiblingCount;
                    }
                }
            }
            yield return AddCoroutine(component, siblingIndex, timer);
        }
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
            int siblingIndex = int.MaxValue;
            if(component.transform.parent == content)
            {
                int enabledSiblingCount = 0;
                for (int i = 0; i < component.transform.parent.childCount; ++i)
                {
                    if (component.transform.parent.GetChild(i) == component.transform)
                    {
                        siblingIndex = enabledSiblingCount;
                        break;
                    }
                    if (component.transform.parent.GetChild(i).gameObject.activeInHierarchy)
                    {
                        ++enabledSiblingCount;
                    }
                }
            }
            Add(component, siblingIndex, timer);
        }
    }
    public override void Add(Component component)
    {
        Add(component, PT.DEFAULT_TIMER);
    }

    void Start ()
    {
		
	}
}
