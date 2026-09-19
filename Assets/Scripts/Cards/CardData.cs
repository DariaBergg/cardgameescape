using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "NewCard", menuName = "CardGame/Card")]
public class CardData : ScriptableObject
{
    public string cardName;
    [TextArea] public string description;
    [Tooltip("Квадратная иллюстрация: вставляется в окно шаблона")]
    public Sprite illustration;
    [Tooltip("Готовая картинка всей карты (старый вариант, используется если нет иллюстрации)")]
    public Sprite artwork;
    public List<CardEffect> effects = new List<CardEffect>();

    [Header("Метка элиты")]
    public EnemyData eliteMark;
    [Range(0, 100)] public int eliteChanceBonus;

    [System.NonSerialized] public bool upgraded;

    public bool IsMarked => eliteMark != null;

    public CardData CreateUpgradedCopy()
    {
        var copy = Instantiate(this);
        copy.name = name + "+";
        copy.cardName = cardName + "+";
        copy.upgraded = true;
        copy.effects = new List<CardEffect>();
        foreach (var e in effects)
        {
            var u = new CardEffect { type = e.type, value = e.value, hits = e.hits, turns = e.turns };
            switch (e.type)
            {
                case CardEffectType.Damage: u.value += 2; break;
                case CardEffectType.Block: u.value += 2; break;
                case CardEffectType.Heal: u.value += 3; break;
                case CardEffectType.PoisonEnemy: u.value += 1; break;
                case CardEffectType.WeakenEnemy: u.turns += 1; break;
            }
            copy.effects.Add(u);
        }
        return copy;
    }

    public string EffectsSummary => string.Join(", ", effects.Select(e => e.Summary));

    public string RulesText => string.Join("\n", effects.Select(e => e.RulesText));

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
