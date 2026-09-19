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
    MoltenGuard
}

public enum CardCondition
{
    None,
    EnemyBurning,
    EnemyNotBurning,
    EnemyHasBlock,
    EnemyNoBlock,
    EnemyAttacking
}
