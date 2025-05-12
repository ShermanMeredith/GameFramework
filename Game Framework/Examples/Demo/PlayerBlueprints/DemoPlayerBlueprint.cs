using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayTable;

[CreateAssetMenu(fileName = "DemoPlayerBlueprint", menuName = "PlayTable/Demo/DemoPlayerBlueprint", order = 1)]
public class DemoPlayerBlueprint : PTPlayerBlueprint
{
    public Sprite sprite;

    public override PTPlayer ApplyTo(PTPlayer player)
    {
        //...
        if (player)
        {
            player.GetComponent<DemoPlayer>().spriteThumbnail.sprite = sprite;
        }
        return player;
    }
    

}