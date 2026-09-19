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
    [TextArea] public string description;

    public string Summary
    {
        get
        {
            var parts = new List<string>();
            if (damage > 0) parts.Add(hits > 1 ? $"Атака {damage}×{hits}" : $"Атака {damage}");
            if (block > 0) parts.Add($"Блок {block}");
            if (poisonTurns > 0) parts.Add($"Яд {poisonDamage}×{poisonTurns}");
            if (handReduce > 0) parts.Add($"−{handReduce} карта");
            return parts.Count > 0 ? string.Join(" + ", parts) : "Ничего";
        }
    }

    public string TooltipText => $"<b>{moveName}</b>\n<color=#ffb3a7>{Summary}</color>\n\n{description}";
}
