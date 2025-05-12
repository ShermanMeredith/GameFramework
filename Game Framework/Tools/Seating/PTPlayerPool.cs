using UnityEngine;
using PlayTable;

public class PTPlayerPool : MonoBehaviour
{
    public const int MAX_PLAYER = 5;

    private PTButtonAddPlayer buttonAddPlayer;
    private int previousNumberOfPlayersInPool;
    private bool nudgeStarted;


    private void Awake()
    {
        buttonAddPlayer = GetComponentInChildren<PTButtonAddPlayer>();
        previousNumberOfPlayersInPool = 1;
    }

    private void Update()
    {
        if (FindObjectsOfType<PTPlayer>().Length == 0 && !nudgeStarted)
        {
            nudgeStarted = true;
            buttonAddPlayer.GetComponent<View.Nudge>().StartCoroutine(buttonAddPlayer.GetComponent<View.Nudge>().StartNudgeCoroutine());
        } else if (FindObjectsOfType<PTPlayer>().Length > 0 && nudgeStarted)
        {
            buttonAddPlayer.GetComponent<View.Nudge>().StopNudge();
            nudgeStarted = false;
        }

        if (FindObjectsOfType<PTPlayer>().Length < MAX_PLAYER && !buttonAddPlayer.gameObject.activeSelf)
        {
            buttonAddPlayer.gameObject.SetActive(true);
            GetComponent<PTLayoutZone>().Arrange();

        } else if (GetComponentsInChildren<PTPlayer>().Length != previousNumberOfPlayersInPool)
        {
            GetComponent<PTLayoutZone>().Arrange();
            previousNumberOfPlayersInPool = GetComponentsInChildren<PTPlayer>().Length;
        }

        foreach (PTPlayer player in FindObjectsOfType<PTPlayer>())
        {
            if (!player.GetComponent<Collider>().enabled)
            {
                player.GetComponent<Collider>().enabled = true;
            }
        }
    }
}