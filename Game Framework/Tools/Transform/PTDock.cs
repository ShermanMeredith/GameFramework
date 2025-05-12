using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PlayTable
{
    /// <summary>
    /// A template for player dock as a start
    /// </summary>
    [RequireComponent(typeof(PTPlayer))]
    public class PTDock : PTLayoutZone
    {
        #region fields
        /// <summary>
        /// The place where a bunch of objects go
        /// </summary>
        public PTZone hand;
        #endregion

        #region delegates
        #endregion

        #region Unity built-in
        #endregion

        #region api
        /// <summary>
        /// The behavior of adding object to hand
        /// </summary>
        /// <param name="hand"></param>
        protected virtual void AddToHand(PTTransform hand)
        {
            this.hand.Add(hand.transform, PT.DEFAULT_TIMER);
        }
        #endregion

    }
}