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
    bool attackLockedNextTurn;
    bool attackLocked;
    int moltenBurnDamage;
    int moltenBurnTurns;
    int rageTurnsLeft;
    int poisonDamage;
    int poisonTurns;
    int nextHandPenalty;

    int cardsPlayedThisTurn;
    Color playerPoisonColor = PoisonColor;

    // --- Малыш: маленькая копия врага за фиолетовой дверью ---
    GameObject minionVisual;
    SpriteRenderer minionRenderer;
    int minionHP, minionMaxHP, minionDamage;
    bool minionPromoted; // малыш остался один и стал главной целью
    bool targetMinion;   // выбранная цель атак: true — малыш
    public bool TargetIsMinion => targetMinion && MinionAlive;
    public void SelectTarget(bool minion)
    {
        if (!combatActive) return;
        targetMinion = minion && MinionAlive;
        ui.Refresh();
    }
    // Куда летит урон карт: в главного или в малыша
    SpriteRenderer TargetRenderer => TargetIsMinion ? minionRenderer : enemyRenderer;
    Transform TargetTransform => TargetIsMinion ? minionVisual.transform : enemyVisual.transform;
    Vector3 TargetCenter => TargetIsMinion ? MinionCenter : EnemyCenter;
    int TargetBlock => TargetIsMinion ? 0 : enemyBlock;
    // Наносит урон цели с учётом её блока; возвращает прошедший урон
    int HitTarget(int amount, bool pierce, out int absorbed)
    {
        absorbed = 0;
        if (TargetIsMinion)
        {
            minionHP = Mathf.Max(0, minionHP - amount);
            if (minionHP <= 0) StartCoroutine(MinionDies());
            return amount;
        }
        if (!pierce) { absorbed = Mathf.Min(enemyBlock, amount); enemyBlock -= absorbed; }
        int dmg = amount - absorbed;
        enemyHP = Mathf.Max(0, enemyHP - dmg);
        return dmg;
    }

    IEnumerator MinionDies()
    {
        targetMinion = false;
        var v = minionVisual; var r = minionRenderer;
        minionVisual = null; minionRenderer = null;
        fx.FloatingText(r.bounds.center, L.T("Малыш повержен!"), DamageColor);
        yield return fx.FadeOut(r, 0.5f);
        if (v != null) Destroy(v);
        ui.Refresh();
    }
    public bool MinionAlive => minionVisual != null && minionHP > 0 && !minionPromoted;
    public int MinionHP => minionHP;
    public int MinionMaxHP => minionMaxHP;
    public int MinionDamage => minionDamage;
    public Vector3 MinionTop => minionRenderer != null ? new Vector3(minionRenderer.bounds.center.x, minionRenderer.bounds.max.y, 0) : Vector3.zero;
    Vector3 MinionCenter => minionRenderer != null ? minionRenderer.bounds.center + Vector3.up * 0.2f : Vector3.zero;

    // --- Замах (лев) ---
    int momentum;
    int pendingMomentum;      // придёт в начале следующего хода (Возмездие)
    int momentumThresholdCut; // порог ниже на N в этот ход
    int passiveStrikeBonus;   // бонус к следующему пассивному удару
    int retaliationMomentum;  // сколько Замаха даст ранение в этот ход врага
    bool comboAttack;         // связка: ещё одна карта атаки в этот ход
    bool passiveStrikeReady;  // шкала заполнилась картой — ударить после её розыгрыша
    bool strikeRunning;
    bool endingTurn;
    static readonly Color MomentumColor = new Color(0.6f, 0.85f, 1f);
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
    public string EnemyName => enemy != null ? L.T(enemy.enemyName) : "";
    public EnemyData CurrentEnemy => enemy;
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
    public IReadOnlyList<CardData> DiscardPile => discardPile;
    public int TurnCardLimit => GameManager.Instance.maxCardsPerTurn + (rageTurnsLeft > 0 ? 1 : 0);
    int BaseCardsLeft => Mathf.Max(0, TurnCardLimit - cardsPlayedThisTurn);
    public int CardsLeftThisTurn => BaseCardsLeft + (comboAttack ? 1 : 0);
    public bool CanPlayCard => combatActive && !enemyActing && CardsLeftThisTurn > 0;
    public bool CanPlay(CardData card) => CanPlayCard && !card.unplayable && !(blockLocked && card.IsDefense) && !(attackLocked && card.IsAttack)
        && (BaseCardsLeft > 0 || card.IsAttack); // связка даёт только атаку

    public bool UsesMomentum => GameManager.Instance.selectedCharacter != null && GameManager.Instance.selectedCharacter.usesMomentum;
    public int Momentum => Mathf.Min(momentum, MomentumMax);
    public int MomentumMax => GameManager.Instance.selectedCharacter != null ? GameManager.Instance.selectedCharacter.momentumMax : 3;
    public int MomentumThreshold => Mathf.Max(1, MomentumMax - momentumThresholdCut);
    public bool CanEndTurn => combatActive && !enemyActing;

    public int EnemyWeakAmount => enemyWeakTurns > 0 ? enemyWeakAmount : 0;

    public struct StatusInfo { public string id; public string label; public int turns; public string tooltip; public Color color; }

    public List<StatusInfo> PlayerStatuses
    {
        get
        {
            var list = new List<StatusInfo>();
            if (poisonTurns > 0) list.Add(new StatusInfo { id = "poison", label = L.T("Яд"), turns = poisonTurns, color = PoisonColor, tooltip = L.F("<b>Яд</b>\n−{0} HP в начале каждого твоего хода. Осталось ходов: {1}.", poisonDamage, poisonTurns) });
            if (rageTurnsLeft > 0) list.Add(new StatusInfo { id = "rage", label = L.T("Ярость"), turns = rageTurnsLeft, color = DamageColor, tooltip = L.F("<b>Ярость</b>\nМожно играть по 2 карты за ход. Осталось ходов: {0}.", rageTurnsLeft) });
            if (playerThorns > 0) list.Add(new StatusInfo { id = "thorns", label = L.T("Шипы"), turns = playerThorns, color = BlockColor, tooltip = L.F("<b>Шипы</b>\nЕсли враг атакует в этот ход — получает {0} урона.", playerThorns) });
            if (moltenBurnTurns > 0) list.Add(new StatusInfo { id = "molten", label = L.T("Жар"), turns = moltenBurnTurns, color = BurnColor, tooltip = L.F("<b>Раскалённая броня</b>\nЕсли враг пробьёт блок и ранит тебя — он загорится: {0} урона в ход, {1} х.", moltenBurnDamage, moltenBurnTurns) });
            if (blockLocked) list.Add(new StatusInfo { id = "noblock", label = L.T("Без защиты"), turns = 1, color = DebuffColor, tooltip = L.T("<b>Без защиты</b>\nВ этот ход нельзя играть защитные карты.") });
            else if (blockLockedNextTurn) list.Add(new StatusInfo { id = "noblock", label = L.T("Без защиты"), turns = 1, color = DebuffColor, tooltip = L.T("<b>Без защиты</b>\nВ следующий ход нельзя играть защитные карты.") });
            if (attackLocked) list.Add(new StatusInfo { id = "noattack", label = L.T("Без атаки"), turns = 1, color = DebuffColor, tooltip = L.T("<b>Без атаки</b>\nВ этот ход нельзя играть атакующие карты.") });
            else if (attackLockedNextTurn) list.Add(new StatusInfo { id = "noattack", label = L.T("Без атаки"), turns = 1, color = DebuffColor, tooltip = L.T("<b>Без атаки</b>\nВ следующий ход нельзя играть атакующие карты.") });
            if (nextHandPenalty > 0) list.Add(new StatusInfo { id = "hand", label = L.T("−карта"), turns = nextHandPenalty, color = DebuffColor, tooltip = L.F("<b>Ослабление</b>\nВ следующий ход враг утащит {0} карт(у) из руки.", nextHandPenalty) });
            if (comboAttack && BaseCardsLeft == 0) list.Add(new StatusInfo { id = "combo", label = L.T("Связка"), turns = 1, color = MomentumColor, tooltip = L.T("<b>Связка</b>\nМожно сыграть ещё одну карту атаки в этот ход.") });
            if (retaliationMomentum > 0) list.Add(new StatusInfo { id = "retaliation", label = L.T("Возмездие"), turns = retaliationMomentum, color = MomentumColor, tooltip = L.F("<b>Возмездие</b>\nЕсли враг пробьёт блок и ранит тебя — +{0} Замах в начале следующего хода.", retaliationMomentum) });
            return list;
        }
    }

    public List<StatusInfo> EnemyStatuses
    {
        get
        {
            var list = new List<StatusInfo>();
            if (enemyPoisonTurns > 0) list.Add(new StatusInfo { id = "poison", label = L.T("Яд"), turns = enemyPoisonTurns, color = PoisonColor, tooltip = L.F("<b>Яд</b>\nВраг теряет {0} HP в начале своего хода. Осталось ходов: {1}.", enemyPoisonDamage, enemyPoisonTurns) });
            if (enemyBurnTurns > 0) list.Add(new StatusInfo { id = "burn", label = L.T("Горит"), turns = enemyBurnTurns, color = BurnColor, tooltip = L.F("<b>Горение</b>\nВраг теряет {0} HP в начале своего хода. Осталось ходов: {1}.", enemyBurnDamage, enemyBurnTurns) });
            if (enemyWeakTurns > 0) list.Add(new StatusInfo { id = "weak", label = L.T("Слаб"), turns = enemyWeakTurns, color = DebuffColor, tooltip = L.F("<b>Ослаблен</b>\nАтаки врага слабее на {0}. Осталось ходов: {1}.", enemyWeakAmount, enemyWeakTurns) });
            if (enemyHidden) list.Add(new StatusInfo { id = "hidden", label = L.T("Скрыт"), turns = 0, color = BlockColor, tooltip = L.T("<b>Скрылся</b>\nВраг ушёл под воду / рассыпался. Вынырнет на своём ходу.") });
            return list;
        }
    }

    public string EnemyStatusText
    {
        get
        {
            var parts = new List<string>();
            if (enemyBlock > 0) parts.Add(L.F("Блок {0}", enemyBlock));
            if (enemyPoisonTurns > 0) parts.Add(L.F("Яд {0}×{1}", enemyPoisonDamage, enemyPoisonTurns));
            if (enemyWeakTurns > 0) parts.Add(L.F("Ослаблен −{0} ({1} х.)", enemyWeakAmount, enemyWeakTurns));
            if (enemyBurnTurns > 0) parts.Add(L.F("Горит {0}×{1}", enemyBurnDamage, enemyBurnTurns));
            return string.Join("   ", parts);
        }
    }

    public string PlayerStatusText
    {
        get
        {
            var parts = new List<string>();
            if (playerThorns > 0) parts.Add(L.F("Шипы {0}", playerThorns));
            if (blockLocked) parts.Add(L.T("Нельзя защищаться в этот ход"));
            else if (blockLockedNextTurn) parts.Add(L.T("В следующий ход нельзя защищаться"));
            if (attackLocked) parts.Add(L.T("Нельзя атаковать в этот ход"));
            else if (attackLockedNextTurn) parts.Add(L.T("В следующий ход нельзя атаковать"));
            if (moltenBurnTurns > 0) parts.Add(L.F("Раскалённая броня: пробьёт блок — загорится {0}×{1}", moltenBurnDamage, moltenBurnTurns));
            if (rageTurnsLeft > 0) parts.Add(L.F("Ярость: 2 карты за ход, ещё {0} х.", rageTurnsLeft));
            if (comboAttack && BaseCardsLeft == 0) parts.Add(L.T("Связка: можно сыграть ещё одну атаку"));
            if (retaliationMomentum > 0) parts.Add(L.F("Возмездие: ранят — +{0} Замах", retaliationMomentum));
            if (poisonTurns > 0) parts.Add(L.F("Яд: {0} урона в начале хода, ещё {1} х.", poisonDamage, poisonTurns));
            if (nextHandPenalty > 0) parts.Add(L.F("Ослаблен: −{0} карта в следующий ход", nextHandPenalty));
            return string.Join("   ", parts);
        }
    }

    void Update()
    {
        if (!combatActive || !MinionAlive) return;
        var mouse = UnityEngine.InputSystem.Mouse.current;
        if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return;
        if (UnityEngine.EventSystems.EventSystem.current != null && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;
        Vector3 world = Camera.main.ScreenToWorldPoint(mouse.position.ReadValue());
        world.z = 0;
        if (minionRenderer != null && minionRenderer.bounds.Contains(new Vector3(world.x, world.y, minionRenderer.bounds.center.z))) SelectTarget(true);
        else if (enemyRenderer != null && enemyRenderer.bounds.Contains(new Vector3(world.x, world.y, enemyRenderer.bounds.center.z))) SelectTarget(false);
    }

    void Awake()
    {
        Instance = this;
        fx = GetComponent<CombatFX>();
        if (fx == null) fx = gameObject.AddComponent<CombatFX>();
    }

    public void StartCombat(EnemyData data, bool withMinion = false)
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
        attackLocked = false;
        attackLockedNextTurn = false;
        moltenBurnDamage = 0;
        moltenBurnTurns = 0;
        rageTurnsLeft = 0;
        momentum = 0; pendingMomentum = 0; momentumThresholdCut = 0; passiveStrikeBonus = 0; retaliationMomentum = 0; comboAttack = false;
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

        enemyVisual = new GameObject("Enemy_" + L.T(data.enemyName));
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

        // Малыш: уменьшенная копия рядом, со своим здоровьем и слабым ударом
        if (minionVisual != null) Destroy(minionVisual);
        minionVisual = null; minionRenderer = null; minionPromoted = false; minionHP = 0; targetMinion = false;
        if (withMinion)
        {
            minionMaxHP = Mathf.Max(5, Mathf.RoundToInt(data.maxHP / 3f));
            minionHP = minionMaxHP;
            int totalDamage = 0, attacks = 0;
            foreach (var m in data.moves) if (m.damage > 0) { totalDamage += m.damage * m.hits; attacks++; }
            minionDamage = Mathf.Max(1, Mathf.RoundToInt(attacks > 0 ? totalDamage / (float)attacks / 2f : 2f));
            minionVisual = new GameObject("Minion_" + L.T(data.enemyName));
            minionVisual.transform.position = enemyVisual.transform.position + new Vector3(2.4f, data.flying ? -1.2f : -1.0f, 0);
            var minionSprite = new GameObject("Sprite");
            minionSprite.transform.SetParent(minionVisual.transform, false);
            minionRenderer = minionSprite.AddComponent<SpriteRenderer>();
            minionRenderer.sortingOrder = 3;
            minionRenderer.sprite = enemyRenderer.sprite;
            minionVisual.transform.localScale = enemyVisual.transform.localScale * 0.5f;
            if (data.flying) minionSprite.AddComponent<HoverBob>();
        }

        if (ui == null) ui = CombatUI.Create(this);
        ui.Show();
        LastEvent = L.F("{0} появляется!", L.T(enemy.enemyName));
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
        comboAttack = false;
        endingTurn = false;
        passiveStrikeReady = false;
        momentumThresholdCut = 0;
        retaliationMomentum = 0;
        if (pendingMomentum > 0) { GainMomentum(pendingMomentum, L.T("Возмездие")); pendingMomentum = 0; }
        blockLocked = blockLockedNextTurn;
        blockLockedNextTurn = false;
        attackLocked = attackLockedNextTurn;
        attackLockedNextTurn = false;
        moltenBurnDamage = 0;
        moltenBurnTurns = 0;
        if (blockLocked) fx.FloatingText(PlayerHead, L.T("Без защиты!"), DebuffColor);
        if (attackLocked) fx.FloatingText(PlayerHead, L.T("Без атаки!"), DebuffColor);

        if (poisonTurns > 0 && poisonDamage > 0)
        {
            int tick = poisonDamage;
            GameManager.Instance.TakeDamage(tick);
            poisonTurns--;
            if (poisonTurns == 0) poisonDamage = 0;
            LastEvent += L.F("  Яд: −{0} HP.", tick);
            fx.Flash(playerRenderer, playerPoisonColor, 0.5f);
            if (playerRenderer != null) fx.Shake(playerRenderer.transform, 0.1f, 0.3f);
            fx.FloatingText(PlayerHead, L.F("-{0} яд", tick), playerPoisonColor);
            if (GameManager.Instance.currentHP <= 0)
            {
                Lose();
                return;
            }
        }

        if (currentMove != null && currentMove.block > 0)
        {
            enemyBlock += currentMove.block;
            fx.Flash(enemyRenderer, BlockColor);
            fx.FloatingText(EnemyCenter, L.F("+{0} блок", currentMove.block), BlockColor);
        }
        // Если враг собирается уйти под воду — он уже там (защита ведь уже действует)
        if (currentMove != null && currentMove.submerge && !enemyHidden)
        {
            enemyHidden = true;
            StartCoroutine(fx.Lunge(enemyVisual.transform, Vector3.down * 0.5f, 0.4f));
            if (enemy.hiddenSprite != null) enemyRenderer.sprite = enemy.hiddenSprite;
        }

        // Карты с retain остаются в руке, остальные уходят в сброс
        for (int i = hand.Count - 1; i >= 0; i--)
        {
            if (hand[i].retain) continue;
            discardPile.Add(hand[i]);
            hand.RemoveAt(i);
        }
        int totalCards = drawPile.Count + discardPile.Count;
        int normalDraws = Mathf.Min(Mathf.Max(0, handSize - hand.Count), totalCards);
        for (int i = 0; i < normalDraws; i++) DrawCard();
        ui.Refresh();
        if (UsesMomentum && momentum >= MomentumThreshold) StartCoroutine(PassiveStrike());

        int steal = Mathf.Min(nextHandPenalty, Mathf.Max(0, hand.Count - 1));
        nextHandPenalty = 0;
        if (steal > 0) StartCoroutine(StealRoutine(steal));
    }

    IEnumerator StealRoutine(int count)
    {
        enemyActing = true;
        ui.Refresh();
        yield return new WaitForSeconds(0.7f);
        for (int n = 0; n < count && hand.Count > 1; n++)
        {
            var stealable = hand.FindAll(c => !c.retain);
            if (stealable.Count == 0) break;
            var stolen = stealable[Random.Range(0, stealable.Count)];
            hand.Remove(stolen);
            discardPile.Add(stolen);
            ui.StealCard(stolen);
            yield return new WaitForSeconds(1.1f);
        }
        enemyActing = false;
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

    // Почему карту нельзя сыграть (для подсказки игроку)
    public string WhyCannotPlay(CardData card)
    {
        if (!combatActive) return "";
        if (enemyActing) return L.T("Ход врага");
        if (card.unplayable) return L.T("Нельзя разыграть");
        if (blockLocked && card.IsDefense) return L.T("Без защиты в этот ход");
        if (attackLocked && card.IsAttack) return L.T("Без атаки в этот ход");
        if (CardsLeftThisTurn == 0) return L.T("Карты на этот ход закончились");
        if (BaseCardsLeft == 0 && !card.IsAttack) return L.T("Связка: только атака");
        return "";
    }

    public void PlayCard(CardData card)
    {
        if (!hand.Contains(card)) return;
        if (!CanPlay(card))
        {
            string why = WhyCannotPlay(card);
            if (!string.IsNullOrEmpty(why)) fx.FloatingText(PlayerHead, why, DebuffColor);
            return;
        }

        hand.Remove(card);
        if (!card.exhaust) discardPile.Add(card); // exhaust: карта выбывает до конца боя
        if (BaseCardsLeft == 0 && comboAttack) comboAttack = false; // сыграно по связке
        else cardsPlayedThisTurn++;
        ui.NotifyCardPlayed(card);
        ApplyCardEffect(card);

        if (EnemyDown())
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

        if (passiveStrikeReady) { passiveStrikeReady = false; StartCoroutine(PassiveStrike()); return; } // конец хода решит молния
        if (CardsLeftThisTurn == 0 || !hand.Exists(CanPlay)) StartCoroutine(AutoEndTurn());
    }

    IEnumerator AutoEndTurn()
    {
        if (endingTurn) yield break;
        endingTurn = true;
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
            if (effect.condition == CardCondition.EnemyBelowHalf && enemyHP * 2 > enemy.maxHP) continue;
            if (effect.condition == CardCondition.EnemyNotBelowHalf && enemyHP * 2 <= enemy.maxHP) continue;
            switch (effect.type)
            {
                case CardEffectType.Damage:
                {
                    int totalDamage = 0, totalAbsorbed = 0;
                    var hitLog = new List<int>();
                    var hitRenderer = TargetRenderer; var hitTransform = TargetTransform; var hitCenter = TargetCenter;
                    for (int i = 0; i < effect.hits; i++)
                    {
                        int damage = HitTarget(effect.value, false, out int absorbed);
                        totalDamage += damage;
                        totalAbsorbed += absorbed;
                        hitLog.Add(damage);
                    }
                    string entry = L.F("{0} урона", totalDamage);
                    if (totalAbsorbed > 0) entry += L.F(" (блок поглотил {0})", totalAbsorbed);
                    log.Add(entry);
                    fx.Flash(hitRenderer, DamageColor);
                    if (hitTransform != null) fx.Shake(hitTransform);
                    if (effect.hits > 1) StartCoroutine(HitPopups(hitLog, hitCenter, hitTransform));
                    else fx.FloatingText(hitCenter, totalDamage > 0 ? $"-{totalDamage}" : L.T("Блок!"), totalDamage > 0 ? DamageColor : BlockColor);
                    break;
                }
                case CardEffectType.Block:
                    playerBlock += effect.value;
                    log.Add(L.F("+{0} блока", effect.value));
                    fx.Flash(playerRenderer, BlockColor);
                    fx.FloatingText(PlayerHead, L.F("+{0} блок", effect.value), BlockColor);
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
                    log.Add(L.F("яд {0}×{1}", effect.value, effect.turns));
                    fx.Flash(enemyRenderer, PoisonColor);
                    fx.FloatingText(EnemyCenter, L.T("Яд!"), PoisonColor);
                    break;
                case CardEffectType.WeakenEnemy:
                    enemyWeakAmount = Mathf.Max(enemyWeakAmount, effect.value);
                    enemyWeakTurns += effect.turns;
                    log.Add(L.F("враг ослаблен −{0} ({1} х.)", effect.value, effect.turns));
                    fx.Flash(enemyRenderer, DebuffColor);
                    fx.FloatingText(EnemyCenter, L.T("Ослаблен!"), DebuffColor);
                    break;
                case CardEffectType.BurnEnemy:
                    enemyBurnDamage = Mathf.Max(enemyBurnDamage, effect.value);
                    enemyBurnTurns += effect.turns;
                    log.Add(L.F("горение {0}×{1}", effect.value, effect.turns));
                    fx.Flash(enemyRenderer, BurnColor);
                    fx.FloatingText(EnemyCenter, L.T("Горит!"), BurnColor);
                    break;
                case CardEffectType.PierceDamage:
                {
                    var pr = TargetRenderer; var pt = TargetTransform; var pc = TargetCenter;
                    HitTarget(effect.value, true, out _);
                    log.Add(L.F("{0} урона сквозь блок", effect.value));
                    fx.Flash(pr, DamageColor);
                    if (pt != null) fx.Shake(pt);
                    fx.FloatingText(pc, $"-{effect.value}", DamageColor);
                    break;
                }
                case CardEffectType.Thorns:
                    playerThorns += effect.value;
                    log.Add(L.F("шипы {0}", effect.value));
                    fx.Flash(playerRenderer, BlockColor);
                    fx.FloatingText(PlayerHead, L.F("Шипы {0}", playerThorns), BlockColor);
                    break;
                case CardEffectType.SelfDamage:
                    GameManager.Instance.TakeDamage(effect.value);
                    log.Add(L.F("−{0} HP себе", effect.value));
                    fx.Flash(playerRenderer, DamageColor);
                    fx.FloatingText(PlayerHead, $"-{effect.value}", DamageColor);
                    break;
                case CardEffectType.NoBlockNextTurn:
                    blockLockedNextTurn = true;
                    log.Add(L.T("без защиты в след. ход"));
                    break;
                case CardEffectType.NoAttackNextTurn:
                    attackLockedNextTurn = true;
                    log.Add(L.T("без атаки в след. ход"));
                    break;
                case CardEffectType.Rage:
                    rageTurnsLeft += effect.value; // копии Ярости складываются
                    log.Add(L.F("ярость на {0} хода", effect.value));
                    fx.Flash(playerRenderer, DamageColor);
                    if (playerRenderer != null) fx.Shake(playerRenderer.transform, 0.12f, 0.35f);
                    fx.FloatingText(PlayerHead, L.T("ЯРОСТЬ!"), DamageColor);
                    break;
                case CardEffectType.MoltenGuard:
                    moltenBurnDamage = Mathf.Max(moltenBurnDamage, effect.value);
                    moltenBurnTurns = Mathf.Max(moltenBurnTurns, effect.turns);
                    log.Add(L.T("раскалённая броня"));
                    fx.Flash(playerRenderer, BurnColor);
                    fx.FloatingText(PlayerHead, L.T("Раскалена!"), BurnColor);
                    break;
                case CardEffectType.Momentum:
                    log.Add(L.F("+{0} Замах", effect.value));
                    GainMomentum(effect.value, L.T(card.cardName));
                    break;
                case CardEffectType.ConsumeMomentum:
                {
                    int taken = Mathf.Min(momentum, effect.value);
                    momentum -= taken;
                    log.Add(taken > 0 ? L.F("−{0} Замах", taken) : L.T("Замаха нет"));
                    break;
                }
                case CardEffectType.MomentumThreshold:
                    momentumThresholdCut += effect.value;
                    log.Add(L.F("порог удара {0}", MomentumThreshold));
                    fx.FloatingText(PlayerHead, L.F("Удар на {0}!", MomentumThreshold), MomentumColor);
                    break;
                case CardEffectType.PassiveStrikeBonus:
                    passiveStrikeBonus += effect.value;
                    log.Add(L.F("след. пасс. удар +{0}", effect.value));
                    fx.FloatingText(PlayerHead, L.F("Сила +{0}", passiveStrikeBonus), MomentumColor);
                    break;
                case CardEffectType.Combo:
                    comboAttack = true;
                    log.Add(L.T("связка"));
                    fx.FloatingText(PlayerHead, L.T("Связка!"), MomentumColor);
                    break;
                case CardEffectType.Retaliation:
                    retaliationMomentum += effect.value;
                    log.Add(L.F("возмездие +{0}", effect.value));
                    break;
                case CardEffectType.Execute:
                    if (TargetIsMinion && minionHP > 0 && minionHP * 100 <= minionMaxHP * effect.value)
                    {
                        HitTarget(minionHP, true, out _);
                        log.Add(L.T("казнь"));
                        break;
                    }
                    if (!TargetIsMinion && enemyHP > 0 && enemyHP * 100 <= enemy.maxHP * effect.value)
                    {
                        enemyHP = 0;
                        log.Add(L.T("казнь"));
                        fx.Flash(enemyRenderer, DamageColor);
                        fx.Shake(enemyVisual.transform, 0.3f, 0.4f);
                        fx.FloatingText(EnemyCenter, L.T("КАЗНЬ!"), DamageColor);
                    }
                    break;
                case CardEffectType.Cleanse:
                    if (poisonTurns > 0) { poisonTurns = 0; poisonDamage = 0; log.Add(L.T("яд снят")); }
                    else if (nextHandPenalty > 0) { nextHandPenalty = 0; log.Add(L.T("ослабление снято")); }
                    else log.Add(L.T("нечего снимать"));
                    fx.Flash(playerRenderer, HealColor);
                    fx.FloatingText(PlayerHead, L.T("Очищение"), HealColor);
                    break;
            }
        }
        LastEvent = $"«{L.T(card.cardName)}»: {string.Join(", ", log)}";
        if (UsesMomentum && momentum >= MomentumThreshold) passiveStrikeReady = true;
    }

    // Несколько ударов одной картой — по цифре на каждый, с паузой
    IEnumerator HitPopups(List<int> hits, Vector3 center, Transform shake)
    {
        foreach (var dmg in hits)
        {
            fx.FloatingText(center + new Vector3(Random.Range(-0.4f, 0.4f), 0, 0), dmg > 0 ? $"-{dmg}" : L.T("Блок!"), dmg > 0 ? DamageColor : BlockColor);
            if (dmg > 0 && shake != null) fx.Shake(shake, 0.1f, 0.15f);
            yield return new WaitForSeconds(0.28f);
        }
    }

    void GainMomentum(int amount, string source)
    {
        if (!UsesMomentum || amount <= 0) return;
        momentum += amount; // лишнее не пропадает: после молнии остаток переносится
        fx.FloatingText(PlayerHead, L.F("Замах {0}/{1}", Mathf.Min(momentum, MomentumMax), MomentumMax), MomentumColor);
        ui.Refresh();
    }

    // Шкала Замаха полна: лев сам бьёт молнией. Не тратит карту и ход, Замах сбрасывается.
    IEnumerator PassiveStrike()
    {
        if (strikeRunning || !combatActive) yield break;
        strikeRunning = true;
        enemyActing = true;
        ui.Refresh();
        yield return new WaitForSeconds(0.35f);
        int damage = (GameManager.Instance.selectedCharacter != null ? GameManager.Instance.selectedCharacter.passiveStrikeDamage : 6) + passiveStrikeBonus;
        passiveStrikeBonus = 0;
        momentum = Mathf.Max(0, momentum - MomentumThreshold); // 2 + 2 → молния, остаётся 1
        var lr = TargetRenderer; var lt = TargetTransform; var lc = TargetCenter;
        yield return fx.Lightning(lc, 0.35f);
        HitTarget(damage, true, out _);
        fx.Flash(lr, MomentumColor);
        if (lt != null) fx.Shake(lt, 0.25f, 0.35f);
        fx.FloatingText(lc, $"⚡ -{damage}", MomentumColor);
        LastEvent = L.F("Замах! Молния бьёт на {0}.", damage);
        yield return new WaitForSeconds(0.4f);
        enemyActing = false;
        strikeRunning = false;
        if (EnemyDown()) { Win(); yield break; }
        ui.Refresh();
        if (CardsLeftThisTurn == 0 || !hand.Exists(CanPlay)) StartCoroutine(AutoEndTurn());
    }

    public void EndTurn()
    {
        if (!CanEndTurn || endingTurn) return;
        endingTurn = true;
        StartCoroutine(EnemyTurnRoutine());
    }

    IEnumerator EnemyTurnRoutine()
    {
        enemyActing = true;
        enemyBlock = 0;
        if (rageTurnsLeft > 0) rageTurnsLeft--;
        ui.SweepHand();
        for (int i = hand.Count - 1; i >= 0; i--)
        {
            if (!hand[i].unplayable) continue;
            discardPile.Add(hand[i]);
            hand.RemoveAt(i);
        }
        var move = currentMove;

        if (enemyPoisonTurns > 0 && enemyPoisonDamage > 0)
        {
            int tick = enemyPoisonDamage;
            enemyHP = Mathf.Max(0, enemyHP - tick);
            enemyPoisonTurns--;
            if (enemyPoisonTurns == 0) enemyPoisonDamage = 0;
            LastEvent = L.F("Яд: {0} теряет {1} HP.", L.T(enemy.enemyName), tick);
            fx.Flash(enemyRenderer, PoisonColor, 0.5f);
            fx.Shake(enemyVisual.transform, 0.1f, 0.3f);
            fx.FloatingText(EnemyCenter, L.F("-{0} яд", tick), PoisonColor);
            ui.Refresh();
            yield return new WaitForSeconds(0.6f);
            if (EnemyDown())
            {
                enemyActing = false;
                Win();
                yield break;
            }
        }

        if (enemyBurnTurns > 0 && enemyBurnDamage > 0)
        {
            int tick = enemyBurnDamage;
            enemyHP = Mathf.Max(0, enemyHP - tick);
            enemyBurnTurns--;
            if (enemyBurnTurns == 0) enemyBurnDamage = 0;
            LastEvent = L.F("Горение: {0} теряет {1} HP.", L.T(enemy.enemyName), tick);
            fx.Flash(enemyRenderer, BurnColor, 0.5f);
            fx.Shake(enemyVisual.transform, 0.1f, 0.3f);
            fx.FloatingText(EnemyCenter, L.F("-{0} огонь", tick), BurnColor);
            ui.Refresh();
            yield return new WaitForSeconds(0.6f);
            if (EnemyDown())
            {
                enemyActing = false;
                Win();
                yield break;
            }
        }

        if (move == null)
        {
            LastEvent = L.F("{0} ничего не делает.", L.T(enemy.enemyName));
            ui.Refresh();
            yield return new WaitForSeconds(0.6f);
        }
        else
        {
            LastEvent = L.F("{0} использует «{1}»", L.T(enemy.enemyName), L.T(move.moveName));
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
                LastEvent = L.F("{0} ничего не делает.", L.T(enemy.enemyName));
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

                    LastEvent = L.F("{0} атакует на {1}.", L.T(enemy.enemyName), move.damage + damageBonus);
                    if (weakened > 0) LastEvent += L.F(" Ослабление сняло {0}.", weakened);
                    if (absorbed > 0) LastEvent += L.F(" Блок поглотил {0}.", absorbed);
                    LastEvent += damage > 0 ? L.F(" Ты получил {0} урона.", damage) : L.T(" Урон не прошёл.");

                    if (damage > 0)
                    {
                        fx.Flash(playerRenderer, DamageColor);
                        if (playerRenderer != null) fx.Shake(playerRenderer.transform);
                        fx.FloatingText(PlayerHead, $"-{damage}", DamageColor);
                        if (retaliationMomentum > 0) { pendingMomentum += retaliationMomentum; retaliationMomentum = 0; }
                        if (moltenBurnTurns > 0)
                        {
                            enemyBurnDamage = Mathf.Max(enemyBurnDamage, moltenBurnDamage);
                            enemyBurnTurns += moltenBurnTurns;
                            fx.Flash(enemyRenderer, BurnColor);
                            fx.FloatingText(EnemyCenter, L.T("Горит!"), BurnColor);
                        }
                    }
                    else
                    {
                        fx.Flash(playerRenderer, BlockColor);
                        fx.FloatingText(PlayerHead, L.T("Блок!"), BlockColor);
                    }
                    ui.Refresh();
                    yield return new WaitForSeconds(move.hits > 1 ? 0.25f : 0.5f);
                    if (GameManager.Instance.currentHP <= 0) break;
                }

                if (playerThorns > 0 && GameManager.Instance.currentHP > 0)
                {
                    enemyHP = Mathf.Max(0, enemyHP - playerThorns);
                    LastEvent = L.F("Шипы: {0} получает {1} урона.", L.T(enemy.enemyName), playerThorns);
                    fx.Flash(enemyRenderer, DamageColor);
                    fx.Shake(enemyVisual.transform);
                    fx.FloatingText(EnemyCenter, L.F("-{0} шипы", playerThorns), DamageColor);
                    ui.Refresh();
                    yield return new WaitForSeconds(0.5f);
                    if (EnemyDown())
                    {
                        enemyActing = false;
                        Win();
                        yield break;
                    }
                }
            }

            if (move.poisonTurns > 0)
            {
                poisonDamage = Mathf.Max(poisonDamage, move.poisonDamage);
                poisonTurns += move.poisonTurns;
                playerPoisonColor = move.poisonColor;
                string poisonName = string.IsNullOrEmpty(L.T(move.poisonLabel)) ? L.T("Яд") : L.T(move.poisonLabel);
                LastEvent = L.F("{0}: {1} урона в начале хода, {2} х.", poisonName, move.poisonDamage, move.poisonTurns);
                fx.Flash(playerRenderer, move.poisonColor);
                fx.FloatingText(PlayerHead, poisonName + "!", move.poisonColor);
                ui.Refresh();
                yield return new WaitForSeconds(0.5f);
            }

            if (move.corruptCards > 0)
            {
                int corrupted = 0;
                for (int n = 0; n < move.corruptCards; n++)
                {
                    var candidates = new List<int>();
                    for (int i = 0; i < hand.Count; i++) if (!hand[i].unplayable && !hand[i].retain) candidates.Add(i);
                    if (candidates.Count == 0) break;
                    int index = candidates[Random.Range(0, candidates.Count)];
                    var weak = CardPools.Instance != null ? CardPools.Instance.RandomWeak() : null;
                    if (weak == null) break;
                    var old = hand[index];
                    hand[index] = weak;
                    ui.CorruptCard(old, weak);
                    corrupted++;
                    yield return new WaitForSeconds(0.7f);
                }
                if (corrupted > 0)
                {
                    LastEvent = L.F("{0} портит твои карты: {1} карта заменена слабой до конца боя.", L.T(enemy.enemyName), corrupted);
                    fx.FloatingText(PlayerHead, corrupted > 1 ? L.F("Испорчено {0} карты!", corrupted) : L.T("Карта испорчена!"), DebuffColor);
                    ui.Refresh();
                    yield return new WaitForSeconds(0.4f);
                }
            }

            if (move.lockBlock)
            {
                blockLockedNextTurn = true;
                LastEvent = L.T("Клешни держат щит: в следующий ход нельзя защищаться.");
                fx.Flash(playerRenderer, DebuffColor);
                fx.FloatingText(PlayerHead, L.T("Без защиты!"), DebuffColor);
                ui.Refresh();
                yield return new WaitForSeconds(0.5f);
            }

            if (move.handReduce > 0)
            {
                nextHandPenalty += move.handReduce;
                LastEvent = L.F("Ты ослаблен: в следующий ход на {0} карту меньше.", move.handReduce);
                fx.Flash(playerRenderer, DebuffColor);
                fx.FloatingText(PlayerHead, L.T("Ослаблен!"), DebuffColor);
                ui.Refresh();
                yield return new WaitForSeconds(0.5f);
            }
        }

        if (move != null && move.submerge)
        {
            if (!enemyHidden)
            {
                enemyHidden = true;
                yield return fx.Lunge(enemyVisual.transform, Vector3.down * 0.5f, 0.4f);
                if (enemy.hiddenSprite != null) enemyRenderer.sprite = enemy.hiddenSprite;
            }
            LastEvent = L.F("{0} скрывается из виду.", L.T(enemy.enemyName));
            ui.Refresh();
            yield return new WaitForSeconds(0.4f);
        }
        if (move != null)
        {
            if (move.forceNextMove >= 0) forcedNextMove = move.forceNextMove;
            if (move.nextDamageBonus > 0) pendingDamageBonus += move.nextDamageBonus;
        }

        if (MinionAlive)
        {
            yield return MinionAttack();
            if (GameManager.Instance.currentHP <= 0) { Lose(); yield break; }
        }

        if (enemyWeakTurns > 0) enemyWeakTurns--;

        enemyActing = false;
        if (GameManager.Instance.currentHP <= 0)
        {
            Lose();
            yield break;
        }
        ChooseNextMove();
        ui.Refresh();
        yield return new WaitForSeconds(0.6f); // пауза: тик яда пусть будет отдельно от удара врага
        StartPlayerTurn();
    }

    // Главный враг повержен? Если рядом жив малыш — он теряет половину здоровья и становится целью, бой продолжается.
    bool EnemyDown()
    {
        if (enemyHP > 0) return false;
        if (!MinionAlive) return true;
        PromoteMinion();
        return false;
    }

    void PromoteMinion()
    {
        minionPromoted = true;
        targetMinion = false;
        var old = enemyVisual;
        var oldRenderer = enemyRenderer;
        StartCoroutine(fx.FadeOut(oldRenderer, 0.6f));
        Destroy(old, 0.7f);
        fx.FloatingText(PlayerHead, L.F("{0} повержен!", L.T(enemy.enemyName)), DamageColor);

        // Малыш становится главным: копия данных с ослабленными ходами
        var copy = Instantiate(enemy);
        copy.name = enemy.name;
        copy.enemyName = enemy.enemyName;
        copy.maxHP = minionMaxHP;
        copy.moves = new List<EnemyMove>();
        foreach (var m in enemy.moves)
        {
            var w = new EnemyMove
            {
                moveName = m.moveName, icon = m.icon, hits = m.hits, description = m.description,
                damage = m.damage > 0 ? Mathf.Max(1, m.damage / 2) : 0,
                block = m.block > 0 ? Mathf.Max(1, m.block / 2) : 0,
                poisonDamage = m.poisonDamage > 0 ? Mathf.Max(1, m.poisonDamage / 2) : 0, poisonTurns = m.poisonTurns,
                poisonLabel = m.poisonLabel, poisonColor = m.poisonColor,
                handReduce = m.handReduce, corruptCards = m.corruptCards, lockBlock = m.lockBlock,
                submerge = false, forceNextMove = -1, nextDamageBonus = 0
            };
            copy.moves.Add(w);
        }
        copy.hiddenSprite = null;
        enemy = copy;
        enemyHP = Mathf.Max(1, Mathf.CeilToInt(minionHP / 2f));
        minionHP = 0;
        enemyVisual = minionVisual;
        enemyRenderer = minionRenderer;
        minionVisual = null; minionRenderer = null;
        enemyBlock = 0; enemyPoisonTurns = 0; enemyPoisonDamage = 0; enemyBurnTurns = 0; enemyBurnDamage = 0; enemyWeakTurns = 0; enemyWeakAmount = 0;
        enemyHidden = false; forcedNextMove = -1; pendingDamageBonus = 0; moveIndex = -1;
        fx.Flash(enemyRenderer, DamageColor, 0.5f);
        fx.FloatingText(EnemyCenter, L.T("Остался один!"), DamageColor);
        ChooseNextMove();
        ui.Refresh();
    }

    // Удар малыша после хода главного врага
    IEnumerator MinionAttack()
    {
        if (!MinionAlive) yield break;
        yield return new WaitForSeconds(0.3f);
        Vector3 toPlayer = (PlayerPos - minionVisual.transform.position).normalized * 1.0f;
        yield return fx.Lunge(minionVisual.transform, toPlayer, 0.25f);
        int weakened = Mathf.Min(EnemyWeakAmount, minionDamage);
        int attack = minionDamage - weakened;
        int absorbed = Mathf.Min(playerBlock, attack);
        int damage = attack - absorbed;
        playerBlock -= absorbed;
        GameManager.Instance.TakeDamage(damage);
        LastEvent = L.F("Малыш кусает на {0}.", minionDamage);
        if (damage > 0)
        {
            fx.Flash(playerRenderer, DamageColor);
            if (playerRenderer != null) fx.Shake(playerRenderer.transform, 0.1f, 0.2f);
            fx.FloatingText(PlayerHead, $"-{damage}", DamageColor);
        }
        else fx.FloatingText(PlayerHead, L.T("Блок!"), BlockColor);
        ui.Refresh();
        yield return new WaitForSeconds(0.4f);
    }

    void Win()
    {
        combatActive = false;
        enemyActing = true;
        GameManager.Instance.OnCombatWon();
        LastEvent = L.F("{0} повержен!", L.T(enemy.enemyName));
        ui.Refresh();
        StartCoroutine(WinRoutine());
    }

    IEnumerator WinRoutine()
    {
        yield return new WaitForSeconds(0.5f);
        fx.Shake(enemyVisual.transform, 0.2f, 0.4f);
        yield return fx.FadeOut(enemyRenderer, 0.7f);
        ui.HideEnemyFrame();
        yield return new WaitForSeconds(0.5f);
        enemyActing = false;

        bool tutorial = RoomManager.Instance != null && !RoomManager.Instance.MarkedRewardsUnlocked;
        if (tutorial)
        {
            var character = GameManager.Instance.selectedCharacter;
            int fight = RoomManager.Instance.CurrentRoomIndex - 1; // 0 — первый обучающий бой
            var fixedReward = character != null && fight >= 0 && fight < character.tutorialCardRewards.Count ? character.tutorialCardRewards[fight] : null;
            if (fixedReward != null) ShowFixedReward(fixedReward);
            else ShowUpgradeReward();
        }
        else ui.ShowRewards(PickRewards(), OnRewardChosen);
    }

    // Обучающая награда без выбора: герой просто получает заданную карту
    void ShowFixedReward(CardData card)
    {
        GameManager.Instance.playerDeck.Add(card);
        CardChoiceUI.Get().Show(L.T("Победа! Новая карта в колоде"), new List<CardData> { card }, _ => OnRewardChosen(null), allowSkip: false);
    }

    void ShowUpgradeReward()
    {
        var gm = GameManager.Instance;
        DeckPickerUI.Get().Show(
            L.T("Победа! Усиль одну карту"),
            gm.playerDeck,
            null,
            card =>
            {
                gm.UpgradeCard(card);
                var bonus = RoomManager.Instance != null ? RoomManager.Instance.TutorialBonusCard() : null;
                if (bonus != null)
                {
                    gm.playerDeck.Add(bonus);
                    CardChoiceUI.Get().Show(L.T("Ярость просыпается. Новая карта в колоде"), new List<CardData> { bonus }, _ => OnRewardChosen(null), allowSkip: false);
                }
                else OnRewardChosen(null);
            },
            ShowUpgradeReward,
            upgradePreview: true);
    }

    List<CardData> PickRewards()
    {
        var character = GameManager.Instance.selectedCharacter;
        var rewards = new List<CardData>();
        var basePool = character.rewardCards.FindAll(c => c != null && c.AvailableNow);
        Shuffle(basePool);
        if (basePool.Count > 0) rewards.Add(basePool[0]);
        var enemyPool = enemy.rewardCards.FindAll(c => c != null && c.AvailableNow);
        if (enemyPool.Count > 0) rewards.Add(enemyPool[Random.Range(0, enemyPool.Count)]);
        else if (basePool.Count > 1) rewards.Add(basePool[1]);
        return rewards;
    }

    void OnRewardChosen(CardData card)
    {
        if (card != null) GameManager.Instance.playerDeck.Add(card);
        EndCombat();
        RoomManager.Instance.LeaveRoom(); // после награды — сразу на перекрёсток
    }

    void EndCombat()
    {
        if (enemyVisual != null) Destroy(enemyVisual);
        if (minionVisual != null) Destroy(minionVisual);
        minionVisual = null; minionHP = 0;
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
