using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PlayTable
{
    /// <summary>
    /// The GameManager template to quick start
    /// </summary>
    public class PTTransformManager : MonoBehaviour
    {
        #region Unity built-in
        protected virtual void Awake()
        {

            //Drop
            /*PTGlobalInput.OnDropped += (PTTouch touch, PTDraggable draggable) =>
            {
                PTTransform ptTransform = draggable.collider.GetComponent<PTTransform>();
                if (ptTransform && ptTransform.OnDropped != null)
                {
                    List<PTZone> zonesHit = new List<PTZone>();
                    List<PTZone> zonesAccepted = new List<PTZone>();

                    foreach (Collider collider in touch.hits.Keys)
                    {
                        if (collider == draggable.collider)
                        {
                            continue;
                        }

                        PTZone hitZone = collider.GetComponent<PTZone>();
                        if (hitZone)
                        {
                            zonesHit.Add(hitZone);
                            if (hitZone.Accepts(ptTransform))
                            {
                                zonesAccepted.Add(hitZone);
                            }
                        }
                    }
                    ptTransform.OnDropped(zonesHit, zonesAccepted);
                }
            };*/
        }
        #endregion
    }
}

