using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayTable;
using UnityEngine.Rendering;
using TMPro;

public class PTSeatingIcon : MonoBehaviour
{
    private static int numberOfDispensers = 0;

    [SerializeField] private SpriteRenderer avatarFull;
    [SerializeField] private SpriteRenderer shadow;

    [SerializeField] private GameObject draggingPortrait;
    [SerializeField] private SpriteRenderer draggingAvatar;

    [SerializeField] private GameObject seatedPortrait;
    [SerializeField] private SpriteRenderer seatedAvatar;

    [SerializeField] private TextMeshPro avatarNameText;
    [SerializeField] private GameObject infoButton;
    [SerializeField] private Vector3 colliderSizeFull;
    [SerializeField] private Vector3 colliderSizePortrait;

    private PTLineupZone myAvatarLineUpZone;
    private PTSeatingBlueprint myBlueprint;
    private float standardShadowOffsetY = -8.9f;
    private int dispenserMaxIndex = 20;
    private CatanState_CharacterStory.Character character = CatanState_CharacterStory.Character.None;

    public PTSeatingManager.PlayerType MyPlayerType { get; private set; }
    public Sprite Avatar { get { return avatarFull.sprite; } }
    public string AvatarName { get; private set; }
    public bool IsDispensed { get; private set; }
    public bool IsSeated { get; private set; }
    public int SiblingIndex { get; private set; }
    public Vector3 Offset { get { return draggingAvatar.transform.localPosition; } }
    public bool IsAI { get; private set; }
    public AICharacterData AiCharacter { get; private set; }

    private void Start()
    {
        /*if (PTSeatingManager.IsCharacterInfoSupported)
        {
            GetComponent<PTLocalInput_new>().OnDoubleClicked += (PTTouch touch) =>
            {
                if (!IsSeated)
                {
                    ShowCharacterInfo();
                }
            };
        }*/
        GetComponent<PTLocalInput_new>().OnDragBegin += OnDragBeginHandler;
        GetComponent<PTLocalInput_new>().OnReleased += OnReleasedHandler;
    }

    private void OnDragBeginHandler(PTTouch touch)
    {
        if (IsSeated == false && IsDispensed == false && (MyPlayerType == PTSeatingManager.PlayerType.Guest || MyPlayerType == PTSeatingManager.PlayerType.Online))
        {
            GameObject newSeatingIconCopy = Instantiate(this.gameObject, transform.parent);
            newSeatingIconCopy.transform.SetWorldScale(myAvatarLineUpZone.childrenWorldScale, 0);
            newSeatingIconCopy.transform.eulerAngles = myAvatarLineUpZone.childrenWorldEularAngles;
            newSeatingIconCopy.transform.SetSiblingIndex(transform.GetSiblingIndex());
            PTSeatingIcon dispensedIcon = newSeatingIconCopy.GetComponent<PTSeatingIcon>();
            dispensedIcon.Init(myBlueprint);
            IsDispensed = true;
        }
        ShowDraggingPortrait();
        PTSeatingSeat mySeat = GetComponentInParent<PTSeatingSeat>();
        if (mySeat != null)
        {
            transform.parent = mySeat.transform;
            mySeat.ResetSeat();
        }
    }

    private void OnReleasedHandler(PTTouchFollower obj)
    {
        bool hitSeat = false;
        foreach (Collider collider in obj.touch.hits.Keys)
        {
            if (collider.GetComponent<PTSeatingSeat>() != null)
            {
                hitSeat = true;
                ShowSeatedPortrait();
                PTSeatingSeat seat = collider.GetComponent<PTSeatingSeat>();
                transform.SetWorldScale(seat.Zone.childrenWorldScale, 0);
                transform.eulerAngles = seat.Zone.childrenWorldEularAngles;
                IsSeated = true;
                break;
            }
        }
        if (hitSeat == false)
        {
            ShowFullProfile();
            if (GetComponentInParent<PTLineupZone>() == null)
            {
                SendToLineUp();
            }
            else
            {
                if (!IsDispensed)
                {
                    // NOTE: the timer doesn't affect the time it takes to add the avatar back to zone,
                    //  it is there to reduce the time the colliders on the OTHER objects are disabled for.
                    //  Setting to 0 affects add time.
                    myAvatarLineUpZone.Arrange(PT.DEFAULT_TIMER / 8);
                }
            }
            if (IsDispensed)
            {
                GetComponent<Collider>().enabled = false;
                foreach (SpriteRenderer sprite in GetComponentsInChildren<SpriteRenderer>())
                {
                    sprite.SetAlpha(0, PT.DEFAULT_TIMER);
                }
            }
        }
    }

    public void Init(PTSeatingBlueprint blueprint)
    {
        myBlueprint = blueprint;
        IsAI = blueprint.playerType == PTSeatingManager.PlayerType.AI;
        SetName(blueprint.avatarName);
        AiCharacter = blueprint.aiCharacter;
        infoButton.SetActive(AiCharacter != null);
        MyPlayerType = blueprint.playerType;
        InitSprites(blueprint);
        InitMyLineupZone();
        if (blueprint.avatarName == "Mary Anne")
        {
            character = CatanState_CharacterStory.Character.Maryanne;
        }
        else
        {
            System.Enum.TryParse(blueprint.avatarName, true, out character);
        }
    }

    private void InitSprites(PTSeatingBlueprint blueprint)
    {

        // SET SPRITES
        if (blueprint.avatarFull == null)
        {
            // no data, revert to default
            Debug.LogError("Null Avatar - Using Default!");
            return;
        }
        // full avatar
        avatarFull.sprite = blueprint.avatarFull;
        if (blueprint.avatarPortrait != null)
        {
            // custom portrait
            draggingAvatar.sprite = blueprint.avatarPortrait;
            seatedAvatar.sprite = blueprint.avatarPortrait;
        }
        else
        {
            // use full + offset as portrait
            draggingAvatar.sprite = avatarFull.sprite;
            seatedAvatar.sprite = avatarFull.sprite;
        }
        draggingAvatar.transform.localPosition = blueprint.portraitOffset;
        seatedAvatar.transform.localPosition = blueprint.portraitOffset;

        // shadow
        if (blueprint.shadow != null)
        {
            shadow.sprite = blueprint.shadow;
            shadow.transform.localScale = blueprint.shadowSize;
        }
        float shadowOffsetY = blueprint.shadowOffset.y == 0 ? standardShadowOffsetY : blueprint.shadowOffset.y;
        shadow.transform.localPosition = new Vector3(blueprint.shadowOffset.x, shadowOffsetY, 0);
    }

    private void InitMyLineupZone()
    {
        myAvatarLineUpZone = GetComponentInParent<PTLineupZone>();
        SiblingIndex = transform.GetSiblingIndex();
        if (MyPlayerType == PTSeatingManager.PlayerType.Guest || MyPlayerType == PTSeatingManager.PlayerType.Online)
        {
            SiblingIndex = int.MaxValue - numberOfDispensers;
            ++numberOfDispensers;
        }
        avatarFull.GetComponent<SortingGroup>().sortingOrder = SiblingIndex < dispenserMaxIndex ? SiblingIndex + 1 : dispenserMaxIndex - (int.MaxValue - SiblingIndex);
        OnPlacedInLineup();
    }

    private void SetName(string newName)
    {
        AvatarName = newName;
        avatarNameText.text = newName;
        if (IsAI)
        {
            //avatarNameText.text += " (AI)";
        }
        name = "seatingIcon_" + newName;
    }

    public void ShowCharacterInfo()
    {
        // Insert any special Avatar 'secondary info' feature here for press and hold interaction
        // e.g. in Catan, we use this for 'Story Mode' to share AI character backstory
        CatanStateManager.Instance.DisplayCharacterStory(character);
    }

    public void ShowFullProfile()
    {
        avatarFull.gameObject.SetActive(true);
        draggingPortrait.SetActive(false);
        seatedPortrait.SetActive(false);
        GetComponent<BoxCollider>().size = colliderSizeFull;
    }

    public void ShowDraggingPortrait()
    {
        avatarFull.gameObject.SetActive(false);
        draggingPortrait.SetActive(true);
        seatedPortrait.SetActive(false);
        GetComponent<BoxCollider>().size = colliderSizePortrait;
    }

    public void ShowSeatedPortrait()
    {
        avatarFull.gameObject.SetActive(false);
        draggingPortrait.SetActive(false);
        seatedPortrait.SetActive(true);
        GetComponent<BoxCollider>().size = colliderSizePortrait;
    }

    public void OnPlacedInLineup()
    {
        ShowFullProfile();
        if (IsDispensed)
        {
            Destroy(gameObject);
        }
        else
        {
            IsSeated = false;
        }
    }

    public void SendToLineUp()
    {
        ShowDraggingPortrait();
        transform.SetWorldScale(myAvatarLineUpZone.childrenWorldScale, 0);
        transform.eulerAngles = myAvatarLineUpZone.childrenWorldEularAngles;
        myAvatarLineUpZone.Add(transform, SiblingIndex, PT.DEFAULT_TIMER / 3);
        if (IsDispensed == false)
        {
            myAvatarLineUpZone.SetRelativePositions();
        }
    }

    public void RevealCharacter(bool isRevealed, float timer)
    {
        if (isRevealed)
        {
            avatarFull.SetColor(Color.white, timer);
            avatarNameText.transform.parent.GetComponent<SpriteRenderer>().SetAlpha(1, timer);
            avatarNameText.SetAlpha(1, timer);
            infoButton.transform.SetAlpha(1, timer);
        }
        else
        {
            avatarFull.color = Color.black;
            avatarNameText.transform.parent.GetComponent<SpriteRenderer>().SetAlpha(0, 0);
            avatarNameText.SetAlpha(0, 0);
            infoButton.transform.SetAlpha(0, 0);
        }
    }
}
