using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
namespace PlayTable
{
    /// <summary>
    /// The place to organize multiple transforms
    /// </summary>
    public abstract class PTZone : MonoBehaviour
    {
        #region fields
        [SerializeField, TagSelector]
        protected List<string> acceptedObjectsWhenDropped;
        /// <summary>
        /// The place where contains a bunch of PTObjects instances
        /// </summary>
        public Transform content;
        /// <summary>
        /// Determines animation speed
        /// </summary>
        public float arrangeAnimationTimer = 0.5f;
        /// <summary>
        /// Determines the refresh rate
        /// </summary>
        public float autoArrangeTimer = 0f;
        /// <summary>
        /// The max number of visible objects. Used ToggleVisibility.
        /// </summary>
        public int maxVisable = -1;
        public bool ignoreDisabledChildren = false;

        /// <summary>
        /// True if want to keep all children world scale, children scale will be updated when Arranging
        /// </summary>
        public bool controlChildrenWorldScale = true;
        /// <summary>
        /// The value to keep for children's world scale, children scale will be updated when Arranging
        /// </summary>
        public Vector3 childrenWorldScale = Vector3.one;
        public bool controlChildrenWorldEularAngles = false;
        public Vector3 childrenWorldEularAngles = Vector3.zero;

        /// <summary>
        /// The maximum number of object this zone can take. Unlimited if value is negative.
        /// </summary>
        public int capacity = -1;
        /*
        /// <summary>
        /// Ignores the acceptedTypes list if true
        /// </summary>
        public bool acceptsAnyType = true;
        /// <summary>
        /// The class names of accepted objects
        /// </summary>
        public List<string> acceptedTypes;
        */
        #endregion

        #region Property
        /// <summary>
        /// Get the total number of current objects in the zone.
        /// </summary>
        public int Count { get {
                if (ignoreDisabledChildren)
                {
                    int ret = 0;
                    foreach (Transform child in content)
                    {
                        ret = child.gameObject.activeSelf ? ret + 1 : ret;
                    }
                    return ret;
                }
                else
                {
                    return content.childCount;
                }
            }
        }

        /// <summary>
        /// Gets the objects in the zone by transform
        /// </summary>
        public List<Transform> Objects
        {
            get
            {
                List<Transform> ret = new List<Transform>();
                for (int i = 0; i < Count; ++i)
                {
                    ret.Add(Get(i));
                }
                return ret;
            }
        }
        #endregion

        #region delegates
        /// <summary>
        /// Invoked when the newcoming object passed Accepts check
        /// </summary>
        public PTDelegateTransformFromTransform OnAccepted;
        /// <summary>
        /// Invoked when the adding animation is done.
        /// </summary>
        public PTDelegateTransformFromTransform OnAdded;
        /// <summary>
        /// Invoked when an object leaves content
        /// </summary>
        public PTDelegateTransform OnRemoved;
        /// <summary>
        /// Invoked when a arrange coroutine is done
        /// </summary>
        public PTDelegateVoid OnArranged;
        /// <summary>
        /// Invoked when a ptTransform is dropped on this.
        /// </summary>
        public PTDelegateFollower OnDropped;
        /// <summary>
        /// Invoked when an acceptable ptTransform is dropped on this.
        /// </summary>
        public PTDelegateFollower OnDroppedAcceptable;
        /// <summary>
        /// Invoked once when an acceptable transform is first dragged on this.
        /// </summary>
        public PTDelegateFollower OnHoverEnter;
        /// <summary>
        /// Invoked once when an acceptable transform is first dragged off this.
        /// </summary>
        public PTDelegateFollower OnHoverExit;
        /// <summary>
        /// Invoked each frame an acceptable transform is dragged on this.
        /// </summary>
        public PTDelegateFollower OnHover;

        #endregion

        #region api
        public virtual bool Accepts(Transform obj)
        {
            bool canAccept = false;
            if (capacity == -1 || Count < capacity)
            {
                if(acceptedObjectsWhenDropped.Count == 0)
                {
                    Debug.LogWarning("Accepted objects not defined for PTZone in " + name);
                }
                foreach (string acceptable in acceptedObjectsWhenDropped)
                {
                    if (obj.tag == acceptable)
                    {
                        canAccept = true;
                        break;
                    }
                }
            }
            return canAccept;
        }

        public List<Transform> GetObjectsOfType<T>()
        {
            List<Transform> ret = new List<Transform>();
            for (int i = 0; i < Count; ++i)
            {
                if (Get(i).GetComponent<T>() != null)
                {
                    ret.Add(Get(i));
                }
            }
            return ret;
        }

        public void SetContentsDraggable(bool enabled)
        {
            for (int i = 0; i < Count; ++i)
            {
                Get(i).GetComponent<PTGamePiece>().IsDraggable = enabled;
            }
        }

        public void SetContentsInteractive(bool enabled)
        {
            for (int i = 0; i < Count; ++i)
            {
                Get(i).GetComponent<Collider>().enabled = enabled;
            }
        }

        public int IndexOf(Transform transform)
        {
            for(int i = 0; i < Count; ++i)
            {
                if (content.GetChild(i) == transform)
                {
                    return i;
                }
            }
            return -1;
        }
        public bool Contains(Transform trans)
        {
            foreach (Transform child in content)
            {
                if (child == trans)
                {
                    return true;
                }
            }
            return false;
        }
        /// <summary>
        /// Get the object by index
        /// </summary>
        /// <param name="index"></param>
        /// <returns>null if no legit object found</returns>
        public Transform Get(int index)
        {
            try
            {
                if (ignoreDisabledChildren)
                {
                    int enabledSiblingCount = 0;
                    for (int i = 0; i < content.childCount; ++i)
                    {
                        if (content.GetChild(i).gameObject.activeInHierarchy)
                        {
                            if(enabledSiblingCount == index)
                            {
                                return content.GetChild(i);
                            }
                            ++enabledSiblingCount;
                        }
                    }
                }
                else
                {
                    return content.GetChild(index);
                }
                return null;
            }
            catch
            {
                return null;
            }
        }
        /// <summary>
        /// Add a transform to content. Ignoring Accepts method.
        /// </summary>
        /// <param name="component"></param>
        /// <param name="siblingIndex"></param>
        /// <param name="timer"></param>
        /// <returns></returns>
        public abstract IEnumerator AddCoroutine(Component component, int siblingIndex, float timer);

        public abstract IEnumerator AddCoroutine(Component component, float timer);

        public abstract IEnumerator AddCoroutine(Component component);

        public abstract void Add(Component component, int siblingIndex, float timer);

        public abstract void Add(Component component, float timer);

        public abstract void Add(Component component);

        /// <summary>
        /// Virtual function that determines if an object is acceptable when trying to add
        /// </summary>
        /// <param name="other">The object attepting to be added in</param>
        /// <returns>Success if accepted</returns>
        /*public virtual bool Accepts(PTTransform other)
        {
            if (acceptsAnyType)
            {
                return true;
            }
            else
            {
                if (!other || capacity > 0 && content.childCount >= capacity)
                {
                    return false;
                }
                else
                {
                    bool transformHasComponentOfAcceptedTypes = false;
                    foreach (string acceptedType in acceptedTypes)
                    {
                        if (other.GetComponent(acceptedType))
                        {
                            transformHasComponentOfAcceptedTypes = true;
                            break;
                        }
                    }

                    return acceptedTypes.Contains(other.TypeName) || transformHasComponentOfAcceptedTypes;
                }
            }
        }*/
        #endregion

        protected virtual void Awake()
        {
            if(content == null)
            {
                Debug.LogWarning(name + ": zone has not been assigned content transform. Defaulting to transform of " + name);
                content = transform;
            }
        }
    }
}