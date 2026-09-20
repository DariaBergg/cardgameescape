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
            case CardEffectType.Damage: body = hits > 1 ? $"Нанести {v} урона {hits} раза." : $"Нанести {v} урона."; break;
            case CardEffectType.Block: body = $"Получить {v} блока."; break;
            case CardEffectType.Heal: body = $"Восстановить {v} HP."; break;
            case CardEffectType.PoisonEnemy: body = $"Отравить врага: {v} урона в ход, {t} х."; break;
            case CardEffectType.WeakenEnemy: body = $"Ослабить врага на {v} ({t} х.)."; break;
            case CardEffectType.BurnEnemy: body = $"Поджечь врага: {v} урона в начале его хода, {t} х."; break;
            case CardEffectType.PierceDamage: body = $"Нанести {v} урона, игнорируя блок."; break;
            case CardEffectType.Thorns: body = $"Если враг атакует в этот ход — он получает {v} урона."; break;
            case CardEffectType.Cleanse: body = "Снять с себя один отрицательный эффект."; break;
            case CardEffectType.SelfDamage: body = $"Получить {v} урона самому."; break;
            case CardEffectType.NoBlockNextTurn: body = "В следующий ход нельзя играть защитные карты."; break;
            case CardEffectType.NoAttackNextTurn: body = "В следующий ход нельзя играть атакующие карты."; break;
            case CardEffectType.MoltenGuard: body = $"Если враг пробьёт блок и ранит тебя — он загорится: {v} урона в ход, {t} х."; break;
            case CardEffectType.Rage: body = $"Ярость на {v} хода: можно играть по 2 карты за ход (включая этот)."; break;
            default: body = type.ToString(); break;
        }

        switch (condition)
        {
            case CardCondition.EnemyBurning: return $"Если враг горит — {LowerFirst(body.TrimEnd('.'))} вместо этого.";
            case CardCondition.EnemyHasBlock: return $"Если у врага есть блок — {LowerFirst(body.TrimEnd('.'))} вместо этого.";
            case CardCondition.EnemyAttacking: return $"Если враг готовит атаку — {LowerFirst(body)}";
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
            case CardEffectType.Damage: s = hits > 1 ? $"{v} урона ×{hits}" : $"{v} урона"; break;
            case CardEffectType.Block: s = $"{v} блока"; break;
            case CardEffectType.Heal: s = $"+{v} HP"; break;
            case CardEffectType.PoisonEnemy: s = $"яд {v}×{t}"; break;
            case CardEffectType.WeakenEnemy: s = $"ослабить −{v} ({t} х.)"; break;
            case CardEffectType.BurnEnemy: s = $"горение {v}×{t}"; break;
            case CardEffectType.PierceDamage: s = $"{v} сквозь блок"; break;
            case CardEffectType.Thorns: s = $"шипы {v}"; break;
            case CardEffectType.Cleanse: s = "снять эффект"; break;
            case CardEffectType.SelfDamage: s = $"−{v} HP себе"; break;
            case CardEffectType.NoBlockNextTurn: s = "без защиты в след. ход"; break;
            case CardEffectType.NoAttackNextTurn: s = "без атаки в след. ход"; break;
            case CardEffectType.MoltenGuard: s = $"пробил блок — горит {v}×{t}"; break;
            case CardEffectType.Rage: s = $"2 карты за ход, {v} хода"; break;
            default: s = type.ToString(); break;
        }
        if (condition == CardCondition.EnemyBurning) s = "если горит: " + s;
        if (condition == CardCondition.EnemyHasBlock) s = "если враг в блоке: " + s;
        if (condition == CardCondition.EnemyAttacking) s = "если враг атакует: " + s;
        return s;
    }

    static string Mark(int current, int? previous)
    {
        return previous.HasValue && previous.Value != current ? $"<color=#2e8b3a><b>{current}</b></color>" : current.ToString();
    }

    static string LowerFirst(string s) => string.IsNullOrEmpty(s) ? s : char.ToLower(s[0]) + s.Substring(1);
}
