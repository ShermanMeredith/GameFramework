using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PlayTable;
using TMPro;


public class PTSeatingSeat : MonoBehaviour
{
    public enum Difficulty { None, Rookie, Veteran, Master };

    [Header("Game-Specific Settings")]
    [SerializeField] private PTZone_new playerMatPortraitZone;
    [SerializeField] private GameObject destinationPlayerMat;
    [SerializeField] private PlayerColor myColor;

    [Header("Seat Options")]
    [SerializeField] private PTSeatOptions languageOptions;
    [SerializeField] private PTSeatOptions onlinePlayerTypeOptions;
    [SerializeField] private PTSeatOptions aiDifficultyOptions;

    [Header("Seat Visual Elements")]
    [SerializeField] private Vector3 moveToLocation;
    [SerializeField] private GameObject unseatButton;
    [SerializeField] private GameObject placeholderSeatIcon;
    [SerializeField] private GameObject portrait;
    [SerializeField] private TMP_Text playerName;
    [SerializeField] private bool mirrorXAxis;

    private bool isBooping = false;

    public PTContainerZone_new Zone { get; private set; }
    public Difficulty AiDifficulty { get { return (Difficulty)aiDifficultyOptions.Choice + 1; } }
    public Sprite AiDifficultySprite { get { return aiDifficultyOptions.chosenOptionIndicator.sprite; } }
    public bool IsSeatingOptionsChosen { get; private set; }


    private void Awake()
    {
        Zone = GetComponent<PTContainerZone_new>();
        Zone.OnAdded += OnAddedHandler;
        Zone.OnSwap += OnSwapHandler;
        Zone.OnRemoved += OnRemovedHandler;

        Flip();
    }

    public void Flip()
    {
        if(transform.localPosition.y > 0)
        {
            transform.localScale = new Vector3(transform.localScale.x, -transform.localScale.y, transform.localScale.z);
            foreach (TMP_Text text in GetComponentsInChildren<TMP_Text>())
            {
                text.transform.localScale = new Vector3(text.transform.localScale.x * -1, text.transform.localScale.y, text.transform.localScale.z);
            }
            languageOptions.Flip();
            aiDifficultyOptions.Flip();
            onlinePlayerTypeOptions.Flip();
            Zone.childrenWorldScale.y *= -1;
        }
        if (mirrorXAxis)
        {
            transform.localScale = new Vector3(-transform.localScale.x, transform.localScale.y, transform.localScale.z);
            foreach (TMP_Text text in GetComponentsInChildren<TMP_Text>())
            {
                text.transform.localScale = new Vector3(text.transform.localScale.x * -1, text.transform.localScale.y, text.transform.localScale.z);
            }
            languageOptions.Flip();
            aiDifficultyOptions.Flip();
            onlinePlayerTypeOptions.Flip();
        }
        Zone.childrenWorldEularAngles = transform.eulerAngles;
        Zone.childrenWorldScale.x *= mirrorXAxis ? -1 : 1;
    }

    private void OnAddedHandler(Transform obj, Transform from)
    {
        obj.GetComponent<Collider>().enabled = true;
        PTSeatingIcon seatingIcon = obj.GetComponent<PTSeatingIcon>();
        seatingIcon.ShowSeatedPortrait();

        PTSeatingSeat fromSeat = from.GetComponentInParent<PTSeatingSeat>();
        if (fromSeat != null && fromSeat.Zone.Count == 0)
        {
            fromSeat.ResetSeat();
        }

        SeatPlayer(seatingIcon);

        // This is where HH connects to the PTPlayer
    }

    private void OnSwapHandler(Transform objectToSendAway, Transform destination)
    {
        ResetSeat();
        objectToSendAway.GetComponent<PTSeatingIcon>().ShowDraggingPortrait();
        PTSeatingSeat destinationSeat = destination.GetComponent<PTSeatingSeat>();
        if (destinationSeat == null)
        {
            objectToSendAway.GetComponent<PTSeatingIcon>().SendToLineUp();
        }
    }

    private void OnRemovedHandler(Transform obj)
    {
        PTSeatingIcon seatingIcon = obj.GetComponent<PTSeatingIcon>();
        if (seatingIcon != null && seatingIcon.IsSeated && seatingIcon.GetComponentInParent<PTLineupZone>() != null)
        {
            ResetSeat();
        }
    }

    private void SeatPlayer(PTSeatingIcon seatingIcon)
    {
        unseatButton.SetActive(true);
        placeholderSeatIcon.SetActive(false);

        playerName.text = seatingIcon.AvatarName;
        if (isBooping == false)
        {
            StartCoroutine(OnSeatedAnimationCoroutine());
        }

        StartCoroutine(ChooseOptionsCoroutine(seatingIcon));
    }

    private IEnumerator OnSeatedAnimationCoroutine()
    {
        isBooping = true;
        yield return portrait.BoopCoroutine();
        isBooping = false;
    }

    private IEnumerator ChooseOptionsCoroutine(PTSeatingIcon seatingIcon)
    {
        IsSeatingOptionsChosen = false;

        if (seatingIcon.MyPlayerType == PTSeatingManager.PlayerType.Account || seatingIcon.MyPlayerType == PTSeatingManager.PlayerType.Guest)
        {
            if (PTSeatingManager.IsLanguageSupported)
            {
                yield return languageOptions.ShowOptionsCoroutine();
            }
        }
        else if (seatingIcon.MyPlayerType == PTSeatingManager.PlayerType.Online)
        {
            if (PTSeatingManager.IsOnlinePlaySupported)
            {
                yield return onlinePlayerTypeOptions.ShowOptionsCoroutine();
            }
        }
        else if (seatingIcon.MyPlayerType == PTSeatingManager.PlayerType.AI)
        {
            yield return aiDifficultyOptions.ShowOptionsCoroutine();
        }
        IsSeatingOptionsChosen = true;
        PTSeatingManager.Instance.CheckPlayButtonStatus();
    }

    public void ResetSeat()
    {
        placeholderSeatIcon.SetActive(true);
        IsSeatingOptionsChosen = false;
        unseatButton.SetActive(false);
        playerName.text = "Drag Here";
        ResetOptions();
    }

    private void ResetOptions()
    {
        languageOptions.ResetChoice();
        aiDifficultyOptions.ResetChoice();
        onlinePlayerTypeOptions.ResetChoice();
        IsSeatingOptionsChosen = false;
    }

    public void SendIconToDestinationZone()
    {
        if (IsSeatingOptionsChosen == false)
        {
            aiDifficultyOptions.ChooseOption(0);
            IsSeatingOptionsChosen = true;
        }
        PTSeatingIcon myIcon = GetComponentInChildren<PTSeatingIcon>();
        if (myIcon != null)
        {
            myIcon.GetComponent<Collider>().enabled = false;
            playerMatPortraitZone.Add(myIcon.transform);
        }
        else
        {
            destinationPlayerMat.SetActive(false);
        }
    }

    public void SendIconBackToSeat()
    {
        PTSeatingIcon seatingIcon = destinationPlayerMat.GetComponentInChildren<PTSeatingIcon>();
        if (seatingIcon != null)
        {
            // Tell handheld that the seat has been emptied
            //myIcon.GetComponent<CatanPlayerCommunication>().SetCatanPlayer(null);
            seatingIcon.GetComponent<Collider>().enabled = true;
            Zone.Add(seatingIcon.transform);
        }
        else
        {
            destinationPlayerMat.SetActive(true);
        }
    }

    public IEnumerator SendSeatToLocationCoroutine()
    {
        yield return transform.SetLocalPositionCoroutine(moveToLocation, CatanGlobals.ANIMATION_TIMER);
    }
}
