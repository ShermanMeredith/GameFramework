using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayTable;

[CreateAssetMenu(fileName = "StandardAvatarBlueprint", menuName = "PlayTable/StandardPlayerBlueprint", order = 1)]
public class PTSeatingBlueprint : PTPlayerBlueprint
{
    public string avatarName;
    public Sprite avatarFull, avatarPortrait, portraitBackground, shadow;
    public Vector2 portraitOffset, shadowOffset, shadowSize;

    public PTSeatingManager.PlayerType playerType;
    // public AICharacterData aiCharacter;

    public override PTPlayer ApplyTo(PTPlayer player)
    {
        //...
        player.GetComponent<PTSeatingIcon>().Init(this);
        return player;
    }
}