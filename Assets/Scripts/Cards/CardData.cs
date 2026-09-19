using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "NewCard", menuName = "CardGame/Card")]
public class CardData : ScriptableObject
{
    public string cardName;
    [TextArea] public string description;
    public Sprite artwork;
    public List<CardEffect> effects = new List<CardEffect>();

    [Header("Метка элиты")]
    public EnemyData eliteMark;
    [Range(0, 100)] public int eliteChanceBonus;

    public bool IsMarked => eliteMark != null;

    public string EffectsSummary => string.Join(", ", effects.Select(e => e.Summary));

    public string MarkSummary => IsMarked ? $"Метка: {eliteMark.enemyName} (+{eliteChanceBonus}% к шансу встречи)" : "";

    public string TooltipText
    {
        get
        {
            string text = $"<b>{cardName}</b>\n<color=#ffd27f>{EffectsSummary}</color>";
            if (!string.IsNullOrEmpty(description)) text += $"\n\n{description}";
            if (IsMarked) text += $"\n\n<color=#d9a6ff>{MarkSummary}</color>\nКаждая такая карта в колоде повышает шанс, что за фиолетовой дверью окажется {eliteMark.enemyName}.";
            return text;
        }
    }
}
