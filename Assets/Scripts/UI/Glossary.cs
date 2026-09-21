using System.Collections.Generic;
using System.Text;

// Словарь терминов: второе окно подсказки, объясняющее ключевые слова карты или намерения врага
public static class Glossary
{
    static readonly Dictionary<string, string> Terms = new Dictionary<string, string>
    {
        { "Замах", "Копится от карт льва. Когда шкала полна (3/3), лев сам бьёт врага молнией на 6 урона сквозь блок — это не тратит ни карту, ни ход. Лишний Замах переносится дальше." },
        { "Пассивный удар", "Молния льва при полной шкале Замаха. Карты могут усиливать следующую молнию." },
        { "Порог удара", "В этот ход молния срабатывает при меньшем Замахе (2/3 вместо 3/3)." },
        { "Связка", "После этой карты в этот же ход можно сразу сыграть ещё одну карту атаки." },
        { "Возмездие", "Если враг в этот ход пробьёт блок и ранит тебя — в начале следующего хода получишь Замах." },
        { "Казнь", "Если после удара у врага останется не больше указанной доли здоровья — он погибает сразу." },
        { "Ярость", "Позволяет играть по 2 карты за ход указанное число ходов. Остаётся в руке, пока не разыграна; один раз за бой. Несколько Яростей складывают ходы." },
        { "Горение", "Враг теряет указанное здоровье в начале каждого своего хода. Повторный поджог продлевает горение, урон берётся наибольший." },
        { "Яд", "Отравленный теряет здоровье в начале своего хода указанное число ходов. Повторное отравление продлевает срок." },
        { "Блок", "Поглощает урон до конца хода. Что не поглотил — бьёт по здоровью." },
        { "Сквозь блок", "Этот урон блок не поглощает." },
        { "Шипы", "Если враг атакует в этот ход — он получает урон, даже если удар полностью поглощён блоком." },
        { "Ослабление", "Атаки ослабленного слабее на указанное число указанное число ходов." },
        { "Раскалённая броня", "Если враг пробьёт блок и ранит тебя — он загорится." },
        { "Очищение", "Снимает с тебя яд или ослабление руки." },
        { "Самоурон", "Карта ранит тебя самого при розыгрыше." },
        { "Без защиты", "В указанный ход нельзя играть защитные карты." },
        { "Без атаки", "В указанный ход нельзя играть атакующие карты." },
        { "Один раз за бой", "После розыгрыша карта выбывает до конца боя, а в следующем бою снова в колоде." },
        { "Остаётся в руке", "В конце хода карта не сбрасывается, враги не могут её украсть или испортить." },
        { "Метка", "Каждая карта с меткой элиты в колоде повышает шанс встретить его за фиолетовой дверью." },
        { "Прячется", "Враг уходит под воду или рассыпается: получает защиту и в следующий ход бьёт сильнее. Пока он скрыт, ты всё равно можешь его бить." },
        { "−карта", "В следующий ход у тебя будет на одну карту меньше: враг утащит её из руки." },
        { "Порча", "Одна карта в руке до конца боя заменяется слабой." },
        { "Защита", "У врага в этот ход есть блок: часть твоего урона он поглотит." },
        { "Кровотечение", "То же, что яд: теряешь здоровье в начале своего хода." },
    };

    public static string Explain(string term) => Terms.TryGetValue(term, out var text) ? L.T(term) + " — " + L.T(text) : null;

    static void Add(List<string> terms, string term)
    {
        if (!terms.Contains(term)) terms.Add(term);
    }

    // Термины, упомянутые на карте
    public static string ForCard(CardData card)
    {
        if (card == null) return "";
        var terms = new List<string>();
        foreach (var e in card.effects)
        {
            switch (e.type)
            {
                case CardEffectType.Block: Add(terms, "Блок"); break;
                case CardEffectType.PoisonEnemy: Add(terms, "Яд"); break;
                case CardEffectType.WeakenEnemy: Add(terms, "Ослабление"); break;
                case CardEffectType.BurnEnemy: Add(terms, "Горение"); break;
                case CardEffectType.PierceDamage: Add(terms, "Сквозь блок"); break;
                case CardEffectType.Thorns: Add(terms, "Шипы"); break;
                case CardEffectType.Cleanse: Add(terms, "Очищение"); break;
                case CardEffectType.SelfDamage: Add(terms, "Самоурон"); break;
                case CardEffectType.NoBlockNextTurn: Add(terms, "Без защиты"); break;
                case CardEffectType.NoAttackNextTurn: Add(terms, "Без атаки"); break;
                case CardEffectType.MoltenGuard: Add(terms, "Раскалённая броня"); break;
                case CardEffectType.Rage: Add(terms, "Ярость"); break;
                case CardEffectType.Momentum:
                case CardEffectType.ConsumeMomentum: Add(terms, "Замах"); break;
                case CardEffectType.MomentumThreshold: Add(terms, "Замах"); Add(terms, "Порог удара"); break;
                case CardEffectType.PassiveStrikeBonus: Add(terms, "Замах"); Add(terms, "Пассивный удар"); break;
                case CardEffectType.Combo: Add(terms, "Связка"); break;
                case CardEffectType.Retaliation: Add(terms, "Возмездие"); Add(terms, "Замах"); break;
                case CardEffectType.Execute: Add(terms, "Казнь"); break;
            }
            if (e.condition == CardCondition.EnemyBurning || e.condition == CardCondition.EnemyNotBurning) Add(terms, "Горение");
            if (e.condition == CardCondition.EnemyHasBlock || e.condition == CardCondition.EnemyNoBlock) Add(terms, "Защита");
        }
        if (card.exhaust) Add(terms, "Один раз за бой");
        if (card.retain) Add(terms, "Остаётся в руке");
        if (card.IsMarked) Add(terms, "Метка");
        return Join(terms);
    }

    // Термины, упомянутые в намерении врага
    public static string ForMove(EnemyMove move)
    {
        if (move == null) return "";
        var terms = new List<string>();
        if (move.block > 0) Add(terms, "Защита");
        if (move.poisonTurns > 0) Add(terms, string.IsNullOrEmpty(move.poisonLabel) || move.poisonLabel == "Яд" ? "Яд" : move.poisonLabel);
        if (move.handReduce > 0) Add(terms, "−карта");
        if (move.corruptCards > 0) Add(terms, "Порча");
        if (move.lockBlock) Add(terms, "Без защиты");
        if (move.submerge) Add(terms, "Прячется");
        return Join(terms);
    }

    static string Join(List<string> terms)
    {
        var sb = new StringBuilder();
        foreach (var t in terms)
        {
            var line = Explain(t);
            if (line == null) continue;
            if (sb.Length > 0) sb.Append("\n\n");
            sb.Append("<color=#9fd8ff><b>").Append(L.T(t)).Append("</b></color> — ").Append(L.T(Terms[t]));
        }
        return sb.ToString();
    }
}
