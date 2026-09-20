using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewCharacter", menuName = "CardGame/Character")]
public class CharacterData : ScriptableObject
{
    public string characterName;
    [TextArea] public string description;
    public int maxHP = 50;
    public Sprite sprite;
    [Tooltip("Портрет для экрана выбора героя")]
    public Sprite portrait;
    [Tooltip("Свой шаблон карт (если пусто — общий)")]
    public CardVisuals cardVisuals;
    public List<CardData> startingDeck = new List<CardData>();
    public List<CardData> rewardCards = new List<CardData>();
    [Tooltip("Карта-бонус после последнего обучающего боя (у дракона — Ярость)")]
    public CardData tutorialBonusCard;
    [Tooltip("Свои обучающие бои (если пусто — общие из RoomManager)")]
    public List<EnemyData> tutorialSequence = new List<EnemyData>();
    [Tooltip("Вместо усиления карты после обучающих боёв герой получает эти карты по порядку (1-й бой, 2-й бой…). Пусто — обычное усиление")]
    public List<CardData> tutorialCardRewards = new List<CardData>();

    [Header("Замах (лев)")]
    [Tooltip("Герой копит Замах; при полной шкале бьёт молнией сам")]
    public bool usesMomentum;
    public int momentumMax = 3;
    public int passiveStrikeDamage = 6;
}
