using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayTable;
using System;

namespace PlayTable
{
    public abstract class PTRuleManager_TurnBased : MonoBehaviour
    {
        #region fields
        public static PTRuleManager_TurnBased singleton = null;
        public static List<PTPlayer> orderOfPlayers = new List<PTPlayer>();
        public static HashSet<PTPlayer> playedThisRound { get; private set; }
        public bool alwaysStartFromFirstPlayer = true;
        public static int playerIndex { get; private set; }
        #endregion

        #region delegates
        public static PTDelegateVoid OnGameStart;
        public static PTDelegateVoid OnGameEnd;
        public static PTDelegateVoid OnRoundStart;
        public static PTDelegateVoid OnRoundEnd;
        public static PTDelegatePlayer OnTurnStart;
        public static PTDelegatePlayer OnTurnEnd;
        #endregion

        #region unity built-in
        private void OnDestroy()
        {
            if (singleton == this)
            {
                singleton = null;
            }
        }
        protected virtual void Awake()
        {
            if (singleton == null)
            {
                singleton = this;
                playedThisRound = new HashSet<PTPlayer>();
                StartCoroutine(Rule());
            }
            else if (singleton != this)
            {
                Destroy(this);
            }
        }
        public static void Reset()
        {
            orderOfPlayers.Clear();
            playedThisRound.Clear();
        }
        #endregion

        #region flow
        private IEnumerator Rule()
        {
            yield return orderOfPlayers.Count > PTTableTop.minPlayer;
            yield return Game();
            StartCoroutine(Rule());
        }
        private IEnumerator Game()
        {
            //Get ready to start
            yield return new WaitUntil(() => GameCanStart());

            //Game start
            if (OnGameStart != null) OnGameStart();
            yield return GameStart();

            //Rounds
            bool hasStarted = false;
            while (!hasStarted || !GameCanEnd())
            {
                hasStarted = true;
                yield return Round();
            }

            //Game end
            if (OnGameEnd != null) OnGameEnd();
            yield return GameEnd();
        }
        private IEnumerator Round()
        {
            //Get ready to start
            yield return new WaitUntil(() => RoundCanStart());

            //Round start
            playedThisRound.Clear();
            if (OnRoundStart != null) OnRoundStart();
            yield return RoundStart();

            //Turns
            bool hasStarted = false;
            playerIndex = alwaysStartFromFirstPlayer ? 0 : playerIndex;
            while (!hasStarted || !RoundCanEnd())
            {
                hasStarted = true;
                yield return Turn(orderOfPlayers[playerIndex]);
                playerIndex = (playerIndex + 1) % orderOfPlayers.Count;
                print("Player Index = " + playerIndex);
            }

            //Round end
            if (OnRoundEnd != null) OnRoundEnd();
            yield return RoundEnd();
        }
        private IEnumerator Turn(PTPlayer player)
        {
            //Get ready to start
            yield return new WaitUntil(() => TurnCanStart(player));

            //Turn start
            try { playedThisRound.Add((PTPlayer)player); } catch { }
            if (OnTurnStart != null) OnTurnStart(player);
            yield return TurnStart(player);

            //Until turn end
            yield return new WaitUntil(() => TurnCanEnd(player));

            //Turn end
            if (OnTurnEnd != null) OnTurnEnd(player);
            yield return TurnEnd(player);
        }
        #endregion

        #region condition and events
        protected abstract bool GameCanStart();
        protected abstract bool RoundCanStart();
        protected abstract bool TurnCanStart(PTPlayer player);
        protected abstract bool GameCanEnd();
        protected abstract bool RoundCanEnd();
        protected abstract bool TurnCanEnd(PTPlayer player);
        protected abstract IEnumerator GameStart();
        protected abstract IEnumerator GameEnd();
        protected abstract IEnumerator RoundStart();
        protected abstract IEnumerator RoundEnd();
        protected abstract IEnumerator TurnStart(PTPlayer player);
        protected abstract IEnumerator TurnEnd(PTPlayer player);
        #endregion

        #region api
        public static void StartRule()
        {
            Reset();
            singleton.StartCoroutine(singleton.Rule());
        }
        public static void RestartRule()
        {
            StopRule();
            StartRule();
        }
        public static void StopRule()
        {
            singleton.StopAllCoroutines();
            orderOfPlayers.Clear();
        }
        public static int OrderOf(PTPlayer player)
        {
            return orderOfPlayers.FindIndex(element => element.Equals(player));
        }
        #endregion
    }

}
