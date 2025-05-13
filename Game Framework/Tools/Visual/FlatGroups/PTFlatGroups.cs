using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;

namespace PlayTable
{
    [RequireComponent(typeof(PTLayoutZone))]
    public class PTFlatGroups : MonoBehaviour
    {
        public bool allowRapidDrag = false;
        public PTFlatGroupElement elementPrefab;
        public float timerAnimation = 0.2f;
        public Vector2 sizeDeltaTwo = new Vector2(270, 266.4f);
        public float widthDelta = 0;
        public float maxCreateGroupDistance = 0.1f;
        //Still need testing for right vector
        public Vector3 right = Vector3.right;
        public float height = 0.1f;
        public Vector3 positionBackgroudSpawn = new Vector3(0, 0, 0.1f);
        public List<PTFlatGroupCollection> Collections { get; private set; }
        public PTFlatGroupBackground backgroundPrefab;
        public int Count
        {
            get
            {
                int ret = 0;
                foreach (PTFlatGroupCollection collection in Collections)
                {
                    ret += collection.Count;
                }
                return ret;
            }
        }

        private PTLayoutZone ptZone = null;
        public bool hasCooledDown { get; private set; }
        private Coroutine coroutineUpdateContent = null;

        public bool isDragging { get; private set; }
        public PTDelegateTouch OnDragBegan;
        public PTDelegateTouch OnDrag;
        public PTDelegateTouch OnDragEnd;

        #region Unity
        private void Awake()
        {
            Collections = new List<PTFlatGroupCollection>();
            ptZone = GetComponent<PTLayoutZone>();
            hasCooledDown = true;

            PTGlobalInput.OnTouchEnd += (PTTouch touch) =>
            {
                UpdateContent();
            };

            OnDragBegan += (PTTouch touch) =>
            {
                if (!allowRapidDrag)
                {
                    isDragging = true;
                    //SetDraggable(false, touch);
                }
            };
            OnDragEnd += (PTTouch touch) =>
            {
                isDragging = false;
            };
            ptZone.OnArranged += () =>
            {
                //SetDraggable(true, null);
            };
        }
        #endregion

        #region helper
        /*private void SetDraggable(bool value, PTTouch touch)
        {
            if (value)
            {
                foreach (PTObject obj in ptZone.Objects)
                {
                    obj.enableDrag = true;
                }
            }
            else
            {
                foreach (PTObject obj in ptZone.Objects)
                {
                    obj.enableDrag = touch == null ? true : touch.hit == obj.Collider;
                }
            }
        }*/
        private void UpdateContent()
        {
            if (coroutineUpdateContent != null)
            {
                StopCoroutine(coroutineUpdateContent);
            }
            coroutineUpdateContent = StartCoroutine(UpdateContentCoroutine());
        }
        private IEnumerator UpdateContentCoroutine()
        {
            int targetIndex = 0;
            foreach (PTFlatGroupCollection collection in Collections)
            {
                //Update siblings of elements
                foreach (PTFlatGroupElement ele in collection.content)
                {
                    ele.transform.SetSiblingIndex(targetIndex++);
                }
                //Update background
                collection.UpdateBackground(this);
            }
            yield return ptZone.ArrangeCoroutine(timerAnimation);
            coroutineUpdateContent = null;
        }
        public IEnumerator Cooldown(float timer)
        {
            hasCooledDown = false;
            while (timer > 0)
            {
                yield return new WaitForEndOfFrame();
                timer -= Time.deltaTime;
            }
            hasCooledDown = true;
        }
        #endregion

        #region API
        public void CreateElements(int num)
        {
            //Only for testing use
            for (int i = 0; i < num; ++i)
            {
                PTFlatGroupElement element = Instantiate(elementPrefab).GetComponent<PTFlatGroupElement>();
                ptZone.Add(element.transform, timerAnimation);
            }
            UpdateContent();

        }
        public void Calibrate(bool rearrange)
        {
            //put all elements on children to a list
            List<PTFlatGroupElement> listChildrenElements = new List<PTFlatGroupElement>();
            foreach (Transform child in ptZone.content)
            {
                PTFlatGroupElement currElement = child.GetComponent<PTFlatGroupElement>();
                if (currElement)
                {
                    listChildrenElements.Add(currElement);
                }
            }

            HashSet<PTFlatGroupElement> elementsToRemoveFromCollections = new HashSet<PTFlatGroupElement>();
            foreach (PTFlatGroupCollection collection in Collections)
            {
                foreach (PTFlatGroupElement element in collection.content)
                {
                    //Remove the element from collection if current element doesn't exist in the transform children
                    if (!listChildrenElements.Contains(element))
                    {
                        //Take a note of which elements are going to delete
                        try { elementsToRemoveFromCollections.Add(element); } catch { }

                    }
                    //Remove the element from list if current element exist in both list and collection
                    else
                    {
                        listChildrenElements.Remove(element);
                    }
                }
            }
            foreach (PTFlatGroupElement element in elementsToRemoveFromCollections)
            {
                Remove(element);
            }

            //Add the rest children to collection
            foreach (PTFlatGroupElement element in listChildrenElements)
            {
                AddCollection(element);
            }

            if (rearrange)
            {
                UpdateContent();
            }
        }
        public int IndexOf(PTFlatGroupCollection collection)
        {
            return Collections.IndexOf(collection);
        }
        public void AddCollection(PTFlatGroupElement element)
        {
            if (Contains(element))
            {
                return;
            }
            else
            {
                PTFlatGroupCollection newCollection = new PTFlatGroupCollection(false);
                newCollection.Add(element);
                Collections.Add(newCollection);
            }
        }
        public void Remove(PTFlatGroupElement element)
        {
            //Remove from collection
            PTFlatGroupCollection collection = FindCollectionBy(element);
            if (collection != null)
            {
                if (collection.Contains(element))
                {
                    collection.Remove(element);
                }
                if (collection.Count == 0)
                {
                    Collections.Remove(collection);
                }
            }

            //unparent
            if (element != null && this == element.GetComponentInParent<PTFlatGroups>())
            {
                element.transform.SetParent(null);
            }
        }
        public bool Contains(PTFlatGroupElement element)
        {
            if (!element)
            {
                return false;
            }
            foreach (PTFlatGroupCollection collection in Collections)
            {
                if (collection.Contains(element))
                {
                    return true;
                }
            }
            return false;
        }
        public void SwapCollections(int collectionIndexA, int collectionIndexB)
        {
            if (collectionIndexA >= 0 && collectionIndexA < Collections.Count
                && collectionIndexB >= 0 && collectionIndexB < Collections.Count
                && Mathf.Abs(collectionIndexA - collectionIndexB) == 1)
            {
                PTFlatGroupCollection tmp = Collections[collectionIndexA];
                Collections[collectionIndexA] = Collections[collectionIndexB];
                Collections[collectionIndexB] = tmp;
                UpdateContent();
            }

        }
        public void SwapElements(int siblingIndexA, int siblingIndexB)
        {
            if (siblingIndexA >= 0 && siblingIndexA < ptZone.Count
                && siblingIndexB >= 0 && siblingIndexB < ptZone.Count
                && Mathf.Abs(siblingIndexA - siblingIndexB) == 1)
            {
                //locate
                KeyValuePair<int, PTFlatGroupCollection> collectionA = FindCollectionWithIndexBy(siblingIndexA);
                KeyValuePair<int, PTFlatGroupCollection> collectionB = FindCollectionWithIndexBy(siblingIndexB);
                PTFlatGroupElement elementA = ptZone.Get(siblingIndexA).GetComponent<PTFlatGroupElement>();
                PTFlatGroupElement elementB = ptZone.Get(siblingIndexB).GetComponent<PTFlatGroupElement>();

                //swap
                int indexElementA = collectionA.Value.IndexOf(elementA);
                int indexElementB = collectionB.Value.IndexOf(elementB); collectionA.Value.Remove(elementA);
                int targetInsertIndex = indexElementA < collectionA.Value.Count ? indexElementA : collectionA.Value.Count;
                collectionA.Value.Insert(targetInsertIndex, elementB);
                collectionB.Value.Remove(elementB);
                collectionB.Value.Insert(indexElementB, elementA);

                //update content
                UpdateContent();
            }
        }
        public void Merge(int collectionIndexFrom, int collectionIndexTo)
        {
            if (collectionIndexFrom >= 0 && collectionIndexFrom < Collections.Count
                && collectionIndexTo >= 0 && collectionIndexTo < Collections.Count
                && collectionIndexFrom != collectionIndexTo
                && Collections[collectionIndexTo].isGroup)
            {
                foreach (PTFlatGroupElement element in Collections[collectionIndexFrom].content)
                {
                    Collections[collectionIndexTo].Add(element);
                }
                Collections.Remove(Collections[collectionIndexFrom]);
                UpdateContent();
            }
        }
        public void SetIsGroupIfSingle(PTFlatGroupCollection collection, bool value)
        {
            SetIsGroupIfSingle(Collections.IndexOf(collection), value);
        }
        public void SetIsGroupIfSingle(int index, bool value)
        {
            if (index >= 0 && index < Collections.Count)
            {
                Collections[index].isGroupIfSingle = value;
                UpdateContent();
            }
        }
        public void UnGroup(PTFlatGroupElement element)
        {
            KeyValuePair<int, PTFlatGroupCollection> collection = FindCollectionWithIndexBy(element);

            if (collection.Value != null)
            {
                collection.Value.Remove(element);
                PTFlatGroupCollection newCollection = new PTFlatGroupCollection(false);
                newCollection.Add(element);
                Collections.Insert(collection.Key + 1, newCollection);
                collection.Value.UpdateBackground(this);
                if (collection.Value.Count == 0)
                {
                    Collections.Remove(collection.Value);
                }
            }

            UpdateContent();
        }
        public Vector2 GetHorizontalRangeOf(PTFlatGroupCollection collection)
        {
            if (collection != null && collection.Count > 0)
            {
                Vector2 ret = new Vector2();

                //min
                ret.x = (collection.content[0].transform.position - 0.5f * ptZone.dimensionSpacings[0]).magnitude;
                //max
                ret.y = (collection.content[collection.Count - 1].transform.position + 0.5f * ptZone.dimensionSpacings[0]).magnitude;
            }
            return Vector2.zero;
        }
        public int GetSiblingIndexHover(Vector3 worldPosition)
        {
            Vector3 firstPosition = ptZone.content.position + ptZone.firstChildLocalPosition;
            float distanceUnitDirection = Vector3.Dot(ptZone.dimensionSpacings[0], right);
            float distanceOnRight = Vector3.Dot(worldPosition - firstPosition, right);
            return (int)(distanceOnRight / distanceUnitDirection + 0.5f);
        }
        public int GetSiblingIndexExpected(PTFlatGroupElement element)
        {
            int siblingIndexHover = GetSiblingIndexHover(element.transform.position);
            KeyValuePair<int, PTFlatGroupCollection> collectionHover = FindCollectionWithIndexBy(siblingIndexHover);
            int siblingIndexCurr = GetSiblingIndex(element);
            int elementDistance = Mathf.Abs(siblingIndexHover - siblingIndexCurr);
            int offset = siblingIndexHover > siblingIndexCurr ? 1 : -1;
            KeyValuePair<int, PTFlatGroupCollection> collectionInTheWay = FindCollectionWithIndexBy(siblingIndexHover - offset);


            if (collectionHover.Value == null || collectionHover.Value.Contains(element) || collectionInTheWay.Value == null)
            {
                return siblingIndexHover;
            }
            else
            {
                //Debug.Log(siblingIndexIntent + " " + siblingIndexCurr + " " + elementDistance + " " + collectionHover.Count);
                if (collectionHover.Value.isGroup)
                {
                    if (elementDistance > collectionInTheWay.Value.Count)
                    {
                        return siblingIndexHover - offset;
                    }
                }
                else
                {
                    if (elementDistance == collectionInTheWay.Value.Count)
                    {
                        return siblingIndexHover;
                    }
                    else
                    {
                        return siblingIndexHover - offset;
                    }
                }
                return siblingIndexCurr;

            }
        }
        public int GetSiblingIndex(PTFlatGroupElement element)
        {
            int ret = 0;
            for (int i = 0; i < Collections.Count; ++i)
            {
                for (int j = 0; j < Collections[i].Count; ++j)
                {
                    if (Collections[i].content[j] == element)
                    {
                        return ret;
                    }
                    ++ret;
                }
            }
            return -1;
        }
        public KeyValuePair<int, PTFlatGroupCollection> FindCollectionWithIndexBy(Vector3 worldPosition)
        {
            int expectedIndex = GetSiblingIndexHover(worldPosition);
            if (expectedIndex >= 0)
            {
                int elementCount = 0;
                for (int i = 0; i < Collections.Count; ++i)
                {
                    if (Collections[0].Count + elementCount > expectedIndex)
                    {
                        return new KeyValuePair<int, PTFlatGroupCollection>(i, Collections[0]);
                    }
                }
            }
            return new KeyValuePair<int, PTFlatGroupCollection>(-1, null);
        }
        public KeyValuePair<int, PTFlatGroupCollection> FindCollectionWithIndexBy(PTFlatGroupElement element)
        {
            if (Collections != null && element != null)
            {
                for (int i = 0; i < Collections.Count; i++)
                {
                    if (Collections[i].Contains(element))
                    {
                        return new KeyValuePair<int, PTFlatGroupCollection>(i, Collections[i]);
                    }
                }
            }
            return new KeyValuePair<int, PTFlatGroupCollection>(-1, null);
        }
        public KeyValuePair<int, PTFlatGroupCollection> FindCollectionWithIndexBy(int siblingIndex)
        {
            if (Collections == null || siblingIndex >= ptZone.Count || siblingIndex < 0)
            {
                return new KeyValuePair<int, PTFlatGroupCollection>(-1, null);
            }

            int count = 0;
            for (int i = 0; i < Collections.Count; ++i)
            {
                if (Collections[i].Count + count > siblingIndex)
                {
                    return new KeyValuePair<int, PTFlatGroupCollection>(i, Collections[i]);
                }
                count += Collections[i].Count;
            }
            return new KeyValuePair<int, PTFlatGroupCollection>(-1, null);
        }
        public PTFlatGroupCollection FindCollectionBy(PTFlatGroupElement element)
        {
            return FindCollectionWithIndexBy(element).Value;
        }
        public PTFlatGroupCollection FindCollectionBy(int siblingIndex)
        {
            return FindCollectionWithIndexBy(siblingIndex).Value;
        }
        #endregion
    }
}