using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlayTable
{
    public sealed class PTInputManager : MonoBehaviour
    {
        #region fields
        /// <summary>
        /// If set to true, all touches since the second one will be ignored
        /// </summary>
        public bool enableMultiTouch = true;
        /// <summary>
        /// Set touches' default canPenetrate value
        /// </summary>
        public bool defaultCanPenetrate = true;
        /// <summary>
        /// The moving speed of second click simulating black dot, when controlled by WASD or arrow keys.
        /// </summary>
        public float blackdotSpeed = 5;
        /// <summary>
        /// The distance between black dot and the actually click position.
        /// </summary>
        public Vector2 offsetMouse = new Vector2(300, 0);
        /// <summary>
        /// The time threshold to enter short hold phase
        /// </summary>
        public float TIME_SHORTHOLD = 0.5f;
        /// <summary>
        /// The time threshold to enter long hold phase
        /// </summary>
        public float TIME_LONGHOLD = 1.5f;
        /// <summary>
        /// The max time span to determine a click.
        /// </summary>
        public float SPAN_CLICK = 0.1f;
        /// <summary>
        /// The total time for rapid clicks
        /// </summary>
        public float SPAN_RAPIDCLICK = 1f;
        /// <summary>
        /// The max time span to determine a flick.
        /// </summary>
        public float SPAN_FLICK = 0.15f;
        /// <summary>
        /// The min distance to determine a flick from a click. Also for rapid clicks
        /// </summary>
        public float DISTANCE_FLICK = 100f;
        /// <summary>
        /// The squared min distance to determine a flick. Also for rapid clicks. (for optimization purpose)
        /// </summary>
        public float DISTANCE_FLICK_SQR { get { return Mathf.Pow(DISTANCE_FLICK, 2); } }
        /// <summary>
        /// The screen space distance to tell the difference between holding and moving. Unit: pixel.
        /// </summary>
        public float DISTANCE_MOVE = 5;
        /// <summary>
        /// The squared screen space distance to tell the difference between holding and moving. (for optimization purpose)
        /// </summary>
        public float DISTANCE_MOVE_SQR { get { return Mathf.Pow(DISTANCE_MOVE, 2); } }

        public static PTInputManager singleton = null;
        private static PTTouch firstMouseTouch = null;
        public static List<PTTouch> touches { get; private set; }
        public readonly static Dictionary<PTTouch, Dictionary<Collider, DateTime>> hits = new Dictionary<PTTouch, Dictionary<Collider, DateTime>>();
        private static Dictionary<Collider, Dictionary<PTTouch, DateTime>> touchesBegan
        {
            get
            {
                Dictionary<Collider, Dictionary<PTTouch, DateTime>> ret = new Dictionary<Collider, Dictionary<PTTouch, DateTime>>();
                foreach (PTTouch touch in touches)
                {
                    foreach (Collider collider in touch.hitsBegin.Keys)
                    {
                        if (collider && !ret.ContainsKey(collider))
                        {
                            ret.Add(collider, new Dictionary<PTTouch, DateTime>());
                        }
                        try { ret[collider].Add(touch, touch.hitsBegin[collider]); } catch { }
                    }
                }
                return ret;
            }
        }
        private static List<PTPapidClick> rapidClicks = new List<PTPapidClick>();
        private static bool isInitated = false;
        #endregion

        #region Unity Built-in
        private void OnDestroy()
        {
            singleton = null;
        }
        private void Awake()
        {
            //singleton
            if (!singleton)
            {
                singleton = this;
            }
            else
            {
                Destroy(this);
            }

            touches = new List<PTTouch>();

            if (!isInitated)
            {
                //Invoke Advaced Gestures
                InvokeAdvancedGestures();
                //Local
                RegisterLocalDelegates();
                //Handle drag and drop
                HandleDraggables();

                isInitated = true;
            }
        }
        private void Update()
        {
            Dictionary<PTTouch, PTTouchEvent> inputs = new Dictionary<PTTouch, PTTouchEvent>();

            //Android
            GetCurrTouchesOnMobile(inputs);

            //win
            GetCurrTouchesOnComputer(inputs);

            //Process each current touches
            ProcessCurrTouches(inputs);

            //Update touches, cast rays from each touch
            UpdateTouchesHits();

            //rapid clicks
            HandleRapidClicks();

            //Multi-Touches As Single Gesture
            MultiTouchesAsSingleGesture();
        }
        private void LateUpdate()
        {
            //Handle Relationship between Current and Last Touch
            HandleRelationshipCurrentLastTouch();
        }
#if UNITY_EDITOR || UNITY_STANDALONE
        private void OnGUI()
        {
            if (Application.platform != RuntimePlatform.Android
                && firstMouseTouch != null)
            {
                GUI.Box(new Rect(firstMouseTouch.position.x + offsetMouse.x, Screen.height - firstMouseTouch.position.y - offsetMouse.y, 1, 1), "");
            }
        }
#endif
        #endregion

        #region private helpers
        private void GetCurrTouchesOnMobile(Dictionary<PTTouch, PTTouchEvent> inputs)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                PTTouchEvent touchEvent = PTTouchEvent.Unknown;
                switch (Input.touches[i].phase)
                {
                    case TouchPhase.Began:
                        touchEvent = PTTouchEvent.Began;
                        break;
                    case TouchPhase.Ended:
                    case TouchPhase.Canceled:
                        touchEvent = PTTouchEvent.Ended;
                        break;
                    default:
                        touchEvent = PTTouchEvent.Existing;
                        break;
                }
                inputs.Add(new PTTouch(Input.touches[i]), touchEvent);
            }
        }
        private void GetCurrTouchesOnComputer(Dictionary<PTTouch, PTTouchEvent> inputs)
        {
            if (touches == null)
            {
                return;
            }
            else
            {
                if (Application.platform == RuntimePlatform.WindowsPlayer
                    || Application.platform == RuntimePlatform.WindowsEditor
                    || Application.platform == RuntimePlatform.OSXEditor
                    || Application.platform == RuntimePlatform.OSXPlayer)
                {
                    //Mouse multitouch simulation
                    PTTouch touchMouseLeft = touches.Find(x => x.ID == PTTouch.TOUCH_ID_MOUSE_LEFT);
                    PTTouch touchMouseRight = touches.Find(x => x.ID == PTTouch.TOUCH_ID_MOUSE_RIGHT);

                    if (touchMouseLeft != null && touchMouseRight == null
                        || touchMouseRight != null && touchMouseLeft == null
                        )
                    {
                        //singleMouseTouch = 
                        if (firstMouseTouch == null)
                        {
                            firstMouseTouch = touchMouseLeft != null ? touchMouseLeft : touchMouseRight; ;
                        }
                    }
                    else
                    {
                        //drawer = null;
                        if (touchMouseLeft == null && touchMouseRight == null)
                        {
                            firstMouseTouch = null;
                        }
                        //singleMouseTouch = null;
                    }

                    //Moving use keyboard
                    if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W))
                    {
                        offsetMouse += new Vector2(0, blackdotSpeed);
                    }
                    if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S))
                    {
                        offsetMouse += new Vector2(0, -blackdotSpeed);
                    }
                    if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D))
                    {
                        offsetMouse += new Vector2(blackdotSpeed, 0);
                    }
                    if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A))
                    {
                        offsetMouse += new Vector2(-blackdotSpeed, 0);
                    }

                    //Get the offsetMouseLeft and offsetMouseRight value
                    Vector3 offsetMouseLeft = Vector3.zero;
                    Vector3 offsetMouseRight = Vector3.zero;
                    if (firstMouseTouch != null)
                    {
                        if (firstMouseTouch.ID != PTTouch.TOUCH_ID_MOUSE_LEFT)
                        {
                            offsetMouseLeft = new Vector3(offsetMouse.x, offsetMouse.y);
                        }
                        if (firstMouseTouch.ID != PTTouch.TOUCH_ID_MOUSE_RIGHT)
                        {
                            offsetMouseRight = new Vector3(offsetMouse.x, offsetMouse.y);
                        }
                    }

                    //Simulation TOUCH_ID_MOUSE_LEFT
                    if (Input.GetMouseButtonDown(0))
                    {
                        try { inputs.Add(new PTTouch(Input.mousePosition, offsetMouseLeft, PTTouch.TOUCH_ID_MOUSE_LEFT), PTTouchEvent.Began); } catch { }
                    }
                    if (Input.GetMouseButton(0))
                    {
                        try { inputs.Add(new PTTouch(Input.mousePosition, offsetMouseLeft, PTTouch.TOUCH_ID_MOUSE_LEFT), PTTouchEvent.Existing); } catch { }
                    }
                    if (Input.GetMouseButtonUp(0))
                    {
                        try { inputs.Add(new PTTouch(Input.mousePosition, offsetMouseLeft, PTTouch.TOUCH_ID_MOUSE_LEFT), PTTouchEvent.Ended); } catch { }
                    }
                    //Simulation TOUCH_ID_MOUSE_RIGHT
                    if (Input.GetMouseButtonDown(1))
                    {
                        //print("Down " + (Input.mousePosition + offsetMouseRight));
                        try { inputs.Add(new PTTouch(Input.mousePosition, offsetMouseRight, PTTouch.TOUCH_ID_MOUSE_RIGHT), PTTouchEvent.Began); } catch { }
                    }
                    if (Input.GetMouseButton(1))
                    {
                        //print("Down " + (Input.mousePosition + offsetMouseRight));
                        try { inputs.Add(new PTTouch(Input.mousePosition, offsetMouseRight, PTTouch.TOUCH_ID_MOUSE_RIGHT), PTTouchEvent.Existing); } catch { }
                    }
                    if (Input.GetMouseButtonUp(1))
                    {
                        //print("Down " + (Input.mousePosition + offsetMouseRight));
                        try { inputs.Add(new PTTouch(Input.mousePosition, offsetMouseRight, PTTouch.TOUCH_ID_MOUSE_RIGHT), PTTouchEvent.Ended); } catch { }
                    }
                }
            }
        }
        private void ProcessCurrTouches(Dictionary<PTTouch, PTTouchEvent> inputs)
        {
            foreach (PTTouch input in inputs.Keys)
            {
                //Get the existing touch from touches
                PTTouch touch = touches.Find(x => x.ID == input.ID);

                if (touch == null)
                {
                    //Began
                    if (inputs[input] == PTTouchEvent.Began)
                    {
                        if (enableMultiTouch || touches.Count == 0)
                        {
                            touches.Add(input);
                            touch = touches.Find(x => x.ID == input.ID);
                            touch.canPenetrate = defaultCanPenetrate;

                            //OnTouchBegin
                            if (PTGlobalInput.OnTouchBegin != null)
                            {
                                PTGlobalInput.OnTouchBegin(touch);
                            }

                            //_hitsBegan
                            touch.hitBegan = touch.hit;
                            if (touch.canPenetrate)
                            {
                                HashSet<Collider> setHits = touch.hitsRealtime;
                                foreach (Collider collider in setHits)
                                {
                                    try { touch.hitsBegin.Add(collider, DateTime.Now); } catch { }

                                }
                            }
                            else
                            {
                                try { touch.hitsBegin.Add(touch.hitBegan, DateTime.Now); } catch { }
                            }

                            //OnTouchHitBegin
                            if (PTGlobalInput.OnTouchHitBegin != null)
                            {
                                PTGlobalInput.OnTouchHitBegin(touch);
                            }
                        }
                    }
                }
                //unregistered touch
                if (touch == null)
                {
                    return;
                }

                //Offset
                touch.offset = input.offset;

                //Trace
                touch.path.Add(new KeyValuePair<DateTime, Vector2>(DateTime.Now, touch.position));

                //OnTouch
                if (PTGlobalInput.OnTouch != null)
                {
                    PTGlobalInput.OnTouch(touch);
                }

                //End
                if (inputs[input] == PTTouchEvent.Ended)
                {
                    try { touches.Remove(touch); } catch { }

                    //OnEnd
                    if (PTGlobalInput.OnTouchEnd != null)
                    {
                        PTGlobalInput.OnTouchEnd(touch);
                    }

                    if (touch.life.TotalSeconds < SPAN_CLICK
                            && (touch.position - touch.initPosition).sqrMagnitude < DISTANCE_FLICK_SQR)
                    {
                        //OnClicked
                        if (PTGlobalInput.OnClicked != null)
                        {
                            PTGlobalInput.OnClicked(touch);
                        }
                    }

                    if (touch.life.TotalSeconds < SPAN_FLICK
                            && (touch.position - touch.initPosition).sqrMagnitude >= DISTANCE_FLICK_SQR)
                    {
                        if (PTGlobalInput.OnFlicked != null)
                        {
                            PTGlobalInput.OnFlicked(touch);
                        }
                    }
                }
            }
        }
        private void UpdateTouchesHits()
        {
            foreach (PTTouch touch in touches)
            {
                if (touch != null && !hits.ContainsKey(touch))
                {
                    try { hits.Add(touch, new Dictionary<Collider, DateTime>()); } catch { }
                }

                //exists
                Dictionary<Collider, DateTime> exists = new Dictionary<Collider, DateTime>();
                foreach (Collider collider in hits[touch].Keys)
                {
                    if (collider && hits[touch].ContainsKey(collider))
                    {
                        exists.Add(collider, hits[touch][collider]);
                    }
                }

                //Get all hits
                HashSet<Collider> currhitsSet = new HashSet<Collider>();
                Dictionary<Collider, DateTime> currhitsDict = new Dictionary<Collider, DateTime>();
                if (touch.canPenetrate)
                {
                    foreach (Collider collider in touch.hitsRealtime)
                    {
                        try { currhitsSet.Add(collider); } catch { }
                    }
                }
                else
                {
                    Collider collider = touch.hit;
                    if (collider)
                    {
                        try { currhitsSet.Add(collider); } catch { }
                    }
                }
                foreach (Collider collider in currhitsSet)
                {
                    if (collider)
                    {
                        if (collider && exists.ContainsKey(collider))
                        {
                            try { currhitsDict.Add(collider, exists[collider]); } catch { }
                            if (PTGlobalInput.OnTouchInside != null)
                            {
                                PTGlobalInput.OnTouchInside(touch, collider);
                            }
                        }
                        else
                        {
                            try { currhitsDict.Add(collider, DateTime.Now); } catch { }

                            if (PTGlobalInput.OnTouchEnter != null)
                            {
                                PTGlobalInput.OnTouchEnter(touch, collider);
                            }
                        }
                    }

                    //Get the left colliders
                    if (collider && exists.ContainsKey(collider))
                    {
                        exists.Remove(collider);
                    }
                }
                hits[touch] = currhitsDict;

                //exited hits
                foreach (Collider collider in exists.Keys)
                {
                    //OnExit
                    if (PTGlobalInput.OnTouchExit != null)
                    {
                        PTGlobalInput.OnTouchExit(touch, collider);
                    }
                }
            }
        }
        private void HandleRapidClicks()
        {
            //Use copy to avoid changing while iteration
            List<PTPapidClick> copyRapidClicks = new List<PTPapidClick>();
            foreach (PTPapidClick click in rapidClicks)
            {
                copyRapidClicks.Add(click);
            }
            foreach (PTPapidClick click in copyRapidClicks)
            {
                click.life -= Time.deltaTime;
                if (click.life < 0)
                {
                    click.InvokeExclusiveClickEvent();
                    rapidClicks.Remove(click);
                }
            }
        }
        private void MultiTouchesAsSingleGesture()
        {
            foreach (Collider collider in touchesBegan.Keys)
            {
                if (PTGlobalInput.OnMultiTouch != null)
                {
                    int count = touchesBegan[collider].Keys.Count;
                    PTTouch[] currTouches = new PTTouch[count];
                    int i = 0;
                    foreach (PTTouch touch in touchesBegan[collider].Keys)
                    {
                        currTouches[i] = touch;
                        i++;
                    }
                    PTGlobalInput.OnMultiTouch(collider, currTouches);
                }
            }
        }
        private void AddRapidClick(PTTouch touch)
        {
            //Find nearest
            PTPapidClick nearest = null;
            float minDistance = float.MaxValue;
            foreach (PTPapidClick click in rapidClicks)
            {
                float sqrdistance = (click.initPosition - touch.position).sqrMagnitude;
                if (sqrdistance < minDistance
                    && sqrdistance < DISTANCE_FLICK_SQR)
                {
                    minDistance = sqrdistance;
                    nearest = click;
                }
            }

            //Add to nearest
            bool isAdded = false;
            if (nearest != null)
            {
                isAdded = nearest.Add(touch);
            }

            if (!isAdded)
            {
                rapidClicks.Add(new PTPapidClick(touch));
            }
        }
        private void InvokeAdvancedGestures()
        {
            PTGlobalInput.OnTouchEnd += (PTTouch touch) =>
            {
                touch.canPenetrate = defaultCanPenetrate;
            };

            //Add exclusive clicks 
            PTGlobalInput.OnClicked += (PTTouch touch) =>
            {
                AddRapidClick(touch);
            };

            //Change hold phases
            PTGlobalInput.OnHold += (PTTouch touch) =>
            {
                //phaseHold
                switch (touch.phase)
                {
                    case PTTouchPhase.StationaryEnter:
                        //OnShortHoldBegin
                        if (touch.spanStationary.TotalSeconds > TIME_SHORTHOLD)
                        {
                            touch.phase = PTTouchPhase.StationaryShort;
                            if (PTGlobalInput.OnShortHoldBegin != null)
                            {
                                PTGlobalInput.OnShortHoldBegin(touch);
                            }
                        }
                        break;
                    case PTTouchPhase.StationaryShort:
                        //OnShortHold
                        if (PTGlobalInput.OnShortHold != null)
                        {
                            PTGlobalInput.OnShortHold(touch);
                        }
                        //OnLongHoldBegin
                        if (touch.spanStationary.TotalSeconds > TIME_LONGHOLD)
                        {
                            touch.phase = PTTouchPhase.StationaryLong;
                            if (PTGlobalInput.OnLongHoldBegin != null)
                            {
                                PTGlobalInput.OnLongHoldBegin(touch);
                            }
                        }
                        break;
                    case PTTouchPhase.StationaryLong:
                        //OnLongHold
                        if (PTGlobalInput.OnLongHold != null)
                        {
                            PTGlobalInput.OnLongHold(touch);
                        }
                        break;

                }
            };

            //OnDropped
            PTGlobalInput.OnTouchEnd += (PTTouch touch) =>
            {
                foreach (PTTouchFollower draggable in touch.followers)
                {
                    if (PTGlobalInput.OnReleased != null)
                    {
                        PTGlobalInput.OnReleased(touch, draggable);
                    }
                }
            };
        }
        private void RegisterLocalDelegates()
        {
            //gesture.OnTouchBegin
            PTGlobalInput.OnTouchHitBegin += (PTTouch touch) =>
            {
                foreach (Collider collider in touch.hitsBegin.Keys)
                {
                    if (collider)
                    {
                        PTLocalInput localInput = collider.GetComponent<PTLocalInput>();
                        if (localInput)
                        {
                            if (localInput.OnTouchBegin != null)
                            {
                                localInput.OnTouchBegin(touch);
                            }

                            //Drag to spawn another object with PTLocalInput component
                            if (!localInput.dragEnabled && localInput.prefabDragToSpawn)
                            {
                                Collider colliderNewObject = Instantiate(
                                    localInput.prefabDragToSpawn.gameObject,
                                    touch.hitPoint,
                                    localInput.prefabDragToSpawn.transform.rotation)
                                    .GetComponent<Collider>();
                                touch.AddFollower(colliderNewObject, localInput.prefabDragToSpawn.dragWorldPositionOffset);
                                if (localInput.OnDragInstantiated != null)
                                {
                                    localInput.OnDragInstantiated(colliderNewObject);
                                }
                            }
                        }
                    }
                }
            };

            //gesture.OnTouchEnd_EndOnThis
            PTGlobalInput.OnTouchEnd += (PTTouch touch) =>
            {
                foreach (Collider collider in touch.hits.Keys)
                {
                    if (collider)
                    {
                        PTLocalInput gesture = collider.GetComponent<PTLocalInput>();
                        if (gesture && gesture.OnTouchEnd_EndOnThis != null)
                        {
                            gesture.OnTouchEnd_EndOnThis(touch);
                        }
                    }
                }
            };

            //gesture.OnTouchEnd_BeginOnThis
            PTGlobalInput.OnTouchEnd += (PTTouch touch) =>
            {
                foreach (Collider collider in touch.hitsBegin.Keys)
                {
                    if (collider)
                    {
                        PTLocalInput gesture = collider.GetComponent<PTLocalInput>();
                        if (gesture && gesture.OnTouchEnd_BeginOnThis != null)
                        {
                            gesture.OnTouchEnd_BeginOnThis(touch);
                        }
                    }

                }
            };

            //gesture.OnTouchEnter
            PTGlobalInput.OnTouchEnter += (PTTouch touch, Collider collider) =>
            {
                if (collider)
                {
                    PTLocalInput localInput = collider.GetComponent<PTLocalInput>();
                    if (localInput && localInput.OnTouchEnter != null)
                    {
                        localInput.OnTouchEnter(touch);
                    }
                }
            };

            //OnTouch, OnTouchHit
            PTGlobalInput.OnTouch += (PTTouch touch) =>
            {
                if (touch.hitBegan)
                {
                    PTLocalInput localInput_Begin = touch.hitBegan.GetComponent<PTLocalInput>();
                    if (localInput_Begin)
                    {
                        if (localInput_Begin.OnTouch != null)
                        {
                            localInput_Begin.OnTouch(touch);
                        }
                    }
                }

                foreach (Collider collider in touch.hits.Keys)
                {
                    if (collider)
                    {
                        PTLocalInput localInput = collider.GetComponent<PTLocalInput>();
                        if (localInput && localInput.OnTouchHit != null)
                        {
                            localInput.OnTouchHit(touch);
                        }
                    }
                }
            };

            //gesture.OnTouchExit
            PTGlobalInput.OnTouchExit += (PTTouch touch, Collider collider) =>
            {
                if (collider)
                {
                    PTLocalInput localInput = collider.GetComponent<PTLocalInput>();
                    if (localInput && localInput.OnTouchExit != null)
                    {
                        localInput.OnTouchExit(touch);
                    }
                }
            };

            //gesture.OnTouchMove
            PTGlobalInput.OnTouchMove += (PTTouch touch) =>
            {
                foreach (Collider collider in touch.hits.Keys)
                {
                    if (collider)
                    {
                        PTLocalInput gesture = collider.GetComponent<PTLocalInput>();
                        if (gesture && gesture.OnTouchMove != null)
                        {
                            gesture.OnTouchMove(touch);
                        }
                    }
                }
            };

            //gesture.OnTouchMoveBegin
            PTGlobalInput.OnTouchMoveBegin += (PTTouch touch) =>
            {
                foreach (Collider collider in touch.hits.Keys)
                {
                    if (collider)
                    {
                        PTLocalInput gesture = collider.GetComponent<PTLocalInput>();
                        if (gesture && gesture.OnTouchMoveBegin != null)
                        {
                            gesture.OnTouchMoveBegin(touch);
                        }
                    }
                }
            };

            //gesture.OnHold
            PTGlobalInput.OnHold += (PTTouch touch) =>
            {
                foreach (Collider collider in touch.hits.Keys)
                {
                    if (collider)
                    {
                        PTLocalInput gesture = collider.GetComponent<PTLocalInput>();
                        if (gesture && gesture.OnHold != null)
                        {
                            gesture.OnHold(touch);
                        }
                    }
                }
            };

            //gesture.OnHoldBegin
            PTGlobalInput.OnHoldBegin += (PTTouch touch) =>
            {
                foreach (Collider collider in touch.hits.Keys)
                {
                    if (collider)
                    {
                        PTLocalInput gesture = collider.GetComponent<PTLocalInput>();
                        if (gesture && gesture.OnHoldBegin != null)
                        {
                            gesture.OnHoldBegin(touch);
                        }
                    }
                }
            };

            //gesture.OnShortHold
            PTGlobalInput.OnShortHold += (PTTouch touch) =>
            {
                foreach (Collider collider in touch.hits.Keys)
                {
                    if (collider)
                    {
                        PTLocalInput gesture = collider.GetComponent<PTLocalInput>();
                        if (gesture && gesture.OnShortHold != null)
                        {
                            gesture.OnShortHold(touch);
                        }
                    }
                }
            };

            //gesture.OnShortHoldBegin
            PTGlobalInput.OnShortHoldBegin += (PTTouch touch) =>
            {
                foreach (Collider collider in touch.hits.Keys)
                {
                    if (collider)
                    {
                        PTLocalInput gesture = collider.GetComponent<PTLocalInput>();
                        if (gesture && gesture.OnShortHoldBegin != null)
                        {
                            gesture.OnShortHoldBegin(touch);
                        }
                    }
                }
            };

            //gesture.OnLongHold
            PTGlobalInput.OnLongHold += (PTTouch touch) =>
            {
                foreach (Collider collider in touch.hits.Keys)
                {
                    if (collider)
                    {
                        PTLocalInput gesture = collider.GetComponent<PTLocalInput>();
                        if (gesture && gesture.OnLongHold != null)
                        {
                            gesture.OnLongHold(touch);
                        }
                    }
                }
            };

            //gesture.OnLongHoldBegin
            PTGlobalInput.OnLongHoldBegin += (PTTouch touch) =>
            {
                foreach (Collider collider in touch.hits.Keys)
                {
                    if (collider)
                    {
                        PTLocalInput localInput = collider.GetComponent<PTLocalInput>();
                        if (localInput && localInput.OnLongHoldBegin != null)
                        {
                            localInput.OnLongHoldBegin(touch);
                        }
                    }
                }
            };

            //OnDrag, OnDragMove
            PTGlobalInput.OnDrag += (PTTouch touch) =>
            {
                //gesture.OnDrag
                HashSet<Transform> transformsOnDrag = new HashSet<Transform>();
                foreach (Collider collider in touch.hitsBegin.Keys)
                {
                    if (collider)
                    {
                        transformsOnDrag.Add(collider.transform);
                    }
                }
                foreach (PTTouchFollower follower in touch.followers)
                {
                    transformsOnDrag.Add(follower.transform);
                }
                foreach (Transform trans in transformsOnDrag)
                {
                    if (trans)
                    {
                        PTLocalInput localInput = trans.GetComponent<PTLocalInput>();
                        if (localInput && localInput.OnDrag != null)
                        {
                            localInput.OnDrag(touch);
                        }
                    }

                }

                //gesture.OnDragMove
                foreach (PTTouchFollower follower in touch.followers)
                {
                    if (follower != null && follower.collider)
                    {
                        PTLocalInput localInput = follower.collider.GetComponent<PTLocalInput>();
                        if (localInput && localInput.OnFollow != null)
                        {
                            localInput.OnFollow(touch);
                        }
                    }
                }
            };

            //OnDragBegin, OnDragMoveBegin
            PTGlobalInput.OnDragBegin += (PTTouch touch) =>
            {
                //Add Draggables
                List<Collider> collidersToDrag = new List<Collider>();
                foreach (Collider collider in touch.hitsBegin.Keys)
                {
                    collidersToDrag.Add(collider);
                }

                collidersToDrag.Add(touch.hitBegan);

                foreach (Collider collider in collidersToDrag)
                {
                    if (collider == null)
                    {
                        continue;
                    }
                    else
                    {
                        PTLocalInput localInput = collider.GetComponent<PTLocalInput>();
                        bool canAddToDraggables = localInput && localInput.dragEnabled;

                        if (canAddToDraggables)
                        {
                            touch.AddFollower(collider, localInput.dragWorldPositionOffset);
                        }

                        if (Camera.main.orthographic)
                        {
                            //resize object
                        }
                    }

                }

                //gesture.OnDragBegin
                foreach (Collider hitBeginCollider in touch.hitsBegin.Keys)
                {
                    if (hitBeginCollider)
                    {
                        PTLocalInput localInput = hitBeginCollider.GetComponent<PTLocalInput>();
                        if (localInput && localInput.OnDragBegin != null)
                        {
                            localInput.OnDragBegin(touch);
                        }
                    }
                }

                //gesture.OnDragMoveBegin
                foreach (PTTouchFollower draggable in touch.followers)
                {
                    if (draggable != null && draggable.collider)
                    {
                        PTLocalInput localInput = draggable.collider.GetComponent<PTLocalInput>();
                        if (localInput && localInput.OnDragMoveBegin != null)
                        {
                            localInput.OnDragMoveBegin(touch);
                        }
                    }
                }
            };

            //gesture.OnClicked
            PTGlobalInput.OnClicked += (PTTouch touch) =>
            {
                foreach (Collider collider in touch.hits.Keys)
                {
                    if (collider)
                    {
                        PTLocalInput gesture = collider.GetComponent<PTLocalInput>();
                        if (gesture && gesture.OnTouched != null)
                        {
                            gesture.OnTouched(touch);
                        }
                    }
                }
            };

            //gesture.OnFlicked
            PTGlobalInput.OnFlicked += (PTTouch touch) =>
            {
                foreach (Collider collider in touch.hitsBegin.Keys)
                {
                    if (collider)
                    {
                        PTLocalInput gesture = collider.GetComponent<PTLocalInput>();
                        if (gesture && gesture.OnFlicked != null)
                        {
                            gesture.OnFlicked(touch);
                        }
                    }
                }
            };

            //gesture.OnDoubleClicked
            PTGlobalInput.OnDoubleClicked += (PTTouch touch) =>
            {
                foreach (Collider collider in touch.hits.Keys)
                {
                    if (collider)
                    {
                        PTLocalInput gesture = collider.GetComponent<PTLocalInput>();
                        if (gesture && gesture.OnDoubleClicked != null)
                        {
                            gesture.OnDoubleClicked(touch);
                        }
                    }
                }
            };

            //gesture.OnTripleClicked
            PTGlobalInput.OnTripleClicked += (PTTouch touch) =>
            {
                foreach (Collider collider in touch.hits.Keys)
                {
                    if (collider)
                    {
                        PTLocalInput gesture = collider.GetComponent<PTLocalInput>();
                        if (gesture && gesture.OnTripleClicked != null)
                        {
                            gesture.OnTripleClicked(touch);
                        }
                    }
                }
            };

            //gesture.OnDropped
            PTGlobalInput.OnReleased += (PTTouch touch, PTTouchFollower draggable) =>
            {
                if (draggable != null && draggable.collider)
                {
                    if (draggable.ptLocalInput && draggable.ptLocalInput.OnReleased != null)
                    {
                        draggable.ptLocalInput.OnReleased(draggable);
                    }
                }
            };

            //gesture.OnExclusiveClicked
            PTGlobalInput.OnExclusiveClicked += (PTTouch touch, int count) =>
            {
                foreach (Collider collider in touch.hits.Keys)
                {
                    if (collider)
                    {
                        PTLocalInput gesture = collider.GetComponent<PTLocalInput>();
                        if (gesture && gesture.OnExclusiveClicked != null)
                        {
                            gesture.OnExclusiveClicked(touch, count);
                        }
                    }
                }
            };

            //gesture.OnExclusiveClicked
            PTGlobalInput.OnMultiTouch += (Collider collider, PTTouch[] touches) =>
            {
                if (collider)
                {
                    PTLocalInput gesture = collider.GetComponent<PTLocalInput>();
                    if (gesture && gesture.OnMultiTouch != null)
                    {
                        gesture.OnMultiTouch(touches);
                    }
                }
            };
        }
        private void HandleRelationshipCurrentLastTouch()
        {
            //Set last touch
            foreach (PTTouch touch in touches)
            {
                bool isFastEnoughForMoving = true;

                //The distance between last position and current position is less than THRESHOLD_TIME_HOLD_TOLERANCE_SQR
                if ((touch.lastPosition - touch.position).sqrMagnitude < DISTANCE_MOVE_SQR)
                {
                    isFastEnoughForMoving = false;
                    if (touch.phase == PTTouchPhase.Moving)
                    {
                        touch.phase = PTTouchPhase.StationaryEnter;
                        //touch.timeStationaryBegan = DateTime.Now;
                        if (PTGlobalInput.OnHoldBegin != null)
                        {
                            PTGlobalInput.OnHoldBegin(touch);
                        }
                    }
                    if (PTGlobalInput.OnHold != null)
                    {
                        PTGlobalInput.OnHold(touch);
                    }
                }

                //The last position and current position are different, drag
                if (touch.lastPosition != touch.position)
                {
                    //The touch is moving fast enough
                    if (isFastEnoughForMoving)
                    {
                        //Begans
                        if (touch.phase != PTTouchPhase.Moving)
                        {
                            if (PTGlobalInput.OnTouchMoveBegin != null)
                            {
                                PTGlobalInput.OnTouchMoveBegin(touch);
                            }
                            if (!touch.isDragging)
                            {
                                touch.isDragging = true;
                                if (PTGlobalInput.OnDragBegin != null)
                                {
                                    PTGlobalInput.OnDragBegin(touch);
                                }
                            }
                        }

                        //Change phase
                        if (touch.phase != PTTouchPhase.Moving)
                        {
                            touch.timeStationaryBegan = DateTime.Now;
                        }
                        touch.phase = PTTouchPhase.Moving;
                    }

                    //OnMoving
                    if (PTGlobalInput.OnTouchMove != null)
                    {
                        PTGlobalInput.OnTouchMove(touch);
                    }

                    //OnDrag
                    if (touch.isDragging)
                    {
                        if (PTGlobalInput.OnDrag != null)
                        {
                            PTGlobalInput.OnDrag(touch);
                        }
                    }
                }
                touch.lastPosition = touch.position;
            }
        }
        /// <summary>
        /// Handling the draggables' world position and scale.
        /// </summary>
        private void HandleDraggables()
        {
            //Scale
            PTGlobalInput.OnDragBegin += (PTTouch touch) =>
            {
                foreach (PTTouchFollower draggable in touch.followers)
                {
                    if (draggable.ptLocalInput)
                    {
                        draggable.transform.SetWorldScale(draggable.collider.transform.GetWorldScale() + draggable.ptLocalInput.dragWorldScaleOffset, PT.DEFAULT_TIMER);
                    }

                }
            };
            //Position
            PTGlobalInput.OnDrag += (PTTouch touch) =>
            {
                foreach (PTTouchFollower draggable in touch.followers)
                {
                    draggable.Follow(touch);
                }
            };
            //Reset position and scale
            PTGlobalInput.OnReleased += (PTTouch touch, PTTouchFollower draggable) =>
            {
                draggable.StartDropAnimationBy(this);
            };
        }
        #endregion

        #region api
        //Not supposed to have api, declare api in PTGlobalInput
        #endregion
    }

    public enum PTTouchEvent { Unknown, Began, Existing, Ended };
    public enum PTTouchPhase { StationaryEnter, StationaryShort, StationaryLong, Moving };

    [Serializable]
    public class PTTouchFollower
    {
        public PTTouch touch { get; private set; }
        public Collider collider { get; private set; }
        public Vector3 enterPosition { get; private set; }
        public Vector3 enterPositionOffset { get; private set; }
        public Vector3 enterWorldScale { get; private set; }
        public bool enableDropAnimation = DEFAULT_ENABLE_DROP_ANIMATION;
        public bool isDropAnimationGoingOn { get { return dropAnimation != null; } }
        public float animationTimer = PT.DEFAULT_TIMER;
        public Coroutine dropAnimation { get; private set; }
        public Dictionary<Collider, float> hits
        {
            get
            {
                Camera mainCam = Camera.main;
                HashSet<Collider> hitColliders = PTUtility.HitsRealtime(
                    mainCam.orthographic ? collider.transform.position - mainCam.transform.forward : mainCam.transform.position,
                    collider.transform.position,
                    mainCam.farClipPlane);
                Dictionary<Collider, float> ret = new Dictionary<Collider, float>();
                foreach (Collider collider in hitColliders)
                {
                    float sqrDistance = (collider.transform.position - this.collider.transform.position).magnitude;
                    ret.Add(collider, sqrDistance);
                }
                return ret;
            }
        }
        public List<PTZone> hitZones
        {
            get
            {
                List<PTZone> ret = new List<PTZone>();
                foreach (Collider hit in hits.Keys)
                {
                    PTZone hitZone = hit.GetComponent<PTZone>();
                    if (hitZone)
                    {
                        ret.Add(hitZone);
                    }
                }
                return ret;
            }
        }
        /*public Dictionary<PTZone, float> hitAcceptedZones
        {
            get
            {
                Dictionary<PTZone, float> ret = new Dictionary<PTZone, float>();
                foreach (PTZone hitZone in hitZones.Keys)
                {
                    if (hitZone.Accepts(ptTransform))
                    {
                        try { ret.Add(hitZone, hitZones[hitZone]); } catch { }
                    }
                }
                return ret;
            }
        }*/
        public string name { get { return collider.name; } }
        public Vector3 dragWorldPositionOffset { get { return ptLocalInput ? ptLocalInput.dragWorldPositionOffset : Vector3.zero; } }
        public Vector3 dragWorldScaleOffset { get { return ptLocalInput ? ptLocalInput.dragWorldScaleOffset : Vector3.zero; } }
        public Transform transform { get { return collider ? collider.transform : null; } }
        public PTTransform ptTransform { get { return collider ? collider.GetComponent<PTTransform>() : null; } }
        public PTLocalInput ptLocalInput { get { return collider ? collider.GetComponent<PTLocalInput>() : null; } }

        private const bool DEFAULT_ENABLE_DROP_ANIMATION = false;

        internal PTTouchFollower(Collider collider, PTTouch touch)
        {
            if (PTTouch.TouchDrags(collider) == null)
            {
                if (collider != null && touch != null)
                {
                    this.touch = touch;
                    this.collider = collider;
                    enterPosition = collider.transform.position;
                    enterPositionOffset = enterPosition - touch.hitPoint;
                    enterPositionOffset = new Vector3(enterPositionOffset.x, 0, enterPositionOffset.z);
                    enterWorldScale = collider.transform.GetWorldScale();
                    enableDropAnimation = ptLocalInput ? ptLocalInput.enableDropAnimation : DEFAULT_ENABLE_DROP_ANIMATION;
                }
                else
                {
                    if (collider == null)
                    {
                        throw new InvalidCastException("[PlayTable] PTTouchFollower construction parameters: Collider must NOT be null.");
                    }
                    if (touch == null)
                    {
                        throw new InvalidCastException("[PlayTable] PTTouchFollower construction parameters: PTTouch must NOT be null.");
                    }
                }
            }
            else
            {
                throw new InvalidCastException("[PlayTable] Collider already following another touch.");
            }

        }

        public override bool Equals(object other)
        {
            PTTouchFollower follower = (PTTouchFollower)other;
            return follower.collider == collider;
        }
        public override int GetHashCode()
        {
            return base.GetHashCode() + collider.GetHashCode();
        }
        public override string ToString()
        {
            return JsonUtility.ToJson(this);
        }
        public void StartDropAnimationBy(MonoBehaviour doer)
        {
            if (!isDropAnimationGoingOn)
            {
                dropAnimation = doer.StartCoroutine(DropAnimationCoroutine());
            }
        }
        public void StopDropAnimationBy(MonoBehaviour doer)
        {
            if (dropAnimation != null)
            {
                doer.StopCoroutine(dropAnimation);
            }
        }
        private IEnumerator DropAnimationCoroutine()
        {
            if (transform && enableDropAnimation)
            {
                Vector3 currPos = transform.position;
                Vector3 targetWorldPosition = new Vector3(currPos.x, enterPosition.y, currPos.z);

                if (!isDropAnimationGoingOn)
                {
                    transform.SetWorldPosition(targetWorldPosition, animationTimer);
                    transform.SetWorldScale(enterWorldScale, animationTimer);
                    yield return new WaitForSeconds(animationTimer);
                    dropAnimation = null;
                }
            }
            if (ptLocalInput && ptLocalInput.OnDropped != null)
            {
                ptLocalInput.OnDropped(this);
            }
        }
        public void Follow(PTTouch touch)
        {
            if (transform)
            {
                if (Camera.main.orthographic)
                {
                    transform.position = dragWorldPositionOffset + enterPositionOffset
                            + new Vector3(touch.hitPoint.x, enterPosition.y, touch.hitPoint.z);
                }
                else
                {
                    Vector3 targetPoint =
                        new Vector3(dragWorldPositionOffset.x, 0, dragWorldPositionOffset.z)
                        //+ enterPositionOffset
                        + touch.ray.PointOnPlane(Vector3.up, Vector3.up * (enterPosition.y + dragWorldPositionOffset.y));

                    transform.position = targetPoint;
                }
                PTDragRestriction dragRestriction = transform.GetComponent<PTDragRestriction>();
                if (dragRestriction != null)
                {
                    Vector3 restrictedLocalPosition = transform.localPosition;
                    // restricted x
                    if (transform.localPosition.x < dragRestriction.MinimumLocalPosition.x)
                    {
                        restrictedLocalPosition.x = dragRestriction.MinimumLocalPosition.x;
                    }
                    else if (transform.localPosition.x > dragRestriction.MaximumLocalPosition.x)
                    {
                        restrictedLocalPosition.x = dragRestriction.MaximumLocalPosition.x;
                    }
                    // restricted y
                    if (transform.localPosition.y < dragRestriction.MinimumLocalPosition.y)
                    {
                        restrictedLocalPosition.y = dragRestriction.MinimumLocalPosition.y;
                    }
                    else if (transform.localPosition.y > dragRestriction.MaximumLocalPosition.y)
                    {
                        restrictedLocalPosition.y = dragRestriction.MaximumLocalPosition.y;
                    }
                    // restricted z
                    if (transform.localPosition.z < dragRestriction.MinimumLocalPosition.z)
                    {
                        restrictedLocalPosition.z = dragRestriction.MinimumLocalPosition.z;
                    }
                    else if (transform.localPosition.z > dragRestriction.MaximumLocalPosition.z)
                    {
                        restrictedLocalPosition.z = dragRestriction.MaximumLocalPosition.z;
                    }
                    // set local position
                    transform.localPosition = restrictedLocalPosition;
                }
            }
        }
    }

    [Serializable]
    public class PTTouch
    {
        public int ID { get; private set; }
        public PTTouchPhase phase { get; internal set; }
        public bool isDragging { get; internal set; }
        public DateTime timeStationaryBegan { get; internal set; }
        public TimeSpan spanStationary
        {
            get
            {
                return DateTime.Now - timeStationaryBegan;
            }
        }
        public DateTime birth { get; private set; }
        public TimeSpan life
        {
            get
            {
                return DateTime.Now - birth;
            }
        }
        public Vector2 initPosition;
        public Vector2 lastPosition;
        public Vector2 position
        {
            get
            {
                if (ID < 0)
                {
                    return (Vector2)Input.mousePosition + offset;
                }
                else
                {
                    return touchUnity.position;
                }
            }
        }
        public Touch touchUnity
        {
            get
            {
                if (Application.platform == RuntimePlatform.Android
                    || Application.platform == RuntimePlatform.IPhonePlayer)
                {
                    for (int i = 0; i < Input.touchCount; i++)
                    {
                        if (Input.touches[i].fingerId == ID)
                        {
                            return Input.touches[i];
                        }
                    }
                    return default(Touch);
                }
                else
                {
                    throw new PlatformNotSupportedException();
                }
            }
        }
        public Ray ray { get { return Camera.main.ScreenPointToRay(position); } }
        public float distanceHitPoint { get { return (hitPoint - Camera.main.transform.position).magnitude; } }
        public float distanceHit { get { return (hit.transform.position - Camera.main.transform.position).magnitude; } }
        public Collider hit
        {
            get
            {
                RaycastHit rayHit;
                Collider ret = null;
                if (Physics.Raycast(ray, out rayHit))
                {
                    ret = rayHit.collider.transform.GetComponent<Collider>();
                    if (ret && !ret.enabled)
                    {
                        ret = null;
                    }
                }
                return ret;
            }
        }
        public Collider hitBegan { get; internal set; }
        internal HashSet<Collider> hitsRealtime { get { return position.GetHitsAsScreenPosition(); } }
        public Dictionary<Collider, DateTime> hits
        {
            get
            {
                if (this != null && PTInputManager.hits.ContainsKey(this))
                {
                    //Use reference instead of real time, for optimization
                    return PTInputManager.hits[this];
                }
                else
                {
                    Dictionary<Collider, DateTime> ret = new Dictionary<Collider, DateTime>();
                    foreach (Collider collider in hitsRealtime)
                    {
                        try { ret.Add(collider, DateTime.Now); } catch { }
                    }
                }
                return new Dictionary<Collider, DateTime>();
            }
        }
        public Dictionary<Collider, DateTime> hitsBegin { get; internal set; }
        public Collider last { get; internal set; }
        public Vector3 hitPoint
        {
            get
            {
                RaycastHit hitCast;
                if (Physics.Raycast(ray, out hitCast))
                {
                    return hitCast.point;
                }
                return default(Vector3);
            }
        }
        public Vector3 hitPointBegin { get; private set; }
        public Vector2 offset = Vector2.zero;
        public readonly static int TOUCH_ID_MOUSE_LEFT = -1;
        public readonly static int TOUCH_ID_MOUSE_RIGHT = -2;
        public List<KeyValuePair<DateTime, Vector2>> path = new List<KeyValuePair<DateTime, Vector2>>();
        public float speed
        {
            get
            {
                if (path.Count < 2)
                {
                    return 0;
                }
                int endPoint = path.Count < 5 ? path.Count - 2 : path.Count - 5;
                return (path[path.Count - 1].Value - path[endPoint].Value).magnitude;
            }
        }
        public List<PTTouchFollower> followers { get; private set; }
        public bool canPenetrate = true;
        //public bool hasDropAnimation = false;

        public static PTTouch TouchDrags(Collider col)
        {
            if (col != null && PTInputManager.touches != null)
            {
                foreach (PTTouch touch in PTInputManager.touches)
                {
                    foreach (PTTouchFollower follower in touch.followers)
                    {
                        if (follower.collider == col)
                        {
                            return touch;
                        }
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Used for touch input
        /// </summary>
        /// <param name="touch">Unity touch input</param>
        public PTTouch(Touch touch)
        {
            ID = touch.fingerId;
            initPosition = touch.position;
            DefaultInit();
        }
        /// <summary>
        /// Used for mouse input
        /// </summary>
        public PTTouch(Vector2 initposition, Vector2 offset, int id)
        {
            ID = id;//Default, also for mouse input
            initPosition = initposition + offset;
            this.offset = offset;
            DefaultInit();
        }
        private void DefaultInit()
        {
            followers = new List<PTTouchFollower>();
            lastPosition = initPosition;
            birth = DateTime.Now;
            timeStationaryBegan = birth;
            phase = PTTouchPhase.StationaryEnter;
            hitPointBegin = hitPoint;
            hitBegan = null;
            hitsBegin = new Dictionary<Collider, DateTime>();
        }
        public override bool Equals(object obj)
        {
            return ID == ((PTTouch)obj).ID;
        }
        public override int GetHashCode()
        {
            unchecked // Overflow is fine, just wrap
            {
                int hash = 17;
                hash = hash * 23 + ID.GetHashCode();
                return hash;
            }
        }
        public override string ToString()
        {
            return JsonUtility.ToJson(this);
        }
        public Vector3 hitPoint_FixedY(Vector3 currPosition, Vector3 enterPosition)
        {
            float distance = (Camera.main.transform.position - currPosition).magnitude;
            Vector3 worldPos = Camera.main.transform.position + ray.direction * distance;
            worldPos = new Vector3(worldPos.x, enterPosition.y, worldPos.z);
            return worldPos;
        }
        public DateTime GetEnterTimeOn(Collider collider)
        {
            try
            {
                return hits[collider];
            }
            catch
            {
                return default(DateTime);
            }
        }
        public void AddFollower(Collider collider, Vector3 dragOffset)
        {
            try
            {
                PTTouchFollower newDraggable = new PTTouchFollower(collider, this);
                if (newDraggable != null && !followers.Contains(newDraggable))
                {
                    followers.Add(newDraggable);
                }
            }
            catch { }

        }
        public void AddFollower(PTLocalInput localInput)
        {
            AddFollower(localInput.GetComponent<Collider>(), localInput.dragWorldPositionOffset);
        }
        public void RemoveFollower(Collider collider)
        {
            PTTouchFollower draggable = FindFollowerBy(collider);
            RemoveFollower(draggable);
        }
        public void RemoveAllFollowers()
        {
            HashSet<PTTouchFollower> toMoves = new HashSet<PTTouchFollower>();
            foreach (PTTouchFollower draggable in followers)
            {
                try { toMoves.Add(draggable); } catch { }
            }
            foreach (PTTouchFollower follower in toMoves)
            {
                RemoveFollower(follower);
            }
        }
        public void RemoveFollower(PTTouchFollower draggable)
        {
            if (draggable != null)
            {
                followers.Remove(draggable);

                if (PTGlobalInput.OnReleased != null)
                {
                    PTGlobalInput.OnReleased(this, draggable);
                }
            }
        }
        public PTTouchFollower FindFollowerBy(Collider collider)
        {
            return followers.Find(x => x.collider == collider);
        }
    }

    [Serializable]
    class PTPapidClick
    {
        internal PTTouch initTouch { get; private set; }
        internal PTTouch lastTouch { get; private set; }
        internal int amount { get; private set; }
        internal float life;

        internal Vector2 initPosition;

        internal PTPapidClick(PTTouch touch)
        {
            initTouch = touch;
            initPosition = touch.position;
            amount = 1;
            life = PTInputManager.singleton.SPAN_RAPIDCLICK;
        }
        internal bool Add(PTTouch touch)
        {
            //within distance
            if ((touch.position - initPosition).sqrMagnitude < PTInputManager.singleton.DISTANCE_FLICK_SQR)
            {
                amount++;
                life = PTInputManager.singleton.SPAN_RAPIDCLICK;

                //OnDoubleClicked
                if (amount >= 2 && amount % 2 == 0)
                {
                    if (PTGlobalInput.OnDoubleClicked != null)
                    {
                        PTGlobalInput.OnDoubleClicked(touch);
                    }
                }
                //OnTripleClicked
                if (amount >= 3 && amount % 3 == 0)
                {
                    if (PTGlobalInput.OnTripleClicked != null)
                    {
                        PTGlobalInput.OnTripleClicked(touch);
                    }
                }
                return true;
            }
            else
            {
                return false;
            }
        }
        internal void InvokeExclusiveClickEvent()
        {
            //Global
            if (PTGlobalInput.OnExclusiveClicked != null)
            {
                PTGlobalInput.OnExclusiveClicked(initTouch, amount);
            }
        }
    }

    public static class PTGlobalInput
    {
        //Global
        #region delegates
        public static PTDelegateTouch OnTouch;
        public static PTDelegateTouch OnTouchBegin;
        public static PTDelegateTouch OnTouchHitBegin;
        public static PTDelegateTouch OnTouchEnd;
        public static PTDelegateTouch OnHold;
        public static PTDelegateTouch OnHoldBegin;
        public static PTDelegateTouch OnShortHold;
        public static PTDelegateTouch OnShortHoldBegin;
        public static PTDelegateTouch OnLongHold;
        public static PTDelegateTouch OnLongHoldBegin;
        public static PTDelegateTouch OnTouchMove;
        public static PTDelegateTouch OnTouchMoveBegin;
        public static PTDelegateTouch OnDrag;
        public static PTDelegateTouch OnDragBegin;
        public static PTDelegateTouch OnClicked;
        public static PTDelegateTouch OnFlicked;
        public static PTDelegateTouch OnDoubleClicked;
        public static PTDelegateTouch OnTripleClicked;
        public static PTDelegateTouchCollider OnTouchEnter;
        public static PTDelegateTouchCollider OnTouchInside;
        public static PTDelegateTouchCollider OnTouchExit;
        public static PTDelegateTouchDraggable OnReleased;
        public static PTDelegateExclusiveTouch OnExclusiveClicked;
        public static PTDelegateColliderMultiTouch OnMultiTouch;
        #endregion

        #region api
        public static List<PTTouch> FindTouchesDragging(Collider collider)
        {
            List<PTTouch> listRet = new List<PTTouch>();
            if (PTInputManager.touches != null)
            {
                foreach (PTTouch touch in PTInputManager.touches)
                {
                    PTTouchFollower draggable = touch.FindFollowerBy(collider);
                    if (draggable != null)
                    {
                        listRet.Add(touch);
                    }
                }
            }
            return listRet;
        }
        public static bool IsDragging(Collider collider)
        {
            return FindTouchesDragging(collider).Count > 0;
        }
        public static List<PTTouch> FindTouchesHitting(Collider collider)
        {
            List<PTTouch> listRet = new List<PTTouch>();
            if (PTInputManager.touches != null)
            {
                foreach (PTTouch touch in PTInputManager.touches)
                {
                    if (touch.hits.ContainsKey(collider) && !listRet.Contains(touch))
                    {
                        listRet.Add(touch);
                    }
                }
            }
            return listRet;
        }
        public static bool IsHitting(Collider collider)
        {
            return FindTouchesHitting(collider).Count > 0;
        }
        #endregion
    }
}
