using UnityEngine;

[System.Serializable]
public class CardEffect
{
    public CardEffectType type;
    public int value;
    [Min(1)] public int hits = 1;
    [Min(1)] public int turns = 1;

    public string RulesText => RulesTextComparedTo(null);

    public string RulesTextComparedTo(CardEffect previous)
    {
        string v = Mark(value, previous?.value);
        string t = Mark(turns, previous?.turns);
        switch (type)
        {
            case CardEffectType.Damage: return hits > 1 ? $"Нанести {v} урона {hits} раза." : $"Нанести {v} урона.";
            case CardEffectType.Block: return $"Получить {v} блока.";
            case CardEffectType.Heal: return $"Восстановить {v} HP.";
            case CardEffectType.PoisonEnemy: return $"Отравить врага: {v} урона в ход, {t} х.";
            case CardEffectType.WeakenEnemy: return $"Ослабить врага на {v} ({t} х.).";
            default: return type.ToString();
        }
    }

    static string Mark(int current, int? previous)
    {
        return previous.HasValue && previous.Value != current ? $"<color=#2e8b3a><b>{current}</b></color>" : current.ToString();
    }

    public string Summary
    {
        get
        {
            switch (type)
            {
                case CardEffectType.Damage: return hits > 1 ? $"{value} урона ×{hits}" : $"{value} урона";
                case CardEffectType.Block: return $"{value} блока";
                case CardEffectType.Heal: return $"+{value} HP";
                case CardEffectType.PoisonEnemy: return $"яд врагу {value}×{turns}";
                case CardEffectType.WeakenEnemy: return $"враг ослаблен −{value} ({turns} х.)";
                default: return type.ToString();
            }
        }
    }
}
