using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayTable;

public class PTButtonAddPlayer : MonoBehaviour {
    public Transform spawn;
    public PTPlayer playerPrefab;

    public int Count { get; private set; }

    private PTLocalInput localInput;


    private void Awake()
    {
        localInput = GetComponent<PTLocalInput>();
        Count = 0;

        localInput.OnTouched += (PTTouch touch) =>
        {
            if (Menu.IS_OPEN)
            {
                return;
            }

            PTPlayer prefPlayer = playerPrefab;

            if (prefPlayer != null)
            {
                //Instantiate new player thumnail
                PTPlayer newPlayer = Instantiate(prefPlayer.gameObject).GetComponent<PTPlayer>();
                newPlayer.transform.position = transform.position;
                newPlayer.transform.SetParent(spawn);

                if (prefPlayer.blueprints.Length > 0)
                {
                    prefPlayer.blueprints[Count++ % prefPlayer.blueprints.Length].ApplyTo(newPlayer);
                }

                //Add button
                transform.SetAsLastSibling();
                gameObject.SetActive(FindObjectsOfType<PTPlayer>().Length < PTPlayerPool.MAX_PLAYER);
                spawn.GetComponent<PTLayoutZone>().Arrange();
            }
        };
    }
}
