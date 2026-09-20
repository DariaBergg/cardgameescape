using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemy", menuName = "CardGame/Enemy")]
public class EnemyData : ScriptableObject
{
    public string enemyName;
    [Tooltip("Чей враг: пусто — встречается всем, иначе только на пути этого героя")]
    public CharacterData owner;
    public int maxHP = 20;
    public Color color = Color.magenta;
    public Sprite sprite;
    [Tooltip("Спрайт в спрятанном состоянии (под водой и т.п.)")]
    public Sprite hiddenSprite;
    [Tooltip("Фон боя именно с этим врагом (если пусто — обычный боевой фон)")]
    public Sprite arena;
    [Tooltip("Своя позиция героя на этой арене")]
    public bool overridePlayerPosition;
    public Vector3 playerPosition = new Vector3(-5.5f, -1.6f, 0);
    [Tooltip("Своя позиция врага на этой арене")]
    public bool overrideEnemyPosition;
    public Vector3 enemyPosition = new Vector3(3.5f, 0.6f, 0);
    [Tooltip("За фиолетовой дверью приходит с малышом — уменьшенной копией")]
    public bool hasMinion;
    [Tooltip("Парит в воздухе: покачивается во время боя")]
    public bool flying;
    public bool randomMoves = true;
    public List<EnemyMove> moves = new List<EnemyMove>();

    [Header("Награды")]
    [Tooltip("Карты с изюминкой этого врага (обычно с меткой элиты)")]
    public List<CardData> rewardCards = new List<CardData>();

    public bool AvailableTo(CharacterData character) => owner == null || owner == character;
    public bool AvailableNow => GameManager.Instance == null || AvailableTo(GameManager.Instance.selectedCharacter);
}
