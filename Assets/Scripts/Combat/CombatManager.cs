using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CombatManager : MonoBehaviour
{
    public static CombatManager Instance { get; private set; }

    public int handSize = 3;
    public Vector3 combatPlayerPosition = new Vector3(-5.5f, -1.6f, 0);
    public float combatPlayerScale = 1.3f;
    public Vector3 combatEnemyPosition = new Vector3(3.5f, 0.6f, 0);
    public float combatEnemyScale = 1.15f;

    EnemyData enemy;
    EnemyMove currentMove;
    int moveIndex = -1;
    int enemyHP;
    int enemyBlock;
    int enemyPoisonDamage;
    int enemyPoisonTurns;
    int enemyWeakAmount;
    int enemyWeakTurns;
    int enemyBurnDamage;
    int enemyBurnTurns;
    int forcedNextMove = -1;
    int pendingDamageBonus;
    bool enemyHidden;

    int playerBlock;
    int playerThorns;
    bool blockLockedNextTurn;
    bool blockLocked;
    int poisonDamage;
    int poisonTurns;
    int nextHandPenalty;

    int cardsPlayedThisTurn;
    bool combatActive;
    bool enemyActing;

    readonly List<CardData> drawPile = new List<CardData>();
    readonly List<CardData> hand = new List<CardData>();
    readonly List<CardData> discardPile = new List<CardData>();

    GameObject enemyVisual;
    SpriteRenderer enemyRenderer;
    SpriteRenderer playerRenderer;
    CombatUI ui;
    CombatFX fx;

    static readonly Color DamageColor = new Color(1f, 0.35f, 0.3f);
    static readonly Color BlockColor = new Color(0.4f, 0.7f, 1f);
    static readonly Color HealColor = new Color(0.4f, 1f, 0.5f);
    static readonly Color PoisonColor = new Color(0.6f, 1f, 0.3f);
    static readonly Color DebuffColor = new Color(0.85f, 0.6f, 1f);
    static readonly Color BurnColor = new Color(1f, 0.6f, 0.2f);

    public string LastEvent { get; private set; } = "";
    public bool CombatActive => combatActive;
    public bool EnemyActing => enemyActing;
    public string EnemyName => enemy != null ? enemy.enemyName : "";
    public int EnemyHP => enemyHP;
    public int EnemyMaxHP => enemy != null ? enemy.maxHP : 0;
    public int EnemyBlock => enemyBlock;
    public EnemyMove CurrentMove => currentMove;
    public int PendingDamageBonus => pendingDamageBonus;
    public Transform EnemyTransform => enemyVisual != null ? enemyVisual.transform : null;
    public Vector3 EnemyTop => enemyRenderer != null ? new Vector3(enemyRenderer.bounds.center.x, enemyRenderer.bounds.max.y, 0) : new Vector3(0, 2.5f, 0);
    public int PlayerBlock => playerBlock;
    public IReadOnlyList<CardData> Hand => hand;
    public int DrawPileCount => drawPile.Count;
    public int DiscardPileCount => discardPile.Count;
    public int CardsLeftThisTurn => Mathf.Max(0, GameManager.Instance.maxCardsPerTurn - cardsPlayedThisTurn);
    public bool CanPlayCard => combatActive && !enemyActing && CardsLeftThisTurn > 0;
    public bool CanPlay(CardData card) => CanPlayCard && !card.unplayable && !(blockLocked && card.HasEffect(CardEffectType.Block));
    public bool CanEndTurn => combatActive && !enemyActing;

    public int EnemyWeakAmount => enemyWeakTurns > 0 ? enemyWeakAmount : 0;

    public string EnemyStatusText
    {
        get
        {
            var parts = new List<string>();
            if (enemyBlock > 0) parts.Add($"Блок {enemyBlock}");
            if (enemyPoisonTurns > 0) parts.Add($"Яд {enemyPoisonDamage}×{enemyPoisonTurns}");
            if (enemyWeakTurns > 0) parts.Add($"Ослаблен −{enemyWeakAmount} ({enemyWeakTurns} х.)");
            if (enemyBurnTurns > 0) parts.Add($"Горит {enemyBurnDamage}×{enemyBurnTurns}");
            return string.Join("   ", parts);
        }
    }

    public string PlayerStatusText
    {
        get
        {
            var parts = new List<string>();
            if (playerThorns > 0) parts.Add($"Шипы {playerThorns}");
            if (blockLocked) parts.Add("Нельзя защищаться в этот ход");
            else if (blockLockedNextTurn) parts.Add("В следующий ход нельзя защищаться");
            if (poisonTurns > 0) parts.Add($"Яд: {poisonDamage} урона в начале хода, ещё {poisonTurns} х.");
            if (nextHandPenalty > 0) parts.Add($"Ослаблен: −{nextHandPenalty} карта в следующий ход");
            return string.Join("   ", parts);
        }
    }

    void Awake()
    {
        Instance = this;
        fx = GetComponent<CombatFX>();
        if (fx == null) fx = gameObject.AddComponent<CombatFX>();
    }

    public void StartCombat(EnemyData data)
    {
        enemy = data;
        enemyHP = data.maxHP;
        enemyBlock = 0;
        enemyPoisonDamage = 0;
        enemyPoisonTurns = 0;
        enemyWeakAmount = 0;
        enemyWeakTurns = 0;
        enemyBurnDamage = 0;
        enemyBurnTurns = 0;
        forcedNextMove = -1;
        pendingDamageBonus = 0;
        enemyHidden = false;
        moveIndex = -1;
        playerBlock = 0;
        playerThorns = 0;
        blockLocked = false;
        blockLockedNextTurn = false;
        poisonDamage = 0;
        poisonTurns = 0;
        nextHandPenalty = 0;
        combatActive = true;
        enemyActing = false;

        drawPile.Clear();
        drawPile.AddRange(GameManager.Instance.playerDeck);
        Shuffle(drawPile);
        hand.Clear();
        discardPile.Clear();

        var player = FindFirstObjectByType<PlayerController>();
        playerRenderer = player != null ? player.GetComponent<SpriteRenderer>() : null;
        if (player != null) player.EnterCombatPose(data.overridePlayerPosition ? data.playerPosition : combatPlayerPosition, combatPlayerScale);

        enemyVisual = new GameObject("Enemy_" + data.enemyName);
        enemyVisual.transform.position = data.overrideEnemyPosition ? data.enemyPosition : combatEnemyPosition;
        var spriteObject = new GameObject("Sprite");
        spriteObject.transform.SetParent(enemyVisual.transform, false);
        enemyRenderer = spriteObject.AddComponent<SpriteRenderer>();
        enemyRenderer.sortingOrder = 2;
        if (data.sprite != null)
        {
            enemyRenderer.sprite = data.sprite;
            enemyVisual.transform.localScale = Vector3.one * combatEnemyScale;
        }
        else
        {
            enemyRenderer.sprite = PlaceholderSprites.Square(data.color);
            enemyVisual.transform.localScale = Vector3.one * 2f;
        }
        if (data.flying) spriteObject.AddComponent<HoverBob>();

        if (ui == null) ui = CombatUI.Create(this);
        ui.Show();
        LastEvent = $"{enemy.enemyName} появляется!";
        ChooseNextMove();
        StartPlayerTurn();
    }

    void ChooseNextMove()
    {
        if (enemy.moves.Count == 0)
        {
            currentMove = null;
            return;
        }
        if (forcedNextMove >= 0 && forcedNextMove < enemy.moves.Count)
        {
            moveIndex = forcedNextMove;
            forcedNextMove = -1;
            currentMove = enemy.moves[moveIndex];
            return;
        }
        if (enemy.randomMoves && enemy.moves.Count > 1)
        {
            int next;
            do next = Random.Range(0, enemy.moves.Count);
            while (next == moveIndex);
            moveIndex = next;
        }
        else
        {
            moveIndex = (moveIndex + 1) % enemy.moves.Count;
        }
        currentMove = enemy.moves[moveIndex];
    }

    void StartPlayerTurn()
    {
        cardsPlayedThisTurn = 0;
        playerBlock = 0;
        playerThorns = 0;
        blockLocked = blockLockedNextTurn;
        blockLockedNextTurn = false;
        if (blockLocked) fx.FloatingText(PlayerHead, "Без защиты!", DebuffColor);

        if (poisonTurns > 0)
        {
            GameManager.Instance.TakeDamage(poisonDamage);
            poisonTurns--;
            LastEvent += $"  Яд: −{poisonDamage} HP.";
            fx.Flash(playerRenderer, PoisonColor);
            fx.FloatingText(PlayerHead, $"-{poisonDamage} яд", PoisonColor);
            if (GameManager.Instance.currentHP <= 0)
            {
                Lose();
                return;
            }
        }

        if (nextHandPenalty > 0 && hand.Count > 1)
        {
            int lost = Mathf.Min(nextHandPenalty, hand.Count - 1);
            for (int i = 0; i < lost; i++)
            {
                int index = Random.Range(0, hand.Count);
                discardPile.Add(hand[index]);
                hand.RemoveAt(index);
            }
            nextHandPenalty = 0;
            fx.FloatingText(PlayerHead, $"-{lost} карта", DebuffColor);
        }

        if (hand.Count == 0)
        {
            int totalCards = drawPile.Count + discardPile.Count;
            int normalDraws = Mathf.Min(handSize, totalCards);
            int draws = Mathf.Max(1, normalDraws - nextHandPenalty);
            if (nextHandPenalty > 0 && draws < normalDraws)
                fx.FloatingText(PlayerHead, $"-{normalDraws - draws} карта", DebuffColor);
            nextHandPenalty = 0;
            for (int i = 0; i < draws; i++) DrawCard();
        }
        ui.Refresh();
    }

    void DrawCard()
    {
        if (drawPile.Count == 0)
        {
            if (discardPile.Count == 0) return;
            drawPile.AddRange(discardPile);
            discardPile.Clear();
            Shuffle(drawPile);
        }
        int last = drawPile.Count - 1;
        hand.Add(drawPile[last]);
        drawPile.RemoveAt(last);
    }

    public void PlayCard(CardData card)
    {
        if (!CanPlay(card) || !hand.Contains(card)) return;

        hand.Remove(card);
        discardPile.Add(card);
        cardsPlayedThisTurn++;
        ApplyCardEffect(card);

        if (enemyHP <= 0)
        {
            Win();
            return;
        }
        if (GameManager.Instance.currentHP <= 0)
        {
            Lose();
            return;
        }
        ui.Refresh();

        if (CardsLeftThisTurn == 0) StartCoroutine(AutoEndTurn());
    }

    IEnumerator AutoEndTurn()
    {
        enemyActing = true;
        ui.Refresh();
        yield return new WaitForSeconds(0.7f);
        enemyActing = false;
        if (combatActive) StartCoroutine(EnemyTurnRoutine());
    }

    Vector3 PlayerPos => playerRenderer != null ? playerRenderer.transform.position : Vector3.down * 3f;
    Vector3 PlayerHead => playerRenderer != null ? playerRenderer.bounds.center + Vector3.up * 0.4f : PlayerPos + Vector3.up;
    Vector3 EnemyCenter => enemyRenderer != null ? enemyRenderer.bounds.center + Vector3.up * 0.3f : new Vector3(0, 1.5f, 0);

    void ApplyCardEffect(CardData card)
    {
        var log = new List<string>();
        foreach (var effect in card.effects)
        {
            if (effect.condition == CardCondition.EnemyBurning && enemyBurnTurns <= 0) continue;
            if (effect.condition == CardCondition.EnemyNotBurning && enemyBurnTurns > 0) continue;
            if (effect.condition == CardCondition.EnemyHasBlock && enemyBlock <= 0) continue;
            if (effect.condition == CardCondition.EnemyNoBlock && enemyBlock > 0) continue;
            if (effect.condition == CardCondition.EnemyAttacking && (currentMove == null || currentMove.damage <= 0)) continue;
            switch (effect.type)
            {
                case CardEffectType.Damage:
                {
                    int totalDamage = 0, totalAbsorbed = 0;
                    for (int i = 0; i < effect.hits; i++)
                    {
                        int absorbed = Mathf.Min(enemyBlock, effect.value);
                        int damage = effect.value - absorbed;
                        enemyBlock -= absorbed;
                        enemyHP = Mathf.Max(0, enemyHP - damage);
                        totalDamage += damage;
                        totalAbsorbed += absorbed;
                    }
                    string entry = $"{totalDamage} урона";
                    if (totalAbsorbed > 0) entry += $" (блок поглотил {totalAbsorbed})";
                    log.Add(entry);
                    fx.Flash(enemyRenderer, DamageColor);
                    fx.Shake(enemyVisual.transform);
                    string popup = totalDamage > 0 ? (effect.hits > 1 ? $"-{totalDamage} (×{effect.hits})" : $"-{totalDamage}") : "Блок!";
                    fx.FloatingText(EnemyCenter, popup, totalDamage > 0 ? DamageColor : BlockColor);
                    break;
                }
                case CardEffectType.Block:
                    playerBlock += effect.value;
                    log.Add($"+{effect.value} блока");
                    fx.Flash(playerRenderer, BlockColor);
                    fx.FloatingText(PlayerHead, $"+{effect.value} блок", BlockColor);
                    break;
                case CardEffectType.Heal:
                    GameManager.Instance.Heal(effect.value);
                    log.Add($"+{effect.value} HP");
                    fx.Flash(playerRenderer, HealColor);
                    fx.FloatingText(PlayerHead, $"+{effect.value} HP", HealColor);
                    break;
                case CardEffectType.PoisonEnemy:
                    enemyPoisonDamage = Mathf.Max(enemyPoisonDamage, effect.value);
                    enemyPoisonTurns += effect.turns;
                    log.Add($"яд {effect.value}×{effect.turns}");
                    fx.Flash(enemyRenderer, PoisonColor);
                    fx.FloatingText(EnemyCenter, "Яд!", PoisonColor);
                    break;
                case CardEffectType.WeakenEnemy:
                    enemyWeakAmount = Mathf.Max(enemyWeakAmount, effect.value);
                    enemyWeakTurns += effect.turns;
                    log.Add($"враг ослаблен −{effect.value} ({effect.turns} х.)");
                    fx.Flash(enemyRenderer, DebuffColor);
                    fx.FloatingText(EnemyCenter, "Ослаблен!", DebuffColor);
                    break;
                case CardEffectType.BurnEnemy:
                    enemyBurnDamage = Mathf.Max(enemyBurnDamage, effect.value);
                    enemyBurnTurns = Mathf.Max(enemyBurnTurns, effect.turns);
                    log.Add($"горение {effect.value}×{effect.turns}");
                    fx.Flash(enemyRenderer, BurnColor);
                    fx.FloatingText(EnemyCenter, "Горит!", BurnColor);
                    break;
                case CardEffectType.PierceDamage:
                    enemyHP = Mathf.Max(0, enemyHP - effect.value);
                    log.Add($"{effect.value} урона сквозь блок");
                    fx.Flash(enemyRenderer, DamageColor);
                    fx.Shake(enemyVisual.transform);
                    fx.FloatingText(EnemyCenter, $"-{effect.value}", DamageColor);
                    break;
                case CardEffectType.Thorns:
                    playerThorns += effect.value;
                    log.Add($"шипы {effect.value}");
                    fx.Flash(playerRenderer, BlockColor);
                    fx.FloatingText(PlayerHead, $"Шипы {playerThorns}", BlockColor);
                    break;
                case CardEffectType.SelfDamage:
                    GameManager.Instance.TakeDamage(effect.value);
                    log.Add($"−{effect.value} HP себе");
                    fx.Flash(playerRenderer, DamageColor);
                    fx.FloatingText(PlayerHead, $"-{effect.value}", DamageColor);
                    break;
                case CardEffectType.NoBlockNextTurn:
                    blockLockedNextTurn = true;
                    log.Add("без защиты в след. ход");
                    break;
                case CardEffectType.Cleanse:
                    if (poisonTurns > 0) { poisonTurns = 0; poisonDamage = 0; log.Add("яд снят"); }
                    else if (nextHandPenalty > 0) { nextHandPenalty = 0; log.Add("ослабление снято"); }
                    else log.Add("нечего снимать");
                    fx.Flash(playerRenderer, HealColor);
                    fx.FloatingText(PlayerHead, "Очищение", HealColor);
                    break;
            }
        }
        LastEvent = $"«{card.cardName}»: {string.Join(", ", log)}";
    }

    public void EndTurn()
    {
        if (!CanEndTurn) return;
        StartCoroutine(EnemyTurnRoutine());
    }

    IEnumerator EnemyTurnRoutine()
    {
        enemyActing = true;
        enemyBlock = 0;
        for (int i = hand.Count - 1; i >= 0; i--)
        {
            if (!hand[i].unplayable) continue;
            discardPile.Add(hand[i]);
            hand.RemoveAt(i);
        }
        var move = currentMove;

        if (enemyPoisonTurns > 0)
        {
            enemyHP = Mathf.Max(0, enemyHP - enemyPoisonDamage);
            enemyPoisonTurns--;
            LastEvent = $"Яд: {enemy.enemyName} теряет {enemyPoisonDamage} HP.";
            fx.Flash(enemyRenderer, PoisonColor);
            fx.FloatingText(EnemyCenter, $"-{enemyPoisonDamage} яд", PoisonColor);
            ui.Refresh();
            yield return new WaitForSeconds(0.6f);
            if (enemyHP <= 0)
            {
                enemyActing = false;
                Win();
                yield break;
            }
        }

        if (enemyBurnTurns > 0)
        {
            enemyHP = Mathf.Max(0, enemyHP - enemyBurnDamage);
            enemyBurnTurns--;
            LastEvent = $"Горение: {enemy.enemyName} теряет {enemyBurnDamage} HP.";
            fx.Flash(enemyRenderer, BurnColor);
            fx.FloatingText(EnemyCenter, $"-{enemyBurnDamage} огонь", BurnColor);
            ui.Refresh();
            yield return new WaitForSeconds(0.6f);
            if (enemyHP <= 0)
            {
                enemyActing = false;
                Win();
                yield break;
            }
        }

        if (move == null)
        {
            LastEvent = $"{enemy.enemyName} ничего не делает.";
            ui.Refresh();
            yield return new WaitForSeconds(0.6f);
        }
        else
        {
            LastEvent = $"{enemy.enemyName} использует «{move.moveName}»";
            ui.Refresh();
            yield return new WaitForSeconds(0.5f);

            if (enemyHidden && !move.submerge)
            {
                enemyHidden = false;
                if (enemy.sprite != null) enemyRenderer.sprite = enemy.sprite;
                yield return fx.Lunge(enemyVisual.transform, Vector3.up * 0.6f, 0.3f);
            }

            int damageBonus = pendingDamageBonus;
            pendingDamageBonus = 0;

            bool hasEffect = move.HasEffect || move.submerge;
            if (!hasEffect)
            {
                yield return fx.Flutter(enemyVisual.transform, 1.1f);
                LastEvent = $"{enemy.enemyName} ничего не делает.";
                ui.Refresh();
                yield return new WaitForSeconds(0.2f);
            }

            if (move.damage > 0)
            {
                for (int hit = 0; hit < move.hits; hit++)
                {
                    Vector3 toPlayer = (PlayerPos - enemyVisual.transform.position).normalized * 1.3f;
                    yield return fx.Lunge(enemyVisual.transform, toPlayer, move.hits > 1 ? 0.22f : 0.3f);

                    int weakened = Mathf.Min(EnemyWeakAmount, move.damage + damageBonus);
                    int attack = move.damage + damageBonus - weakened;
                    int absorbed = Mathf.Min(playerBlock, attack);
                    int damage = attack - absorbed;
                    playerBlock -= absorbed;
                    GameManager.Instance.TakeDamage(damage);

                    LastEvent = $"{enemy.enemyName} атакует на {move.damage + damageBonus}.";
                    if (weakened > 0) LastEvent += $" Ослабление сняло {weakened}.";
                    if (absorbed > 0) LastEvent += $" Блок поглотил {absorbed}.";
                    LastEvent += damage > 0 ? $" Ты получил {damage} урона." : " Урон не прошёл.";

                    if (damage > 0)
                    {
                        fx.Flash(playerRenderer, DamageColor);
                        if (playerRenderer != null) fx.Shake(playerRenderer.transform);
                        fx.FloatingText(PlayerHead, $"-{damage}", DamageColor);
                    }
                    else
                    {
                        fx.Flash(playerRenderer, BlockColor);
                        fx.FloatingText(PlayerHead, "Блок!", BlockColor);
                    }
                    ui.Refresh();
                    yield return new WaitForSeconds(move.hits > 1 ? 0.25f : 0.5f);
                    if (GameManager.Instance.currentHP <= 0) break;
                }

                if (playerThorns > 0 && GameManager.Instance.currentHP > 0)
                {
                    enemyHP = Mathf.Max(0, enemyHP - playerThorns);
                    LastEvent = $"Шипы: {enemy.enemyName} получает {playerThorns} урона.";
                    fx.Flash(enemyRenderer, DamageColor);
                    fx.Shake(enemyVisual.transform);
                    fx.FloatingText(EnemyCenter, $"-{playerThorns} шипы", DamageColor);
                    ui.Refresh();
                    yield return new WaitForSeconds(0.5f);
                    if (enemyHP <= 0)
                    {
                        enemyActing = false;
                        Win();
                        yield break;
                    }
                }
            }

            if (move.block > 0)
            {
                enemyBlock += move.block;
                LastEvent = $"{enemy.enemyName} получает {move.block} блока.";
                fx.Flash(enemyRenderer, BlockColor);
                fx.FloatingText(EnemyCenter, $"+{move.block} блок", BlockColor);
                ui.Refresh();
                yield return new WaitForSeconds(0.5f);
            }

            if (move.poisonTurns > 0)
            {
                poisonDamage = Mathf.Max(poisonDamage, move.poisonDamage);
                poisonTurns += move.poisonTurns;
                LastEvent = $"Ты отравлен: {move.poisonDamage} урона в начале хода, {move.poisonTurns} х.";
                fx.Flash(playerRenderer, PoisonColor);
                fx.FloatingText(PlayerHead, "Яд!", PoisonColor);
                ui.Refresh();
                yield return new WaitForSeconds(0.5f);
            }

            if (move.handReduce > 0)
            {
                nextHandPenalty += move.handReduce;
                LastEvent = $"Ты ослаблен: в следующий ход на {move.handReduce} карту меньше.";
                fx.Flash(playerRenderer, DebuffColor);
                fx.FloatingText(PlayerHead, "Ослаблен!", DebuffColor);
                ui.Refresh();
                yield return new WaitForSeconds(0.5f);
            }
        }

        if (move != null && move.submerge)
        {
            enemyHidden = true;
            yield return fx.Lunge(enemyVisual.transform, Vector3.down * 0.5f, 0.4f);
            if (enemy.hiddenSprite != null) enemyRenderer.sprite = enemy.hiddenSprite;
            LastEvent = $"{enemy.enemyName} скрывается из виду.";
            ui.Refresh();
            yield return new WaitForSeconds(0.4f);
        }
        if (move != null)
        {
            if (move.forceNextMove >= 0) forcedNextMove = move.forceNextMove;
            if (move.nextDamageBonus > 0) pendingDamageBonus += move.nextDamageBonus;
        }

        if (enemyWeakTurns > 0) enemyWeakTurns--;

        enemyActing = false;
        if (GameManager.Instance.currentHP <= 0)
        {
            Lose();
            yield break;
        }
        ChooseNextMove();
        StartPlayerTurn();
    }

    void Win()
    {
        combatActive = false;
        enemyActing = true;
        GameManager.Instance.OnCombatWon();
        LastEvent = $"{enemy.enemyName} повержен!";
        ui.Refresh();
        StartCoroutine(WinRoutine());
    }

    IEnumerator WinRoutine()
    {
        yield return new WaitForSeconds(0.5f);
        fx.Shake(enemyVisual.transform, 0.2f, 0.4f);
        yield return fx.FadeOut(enemyRenderer, 0.7f);
        yield return new WaitForSeconds(0.5f);
        enemyActing = false;

        bool tutorial = RoomManager.Instance != null && !RoomManager.Instance.MarkedRewardsUnlocked;
        if (tutorial) ShowUpgradeReward();
        else ui.ShowRewards(PickRewards(), OnRewardChosen);
    }

    void ShowUpgradeReward()
    {
        var gm = GameManager.Instance;
        DeckPickerUI.Get().Show(
            "Победа! Усиль одну карту",
            gm.playerDeck,
            null,
            card =>
            {
                gm.UpgradeCard(card);
                OnRewardChosen(null);
            },
            ShowUpgradeReward,
            upgradePreview: true);
    }

    List<CardData> PickRewards()
    {
        var character = GameManager.Instance.selectedCharacter;
        var rewards = new List<CardData>();
        var basePool = new List<CardData>(character.rewardCards);
        Shuffle(basePool);
        if (basePool.Count > 0) rewards.Add(basePool[0]);
        if (enemy.rewardCards.Count > 0) rewards.Add(enemy.rewardCards[Random.Range(0, enemy.rewardCards.Count)]);
        else if (basePool.Count > 1) rewards.Add(basePool[1]);
        return rewards;
    }

    void OnRewardChosen(CardData card)
    {
        if (card != null) GameManager.Instance.playerDeck.Add(card);
        EndCombat();
        RoomManager.Instance.OnRoomCleared();
    }

    void EndCombat()
    {
        if (enemyVisual != null) Destroy(enemyVisual);
        ui.Hide();
        enemy = null;
        currentMove = null;
    }

    void Lose()
    {
        combatActive = false;
        ui.Refresh();
        ui.ShowDefeat();
    }

    public void RestartRun()
    {
        GameManager.Instance.ResetRun();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    static void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
