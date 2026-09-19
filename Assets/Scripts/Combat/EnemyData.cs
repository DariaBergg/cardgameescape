using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemy", menuName = "CardGame/Enemy")]
public class EnemyData : ScriptableObject
{
    public string enemyName;
    public int maxHP = 20;
    public Color color = Color.magenta;
    public Sprite sprite;
    public bool randomMoves = true;
    public List<EnemyMove> moves = new List<EnemyMove>();

    [Header("Награды")]
    [Tooltip("Карты с изюминкой этого врага (обычно с меткой элиты)")]
    public List<CardData> rewardCards = new List<CardData>();
}
