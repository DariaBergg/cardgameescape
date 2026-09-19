using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewCharacter", menuName = "CardGame/Character")]
public class CharacterData : ScriptableObject
{
    public string characterName;
    [TextArea] public string description;
    public int maxHP = 50;
    public Sprite sprite;
    public List<CardData> startingDeck = new List<CardData>();
    public List<CardData> rewardCards = new List<CardData>();
}
