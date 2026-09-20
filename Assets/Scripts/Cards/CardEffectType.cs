public enum CardEffectType
{
    Damage,
    Block,
    Heal,
    PoisonEnemy,
    WeakenEnemy,
    BurnEnemy,
    PierceDamage,
    Thorns,
    Cleanse,
    SelfDamage,
    NoBlockNextTurn,
    NoAttackNextTurn,
    MoltenGuard,
    Rage,
    // --- Лев: Замах ---
    Momentum,            // +value Замаха
    ConsumeMomentum,     // −value Замаха (если есть)
    MomentumThreshold,   // порог пассивного удара на этот ход ниже на value
    PassiveStrikeBonus,  // следующий пассивный удар +value урона
    Combo,               // в этот ход можно сыграть ещё одну карту атаки
    Retaliation,         // если враг пробьёт блок и ранит — +value Замаха в начале след. хода
    Execute              // если после удара у врага ≤ value% HP — добить
}

public enum CardCondition
{
    None,
    EnemyBurning,
    EnemyNotBurning,
    EnemyHasBlock,
    EnemyNoBlock,
    EnemyAttacking,
    EnemyBelowHalf,
    EnemyNotBelowHalf
}
