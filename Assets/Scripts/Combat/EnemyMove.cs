using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class EnemyMove
{
    public string moveName;
    public Sprite icon;
    public int damage;
    [Min(1)] public int hits = 1;
    public int block;
    public int poisonDamage;
    public int poisonTurns;
    public int handReduce;
    [Tooltip("Подменить N случайных карт в руке слабыми до конца боя")]
    public int corruptCards;
    [TextArea] public string description;

    [Header("Особое")]
    [Tooltip("Уйти под воду / спрятаться: спрайт меняется на скрытый до следующего хода")]
    public bool submerge;
    [Tooltip("Следующий ход будет именно этим (индекс в списке ходов), -1 = обычный порядок")]
    public int forceNextMove = -1;
    [Tooltip("Бонус к урону следующего хода")]
    public int nextDamageBonus;

    public bool HasEffect => damage > 0 || block > 0 || poisonTurns > 0 || handReduce > 0 || corruptCards > 0;

    public string Summary => SummaryWithBonus(0);

    public string SummaryWithBonus(int damageBonus)
    {
        var parts = new List<string>();
        int dmg = damage + (damage > 0 ? damageBonus : 0);
        if (damage > 0) parts.Add(hits > 1 ? $"Атака {dmg}×{hits}" : $"Атака {dmg}");
        if (block > 0) parts.Add($"Защита {block}");
        if (poisonTurns > 0) parts.Add($"Яд {poisonDamage}×{poisonTurns}");
        if (handReduce > 0) parts.Add($"−{handReduce} карта");
        if (corruptCards > 0) parts.Add(corruptCards > 1 ? $"портит {corruptCards} карты" : "портит карту");
        if (submerge) parts.Add("Прячется");
        return parts.Count > 0 ? string.Join(" + ", parts) : "Ничего";
    }

    public string TooltipText => TooltipTextWithBonus(0);

    public string TooltipTextWithBonus(int damageBonus)
    {
        string text = $"<b>{moveName}</b>\n<color=#ffb3a7>{SummaryWithBonus(damageBonus)}</color>\n\n{description}";
        if (damageBonus > 0 && damage > 0) text += $"\n\n<color=#ff8080>Усиленный удар: +{damageBonus} урона.</color>";
        return text;
    }
}
