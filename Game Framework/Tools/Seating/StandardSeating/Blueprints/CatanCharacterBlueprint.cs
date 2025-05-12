using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CharacterBlueprint", menuName = "PlayTable/CharacterBlueprint", order = 1)]
public class CatanCharacterBlueprint : ScriptableObject
{
    public Sprite avatarFull;
    public Sprite portrait;
    public string characterName;
    public string characterStory;
}
