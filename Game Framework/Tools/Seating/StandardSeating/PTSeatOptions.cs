using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayTable;

public class PTSeatOptions : MonoBehaviour
{
    [System.Serializable]
    private class SeatingOption
    {
        public GameObject container;
        public SpriteRenderer icon;
        public TMPro.TMP_Text text;
        private bool isBooping = false;
        public IEnumerator BoopCoroutine(float delay)
        {
            if (!isBooping)
            {
                isBooping = true;
                yield return icon.gameObject.BoopCoroutine(1.5f, 1.2f, 0.2f, 0.2f, delay);
                isBooping = false;
            }
        }
    }
    public SpriteRenderer chosenOptionIndicator;
    [SerializeField] private GameObject optionsBanner;
    [SerializeField] private SeatingOption[] optionButtons;
    private float optionsBannerOpenScale = 1;
    private bool isWaitingForChoice = false;
    private float autoChooseTime = 5;
    public int Choice { get; private set; }

    public void Flip()
    {
        foreach (SeatingOption option in optionButtons)
        {
            option.icon.transform.localScale = new Vector3(option.icon.transform.localScale.x * -1, option.icon.transform.localScale.y, option.icon.transform.localScale.z);
        }
        chosenOptionIndicator.transform.localScale = new Vector3(chosenOptionIndicator.transform.localScale.x * -1, chosenOptionIndicator.transform.localScale.y, chosenOptionIndicator.transform.localScale.z);
    }

    public IEnumerator ShowOptionsCoroutine()
    {
        isWaitingForChoice = true;
        Choice = -1;
        StartCoroutine(OpenOptionsBannerCoroutine());
        float delay = 0;
        foreach(SeatingOption option in optionButtons)
        {
            option.container.SetActive(true);
            StartCoroutine(option.BoopCoroutine(delay));
            delay += 0.1f;
        }
        Coroutine autoChoose = StartCoroutine(AutoChooseCoroutine());
        yield return new WaitUntil(() => Choice != -1 || isWaitingForChoice == false);
        if(autoChoose != null)
        {
            StopCoroutine(autoChoose);
        }
    }

    private IEnumerator AutoChooseCoroutine()
    {
        yield return new WaitForSeconds(autoChooseTime);
        if(isWaitingForChoice == true)
        {
            ChooseOption(0);
        }
    }

    public int GetOptionIndex(GameObject optionContainerToQuery)
    {
        for (int i = 0; i < optionButtons.Length; ++i)
        {
            if(optionButtons[i].container == optionContainerToQuery)
            {
                return i;
            }
        }
        return -1;
    }

    public void ChooseOption(int choice)
    {
        chosenOptionIndicator.gameObject.SetActive(true);
        if (choice >= 0 && choice < optionButtons.Length)
        {
            chosenOptionIndicator.sprite = optionButtons[choice].icon.sprite;
            Choice = choice;
        }
        foreach (SeatingOption option in optionButtons)
        {
            option.container.SetActive(false);
        }
        CloseOptionsBanner();
        isWaitingForChoice = false;
        chosenOptionIndicator.gameObject.Boop();
    }

    public void ResetChoice()
    {
        chosenOptionIndicator.sprite = null;
        chosenOptionIndicator.gameObject.SetActive(false);
        Choice = -1;
        isWaitingForChoice = false;
        CloseOptionsBanner();
    }

    private IEnumerator OpenOptionsBannerCoroutine()
    {
        iTween.ScaleTo(optionsBanner, iTween.Hash("x", optionsBannerOpenScale, "time", 0.5f));
        yield return new WaitForSeconds(0.5f);
    }

    private void CloseOptionsBanner()
    {
        iTween.ScaleTo(optionsBanner, iTween.Hash("x", 0, "time", 0.1f));
    }
}
