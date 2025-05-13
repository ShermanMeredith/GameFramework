using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlayTable
{
    public sealed class PTInputManager : MonoBehaviour
    {
        #region fields
        public const float TOUCH_RADIUS_THRESHOLD = 60;
        /// <summary>
        /// If set to true, all touches since the second one will be ignored
        /// </summary>
        public bool enableMultiTouch = true;
        /// <summary>
        /// Set touches' default canPenetrate value
        /// </summary>
        public bool defaultCanPenetrate = true;
        /// <summary>
        /// If set to true, all draggables after the first one will be ignored
        /// </summary>
        public bool oneDraggablePerTouch = true;
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
            GetCurrTouchesOnAndroid(inputs);

            //win
            GetCurrTouchesOnWindows(inputs);

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
        private void GetCurrTouchesOnAndroid(Dictionary<PTTouch, PTTouchEvent> inputs)
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
        private void GetCurrTouchesOnWindows(Dictionary<PTTouch, PTTouchEvent> inputs)
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
                            if (PTGlobalInput_new.OnTouchBegin != null)
                            {
                                PTGlobalInput_new.OnTouchBegin(touch);
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
                            if (PTGlobalInput_new.OnTouchHitBegin != null)
                            {
                                PTGlobalInput_new.OnTouchHitBegin(touch);
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
                if (PTGlobalInput_new.OnTouch != null)
                {
                    PTGlobalInput_new.OnTouch(touch);
                }

                //End
                if (inputs[input] == PTTouchEvent.Ended)
                {
                    try { touches.Remove(touch); } catch { }

                    //OnEnd
                    if (PTGlobalInput_new.OnTouchEnd != null)
                    {
                        PTGlobalInput_new.OnTouchEnd(touch);
                    }

                    if (touch.life.TotalSeconds < SPAN_CLICK
                            && (touch.position - touch.initPosition).sqrMagnitude < DISTANCE_FLICK_SQR)
                    {
                        //OnClicked
                        if (PTGlobalInput_new.OnClicked != null)
                        {
                            PTGlobalInput_new.OnClicked(touch);
                        }
                    }

                    if (touch.life.TotalSeconds < SPAN_FLICK
                            && (touch.position - touch.initPosition).sqrMagnitude >= DISTANCE_FLICK_SQR)
                    {
                        if (PTGlobalInput_new.OnFlicked != null)
                        {
                            PTGlobalInput_new.OnFlicked(touch);
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
                            if (PTGlobalInput_new.OnTouchInside != null)
                            {
                                PTGlobalInput_new.OnTouchInside(touch, collider);
                            }
                        }
                        else
                        {
                            try { currhitsDict.Add(collider, DateTime.Now); } catch { }

                            if (PTGlobalInput_new.OnTouchEnter != null)
                            {
                                PTGlobalInput_new.OnTouchEnter(touch, collider);
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
                    if (PTGlobalInput_new.OnTouchExit != null)
                    {
                        PTGlobalInput_new.OnTouchExit(touch, collider);
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
                if (PTGlobalInput_new.OnMultiTouch != null)
                {
                    int count = touchesBegan[collider].Keys.Count;
                    PTTouch[] currTouches = new PTTouch[count];
                    int i = 0;
                    foreach (PTTouch touch in touchesBegan[collider].Keys)
                    {
                        currTouches[i] = touch;
                        i++;
                    }
                    PTGlobalInput_new.OnMultiTouch(collider, currTouches);
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
            PTGlobalInput_new.OnTouchEnd += (PTTouch touch) =>
            {
                touch.canPenetrate = defaultCanPenetrate;
            };

            //Add exclusive clicks 
            PTGlobalInput_new.OnClicked += (PTTouch touch) =>
            {
                AddRapidClick(touch);
            };

            //Change hold phases
            PTGlobalInput_new.OnHold += (PTTouch touch) =>
            {
                //phaseHold
                switch (touch.phase)
                {
                    case PTTouchPhase.StationaryEnter:
                        //OnShortHoldBegin
                        if (touch.spanStationary.TotalSeconds > TIME_SHORTHOLD)
                        {
                            touch.phase = PTTouchPhase.StationaryShort;
                            if (PTGlobalInput_new.OnShortHoldBegin != null)
                            {
                                PTGlobalInput_new.OnShortHoldBegin(touch);
                            }
                        }
                        break;
                    case PTTouchPhase.StationaryShort:
                        //OnShortHold
                        if (PTGlobalInput_new.OnShortHold != null)
                        {
                            PTGlobalInput_new.OnShortHold(touch);
                        }
                        //OnLongHoldBegin
                        if (touch.spanStationary.TotalSeconds > TIME_LONGHOLD)
                        {
                            touch.phase = PTTouchPhase.StationaryLong;
                            if (PTGlobalInput_new.OnLongHoldBegin != null)
                            {
                                PTGlobalInput_new.OnLongHoldBegin(touch);
                            }
                        }
                        break;
                    case PTTouchPhase.StationaryLong:
                        //OnLongHold
                        if (PTGlobalInput_new.OnLongHold != null)
                        {
                            PTGlobalInput_new.OnLongHold(touch);
                        }
                        break;

                }
            };

            //OnDropped
            PTGlobalInput_new.OnTouchEnd += (PTTouch touch) =>
            {
                foreach (PTTouchFollower draggable in touch.followers)
                {
                    if (PTGlobalInput_new.OnReleased != null)
                    {
                        PTGlobalInput_new.OnReleased(touch, draggable);
                    }
                }
            };
        }
        private void RegisterLocalDelegates()
        {
            //localInput.OnTouchBegin
            PTGlobalInput_new.OnTouchHitBegin += (PTTouch touch) =>
            {
                foreach (Collider collider in touch.hitsBegin.Keys)
                {
                    if (collider)
                    {
                        PTLocalInput_new localInput = collider.GetComponent<PTLocalInput_new>();
                        if (localInput && isLocalInputInteractiveToTouch(localInput, touch))
                        {
                            if (localInput.OnTouchBegin != null)
                            {
                                localInput.OnTouchBegin(touch);
                            }

                            PTGamePiece_new gamePiece = collider.GetComponent<PTGamePiece_new>();
                            //Drag to spawn another object with PTLocalInput_new component
                            if (gamePiece && !gamePiece.IsDraggable && localInput.prefabDragToSpawn)
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

            //localInput.OnTouchEnd_EndOnThis
            PTGlobalInput_new.OnTouchEnd += (PTTouch touch) =>
            {
                foreach (Collider collider in touch.hits.Keys)
                {
                    if (collider)
                    {
                        PTLocalInput_new localInput = collider.GetComponent<PTLocalInput_new>();
                        if (localInput && localInput.OnTouchEnd_EndOnThis != null && isLocalInputInteractiveToTouch(localInput, touch))
                        {
                            localInput.OnTouchEnd_EndOnThis(touch);
                        }
                    }
                }
            };

            //localInput.OnTouchEnd_BeginOnThis
            PTGlobalInput_new.OnTouchEnd += (PTTouch touch) =>
            {
                foreach (Collider collider in touch.hitsBegin.Keys)
                {
                    if (collider)
                    {
                        PTLocalInput_new localInput = collider.GetComponent<PTLocalInput_new>();
                        if (localInput && localInput.OnTouchEnd_BeginOnThis != null && isLocalInputInteractiveToTouch(localInput, touch))
                        {
                            localInput.OnTouchEnd_BeginOnThis(touch);
                        }
                    }
                }
            };

            //localInput.OnTouchEnter
            PTGlobalInput_new.OnTouchEnter += (PTTouch touch, Collider collider) =>
            {
                if (collider)
                {
                    PTLocalInput_new localInput = collider.GetComponent<PTLocalInput_new>();
                    if (localInput && localInput.OnTouchEnter != null && isLocalInputInteractiveToTouch(localInput, touch))
                    {
                        localInput.OnTouchEnter(touch);
                    }
                }
            };

            //OnTouch, OnTouchHit
            PTGlobalInput_new.OnTouch += (PTTouch touch) =>
            {
                if (touch.hitBegan)
                {
                    PTLocalInput_new localInput_Begin = touch.hitBegan.GetComponent<PTLocalInput_new>();
                    if (localInput_Begin && isLocalInputInteractiveToTouch(localInput_Begin, touch))
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
                        PTLocalInput_new localInput = collider.GetComponent<PTLocalInput_new>();
                        if (localInput && localInput.OnTouchHit != null && isLocalInputInteractiveToTouch(localInput, touch))
                        {
                            localInput.OnTouchHit(touch);
                        }
                    }
                }
            };

            //localInput.OnTouchExit
            PTGlobalInput_new.OnTouchExit += (PTTouch touch, Collider collider) =>
            {
                if (collider)
                {
                    PTLocalInput_new localInput = collider.GetComponent<PTLocalInput_new>();
                    if (localInput && localInput.OnTouchExit != null && isLocalInputInteractiveToTouch(localInput, touch))
                    {
                        localInput.OnTouchExit(touch);
                    }
                }
            };

            //localInput.OnTouchMove
            PTGlobalInput_new.OnTouchMove += (PTTouch touch) =>
            {
                foreach (Collider collider in touch.hits.Keys)
                {
                    if (collider)
                    {
                        PTLocalInput_new localInput = collider.GetComponent<PTLocalInput_new>();
                        if (localInput && localInput.OnTouchMove != null && isLocalInputInteractiveToTouch(localInput, touch))
                        {
                            localInput.OnTouchMove(touch);
                        }
                    }
                }
            };

            //localInput.OnTouchMoveBegin
            PTGlobalInput_new.OnTouchMoveBegin += (PTTouch touch) =>
            {
                foreach (Collider collider in touch.hits.Keys)
                {
                    if (collider)
                    {
                        PTLocalInput_new localInput = collider.GetComponent<PTLocalInput_new>();
                        if (localInput && localInput.OnTouchMoveBegin != null && isLocalInputInteractiveToTouch(localInput, touch))
                        {
                            localInput.OnTouchMoveBegin(touch);
                        }
                    }
                }
            };

            //localInput.OnHold
            PTGlobalInput_new.OnHold += (PTTouch touch) =>
            {
                foreach (Collider collider in touch.hits.Keys)
                {
                    if (collider)
                    {
                        PTLocalInput_new localInput = collider.GetComponent<PTLocalInput_new>();
                        if (localInput && localInput.OnHold != null && isLocalInputInteractiveToTouch(localInput, touch))
                        {
                            localInput.OnHold(touch);
                        }
                    }
                }
            };

            //localInput.OnHoldBegin
            PTGlobalInput_new.OnHoldBegin += (PTTouch touch) =>
            {
                foreach (Collider collider in touch.hits.Keys)
                {
                    if (collider)
                    {
                        PTLocalInput_new localInput = collider.GetComponent<PTLocalInput_new>();
                        if (localInput && localInput.OnHoldBegin != null && isLocalInputInteractiveToTouch(localInput, touch))
                        {
                            localInput.OnHoldBegin(touch);
                        }
                    }
                }
            };

            //localInput.OnShortHold
            PTGlobalInput_new.OnShortHold += (PTTouch touch) =>
            {
                foreach (Collider collider in touch.hits.Keys)
                {
                    if (collider)
                    {
                        PTLocalInput_new localInput = collider.GetComponent<PTLocalInput_new>();
                        if (localInput && localInput.OnShortHold != null && isLocalInputInteractiveToTouch(localInput, touch))
                        {
                            localInput.OnShortHold(touch);
                        }
                    }
                }
            };

            //localInput.OnShortHoldBegin
            PTGlobalInput_new.OnShortHoldBegin += (PTTouch touch) =>
            {
                foreach (Collider collider in touch.hits.Keys)
                {
                    if (collider)
                    {
                        PTLocalInput_new localInput = collider.GetComponent<PTLocalInput_new>();
                        if (localInput && localInput.OnShortHoldBegin != null && isLocalInputInteractiveToTouch(localInput, touch))
                        {
                            localInput.OnShortHoldBegin(touch);
                        }
                    }
                }
            };

            //localInput.OnLongHold
            PTGlobalInput_new.OnLongHold += (PTTouch touch) =>
            {
                foreach (Collider collider in touch.hits.Keys)
                {
                    if (collider)
                    {
                        PTLocalInput_new localInput = collider.GetComponent<PTLocalInput_new>();
                        if (localInput && localInput.OnLongHold != null && isLocalInputInteractiveToTouch(localInput, touch))
                        {
                            localInput.OnLongHold(touch);
                        }
                    }
                }
            };

            //localInput.OnLongHoldBegin
            PTGlobalInput_new.OnLongHoldBegin += (PTTouch touch) =>
            {
                foreach (Collider collider in touch.hits.Keys)
                {
                    if (collider)
                    {
                        PTLocalInput_new localInput = collider.GetComponent<PTLocalInput_new>();
                        if (localInput && localInput.OnLongHoldBegin != null && isLocalInputInteractiveToTouch(localInput, touch))
                        {
                            localInput.OnLongHoldBegin(touch);
                        }
                    }
                }
            };

            //OnDrag, OnDragMove
            PTGlobalInput_new.OnDrag += (PTTouch touch) =>
            {
                //localInput.OnDrag
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
                        PTLocalInput_new localInput = trans.GetComponent<PTLocalInput_new>();
                        if (localInput && localInput.OnDrag != null && isLocalInputInteractiveToTouch(localInput, touch))
                        {
                            localInput.OnDrag(touch);
                        }
                    }

                }

                //localInput.OnDragMove
                foreach (PTTouchFollower follower in touch.followers)
                {
                    if (follower != null && follower.collider)
                    {
                        PTLocalInput_new localInput = follower.collider.GetComponent<PTLocalInput_new>();
                        if (localInput && localInput.OnFollow != null && isLocalInputInteractiveToTouch(localInput, touch))
                        {
                            localInput.OnFollow(touch);
                        }
                    }
                }
            };

            //OnDragBegin, OnDragMoveBegin
            PTGlobalInput_new.OnDragBegin += (PTTouch touch) =>
            {
                //Add Draggables
                List<Collider> collidersToDrag = new List<Collider>();
                foreach (Collider collider in touch.hits.Keys)
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
                        PTGamePiece_new gamePiece = collider.GetComponent<PTGamePiece_new>();
                        bool canAddToDraggables = gamePiece && gamePiece.IsDraggable && isLocalInputInteractiveToTouch(gamePiece, touch);

                        if (canAddToDraggables)
                        {
                            touch.AddFollower(collider, gamePiece.dragWorldPositionOffset);
                        }

                        if (Camera.main.orthographic)
                        {
                            //resize object
                        }
                    }

                }

                //localInput.OnDragBegin
                foreach (Collider hitBeginCollider in touch.hitsBegin.Keys)
                {
                    if (hitBeginCollider)
                    {
                        PTLocalInput_new localInput = hitBeginCollider.GetComponent<PTLocalInput_new>();
                        if (localInput && localInput.OnDragBegin != null && touch.FindFollowerBy(localInput.GetComponent<Collider>()) != null && isLocalInputInteractiveToTouch(localInput, touch))
                        {
                            localInput.OnDragBegin(touch);
                        }
                    }
                }

                //localInput.OnDragMoveBegin
                foreach (PTTouchFollower draggable in touch.followers)
                {
                    if (draggable != null && draggable.collider)
                    {
                        PTLocalInput_new localInput = draggable.collider.GetComponent<PTLocalInput_new>();
                        if (localInput && localInput.OnDragMoveBegin != null && isLocalInputInteractiveToTouch(localInput, touch))
                        {
                            localInput.OnDragMoveBegin(touch);
                        }
                    }
                }
            };

            //localInput.OnClicked
            PTGlobalInput_new.OnClicked += (PTTouch touch) =>
            {
                foreach (Collider collider in touch.hits.Keys)
                {
                    if (collider)
                    {
                        PTLocalInput_new localInput = collider.GetComponent<PTLocalInput_new>();
                        if (localInput && localInput.OnClicked != null && isLocalInputInteractiveToTouch(localInput, touch))
                        {
                            localInput.OnClicked(touch);
                        }
                    }
                }
            };

            //localInput.OnFlicked
            PTGlobalInput_new.OnFlicked += (PTTouch touch) =>
            {
                foreach (Collider collider in touch.hitsBegin.Keys)
                {
                    if (collider)
                    {
                        PTLocalInput_new localInput = collider.GetComponent<PTLocalInput_new>();
                        if (localInput && localInput.OnFlicked != null && isLocalInputInteractiveToTouch(localInput, touch))
                        {
                            localInput.OnFlicked(touch);
                        }
                    }
                }
            };

            //localInput.OnDoubleClicked
            PTGlobalInput_new.OnDoubleClicked += (PTTouch touch) =>
            {
                foreach (Collider collider in touch.hits.Keys)
                {
                    if (collider)
                    {
                        PTLocalInput_new localInput = collider.GetComponent<PTLocalInput_new>();
                        if (localInput && localInput.OnDoubleClicked != null && isLocalInputInteractiveToTouch(localInput, touch))
                        {
                            localInput.OnDoubleClicked(touch);
                        }
                    }
                }
            };

            //localInput.OnTripleClicked
            PTGlobalInput_new.OnTripleClicked += (PTTouch touch) =>
            {
                foreach (Collider collider in touch.hits.Keys)
                {
                    if (collider)
                    {
                        PTLocalInput_new localInput = collider.GetComponent<PTLocalInput_new>();
                        if (localInput && localInput.OnTripleClicked != null && isLocalInputInteractiveToTouch(localInput, touch))
                        {
                            localInput.OnTripleClicked(touch);
                        }
                    }
                }
            };

            //localInput.OnDropped
            PTGlobalInput_new.OnReleased += (PTTouch touch, PTTouchFollower draggable) =>
            {
                if (draggable != null && draggable.collider)
                {
                    if (draggable.ptLocalInput && draggable.ptLocalInput.OnReleased != null)
                    {
                        draggable.ptLocalInput.OnReleased(draggable);
                    }
                }
            };

            //localInput.OnExclusiveClicked
            PTGlobalInput_new.OnExclusiveClicked += (PTTouch touch, int count) =>
            {
                foreach (Collider collider in touch.hits.Keys)
                {
                    if (collider)
                    {
                        PTLocalInput_new localInput = collider.GetComponent<PTLocalInput_new>();
                        if (localInput && localInput.OnExclusiveClicked != null && isLocalInputInteractiveToTouch(localInput, touch))
                        {
                            localInput.OnExclusiveClicked(touch, count);
                        }
                    }
                }
            };

            //localInput.OnExclusiveClicked
            PTGlobalInput_new.OnMultiTouch += (Collider collider, PTTouch[] touches) =>
            {
                if (collider)
                {
                    PTLocalInput_new localInput = collider.GetComponent<PTLocalInput_new>();
                    if (localInput && localInput.OnMultiTouch != null)
                    {
                        localInput.OnMultiTouch(touches);
                    }
                }
            };
        }

        private bool isLocalInputInteractiveToTouch(PTLocalInput_new localInput, PTTouch touch)
        {
            if (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer)
            {
                if (touch.touchUnity.radius >= TOUCH_RADIUS_THRESHOLD && localInput.isLargeTouchInteractive == false)
                {
                    return false;
                }
            }
            return true;
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
                        if (PTGlobalInput_new.OnHoldBegin != null)
                        {
                            PTGlobalInput_new.OnHoldBegin(touch);
                        }
                    }
                    if (PTGlobalInput_new.OnHold != null)
                    {
                        PTGlobalInput_new.OnHold(touch);
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
                            if (PTGlobalInput_new.OnTouchMoveBegin != null)
                            {
                                PTGlobalInput_new.OnTouchMoveBegin(touch);
                            }
                            if (!touch.isDragging)
                            {
                                touch.isDragging = true;
                                if (PTGlobalInput_new.OnDragBegin != null)
                                {
                                    PTGlobalInput_new.OnDragBegin(touch);
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
                    if (PTGlobalInput_new.OnTouchMove != null)
                    {
                        PTGlobalInput_new.OnTouchMove(touch);
                    }

                    //OnDrag
                    if (touch.isDragging)
                    {
                        if (PTGlobalInput_new.OnDrag != null)
                        {
                            PTGlobalInput_new.OnDrag(touch);
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
            PTGlobalInput_new.OnDragBegin += (PTTouch touch) =>
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
            PTGlobalInput_new.OnDrag += (PTTouch touch) =>
            {
                foreach (PTTouchFollower draggable in touch.followers)
                {
                    draggable.Follow(touch);
                }
            };
            //Reset position and scale
            PTGlobalInput_new.OnReleased += (PTTouch touch, PTTouchFollower draggable) =>
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
                HashSet<Collider> hitColliders;
                if (Camera.main.orthographic)
                {
                    hitColliders = PTUtility.HitsRealtime(
                        new Vector3(transform.position.x, Camera.main.transform.position.y, transform.position.z),
                        transform.position,
                        Camera.main.farClipPlane);
                }
                else
                {
                    hitColliders = PTUtility.HitsRealtime(
                        Camera.main.transform.position,
                        transform.position,
                        Camera.main.farClipPlane);
                }
                Dictionary<Collider, float> ret = new Dictionary<Collider, float>();
                foreach (Collider collider in hitColliders)
                {
                    float sqrDistance = (collider.transform.position - this.collider.transform.position).magnitude;
                    ret.Add(collider, sqrDistance);
                }
                return ret;
            }
        }
        public List<PTZone_new> hitZones
        {
            get
            {
                List<PTZone_new> ret = new List<PTZone_new>();
                foreach (Collider hit in hits.Keys)
                {
                    PTZone_new hitZone = hit.GetComponent<PTZone_new>();
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
        public PTLocalInput_new ptLocalInput { get { return collider ? collider.GetComponent<PTLocalInput_new>() : null; } }

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
                    PTGamePiece_new myGamePiece = transform.GetComponent<PTGamePiece_new>();
                    if (myGamePiece != null)
                    {
                        if (myGamePiece.isCenteredOnDrag)
                        {
                            enterPositionOffset = Vector3.zero;
                        }
                        else
                        {
                            enterPositionOffset = enterPosition - touch.hitPoint;
                            enterPositionOffset = new Vector3(enterPositionOffset.x, 0, enterPositionOffset.z);
                        }
                    }
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

        internal PTTouchFollower(Collider collider, PTTouch touch, float yOffset = 0)
        {
            if (PTTouch.TouchDrags(collider) == null)
            {
                if (collider != null && touch != null)
                {
                    this.touch = touch;
                    this.collider = collider;
                    enterPosition = collider.transform.position;
                    PTGamePiece_new myGamePiece = transform.GetComponent<PTGamePiece_new>();
                    if (myGamePiece != null)
                    {
                        if (myGamePiece.isCenteredOnDrag)
                        {
                            enterPositionOffset = Vector3.zero;
                        }
                        else
                        {
                            enterPositionOffset = enterPosition - touch.hitPoint;
                            enterPositionOffset = new Vector3(enterPositionOffset.x, yOffset, enterPositionOffset.z);
                        }
                    }
                    else
                    {
                        enterPositionOffset = enterPosition - touch.hitPoint;
                        enterPositionOffset = new Vector3(enterPositionOffset.x, yOffset, enterPositionOffset.z);
                    }
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

                    transform.position = Vector3.Lerp(targetPoint, new Vector3(0,10,0), enterPositionOffset.y / 10);
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
                PTTouchFollower newDraggable = new PTTouchFollower(collider, this, dragOffset.y);
                if (newDraggable != null && !followers.Contains(newDraggable))
                {
                    if(PTInputManager.singleton.oneDraggablePerTouch == false || followers.Count == 0)
                    {
                        followers.Add(newDraggable);
                    }
                }
            }
            catch { }

        }
        public void AddFollower(PTLocalInput_new localInput)
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

                if (PTGlobalInput_new.OnReleased != null)
                {
                    PTGlobalInput_new.OnReleased(this, draggable);
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
                    if (PTGlobalInput_new.OnDoubleClicked != null)
                    {
                        PTGlobalInput_new.OnDoubleClicked(touch);
                    }
                }
                //OnTripleClicked
                if (amount >= 3 && amount % 3 == 0)
                {
                    if (PTGlobalInput_new.OnTripleClicked != null)
                    {
                        PTGlobalInput_new.OnTripleClicked(touch);
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
            if (PTGlobalInput_new.OnExclusiveClicked != null)
            {
                PTGlobalInput_new.OnExclusiveClicked(initTouch, amount);
            }
        }
    }

    public static class PTGlobalInput_new
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
