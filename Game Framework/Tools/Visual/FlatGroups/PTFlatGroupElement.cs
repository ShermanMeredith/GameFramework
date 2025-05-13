using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlayTable
{
    [RequireComponent(typeof(PTTransform))]
    [RequireComponent(typeof(PTLocalInput))]
    public class PTFlatGroupElement : MonoBehaviour
    {

        private PTFlatGroups groups { get { return GetComponentInParent<PTFlatGroups>(); } }
        private PTLocalInput gesture = null;

        public KeyValuePair<int, PTFlatGroupCollection> Collection
        {
            get
            {
                if (groups == null)
                {
                    return new KeyValuePair<int, PTFlatGroupCollection>(-1, null);
                }
                else
                {
                    return groups.FindCollectionWithIndexBy(this);
                }
            }
        }
        public int SiblingIndexHover
        {
            get
            {
                try { return groups.GetSiblingIndexHover(transform.position); }
                catch { return -1; }
            }
        }
        KeyValuePair<int, PTFlatGroupCollection> CollectionHover
        {
            get
            {
                try { return groups.FindCollectionWithIndexBy(SiblingIndexHover); }
                catch { return new KeyValuePair<int, PTFlatGroupCollection>(-1, null); }
            }
        }
        public int SiblingIndexExpected
        {
            get
            {
                try { return groups.GetSiblingIndexExpected(this); }
                catch { return -1; }
            }
        }
        public Vector3 PositionExpected
        {
            get
            {
                if (groups)
                {
                    return groups.GetComponent<PTLayoutZone>().TargetWorldPositionOf(SiblingIndexExpected);
                }
                else
                {
                    return Vector3.zero;
                }
            }
        }
        KeyValuePair<int, PTFlatGroupCollection> CollectionExpected
        {
            get
            {
                try { return groups.FindCollectionWithIndexBy(SiblingIndexExpected); }
                catch { return new KeyValuePair<int, PTFlatGroupCollection>(-1, null); }
            }
        }

        private void Awake()
        {
            gesture = GetComponent<PTLocalInput>();

            gesture.OnDragBegin += (PTTouch touch) =>
            {
                if (gameObject 
                    && groups
                    && !groups.GetComponent<PTLayoutZone>().IsArranging 
                    && !groups.isDragging 
                    && touch.hitsBegin.ContainsKey(gesture.GetComponent<Collider>()))
                {
                    if (Collection.Value != null && Collection.Value.isGroup && Collection.Value.Count == 1)
                    {
                        //ungroup for a single tile 
                        groups.UnGroup(this);
                    }
                    if (groups.OnDragBegan != null)
                    {
                        groups.OnDragBegan(touch);
                    }
                }
            };
            gesture.OnDrag += (PTTouch touch) =>
            {
                if (gameObject && PTGlobalInput.IsDragging(gesture.GetComponent<Collider>()) && touch.hitsBegin.ContainsKey(gesture.GetComponent<Collider>()))
                {
                    if ((transform.position - groups.transform.position).z > groups.height)
                    {
                        //Ungroup when too high
                        groups.UnGroup(this);
                    }

                    if (Collection.Key == CollectionHover.Key)
                    {
                        //arrange inside the group
                        groups.SwapElements(transform.GetSiblingIndex(), SiblingIndexExpected);
                    }
                    else
                    {
                        if (CollectionHover.Key != Collection.Key && Collection.Value.isGroup)
                        {
                            //ungroup
                            groups.UnGroup(this);
                        }
                        else
                        {
                            //swap collections
                            groups.SwapCollections(Collection.Key, CollectionExpected.Key);
                        }
                    }
                }
            };
            gesture.OnTouchEnd_BeginOnThis += (PTTouch touch) =>
            {
                if (gameObject 
                    && groups
                    && (groups.isDragging || groups.GetComponent<PTLayoutZone>().IsArranging)
                    && gesture.GetComponent<PTGamePiece>() && gesture.GetComponent<PTGamePiece>().IsDraggable)
                {
                    if (groups.OnDragEnd != null)
                    {
                        groups.OnDragEnd(touch);
                    }
                }
            };
            gesture.OnLongHoldBegin += (PTTouch touch) =>
            {
                if (groups)
                {
                    //Become a group
                    if (Collection.Key == CollectionHover.Key && Collection.Value.Count == 1)
                    {
                        Vector3 positionExpected = PositionExpected;
                        Vector2 positionExpected2D = new Vector2(positionExpected.x, positionExpected.z);
                        Vector2 position2D = new Vector2(transform.position.x, transform.position.z);
                        float distance2D = (positionExpected2D - position2D).magnitude;

                        //If the distance is close enough from the expected position
                        if (distance2D < groups.maxCreateGroupDistance)
                        {
                            groups.SetIsGroupIfSingle(Collection.Key, !Collection.Value.isGroupIfSingle);
                        }
                    }
                    //Merge with another group
                    else
                    {
                        groups.Merge(Collection.Key, CollectionHover.Key);
                    }

                }
            };
        }

    }
}