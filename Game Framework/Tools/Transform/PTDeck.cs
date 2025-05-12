using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlayTable
{
    public class PTDeck : PTLayoutZone
    {
        [SerializeField]
        private bool firstCardFaceUp, autoRefill, playerInteractive, topCardDraggable;
        public bool IsTopCardDraggable { get { return topCardDraggable; } private set { topCardDraggable = value; } }
        // coming soon: holdToSplit, firstCardFaceUp, shuffleAnimation
        
        [SerializeField]
        private float dealSpeed = 0.2f;

        [SerializeField]
        private PTLayoutZone discardZone;

        #region properties
        private Transform topCard { get { return Get(0); } }
        #endregion

        private void Start()
        {
            base.Awake();
            {
                OnRemoved += (obj) =>
                {
                    if (playerInteractive)
                    {
                        if (Count > 0)
                        {
                            SetContentsInteractive(false);
                            Get(0).GetComponent<Collider>().enabled = topCardDraggable;
                            topCard.GetComponent<PTLocalInput>().dragEnabled = topCardDraggable;
                        }
                    }
                };
                FlipTogether(false, true, PT.DEFAULT_TIMER);
                StartCoroutine(ArrangeCoroutine(PT.DEFAULT_TIMER));
            }
        }

        public void SetTopCardDraggable(bool draggable)
        {
            if(Count > 0)
            {
                topCardDraggable = draggable;
                SetContentsInteractive(false);
                Get(0).GetComponent<Collider>().enabled = topCardDraggable;
                topCard.GetComponent<PTLocalInput>().dragEnabled = topCardDraggable;
            }
        }

        /// <summary>
        /// Change the children sequence in transform order, but don't change the actual position in the scene
        /// </summary>
        public virtual void Shuffle()
        {
            int count = Count;
            if (count < 2)
            {
                return;
            }
            for (int i = 0; i < count - 1; i++)
            {
                Get(UnityEngine.Random.Range(i + 1, count - 1)).transform.SetSiblingIndex(i);
            }
        }

        public virtual IEnumerator DealToAllPlayers(int amount)
        {
            for(int i = 0; i < amount; ++i)
            {
                foreach (PTPlayer player in PTTableTop.players)
                {
                    if (content.childCount == 0 && autoRefill)
                    {
                        yield return RefillDeck();
                    }
                    PTZone currZone = player.GetComponentInChildren<PTZone>();
                    yield return DealOneCard(currZone, false);
                }
            }
        }
        public virtual IEnumerator DealOneCard(PTZone zone, bool faceup)
        {
            if(content.childCount > 0)
            {
                topCard.ToggleVisibility(true, PT.DEFAULT_TIMER);
                yield return zone.AddCoroutine(topCard, dealSpeed);
            }
            Arrange(PT.DEFAULT_TIMER);
        }

        protected virtual IEnumerator RefillDeck()
        {
            if (discardZone != null)
            {
                foreach (Transform discardedCard in discardZone.Objects)
                {
                    yield return AddCoroutine(discardedCard);
                }
                Shuffle();
            }
            else
            {
                Debug.LogError("discardZone is not set");
            }
        }
    }
}