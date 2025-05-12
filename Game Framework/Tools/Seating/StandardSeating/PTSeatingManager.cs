using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayTable;
using TMPro;
using UnityEngine.UI;


public class PTSeatingManager : MonoBehaviour
{
    public enum PlayerColor { None }; // Fill this with game-specific player colors
    public enum PlayerType { AI, Account, Guest, Online }
    // singleton
    public static PTSeatingManager Instance { get; private set; }

    [Header("Game Settings")]
    [SerializeField] private bool isOnlinePlaySupported;
    [SerializeField] private bool isCharacterInfoSupported;
    [SerializeField] private bool isLanguageSupported;
    [SerializeField] private int minPlayers;
    [SerializeField] private int maxPlayers;
    public static bool IsOnlinePlaySupported { get { return Instance.isOnlinePlaySupported; } }
    public static bool IsCharacterInfoSupported { get { return Instance.isCharacterInfoSupported; } }
    public static bool IsLanguageSupported { get { return Instance.isLanguageSupported; } }
    public static int MinPlayers { get { return Instance.minPlayers; } }
    public static int MaxPlayers { get { return Instance.maxPlayers; } }

    [Header("UI Elements")]
    public GameObject playButton;
    public GameObject optionsButton;
    public GameObject nextLineupButton;
    public GameObject prevLineupButton;
    [SerializeField] private GameObject backgroundContainer;
    [SerializeField] private TMP_Text versionText;

    [Header("Avatar Lineup Zones")]
    public GameObject[] AvatarLineUps;
    private float spaceBetweenLineups = 19.3f;

    [Header("Seating Zones")]
    [SerializeField] private List<PTSeatingSeat> seats = new List<PTSeatingSeat>();

    [Header("Seating Icons")]
    [SerializeField] private PTSeatingIcon seatingIconPrefab;
    [SerializeField] private PTSeatingBlueprint[] avatarsToLoadOnStart;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this);
            return;
        }
        // set static getters
        if (maxPlayers == 0 || minPlayers == 0)
        {
            Debug.LogError("ERROR! Min/Max player counts not set");
        }
        else
        {
            if (PTTabletopManager.singleton != null)
            {
                PTTabletopManager.singleton.minPlayer = minPlayers;
                PTTabletopManager.singleton.maxPlayer = maxPlayers;
            }
            else
            {
                Debug.LogWarning("Warning! No PTTableTop manager in scene. Handheld will not be able to connect.");
            }
        }
        versionText.text = "v" + Application.version;
        SetupDefaultAvatars();
        StartCoroutine(FetchLocalAccounts());
    }

    private void Start()
    {
        foreach (PTSeatingSeat seat in seats)
        {
            seat.Zone.OnAdded += (obj, from) =>
            {
                CheckPlayButtonStatus();
            };
            seat.Zone.OnRemoved += (obj) =>
            {
                CheckPlayButtonStatus();
            };
        }
    }

    void SetupDefaultAvatars()
    {
        bool hasAI = false;
        foreach (PTSeatingBlueprint blueprint in avatarsToLoadOnStart)
        {
            MakeAvatar(blueprint);
            if (hasAI == false && blueprint.playerType == PlayerType.AI)
            {
                hasAI = true;
            }
        }
        if (hasAI)
        {
            AvatarLineUps[0].SetActive(true);
            //nextLineupButton.SetActive(true);
        }
        else
        {
            backgroundContainer.transform.Translate(Vector3.left * spaceBetweenLineups);
        }
    }

    IEnumerator FetchLocalAccounts()
    {
        yield return null;
        //////////// USER ACCOUNT-DRIVEN AVATAR CREATION //////////////////////////////////////////////////////////////////////////////////////
        // Here we would start a coroutine to request any player accounts linked to the local device
        /* 
        int[] accountIDs = PTService.GetLocalAccountIDs();

        foreach (uint id in accountIDs) 
        {
            yield return server lookup
            if(result.success = false)
            {
                debug.logerror("ya done fucked up");
            }
            else
            {
                StandardAvatarBlueprint blueprint = new StandardAvatarBlueprint();
                //////deserialize the result here
                SeatingIcon seatingIcon = MakeAvatar(blueprint.playerType == PlayerType.AI);
                seatingIcon.Init(blueprint);
            }
        }
        */
    }

    public void AddPlayerByHH(PTSeatingBlueprint blueprint)
    {
        // Do we have a player account for this user joining by HH?
        // Get their account, and make an avatar
        // MakeAvatar(SeatingIcon.PLAYER_TYPE.HUMAN_AT_ACCOUNT, "<username>");

        // No?  Okay, let's make a unique Guest icon for them
        MakeAvatar(blueprint);
    }

    public PTSeatingIcon MakeAvatar(PTSeatingBlueprint blueprint)
    {
        PTLineupZone newAvatarZone = (blueprint.playerType == PlayerType.AI) ? AvatarLineUps[0].GetComponent<PTLineupZone>() : AvatarLineUps[1].GetComponent<PTLineupZone>();
        PTSeatingIcon newAvatar = Instantiate(seatingIconPrefab.gameObject, newAvatarZone.content).GetComponent<PTSeatingIcon>();

        newAvatar.Init(blueprint);
        return newAvatar;
    }

    public void CheckPlayButtonStatus()
    {
        int numSeatedPlayers = 0;

        foreach (PTSeatingSeat seat in seats)
        {
            if (seat.Zone.content.childCount > 0)
            {
                if (seat.IsSeatingOptionsChosen)
                {
                    numSeatedPlayers++;
                }
            }
        }
        if (playButton.activeInHierarchy)
        {
            if (numSeatedPlayers >= minPlayers)
            {
                playButton.transform.Fade(true);
            }
            else
            {
                playButton.transform.Fade(false);
            }
        }

        playButton.GetComponent<Collider>().enabled = numSeatedPlayers >= minPlayers;
    }

    public void PressedPlay()
    {
        if (GameCanStart())
        {
            foreach (PTSeatingSeat seat in seats)
            {
                seat.SendIconToDestinationZone();
            }
        }
    }

    public void ScrollCharacterLineups(bool next)
    {
        float xOffset;
        if (next)
        {
            xOffset = -spaceBetweenLineups;
        }
        else
        {
            xOffset = spaceBetweenLineups;
        }
        iTween.MoveTo(backgroundContainer, iTween.Hash("x", backgroundContainer.transform.localPosition.x + xOffset, "time", 0.5f));
        iTween.MoveTo(AvatarLineUps[0], iTween.Hash("x", AvatarLineUps[0].transform.localPosition.x + xOffset, "time", 0.5f));
        iTween.MoveTo(AvatarLineUps[1], iTween.Hash("x", AvatarLineUps[1].transform.localPosition.x + xOffset, "time", 0.5f));
    }

    public IEnumerator BringOutSeatsCoroutine()
    {
        foreach(PTSeatingSeat seat in seats)
        {
            yield return seat.SendSeatToLocationCoroutine();
        }
    }

    private bool GameCanStart()
    {
        foreach (PTTouch touch in PTInputManager.touches)
        {
            foreach (PTTouchFollower follower in touch.followers)
            {
                if (follower.transform.tag == "PlayerSeatingIcon")
                {
                    return false;
                }
            }
        }

        int seatedPlayers = 0;
        foreach (PTSeatingSeat seat in seats)
        {
            if (seat.IsSeatingOptionsChosen)
            {
                seatedPlayers++;
            }
        }

        return (seatedPlayers >= PTTableTop.minPlayer);
    }
}
