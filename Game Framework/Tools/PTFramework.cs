using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using System.Text;
using System;
using System.Reflection;
using UnityEngine.UI;

namespace PlayTable
{
    #region PlayTable Delegates
    public delegate void PTDelegateGraphNode(List<PTGraphNode> node);
    public delegate void PTDelegateGraphEdge(List<PTGraphEdge> edge);
    public delegate void PTDelegateGraphEdgeTile(List<PTGraphEdgeTile> tile);
    public delegate void PTDelegateTouch(PTTouch touch);
    public delegate void PTDelegateExclusiveTouch(PTTouch touch, int count);
    public delegate void PTDelegateColliderMultiTouch(Collider collider, params PTTouch[] touches);
    public delegate void PTDelegateMultiTouch(params PTTouch[] touches);
    public delegate void PTDelegateTouchCollider(PTTouch touch, Collider collider);
    public delegate void PTDelegateTouchDraggable(PTTouch touch, PTTouchFollower draggable);
    public delegate void PTDelegateTransform(Transform obj);
    public delegate void PTDelegateListZone(List<PTZone_new> zonesHit, List<PTZone_new> zonesAccepted);
    public delegate void PTDelegateZone(PTZone_new zone);
    public delegate void PTDelegateFollower(PTTouchFollower follower);
    public delegate void PTDelegateTransformFromTransform(Transform trans, Transform from);
    #endregion

    public static class PTFramework
    {
        public const string DIR_RESOURCE = "PlayTable/";
        public const string DIR_TTMANAGER = DIR_RESOURCE + "TabletopManager";

        public const string version = "1.1.4";
        public static Dictionary<string, string> versions
        {
            get
            {
                return new Dictionary<string, string>()
                {
                    { "1.2.0", "09/18/2019 PTZone split into PTLayoutZone (Arrange, dimension spacings)， " +
                    "PTContainerZone (Single object containers, swappable), " +
                    "PTDropZone (Accepts multiple object types and delegates to subzones).  " +
                    "PTZones all have AcceptableObjects list. PTZone.OnDropped delegate is called when a " +
                    "PTGamePiece with same tag as an object in AcceptableObjects is dropped on top of the PTZone. " +
                    "Arrange no longer requires the previous arrange to have finished. Arranging does not trigger OnAdded. " +
                    "PTLocalInput split into PTGamePiece (draggable). OnHover, OnHoverEnter, OnHoverExit delegates added to both " +
                    "PTGamePiece and PTZone. They get called when an acceptable PTGamePiece is being dragged across a PTZone. " +
                    "OnRemoved delegate added to PTZone, triggered when a PTGamePiece in content enters a new zone. " +
                    "PTGamePieces should never be without a PTZone. Added PTDragRestriction. Added WiggleCoroutine to PTFramework." },
                    { "1.1.4", "08/01/2019 Fixed offset bugs under perspective camera mode." },
                    { "1.1.3", "07/30/2019 More accurate dragging under perspective camera mode. OSX touch input." },
                    { "1.1.2", "07/11/2019 Seating demo. PlayTable menu: Create ttmngr and Set up TT. Fixed touch bugs." },
                    { "1.1.1", "06/21/2019 SDK player blueprint. Tabltop seating screen." },
                    { "1.1.0", "04/04/2019 Extensions: Migrated most of PTTransform api to UnityEngine.Transform. " +
                        "Removed Accepts from PTZone. PTZone can take UnityEngine.Transform and is more focusing on managing content children" },
                    { "1.0.3", "03/26/2019 PTTouch: added hitPointBegin" },
                    { "1.0.2", "03/21/2019 ptTransform: SnapToRotation, SnapToGrid and set world and local rotation" },
                    { "1.0.1", "03/20/2019 Removed collider requirement from PTTransform" },
                    { "1.0.0", "" }
                };
            }
        }

        #region Extensions
        #region UnityEngine.Transform
        public static Vector2 GetScreenPosition(this Transform trans)
        {
            return Camera.main.WorldToScreenPoint(trans.position);
        }
        public static Vector3 GetWorldScale(this Transform trans)
        {
            Vector3 productParentLocalScale = trans.GetParentLocalScaleProduct();
            return new Vector3(
                productParentLocalScale.x * trans.localScale.x,
                productParentLocalScale.y * trans.localScale.y,
                productParentLocalScale.z * trans.localScale.z);
        }
        public static IEnumerator SetLocalPositionCoroutine(this Transform trans, Vector3 target, float timer)
        {
            Vector3 init = trans.localPosition;
            float coveredTime = 0;
            while (coveredTime < timer)
            {
                yield return new WaitForEndOfFrame();
                coveredTime += Time.deltaTime;
                float frac = coveredTime / timer;
                frac = frac < 1 ? frac : 1;
                trans.localPosition = init + (target - init) * frac;
            }
            trans.localPosition = target;
        }
        public static void SetLocalPosition(this Transform trans, Vector3 target, float timer)
        {
            trans.StartCoroutineSelf(trans.SetLocalPositionCoroutine(target, timer));
        }
        public static IEnumerator SetWorldPositionCoroutine(this Transform trans, Vector3 target, float timer)
        {
            Vector3 posInit = trans.position;
            Vector3 difference = target - posInit;
            float coveredTime = 0;
            while (coveredTime < timer && trans)
            {
                coveredTime += Time.deltaTime;
                float frac = coveredTime / timer;
                frac = frac < 1 ? frac : 1;
                trans.position = posInit + difference * frac;
                yield return new WaitForEndOfFrame();
            }
            if (trans)
            {
                trans.position = target;
            }
        }
        public static void SetWorldPosition(this Transform trans, Vector3 target, float timer)
        {
            trans.StartCoroutineSelf(trans.SetWorldPositionCoroutine(target, timer));
        }
        public static IEnumerator SetLocalScaleCoroutine(this Transform trans, Vector3 target, float timer)
        {
            Vector3 init = trans.localScale;
            Vector3 difference = target - init;
            float coveredTime = 0;
            while (coveredTime < timer && trans)
            {
                coveredTime += Time.deltaTime;
                float frac = coveredTime / timer;
                frac = frac < 1 ? frac : 1;
                trans.localScale = init + difference * frac;
                yield return new WaitForEndOfFrame();
            }
            if (trans)
            {
                trans.localScale = target;
            }
        }
        public static void SetLocalScale(this Transform trans, Vector3 target, float timer)
        {
            trans.StartCoroutineSelf(trans.SetLocalScaleCoroutine(target, timer));
        }
        public static IEnumerator SetWorldScaleCoroutine(this Transform trans, Vector3 target, float timer)
        {
            Vector3 targetLocalScale = trans.GetLocalScaleByWorldScale(target);
            yield return trans.SetLocalScaleCoroutine(targetLocalScale, timer);
        }
        public static void SetWorldScale(this Transform trans, Vector3 target, float timer)
        {
            trans.StartCoroutineSelf(trans.SetWorldScaleCoroutine(target, timer));
        }
        public static IEnumerator PulseCoroutine(this Transform trans, Vector3 maxScale, Vector3 minScale, float timer, bool loop)
        {
            while (true)
            {
                yield return trans.SetLocalScaleCoroutine(maxScale, timer * 0.5f);
                yield return trans.SetLocalScaleCoroutine(minScale, timer * 0.5f);
                if (!loop)
                {
                    break;
                }
            }
        }
        public static void Pulse(this Transform trans, Vector3 maxScale, Vector3 minScale, float timer, bool loop)
        {
            trans.StartCoroutineSelf(trans.PulseCoroutine(maxScale, minScale, timer, loop));
        }
        public static void Pulse(this Transform trans)
        {
            trans.Pulse(1.2f * Vector3.one, 0.8f * Vector3.one, PT.DEFAULT_TIMER * 5, true);
        }
        public static IEnumerator SetSizeDeltaCoroutine(this Transform trans, Vector2 target, float timer)
        {
            RectTransform rectTransform = trans.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                Vector2 init = rectTransform.sizeDelta;
                Vector2 difference = target - init;
                float coveredTime = 0;
                while (coveredTime < timer)
                {
                    yield return new WaitForEndOfFrame();
                    coveredTime += Time.deltaTime;
                    float frac = coveredTime / timer;
                    frac = frac < 1 ? frac : 1;
                    rectTransform.sizeDelta = init + difference * frac;

                }
                rectTransform.sizeDelta = target;

            }
        }
        public static void SetSizeDelta(this Transform trans, Vector3 target, float timer)
        {
            trans.StartCoroutineSelf(trans.SetSizeDeltaCoroutine(target, timer));
        }

        public static void Wiggle(this Transform trans, float intensity = 5, int times = 1, float speed = PT.DEFAULT_TIMER)
        {
            trans.StartCoroutineSelf(trans.WiggleCoroutine(intensity, times, speed));
        }
        
        public static IEnumerator WiggleCoroutine(this Transform trans, float intensity = 15, int times = 1, float speed = PT.DEFAULT_TIMER)
        {
            Vector3 rot = trans.localEulerAngles;
            Vector3 upRot = new Vector3(rot.x, rot.y, rot.z + (intensity / 2));
            Vector3 downRot = new Vector3(rot.x, rot.y, rot.z - (intensity / 2));
            for(int i = 0; i < times; ++i)
            {
                yield return trans.SetLocalRotationCoroutine(Quaternion.Euler(upRot), speed / 4);
                yield return trans.SetLocalRotationCoroutine(Quaternion.Euler(downRot), speed / 2);
                yield return trans.SetLocalRotationCoroutine(Quaternion.Euler(rot), speed / 4);
            }
        }

        public static IEnumerator SetWorldRotationCoroutine(this Transform trans, Quaternion target, float timer)
        {
            Quaternion init = trans.rotation;

            float coveredTime = 0;
            while (coveredTime < timer)
            {
                yield return new WaitForEndOfFrame();
                coveredTime += Time.deltaTime;
                float frac = coveredTime / timer;
                trans.rotation = Quaternion.Slerp(init, target, frac);

            }
            trans.rotation = target;

            PTTransform ptTransform = trans.GetComponent<PTTransform>();
            if (ptTransform)
            {
                if (ptTransform.OnRotated != null)
                {
                    ptTransform.OnRotated();
                }
            }
        }
        public static void SetWorldRotation(this Transform trans, Quaternion target, float timer)
        {
            trans.StartCoroutineSelf(trans.SetWorldRotationCoroutine(target, timer));
        }
        public static IEnumerator SetLocalRotationCoroutine(this Transform trans, Quaternion target, float timer)
        {
            Quaternion init = trans.localRotation;

            float coveredTime = 0;
            while (coveredTime < timer)
            {
                yield return new WaitForEndOfFrame();
                coveredTime += Time.deltaTime;
                float frac = coveredTime / timer;
                trans.localRotation = Quaternion.Slerp(init, target, frac);

            }
            trans.localRotation = target;

            PTTransform ptTransform = trans.GetComponent<PTTransform>();
            if (ptTransform)
            {
                if (ptTransform.OnRotated != null)
                {
                    ptTransform.OnRotated();
                }
            }
        }
        public static void SetLocalRotation(this Transform trans, Quaternion target, float timer)
        {
            trans.StartCoroutineSelf(trans.SetLocalRotationCoroutine(target, timer));
        }
        public static IEnumerator SetWorldRotationCoroutine(this Transform trans, float targetAngle, PTAxis axis, float timer)
        {
            Vector3 eulerTarget = trans.eulerAngles;
            switch (axis)
            {
                case PTAxis.x:
                    eulerTarget = new Vector3(targetAngle, trans.eulerAngles.y, trans.eulerAngles.z);
                    break;
                case PTAxis.y:
                    eulerTarget = new Vector3(trans.eulerAngles.x, targetAngle, trans.eulerAngles.z);
                    break;
                case PTAxis.z:
                    eulerTarget = new Vector3(trans.eulerAngles.x, trans.eulerAngles.y, targetAngle);
                    break;
            }
            Quaternion rotationTarget = Quaternion.Euler(eulerTarget);
            yield return trans.SetWorldRotationCoroutine(rotationTarget, timer);
        }
        public static void SetWorldRotation(this Transform trans, float targetAngle, PTAxis axis, float timer)
        {
            trans.StartCoroutineSelf(trans.SetWorldRotationCoroutine(targetAngle, axis, timer));
        }
        public static IEnumerator SetWorldRotationCoroutine(this Transform trans, Vector3 eularAngle, float timer)
        {
            Quaternion target = Quaternion.Euler(eularAngle);
            yield return trans.SetWorldRotationCoroutine(target, timer);
        }
        public static void SetWorldRotation(this Transform trans, Vector3 eularAngle, float timer)
        {
            trans.StartCoroutineSelf(trans.SetWorldRotationCoroutine(eularAngle, timer));
        }
        public static IEnumerator SetFacingCoroutine(this Transform trans, bool isFaceUp, bool keepTilt, float timer)
        {
            //Quaternion target = Quaternion.LookRotation(isFaceUp ? -Vector3.up : Vector3.up, keepTilt ? transform.up : Vector3.forward);
            Quaternion target = Quaternion.LookRotation(keepTilt ? trans.forward : Vector3.forward, isFaceUp ? Vector3.up : -Vector3.up);
            yield return trans.SetWorldRotationCoroutine(target, timer);
        }
        public static void SetFacing(this Transform trans, bool isFaceUp, bool keepTilt, float timer)
        {
            trans.StartCoroutineSelf(trans.SetFacingCoroutine(isFaceUp, keepTilt, timer));
        }
        public static IEnumerator ShakeCoroutine(this Transform trans, Vector3 angle, Vector3 movement, int times, float timer)
        {
            throw new NotImplementedException();
        }
        public static void Shake(this Transform trans, Vector3 angle, Vector3 movement, int times, float timer)
        {
            trans.StartCoroutineSelf(trans.ShakeCoroutine(angle, movement, times, timer));
        }
        public static IEnumerator LookAtCoroutine(this Transform trans, Vector3 target, float timer)
        {
            Vector3 relativePos = target - trans.position;
            Quaternion rotationTarget = Quaternion.LookRotation(relativePos, Vector3.up);
            yield return trans.SetWorldRotationCoroutine(rotationTarget, timer);
        }
        public static void LookAt(this Transform trans, Vector3 target, float timer)
        {
            trans.StartCoroutineSelf(trans.LookAtCoroutine(target, timer));
        }
        public static bool SetParent(this Transform trans, Transform parent, int targetSiblingIndex)
        {
            if (trans)
            {
                trans.SetParent(parent);
                if (parent)
                {
                    targetSiblingIndex = targetSiblingIndex > 0 ? targetSiblingIndex : 0;
                    targetSiblingIndex = targetSiblingIndex < parent.childCount ? targetSiblingIndex : parent.childCount - 1;
                    trans.SetSiblingIndex(targetSiblingIndex);
                }
                return true;
            }
            else
            {
                return false;
            }
        }
        public static Vector3 GetLocalScaleByWorldScale(this Transform trans, Vector3 worldScale)
        {
            Vector3 parentsScale = trans.GetParentLocalScaleProduct();
            return new Vector3(
                parentsScale.x != 0 ? worldScale.x / parentsScale.x : 0,
                parentsScale.y != 0 ? worldScale.y / parentsScale.y : 0,
                parentsScale.z != 0 ? worldScale.z / parentsScale.z : 0);
        }
        public static Vector3 GetParentLocalScaleProduct(this Transform trans)
        {
            if (trans)
            {
                Vector3 ret = Vector3.one;
                Transform parent = trans.parent;
                while (parent != null)
                {
                    Vector3 parentScale = parent.transform.localScale;
                    ret = new Vector3(
                        ret.x * parentScale.x, 
                        ret.y * parentScale.y, 
                        ret.z * parentScale.z);
                    parent = parent.parent;
                }
                return ret;
            }
            else
            {
                Debug.LogWarning("Input is null");
                return Vector3.zero;
            }
        }
        /// <summary>
        /// Return if the card is facing up, towards world axis Y
        /// </summary>
        public static bool IsFaceUp(this Transform trans)
        {
            return Vector3.Dot(trans.up, Vector3.up) > 0;
        }
        public static IEnumerator FlipCoroutine(this Transform trans, bool isTargetFaceUp, float timer)
        {
            yield return trans.SetFacingCoroutine(isTargetFaceUp, true, timer);
        }
        /// <summary>
        /// Default flip, flip to target facing using customized timer.
        /// </summary>
        /// <param name="timer">the timer to finish the animation</param>
        /// <param name="isTargetFaceUp">target facing is up</param>
        public static void Flip(this Transform trans, bool isTargetFaceUp, float timer)
        {
            trans.StartCoroutineSelf(trans.FlipCoroutine(isTargetFaceUp, timer));
        }
        /// <summary>
        /// Default flip, flip to the other side using default timer.
        /// </summary>
        public static void Flip(this Transform trans)
        {
            trans.Flip(!trans.IsFaceUp(), PT.DEFAULT_TIMER);
        }
        /// <summary>
        /// Snap this to a multiple of incrementInDegrees (around y axis)
        /// </summary>
        /// <param name="incrementInDegrees">The minimal degree to use for snapping</param>
        /// <param name="timer">The timer for animation</param>
        /// <returns></returns>
        public static int SnapToRotation(this Transform trans, int incrementInDegrees, float timer)
        {
            int myRotation = (int)trans.eulerAngles.y % 360;
            if (myRotation < 0) { myRotation = 360 + myRotation; }

            int remainder = myRotation % incrementInDegrees;
            int newRotation;
            if (remainder < incrementInDegrees / 2)
            {
                newRotation = (myRotation / incrementInDegrees) * incrementInDegrees;
            }
            else
            {
                newRotation = (myRotation / incrementInDegrees + 1) * incrementInDegrees;
            }
            trans.SetWorldRotation(Quaternion.Euler(trans.eulerAngles.x, newRotation, trans.eulerAngles.z), timer);

            return newRotation;
        }
        /// <summary>
        /// The method to snap this to a grid using interval and offset (around y axis)
        /// </summary>
        /// <param name="interval">The size of grid elements</param>
        /// <param name="offset">The offset of the grid</param>
        /// <param name="timer">The timer for animation</param>
        public static void SnapToGrid(this Transform trans, Vector2 interval, Vector2 offset, float timer)
        {
            float nearestX;
            float remainder = (trans.position.x + offset.x) % interval.x;
            if (remainder < 0) { remainder += interval.x; }
            if (remainder < interval.x / 2)
            {
                nearestX = trans.position.x - remainder;
            }
            else
            {
                nearestX = trans.position.x + (interval.x - remainder);
            }

            float nearestZ = trans.position.z;
            remainder = (trans.position.z + offset.y) % interval.y;
            if (remainder < 0) { remainder += interval.y; }
            if (remainder < interval.y / 2)
            {
                nearestZ = trans.position.z - remainder;
            }
            else
            {
                nearestZ = trans.position.z + (interval.y - remainder);
            }
            trans.SetWorldPosition(new Vector3(nearestX, trans.position.y, nearestZ), timer);
        }
        public static IEnumerator SetColorCoroutine(this Transform trans, Color target, float timer)
        {
            Graphic graphic = trans.GetComponent<Graphic>();
            if (graphic)
            {
                graphic.SetColor(target, timer);
            }
            Renderer renderer = trans.GetComponent<Renderer>();
            if (renderer)
            {
                renderer.SetColor(target, timer);
            }
            yield return new WaitForSeconds(timer);
        }
        public static void SetColor(this Transform trans, Color target, float timer)
        {
            trans.StartCoroutineSelf(trans.SetColorCoroutine(target, timer));
        }
        public static IEnumerator SetAlphaCoroutine(this Transform trans, float target, float timer)
        {
            Graphic graphic = trans.GetComponent<Graphic>();
            if (graphic)
            {
                graphic.SetAlpha(target, timer);
            }
            Renderer renderer = trans.GetComponent<Renderer>();
            if (renderer)
            {
                renderer.SetAlpha(target, timer);
            }
            yield return new WaitForSeconds(timer);
        }
        public static void SetAlpha(this Transform trans, float target, float timer)
        {
            trans.StartCoroutineSelf(trans.SetAlphaCoroutine(target, timer));
        }
        public static void Fade(this Transform trans, bool isVisible, float timer = PT.DEFAULT_TIMER)
        {
            trans.StartCoroutineSelf(trans.FadeCoroutine(isVisible, timer));
        }
        public static IEnumerator FadeCoroutine(this Transform trans, bool isVisible, float timer = PT.DEFAULT_TIMER)
        {
            foreach (Image image in trans.GetComponentsInChildren<Image>())
            {
                trans.StartCoroutineSelf(image.SetAlphaCoroutine(isVisible ? 1 : 0, timer));
            }
            foreach (SpriteRenderer sprite in trans.GetComponentsInChildren<SpriteRenderer>())
            {
                trans.StartCoroutineSelf(sprite.SetAlphaCoroutine(isVisible ? 1 : 0, timer));
            }
            foreach (TMPro.TMP_Text text in trans.GetComponentsInChildren<TMPro.TMP_Text>())
            {
                trans.StartCoroutineSelf(text.SetAlphaCoroutine(isVisible ? 1 : 0, timer));
            }
            yield return new WaitForSeconds(timer);
        }
        public static IEnumerator BuzzCoroutine(this Transform trans, float intensity = 0.2f, int times = 3, PTDirection buzzDirection = PTDirection.Right, float timer = PT.DEFAULT_TIMER)
        {
            Vector3 dir;
            switch (buzzDirection)
            {
                case PTDirection.Up:
                    dir = Vector3.up;
                    break;
                case PTDirection.Down:
                    dir = Vector3.down;
                    break;
                case PTDirection.Left:
                    dir = Vector3.left;
                    break;
                case PTDirection.Right:
                    dir = Vector3.right;
                    break;
                case PTDirection.RightUp:
                    dir = new Vector3(1, 1, 0);
                    break;
                case PTDirection.RightDown:
                    dir = new Vector3(1, -1, 0);
                    break;
                case PTDirection.LeftUp:
                    dir = new Vector3(-1, 1, 0);
                    break;
                case PTDirection.LeftDown:
                    dir = new Vector3(-1, -1, 0);
                    break;
                default:
                    Debug.LogError("No buzz direction specified for : " + trans.name);
                    yield break;
            }
            Vector3 startLocalPos = trans.localPosition;
            dir *= intensity;
            float buzzTime = timer / (times * 2 + 1);
            for (int i = 0; i < times; ++i)
            {
                yield return trans.SetLocalPositionCoroutine(new Vector3(startLocalPos.x + dir.x, startLocalPos.y + dir.y, startLocalPos.z + dir.z), buzzTime);
                yield return trans.SetLocalPositionCoroutine(new Vector3(startLocalPos.x - dir.x, startLocalPos.y - dir.y, startLocalPos.z - dir.z), buzzTime);
            }
            yield return trans.SetLocalPositionCoroutine(startLocalPos, buzzTime);
        }
        public static void Buzz(this Transform trans, float intensity = 0.2f, int times = 3, PTDirection buzzDirection = PTDirection.Right, float timer = PT.DEFAULT_TIMER)
        {
            trans.StartCoroutineSelf(trans.BuzzCoroutine(intensity, times, buzzDirection, timer));
        }
        public static void Boop(this GameObject obj, float startDelay)
        {
            Boop(obj, 1.5f, 1.2f, 0.2f, 0.1f, startDelay);
        }
        public static void Boop(this GameObject obj, float scaleX = 1.5f, float scaleY = 1.2f, float scaleUpTime = 0.2f, float scaleDownTime = 0.1f, float startDelay = 0)
        {
            // Debug.Log("Booping " + obj);
            iTween.ScaleTo(obj, iTween.Hash("x", obj.transform.localScale.x * scaleX, "y", obj.transform.localScale.y * scaleY, "time", scaleUpTime, "delay", startDelay));
            iTween.ScaleTo(obj, iTween.Hash("x", obj.transform.localScale.x, "y", obj.transform.localScale.y, "delay", scaleUpTime + startDelay, "time", scaleDownTime));
        }
        public static IEnumerator BoopCoroutine(this GameObject pbj, float scaleX = 1.5f, float scaleY = 1.2f, float scaleUpTime = 0.2f, float scaleDownTime = 0.1f, float startDelay = 0)
        {
            // Debug.Log("Booping " + obj);
            iTween.ScaleTo(pbj, iTween.Hash("x", pbj.transform.localScale.x * scaleX, "y", pbj.transform.localScale.y * scaleY, "time", scaleUpTime, "delay", startDelay));
            iTween.ScaleTo(pbj, iTween.Hash("x", pbj.transform.localScale.x, "y", pbj.transform.localScale.y, "delay", scaleUpTime + startDelay, "time", scaleDownTime));
            yield return new WaitForSeconds(scaleUpTime + scaleDownTime + startDelay);
        }
        public static IEnumerator BlinkCoroutine(this Transform trans, float timer, int times, float minAlpha, float maxAlpha)
        {
            while (times > 0)
            {
                yield return trans.SetAlphaCoroutine(maxAlpha, timer / 2.0f);
                yield return trans.SetAlphaCoroutine(minAlpha, timer / 2.0f);
                times--;
            }
        }
        public static void Blink(this Transform trans, float timer, int times, float minAlpha, float maxAlpha)
        {
            trans.StartCoroutineSelf(trans.BlinkCoroutine(timer, times, minAlpha, maxAlpha));
        }
        public static IEnumerator ToggleVisibilityCoroutine(this Transform trans, bool b, float timer)
        {
            yield return trans.SetAlphaCoroutine(b ? 1 : 0, timer);
        }
        public static void ToggleVisibility(this Transform trans, bool b, float timer)
        {
            //trans.gameObject.SetActive(b);
            //trans.StartCoroutineSelf(trans.ToggleVisibilityCoroutine(b, timer));
        }
        public static bool IsInZone(this Transform trans) { return trans.GetComponentInParent<PTZone_new>(); }
        public static bool IsInZone(this Transform trans, PTZone_new zone)
        {
            PTZone_new parentZone = trans.GetComponentInParent<PTZone_new>();
            return parentZone && parentZone == zone;
        }
        /// <summary>
        /// Mirror using negetive scale. Using this will trigger Unity's negative scaled collider not supported warning.
        /// </summary>
        /// <param name="trans">The transform to be mirrored</param>
        /// <param name="mirrorX"></param>
        /// <param name="mirrorY"></param>
        /// <param name="mirrorZ"></param>
        public static void Mirror(this Transform trans, bool mirrorX, bool mirrorY, bool mirrorZ)
        {
            Vector3 localScale = trans.localScale;
            trans.localScale = new Vector3(
                mirrorX ? -localScale.x : localScale.x,
                mirrorY ? -localScale.y : localScale.y,
                mirrorZ ? -localScale.z : localScale.z);
        }
        private static void AttemptMirror_HardToHandleCollider(this Transform trans, bool mirrorX, bool mirrorY, bool mirrorZ)
        {
            if (trans)
            {
                PTTransform ptTransform = trans.GetComponent<PTTransform>();

                //Mirror sprite renderer
                SpriteRenderer spriteRenderer = trans.GetComponent<SpriteRenderer>();
                if (ptTransform && !ptTransform.ignoreMirrorRotation || !ptTransform)
                {
                    AttemptMirrorSpriteRenderer_HardToHandleCollider(spriteRenderer, mirrorX, mirrorZ);
                }

                //Mirror the children local position
                foreach (Transform child in trans)
                {
                    PTTransform childPtTransform = child.GetComponent<PTTransform>();
                    if (childPtTransform && !childPtTransform.ignoreMirrorLocation || !childPtTransform)
                    {
                        AttemptMirrorLocation_HardToHandleCollider(child, mirrorX, mirrorY, mirrorZ);
                    }
                    Mirror(child, mirrorX, mirrorY, mirrorZ);
                }
            }
        }
        private static void AttemptMirrorLocation_HardToHandleCollider(this Transform trans, bool mirrorX, bool mirrorY, bool mirrorZ)
        {
            if (trans)
            {
                Transform parent = trans.parent;
                Vector3 worldPos = trans.position;
                Vector3 offsetParent = parent ? trans.position - parent.position : Vector3.zero;
                Vector3 target = new Vector3(
                    mirrorX ? worldPos.x - 2 * offsetParent.x : worldPos.x,
                    mirrorY ? worldPos.y - 2 * offsetParent.y : worldPos.y,
                    mirrorZ ? worldPos.z - 2 * offsetParent.z : worldPos.z);
                trans.position = target;
            }
        }
        private static void AttemptMirrorSpriteRenderer_HardToHandleCollider(SpriteRenderer spriteRenderer, bool mirrorX, bool mirrorY)
        {
            if (spriteRenderer)
            {
                spriteRenderer.flipX = mirrorX ? !spriteRenderer.flipX : spriteRenderer.flipX;
                spriteRenderer.flipY = mirrorY ? !spriteRenderer.flipY : spriteRenderer.flipY;
            }
        }
        
        #endregion

        #region UnityEngine.Collider
        public static bool IsBeingDragged(this Collider collider)
        {
            return PTGlobalInput_new.IsDragging(collider);
        }
        #endregion

        #region System.String
        public static bool Contains(this string source, string value, StringComparison comp)
        {
            return source == null ? false : source.IndexOf(value, comp) >= 0;
        }
        #endregion

        #region UnityEngine.UI.Graphic
        public static IEnumerator SetColorCoroutine(this Graphic graphic, Color target, float timer)
        {
            Color init = graphic.color;
            float coveredTime = 0;
            while (coveredTime < timer)
            {
                yield return new WaitForEndOfFrame();
                float frac = coveredTime / timer;
                graphic.color = Color.Lerp(init, target, frac);
                coveredTime += Time.deltaTime;
            }
            graphic.color = target;
        }
        public static void SetColor(this Graphic graphic, Color target, float timer)
        {
            graphic.StartCoroutine(graphic.SetColorCoroutine(target, timer));
        }
        public static IEnumerator SetAlphaCoroutine(this Graphic graphic, float target, float timer)
        {
            Color initColor = graphic.color;
            Color targetColor = new Color(initColor.r, initColor.g, initColor.b, target);
            yield return graphic.SetColorCoroutine(targetColor, timer);
        }
        public static void SetAlpha(this Graphic graphic, float target, float timer)
        {
            graphic.StartCoroutine(graphic.SetAlphaCoroutine(target, timer));
        }
        #endregion

        #region UnityEngine.Renderer
        public static IEnumerator SetColorCoroutine(this Renderer renderer, Color target, float timer)
        {
            if (renderer.material.HasProperty("_Color"))
            {
                Color init = renderer.material.color;
                float coveredTime = 0;
                while (coveredTime < timer)
                {
                    yield return new WaitForEndOfFrame();
                    float frac = coveredTime / timer;
                    //Debug.Log("Renderer " + renderer);
                    //Debug.Log("Renderer.transform " + renderer.transform);
                    //Debug.Log("Renderer.transform.parent " + renderer.transform.parent);
                    renderer.material.color = Color.Lerp(init, target, frac);
                    coveredTime += Time.deltaTime;
                }
                renderer.material.color = target;
            }
        }
        public static void SetColor(this Renderer renderer, Color target, float timer)
        {
            renderer.StartCoroutineSelf(renderer.SetColorCoroutine(target, timer));
        }
        public static IEnumerator SetAlphaCoroutine(this Renderer renderer, float target, float timer)
        {
            if (renderer.material.HasProperty("_Color"))
            {
                Color initColor = renderer.material.color;
                Color targetColor = new Color(initColor.r, initColor.g, initColor.b, target);
                yield return renderer.SetColorCoroutine(targetColor, timer);
            }
            else
            {
                if (target == 0)
                {
                    renderer.enabled = false;
                }
                else if (target == 1)
                {
                    renderer.enabled = true;
                }
            }
        }
        public static void SetAlpha(this Renderer renderer, float target, float timer)
        {
            renderer.StartCoroutineSelf(renderer.SetAlphaCoroutine(target, timer));
        }
        #endregion

        #region UnityEngine.SpriteRenderer
        public static IEnumerator SetColorCoroutine(this SpriteRenderer renderer, Color target, float timer)
        {
            Color init = renderer.color;
            float coveredTime = 0;
            while (coveredTime < timer)
            {
                yield return new WaitForEndOfFrame();
                float frac = coveredTime / timer;
                renderer.color = Color.Lerp(init, target, frac);
                coveredTime += Time.deltaTime;
            }
            renderer.color = target;
        }
        public static void SetColor(this SpriteRenderer renderer, Color target, float timer)
        {
            renderer.StartCoroutineSelf(renderer.SetColorCoroutine(target, timer));
        }
        public static IEnumerator SetAlphaCoroutine(this SpriteRenderer renderer, float target, float timer)
        {
            Color initColor = renderer.color;
            Color targetColor = new Color(initColor.r, initColor.g, initColor.b, target);
            yield return renderer.SetColorCoroutine(targetColor, timer);
        }
        public static void SetAlpha(this SpriteRenderer renderer, float target, float timer)
        {
            renderer.StartCoroutineSelf(renderer.SetAlphaCoroutine(target, timer));
        }
        #endregion

        #region UnityEngine.Vector2
        public static HashSet<Collider> GetHitsAsScreenPosition(this Vector2 screenPosition)
        {
            HashSet<Collider> ret = new HashSet<Collider>();
            Ray ray = Camera.main.ScreenPointToRay(screenPosition);
            RaycastHit[] rayHits;
            rayHits = Physics.RaycastAll(ray, Camera.main.farClipPlane);
            foreach (RaycastHit currhit in rayHits)
            {
                Collider collider = currhit.collider.transform.GetComponent<Collider>();
                if (collider != null)
                {
                    ret.Add(collider);
                }
            }
            return ret;
        }
        #endregion

        #region UnityEngine.Component
        public static void StartCoroutineSelf(this Component component, IEnumerator coroutine)
        {
            MonoBehaviour starter = component.GetComponent<MonoBehaviour>();
            starter = starter != null ? starter : Camera.main.GetComponent<MonoBehaviour>();
            if (starter && starter.isActiveAndEnabled)
            {
                starter.StartCoroutine(coroutine);
            }
        }

        public static T GetCopyOf<T>(this Component component, T other) where T : Component
        {
            System.Type type = component.GetType();
            if (type != other.GetType()) return null; // type mis-match
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Default | BindingFlags.DeclaredOnly;
            PropertyInfo[] propertyInfos = type.GetProperties(flags);
            foreach (var propertyInfo in propertyInfos)
            {
                if (propertyInfo.CanWrite)
                {
                    try
                    {
                        propertyInfo.SetValue(component, propertyInfo.GetValue(other, null), null);
                    }
                    catch { } // In case of NotImplementedException being thrown. For some reason specifying that exception didn't seem to catch it, so I didn't catch anything specific.
                }
            }
            FieldInfo[] fieldInfos = type.GetFields(flags);
            foreach (var fieldInfo in fieldInfos)
            {
                fieldInfo.SetValue(component, fieldInfo.GetValue(other));
            }
            return component as T;
        }

        public static T AddComponent<T>(this GameObject go, T toAdd) where T : Component
        {
            return go.AddComponent<T>().GetCopyOf(toAdd) as T;
        }
        #endregion

        #region UnityEngine.Ray
        public static Vector3 PointOnPlane(this Ray ray, Vector3 planeVector, Vector3 planePoint)
        {
            Vector3 returnResult = Vector3.zero;
            float vp1, vp2, vp3, n1, n2, n3, v1, v2, v3, m1, m2, m3, t, vpt;
            v1 = ray.direction.x;
            v2 = ray.direction.y;
            v3 = ray.direction.z;

            m1 = ray.origin.x;
            m2 = ray.origin.y;
            m3 = ray.origin.z;

            vp1 = planeVector.x;
            vp2 = planeVector.y;
            vp3 = planeVector.z;

            n1 = planePoint.x;
            n2 = planePoint.y;
            n3 = planePoint.z;

            vpt = v1 * vp1 + v2 * vp2 + v3 * vp3;

            //Not parallel
            if (vpt != 0)
            {
                t = ((n1 - m1) * vp1 + (n2 - m2) * vp2 + (n3 - m3) * vp3) / vpt;
                returnResult = new Vector3(m1 + v1 * t, m2 + v2 * t, m3 + v3 * t);
            }
            return returnResult;
        }
        #endregion

        #endregion
    }

    public static class PTUtility
    {
        #region API
        /// <summary>
        /// Toggle a game object's activity
        /// </summary>
        /// <param name="obj"></param>
        public static void ToggleActivity(GameObject obj)
        {
            obj.SetActive(!obj.activeSelf);
        }
        /// <summary>
        /// Get the length of total types in an enum
        /// </summary>
        /// <typeparam name="EnumType">The target enum type</typeparam>
        /// <returns></returns>
        public static int EnumLength<EnumType>()
        {
            if (typeof(EnumType).BaseType != typeof(Enum))
            {
                throw new InvalidCastException();
            }
            return Enum.GetNames(typeof(EnumType)).Length;
        }
        /// <summary>
        /// Get all colliders on the direction from a position to another
        /// </summary>
        /// <param name="fromWorldPosition"></param>
        /// <param name="toWorldPosition"></param>
        /// <param name="maxDistance"></param>
        /// <returns></returns>
        public static HashSet<Collider> HitsRealtime(Vector3 fromWorldPosition, Vector3 toWorldPosition, float maxDistance)
        {
            //Debug.Log("HitsRealtime" + fromWorldPosition + toWorldPosition + " " + maxDistance);
            HashSet<Collider> ret = new HashSet<Collider>();
            RaycastHit[] rayHits;
            rayHits = Physics.RaycastAll(fromWorldPosition, toWorldPosition - fromWorldPosition, maxDistance);
            //Debug.Log("raycast hit " + rayHits.Length + " objects");
            foreach (RaycastHit currhit in rayHits)
            {
                Collider collider = currhit.collider.transform.GetComponent<Collider>();
                if (collider != null)
                {
                    ret.Add(collider);
                }
            }
            return ret;
        }
        /// <summary>
        /// Generate a random name from the preset name pool
        /// </summary>
        /// <returns></returns>
        public static string RandName()
        {
            int countPresetName = Enum.GetNames(typeof(PresetName)).Length;
            return ((PresetName)UnityEngine.Random.Range(0, countPresetName)).ToString();
        }
        /// <summary>
        /// Return by probability (50% for instance)
        /// </summary>
        /// <param name="percentage">The probability of it happening. eg: 60</param>
        /// <returns></returns>
        public static bool Probability(float percentage)
        {
            //return by probablity (50% for instance)
            return UnityEngine.Random.Range(0, 101) <= percentage;
        }
        /// <summary>
        /// Generic method to swap two variables by ref
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="one">one of the parameter to swap</param>
        /// <param name="two">one of the parameter to swap</param>
        public static void Swap<T>(ref T one, ref T two)
        {
            T temp;
            temp = one;
            one = two;
            two = temp;
        }
        /// <summary>
        /// Generic method to swap two variables in a List
        /// </summary>
        /// <typeparam name="T">Type of objects in the list</typeparam>
        /// <param name="list">The list, where to swap two elements</param>
        /// <param name="indexA">One of the parameter index to swap</param>
        /// <param name="indexB">One of the parameter index to swap</param>
        public static void Swap<T>(List<T> list, int indexA, int indexB)
        {
            T tmp = list[indexA];
            list[indexA] = list[indexB];
            list[indexB] = tmp;
        }
        /// <summary>
        /// Get relative angle between two Vector2. Up=0, Right=90, Down=180, Left=270
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        public static float Angle(Vector2 origin, Vector2 position)
        {
            //angle and direction
            float rawAngle;
            Vector2 diff = new Vector2(position.x - origin.x, position.y - origin.y);
            rawAngle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;
            float ret = (90 - rawAngle) % 360f;
            ret = ret > 0 ? ret : ret + 360;
            return ret;
        }
        #endregion
    }
}

