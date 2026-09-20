using UnityEngine;

[System.Serializable]
public class CardEffect
{
    public CardEffectType type;
    public int value;
    [Min(1)] public int hits = 1;
    [Min(1)] public int turns = 1;
    public CardCondition condition = CardCondition.None;

    public string RulesText => RulesTextComparedTo(null);

    public string RulesTextComparedTo(CardEffect previous)
    {
        string v = Mark(value, previous?.value);
        string t = Mark(turns, previous?.turns);
        string body;
        switch (type)
        {
            case CardEffectType.Damage: body = hits > 1 ? L.F("Нанести {0} урона {1} раза.", v, hits) : L.F("Нанести {0} урона.", v); break;
            case CardEffectType.Block: body = L.F("Получить {0} блока.", v); break;
            case CardEffectType.Heal: body = L.F("Восстановить {0} HP.", v); break;
            case CardEffectType.PoisonEnemy: body = L.F("Отравить врага: {0} урона в ход, {1} х.", v, t); break;
            case CardEffectType.WeakenEnemy: body = L.F("Ослабить врага на {0} ({1} х.).", v, t); break;
            case CardEffectType.BurnEnemy: body = L.F("Поджечь врага: {0} урона в начале его хода, {1} х.", v, t); break;
            case CardEffectType.PierceDamage: body = L.F("Нанести {0} урона, игнорируя блок.", v); break;
            case CardEffectType.Thorns: body = L.F("Если враг атакует в этот ход — он получает {0} урона.", v); break;
            case CardEffectType.Cleanse: body = L.T("Снять с себя один отрицательный эффект."); break;
            case CardEffectType.SelfDamage: body = L.F("Получить {0} урона самому.", v); break;
            case CardEffectType.NoBlockNextTurn: body = L.T("В следующий ход нельзя играть защитные карты."); break;
            case CardEffectType.NoAttackNextTurn: body = L.T("В следующий ход нельзя играть атакующие карты."); break;
            case CardEffectType.MoltenGuard: body = L.F("Если враг пробьёт блок и ранит тебя — он загорится: {0} урона в ход, {1} х.", v, t); break;
            case CardEffectType.Rage: body = L.F("Ярость на {0} хода: можно играть по 2 карты за ход (включая этот).", v); break;
            case CardEffectType.Momentum: body = L.F("+{0} Замах.", v); break;
            case CardEffectType.ConsumeMomentum: body = L.F("Забирает {0} Замах.", v); break;
            case CardEffectType.MomentumThreshold: body = L.F("В этот ход пассивный удар срабатывает на {0} Замах раньше.", v); break;
            case CardEffectType.PassiveStrikeBonus: body = L.F("Следующий пассивный удар наносит +{0} урона.", v); break;
            case CardEffectType.Combo: body = L.T("Связка: после этой карты можно сразу сыграть ещё одну карту атаки."); break;
            case CardEffectType.Retaliation: body = L.F("Если враг пробьёт блок и ранит тебя — +{0} Замах в начале следующего хода.", v); break;
            case CardEffectType.Execute: body = L.F("Если после удара у врага останется меньше {0}% HP — добить.", v); break;
            default: body = type.ToString(); break;
        }

        switch (condition)
        {
            case CardCondition.EnemyBurning: return L.F("Если враг горит — {0} вместо этого.", LowerFirst(body.TrimEnd('.')));
            case CardCondition.EnemyHasBlock: return L.F("Если у врага есть блок — {0} вместо этого.", LowerFirst(body.TrimEnd('.')));
            case CardCondition.EnemyAttacking: return L.F("Если враг готовит атаку — {0}", LowerFirst(body));
            case CardCondition.EnemyBelowHalf: return L.F("Если у врага меньше половины HP — {0} вместо этого.", LowerFirst(body.TrimEnd('.')));
            default: return body;
        }
    }

    public string Summary => SummaryComparedTo(null);

    public string SummaryComparedTo(CardEffect previous)
    {
        string v = Mark(value, previous?.value);
        string t = Mark(turns, previous?.turns);
        string s;
        switch (type)
        {
            case CardEffectType.Damage: s = hits > 1 ? L.F("{0} урона ×{1}", v, hits) : L.F("{0} урона", v); break;
            case CardEffectType.Block: s = L.F("{0} блока", v); break;
            case CardEffectType.Heal: s = $"+{v} HP"; break;
            case CardEffectType.PoisonEnemy: s = L.F("яд {0}×{1}", v, t); break;
            case CardEffectType.WeakenEnemy: s = L.F("ослабить −{0} ({1} х.)", v, t); break;
            case CardEffectType.BurnEnemy: s = L.F("горение {0}×{1}", v, t); break;
            case CardEffectType.PierceDamage: s = L.F("{0} сквозь блок", v); break;
            case CardEffectType.Thorns: s = L.F("шипы {0}", v); break;
            case CardEffectType.Cleanse: s = L.T("снять эффект"); break;
            case CardEffectType.SelfDamage: s = L.F("−{0} HP себе", v); break;
            case CardEffectType.NoBlockNextTurn: s = L.T("без защиты в след. ход"); break;
            case CardEffectType.NoAttackNextTurn: s = L.T("без атаки в след. ход"); break;
            case CardEffectType.MoltenGuard: s = L.F("пробил блок — горит {0}×{1}", v, t); break;
            case CardEffectType.Rage: s = L.F("2 карты за ход, {0} хода", v); break;
            case CardEffectType.Momentum: s = L.F("+{0} Замах", v); break;
            case CardEffectType.ConsumeMomentum: s = L.F("−{0} Замах", v); break;
            case CardEffectType.MomentumThreshold: s = L.F("порог удара −{0}", v); break;
            case CardEffectType.PassiveStrikeBonus: s = L.F("пасс. удар +{0}", v); break;
            case CardEffectType.Combo: s = L.T("связка: ещё атака"); break;
            case CardEffectType.Retaliation: s = L.F("ранят — +{0} Замах", v); break;
            case CardEffectType.Execute: s = L.F("< {0}% HP — добить", v); break;
            default: s = type.ToString(); break;
        }
        if (condition == CardCondition.EnemyBurning) s = L.T("если горит:") + " " + s;
        if (condition == CardCondition.EnemyBelowHalf) s = L.T("если < ½ HP:") + " " + s;
        if (condition == CardCondition.EnemyHasBlock) s = L.T("если враг в блоке:") + " " + s;
        if (condition == CardCondition.EnemyAttacking) s = L.T("если враг атакует:") + " " + s;
        return s;
    }

    static string Mark(int current, int? previous)
    {
        return previous.HasValue && previous.Value != current ? $"<color=#2e8b3a><b>{current}</b></color>" : current.ToString();
    }

    static string LowerFirst(string s) => string.IsNullOrEmpty(s) ? s : char.ToLower(s[0]) + s.Substring(1);
}
