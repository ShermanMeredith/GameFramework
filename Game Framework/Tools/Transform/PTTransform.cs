using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PlayTable
{
    public class PTTransform : MonoBehaviour
    {
        #region fields
        /// <summary>
        /// Usually the initials. Can be used for situations like radial menu buttons.
        /// </summary>
        public string nameShort;
        /// <summary>
        /// The extra spacing needed after arranging this object in the zone. 
        /// All later objects' spacing index will be changed accordingly.
        /// Eg. If the index of this object is 2 and countExtraSpacing is set to 1, 
        /// objects[3] will be arranged at the position of objects[4]. 
        /// </summary>
        public int countExtraSpacing = 0;
        public bool ignoreMirrorRotation = false;
        public bool ignoreMirrorLocation = false;
        /// <summary>
        /// Used to keep world scale, only uses positivity. 0 means keeping original scale.
        /// </summary>
        public Vector3 worldScalePositivity = Vector3.zero;
        /// <summary>
        /// The radial menu GameObject
        /// </summary>
        public PTUI_RadialMenu radialMenu;
        #endregion

        #region Property
        /// <summary>
        /// Get the SHORT type name of this.
        /// </summary>
        /// <returns>The SHORT type name, without namespace</returns>
        public string TypeName
        {
            get
            {
                return GetType().Name;
            }
        }
        /// <summary>
        /// Get the FULL type name of this.
        /// </summary>
        /// <returns>The FULL type name, with namespace</returns>
        public string TypeNameFull
        {
            get
            {
                return GetType().FullName;
            }
        }
        public PTLocalInput localInput { get { return GetComponent<PTLocalInput>(); } }
        #endregion

        #region Delegates
        public PTDelegateVoid OnRotated { get; internal set; }
        public PTDelegateZone OnAcceptedBy { get; internal set; }
        public PTDelegateZone OnAddedTo { get; internal set; }
        public PTDelegateZone OnSiblingChanged { get; internal set; }
        #endregion

        #region Unity built-in
        private void OnEnable()
        {
            StartCoroutine(UpdateWorldScalePositivityCoroutine());
        }
        private void OnTransformParentChanged()
        {
            PTZone parentZone = GetComponentInParent<PTZone>();
            if (parentZone)
            {
                foreach (Transform child in parentZone.content)
                {
                    PTTransform ptTrans = child.GetComponent<PTTransform>();
                    if (ptTrans && ptTrans.OnSiblingChanged != null)
                    {
                        ptTrans.OnSiblingChanged(parentZone);
                    }
                }
            }
        }
        #endregion

        #region helpers
        private void HandleRadialMenuInput()
        {
            if (localInput)
            {
                localInput.OnShortHoldBegin += (PTTouch touch) =>
                {
                    if (radialMenu && touch.FindFollowerBy(localInput.GetComponent<Collider>()) == null)
                    {
                        radialMenu.transform.eulerAngles = Vector3.zero;
                        radialMenu.Appear();
                    }
                };

                localInput.OnTouchEnd_BeginOnThis += (PTTouch touch) =>
                {
                    if (radialMenu && !radialMenu.isOnButtonOK)
                    {
                        radialMenu.Reset();
                    }
                };
            }

            localInput.OnTouch += (PTTouch touch) =>
            {
                if (radialMenu)
                {
                    radialMenu.SelecetSection(touch);
                }
            };
        }
        private IEnumerator UpdateWorldScalePositivityCoroutine()
        {
            while (true)
            {
                Vector3 productParents = transform.GetParentLocalScaleProduct();

                Vector3 productLocalParent = new Vector3(
                    productParents.x * transform.localScale.x,
                    productParents.y * transform.localScale.y,
                    productParents.z * transform.localScale.z);

                productLocalParent = new Vector3(
                    productLocalParent.x / Mathf.Abs(productLocalParent.x),
                    productLocalParent.y / Mathf.Abs(productLocalParent.y),
                    productLocalParent.z / Mathf.Abs(productLocalParent.z));

                Vector3 normalizedPos = new Vector3(
                    worldScalePositivity.x != 0 ? (int)(worldScalePositivity.x / Mathf.Abs(worldScalePositivity.x)) : 0,
                    worldScalePositivity.y != 0 ? (int)(worldScalePositivity.y / Mathf.Abs(worldScalePositivity.y)) : 0,
                    worldScalePositivity.z != 0 ? (int)(worldScalePositivity.z / Mathf.Abs(worldScalePositivity.z)) : 0);

                Vector3 targetLocalScale = new Vector3(
                    normalizedPos.x * productLocalParent.x * transform.localScale.x,
                    normalizedPos.y * productLocalParent.y * transform.localScale.y,
                    normalizedPos.z * productLocalParent.z * transform.localScale.z);

                targetLocalScale = new Vector3(
                    worldScalePositivity.x == 0 ? transform.localScale.x : targetLocalScale.x,
                    worldScalePositivity.y == 0 ? transform.localScale.y : targetLocalScale.y,
                    worldScalePositivity.z == 0 ? transform.localScale.z : targetLocalScale.z);

                transform.localScale = targetLocalScale;

                yield return new WaitForSeconds(PT.DEFAULT_TIMER);
            }
        }
        #endregion
    }
}

