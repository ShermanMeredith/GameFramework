using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PlayTable;
using System;

namespace PlayTable
{
    public class PTTimer : MonoBehaviour
    {
        public int timer = 60;
        public Image image;

        public int secondsLeft { get; private set; }
        public bool isOnGoing { get; private set; }
        public List<Sprite> sprites;

        public PTDelegateVoid OnStart;
        public PTDelegateVoid OnTimeUp;
        public PTDelegateVoid OnStopped;
        public PTDelegateInt OnSecondsLeftUpdated;

        private Coroutine coroutineMain;
        private Coroutine coroutineImage;

        public void StartTimer()
        {
            StopTimer();
            coroutineMain = StartCoroutine(MainCoroutine());
        }

        public void StopTimer()
        {
            if (coroutineMain != null)
            {
                StopCoroutine(coroutineMain);
            }
            if (coroutineImage != null)
            {
                StopCoroutine(coroutineImage);
            }

            isOnGoing = false;
            if (OnStopped != null)
            {
                OnStopped();
            }
        }

        private IEnumerator MainCoroutine()
        {
            if (!isOnGoing)
            {
                if (OnStart != null)
                {
                    OnStart();
                }
                isOnGoing = true;

                coroutineImage = StartCoroutine(ImageCoroutine());

                secondsLeft = timer;
                while (secondsLeft >= 0)
                {
                    if (OnSecondsLeftUpdated != null)
                    {
                        OnSecondsLeftUpdated(secondsLeft);
                    }
                    --secondsLeft;

                    yield return new WaitForSeconds(1);
                }
                coroutineMain = null;

                isOnGoing = false;
                if (OnTimeUp != null)
                {
                    OnTimeUp();
                }
            }
        }

        private IEnumerator ImageCoroutine()
        {
            float deltaTime = (float)timer / (float)sprites.Count;
            int index = 0;
            while (index < sprites.Count)
            {
                if (image)
                {
                    image.sprite = sprites[index++];
                }
                yield return new WaitForSeconds(deltaTime);
            }
        }
    }

}