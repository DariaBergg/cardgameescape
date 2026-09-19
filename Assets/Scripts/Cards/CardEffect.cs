using UnityEngine;

[System.Serializable]
public class CardEffect
{
    public CardEffectType type;
    public int value;
    [Min(1)] public int hits = 1;
    [Min(1)] public int turns = 1;

    public string RulesText
    {
        get
        {
            switch (type)
            {
                case CardEffectType.Damage: return hits > 1 ? $"Нанести {value} урона {hits} раза." : $"Нанести {value} урона.";
                case CardEffectType.Block: return $"Получить {value} блока.";
                case CardEffectType.Heal: return $"Восстановить {value} HP.";
                case CardEffectType.PoisonEnemy: return $"Отравить врага: {value} урона в ход, {turns} х.";
                case CardEffectType.WeakenEnemy: return $"Ослабить врага на {value} ({turns} х.).";
                default: return type.ToString();
            }
        }
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
