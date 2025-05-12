using UnityEngine;
using PlayTable;
using System.Collections;

public class PTSeat : MonoBehaviour {
    public GameObject spriteDragHere;
    public PTZone zone;

    public bool isFull { get { return !spriteDragHere.gameObject.activeSelf; } }
    public PTSeatSelector playerSitting { get { return GetComponentInChildren<PTSeatSelector>(); } }

    private void Awake()
    {
        zone = GetComponent<PTZone>();
    }

    public void LetSit(Transform player)
    {
        if (player)
        {
            zone.Add(player);
            player.transform.SetPositionAndRotation(transform.position, transform.rotation);
        }
        UpdateLabel();
    }

    public void UpdateLabel()
    {
        //Debug.Log("UpdateLabel");
        spriteDragHere.SetActive(zone.Count == 0);
    }

    public void UpdateLabel(bool updateLabel)
    {
        spriteDragHere.SetActive(updateLabel);
    }

    public IEnumerator RemoveSeatedPlayerCoroutine()
    {
        yield return FindObjectOfType<PTPlayerPool>().GetComponent<PTLayoutZone>().AddCoroutine(playerSitting, 0, PT.DEFAULT_TIMER);
        yield return FindObjectOfType<PTPlayerPool>().GetComponent<PTLayoutZone>().ArrangeCoroutine(PT.DEFAULT_TIMER);
        UpdateLabel();
    }
}
