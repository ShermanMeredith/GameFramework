using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayTable;

public class PTButtonAddPlayer : MonoBehaviour {
    public PTLayoutZone_new spawn;
    public PTPlayer playerPrefab;

    public int Count { get; private set; }

    private PTLocalInput_new localInput;
    private GameObject addPlayerTextObject;


    private void Awake()
    {
        addPlayerTextObject = GetComponentInChildren<TMPro.TMP_Text>().gameObject;
        localInput = GetComponent<PTLocalInput_new>();
        Count = 0;

        localInput.OnTouchBegin += (PTTouch touch) =>
        {
            PTPlayer prefPlayer = playerPrefab;

            if (prefPlayer != null)
            {
                //Instantiate new player thumnail
                PTPlayer newPlayer = Instantiate(prefPlayer.gameObject).GetComponent<PTPlayer>();
                newPlayer.transform.position = transform.position;
                spawn.Add(newPlayer);

                if (prefPlayer.blueprints.Length > 0)
                {
                    prefPlayer.blueprints[Count++ % prefPlayer.blueprints.Length].ApplyTo(newPlayer);
                }

                //Add button
                //transform.SetSiblingIndex(0);
                addPlayerTextObject.SetActive(FindObjectsOfType<PTPlayer>().Length < PTPlayerPool.MAX_PLAYER);
            }
        };
    }

    public void ReenableText()
    {
        addPlayerTextObject.SetActive(true);
    }
}
