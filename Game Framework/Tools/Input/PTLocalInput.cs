using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlayTable
{
    [RequireComponent(typeof(Collider))]
    public abstract class PTLocalInput : MonoBehaviour
    {
        #region fields
        /// <summary>
        /// Is draggable by default drag.
        /// </summary>
        public bool dragEnabled = false;
        /// <summary>
        /// Play animation on dropped, if is true
        /// </summary>
        [HideInInspector]
        public bool enableDropAnimation = false;
        /// <summary>
        /// The offset to drag
        /// </summary>
        [HideInInspector]
        public Vector3 dragWorldPositionOffset = Vector3.zero;
        /// <summary>
        /// The factor applied to worldScale, when dragging
        /// </summary>
        [HideInInspector]
        public Vector3 dragWorldScaleOffset = Vector3.zero;
        /// <summary>
        /// The object being instantiated when touch begins, following touch when draging
        /// </summary>
        [HideInInspector]
        public PTLocalInput prefabDragToSpawn;
        /// <summary>
        /// The origin zone of this object when dragging
        /// </summary>
        private PTZone fromZone;
        #endregion

        #region property
        /// <summary>
        /// all collider on the direction from main camera to this transform position
        /// </summary>
        public HashSet<Collider> hitsRealtime
        {
            get
            {
                if (Camera.main.orthographic)
                {
                    return PTUtility.HitsRealtime(
                        transform.position + Vector3.up,
                        transform.position,
                        Camera.main.farClipPlane);
                }
                else
                {
                    return PTUtility.HitsRealtime(
                        Camera.main.transform.position, 
                        transform.position, 
                        Camera.main.farClipPlane);
                }
            }
        }
        #endregion

        #region delegates
        /// <summary>
        /// Invoked when a touch began on this.
        /// </summary>
        public PTDelegateTouch OnTouchBegin;
        /// <summary>
        /// Invoked when a touch ended on this.
        /// </summary>
        public PTDelegateTouch OnTouchEnd_EndOnThis;
        /// <summary>
        /// Invoked when a touch ended and the touch began on this.
        /// </summary>
        public PTDelegateTouch OnTouchEnd_BeginOnThis;
        /// <summary>
        /// Invoked when a touch that was not on this began to be on this.
        /// </summary>
        public PTDelegateTouch OnTouchEnter;
        /// <summary>
        /// Invoked each frame when a touch is on this.
        /// </summary>
        public PTDelegateTouch OnTouchHit;
        /// <summary>
        /// Invoked every frame on the touch began on this
        /// </summary>
        public PTDelegateTouch OnTouch;
        /// <summary>
        /// Invoked when a touch that was on this began to be not on this.
        /// </summary>
        public PTDelegateTouch OnTouchExit;
        /// <summary>
        /// Invoked each frame when a touch on this is moving.
        /// </summary>
        public PTDelegateTouch OnTouchMove;
        /// <summary>
        /// Invoked when a touch on this that was stationary started to move.
        /// </summary>
        public PTDelegateTouch OnTouchMoveBegin;
        /// <summary>
        /// Invoked each frame when the touch on this is stationary.
        /// </summary>
        public PTDelegateTouch OnHold;
        /// <summary>
        /// Invoked when a touch on this that was moving started to stationary.
        /// </summary>
        public PTDelegateTouch OnHoldBegin;
        /// <summary>
        /// Invoked when a touch on this is in the phase of short holding.
        /// </summary>
        public PTDelegateTouch OnShortHold;
        /// <summary>
        /// Invoked when the hold time of a touch on this just exceeded the short hold thredshold.
        /// </summary>
        public PTDelegateTouch OnShortHoldBegin;
        /// <summary>
        /// Invoked when a touch on this is in the phase of long holding.
        /// </summary>
        public PTDelegateTouch OnLongHold;
        /// <summary>
        /// Invoked when the hold time of a touch on this just exceeded the long hold thredshold.
        /// </summary>
        public PTDelegateTouch OnLongHoldBegin;
        /// <summary>
        /// Invoked when dragging. The touch must started on this. This does NOT need to be in the draggable list of the touch.
        /// </summary>
        public PTDelegateTouch OnDrag;
        /// <summary>
        /// Invoked when drag begins. For each touch, this will only be invoked once.
        /// </summary>
        public PTDelegateTouch OnDragBegin;
        /// <summary>
        /// Invoked when moved by dragging. The touch must started on this. This does need to be in the draggable list of the touch.
        /// </summary>
        public PTDelegateTouch OnFollow;
        /// <summary>
        /// Invoked when drag move begins. For each touch, this will only be invoked once.
        /// </summary>
        public PTDelegateTouch OnDragMoveBegin;
        /// <summary>
        /// Invoked when clicked on this
        /// </summary>
        public PTDelegateTouch OnTouched;
        /// <summary>
        /// Invoked when flick happened and the touch began this.
        /// </summary>
        public PTDelegateTouch OnFlicked;
        /// <summary>
        /// Invoked when double click happene on this. If click count is 4, this will be invoked twice.
        /// </summary>
        public PTDelegateTouch OnDoubleClicked;
        /// <summary>
        /// Invoked when double click happene on this. If click count is 6, this will be invoked twice.
        /// </summary>
        public PTDelegateTouch OnTripleClicked;
        /// <summary>
        /// Invoked when a follower is released
        /// </summary>
        public PTDelegateFollower OnReleased;
        /// <summary>
        /// Invoked when a follower's drop animation is finished after being released
        /// </summary>
        public PTDelegateFollower OnDropped;
        /// <summary>
        /// Invoked when exact touch count click happened. Will only be invoked once no matter how many clicks.
        /// </summary>
        public PTDelegateExclusiveTouch OnExclusiveClicked;
        /// <summary>
        /// Invoked when more than one touch began and active on this.
        /// </summary>
        public PTDelegateMultiTouch OnMultiTouch;
        /// <summary>
        /// Invoked when a collider is instantiated by dragging
        /// </summary>
        public PTDelegateCollider OnDragInstantiated;
        #endregion

        #region api
        //Not supposed to have api
        #endregion
            
        protected virtual void Awake()
        {
            enableDropAnimation = false;

            OnDragBegin += (PTTouch touch) =>
            {
                fromZone = GetComponentInParent<PTZone>();
                touch.canPenetrate = true;
            };

            OnDropped += (PTTouchFollower follower) =>
            {
                if (follower.hitZones.Count != 0)
                {
                    bool acceptedIntoZone = false;
                    foreach (PTZone zone in follower.hitZones)
                    {
                        if (zone.Accepts(follower.transform))
                        {
                            acceptedIntoZone = true;
                            if(zone.OnDropped != null)
                            {
                                zone.OnDropped(follower);
                            }
                            else
                            {
                                zone.Add(follower.transform);
                            }
                        }
                    }
                    if (!acceptedIntoZone)
                    {
                        SendBack();
                    }
                }
                else
                {
                    SendBack();
                }
            };
        }

        public void SendBack()
        {
            if (GetComponentInParent<PTZone>())
            {
                GetComponentInParent<PTZone>().Add(transform);
            }
        }
    }
}
