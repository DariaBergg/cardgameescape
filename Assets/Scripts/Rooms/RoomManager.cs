using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class RoomManager : MonoBehaviour
{
    public static RoomManager Instance { get; private set; }

    public DoorVisuals doorVisuals;
    public Sprite titleScreen;
    public SpriteRenderer background;
    public Sprite startBackground;
    public List<Sprite> combatBackgrounds = new List<Sprite>();
    public List<RestRoomVariant> restVariants = new List<RestRoomVariant>();
    [Tooltip("События за синей дверью")]
    public List<EventData> events = new List<EventData>();

    [Header("Сокровищница")]
    public Sprite treasureBackground;
    public Sprite chestClosed;
    public Sprite chestOpen;
    public Vector3 chestPosition = new Vector3(3f, -1.4f, 0);
    [Tooltip("Карты, которые можно найти в сундуке")]
    public List<CardData> treasureCards = new List<CardData>();
    public int treasureChoices = 3;

    [Header("Купцы (жёлтая дверь)")]
    public List<MerchantData> merchants = new List<MerchantData>();
    [Range(0, 100), Tooltip("Шанс, что за жёлтой дверью сундук, а не купец")]
    public int chestChance = 50;
    [Tooltip("Окно (в жёлтых комнатах), в котором гарантированно появится заказанный купец")]
    public int forcedMerchantWindow = 5;
    public Vector3 merchantPlayerPosition = new Vector3(-3.5f, -2.4f, 0);
    public Vector3 restPlayerPosition = new Vector3(-6.2f, -3.6f, 0);
    public float merchantPlayerScale = 1.3f;

    [Tooltip("Строгая последовательность первых боёв (мышь, слизень, крыса...)")]
    public List<EnemyData> tutorialSequence = new List<EnemyData>();
    [Tooltip("Лёгкие враги для ранних комнат после обучения")]
    public List<EnemyData> easyEnemies = new List<EnemyData>();
    [Tooltip("Обычные враги за красной дверью")]
    public List<EnemyData> enemies = new List<EnemyData>();
    [Tooltip("Опасные враги за фиолетовой дверью (без учёта элит)")]
    public List<EnemyData> dangerEnemies = new List<EnemyData>();
    [Tooltip("Элиты: могут выйти за фиолетовой дверью, метки на картах повышают их шанс")]
    public List<EnemyData> elites = new List<EnemyData>();
    [Range(0, 100)] public int eliteBaseChance = 25;
    [Tooltip("Комнаты 1..N после боя дают усиление карты вместо новой карты")]
    public int upgradeRewardRooms = 2;
    [Tooltip("Комнаты до N включительно — смешанные враги, без фиолетовой двери")]
    public int earlyRooms = 4;
    [Tooltip("Перед этой комнатой одна из дверей гарантированно зелёная")]
    public int guaranteedRestRoom = 4;
    public int doorsPerChoice = 2;
    public Vector3 playerSpawn = new Vector3(0, -3f, 0);
    public float doorsY = 3.5f;
    public float doorSpacing = 3f;
    public float fadeDuration = 0.3f;

    static string HubName => L.T("Перекрёсток");

    static readonly DoorType[] tutorialDoors = { DoorType.Combat };
    static readonly DoorType[] earlyDoors = { DoorType.Combat, DoorType.Combat, DoorType.Combat, DoorType.Rest, DoorType.Treasure, DoorType.Event };
    static readonly DoorType[] fullDoors =
    {
        DoorType.Combat, DoorType.Combat, DoorType.Combat,
        DoorType.Danger, DoorType.Rest, DoorType.Treasure, DoorType.Event, DoorType.Random
    };

    int NextRoomIndex => GameManager.Instance.roomsVisited + 1;
    public int CurrentRoomIndex => GameManager.Instance.roomsVisited;
    // Обучающие бои: у героя могут быть свои (лев — скелет вместо крысы)
    List<EnemyData> Tutorial
    {
        get
        {
            var c = GameManager.Instance.selectedCharacter;
            return c != null && c.tutorialSequence.Count > 0 ? c.tutorialSequence : tutorialSequence;
        }
    }
    int TutorialRooms => Tutorial.Count;
    public bool MarkedRewardsUnlocked => CurrentRoomIndex > upgradeRewardRooms;

    public CardData TutorialBonusCard()
    {
        if (CurrentRoomIndex != upgradeRewardRooms) return null;
        var character = GameManager.Instance.selectedCharacter;
        return character != null && character.tutorialBonusCard != null ? character.tutorialBonusCard : null;
    }

    class RoomPlan
    {
        public string title;
        public Sprite background;
        public Action start;
    }

    readonly List<GameObject> doors = new List<GameObject>();
    DoorType lastRoomType = DoorType.Combat;
    EnemyData lastEnemy;
    PlayerController player;
    Rigidbody2D playerBody;
    GameHUD hud;
    bool transitioning;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        player = FindFirstObjectByType<PlayerController>();
        playerBody = player.GetComponent<Rigidbody2D>();
        hud = GameHUD.Create();
        hud.SetRoom(HubName);
        SetBackground(startBackground);
        GameManager.Instance.OnObligationResolved += OnObligationResolved;

        if (titleScreen != null)
        {
            player.enabled = false;
            MainMenuUI.Show(titleScreen, () =>
            {
                var gm = GameManager.Instance;
                if (gm.characters.Count > 1)
                {
                    CharacterSelectUI.Show(titleScreen, gm.characters, character =>
                    {
                        gm.SelectCharacter(character);
                        player.ApplyCharacter(character);
                        player.enabled = true;
                        SpawnDoors();
                    });
                }
                else { player.enabled = true; SpawnDoors(); }
            }, () =>
            {
                // HUD построен до выбора языка — пересобираем его на новом языке
                Destroy(hud.gameObject);
                hud = GameHUD.Create();
                hud.SetRoom(HubName);
            });
        }
        else SpawnDoors();
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null) GameManager.Instance.OnObligationResolved -= OnObligationResolved;
    }

    void OnObligationResolved(Obligation o, bool success)
    {
        switch (o.type)
        {
            case ObligationType.Credit:
                Announce(L.F("Пришло время расплаты.\n{0} забирает {1} HP.", o.source, o.hpCost), true);
                break;
            case ObligationType.PledgeFights:
                Announce(success ? L.F("Залог выполнен.\nКарта остаётся у тебя ({0}).", o.source) : L.F("Залог провален.\nКарта ослабла ({0}).", o.source), !success);
                break;
            case ObligationType.PledgeNoHeal:
                Announce(success ? L.F("Залог выполнен.\nКарта остаётся у тебя ({0}).", o.source) : L.F("Залог нарушен лечением.\nКарта ослабла ({0}).", o.source), !success);
                break;
        }
    }

    public void EnterDoor(DoorType type)
    {
        if (transitioning) return;
        StartCoroutine(EnterRoom(type));
    }

    IEnumerator EnterRoom(DoorType type)
    {
        transitioning = true;
        player.enabled = false;
        ClearDoors();

        if (type == DoorType.Random) type = (DoorType)Random.Range(0, 5);
        lastRoomType = type;
        GameManager.Instance.roomsVisited++;
        GameManager.Instance.OnRoomEntered();
        var plan = PlanRoom(type);

        yield return ScreenFader.Get().FadeTo(1f, fadeDuration);
        SetBackground(plan.background);
        hud.SetRoom(L.F("Комната {0}: {1}", GameManager.Instance.roomsVisited, L.T(plan.title)));
        PlacePlayer(playerSpawn);
        transitioning = false;
        plan.start();
        yield return null;
        yield return ScreenFader.Get().FadeTo(0f, fadeDuration);
    }

    RoomPlan PlanRoom(DoorType type)
    {
        switch (type)
        {
            case DoorType.Combat:
            {
                var enemy = CurrentRoomIndex <= TutorialRooms
                    ? Tutorial[CurrentRoomIndex - 1]
                    : PickNotLast(CombatPool());
                lastEnemy = enemy;
                return new RoomPlan { title = L.T("Бой"), background = enemy.arena != null ? enemy.arena : PickOrNull(combatBackgrounds), start = () => CombatManager.Instance.StartCombat(enemy) };
            }
            case DoorType.Danger:
            {
                var enemy = RollDangerEncounter(out bool isElite);
                lastEnemy = enemy;
                return new RoomPlan
                {
                    title = L.T("Опасная комната"),
                    background = enemy.arena != null ? enemy.arena : PickOrNull(combatBackgrounds),
                    start = () =>
                    {
                        if (isElite) hud.Notify(L.F("{0} почуял тебя!", L.T(enemy.enemyName)), 2.5f);
                        else if (enemy.hasMinion) hud.Notify(L.F("{0} — и с ним малыш!", L.T(enemy.enemyName)), 2.5f);
                        CombatManager.Instance.StartCombat(enemy, withMinion: !isElite && enemy.hasMinion); // только помеченные враги приходят с малышом
                    }
                };
            }
            case DoorType.Rest:
            {
                var variant = PickOrNull(restVariants);
                return new RoomPlan
                {
                    title = variant != null ? L.T(variant.title) : L.T("Отдых"),
                    background = variant != null ? variant.background : null,
                    start = () =>
                    {
                        // Родник без окна — герой в центре; у остальных — сбоку или там, где указано в варианте
                        var pos = variant != null && variant.overridePlayerPosition ? variant.playerPosition : (variant != null && variant.kind == RestRoomKind.Spring ? new Vector3(0, -3.2f, 0) : restPlayerPosition);
                        float scale = variant != null && variant.overridePlayerPosition ? variant.playerScale : 1.15f;
                        player.EnterCombatPose(pos, scale);
                        player.enabled = false;
                        RestRoomUI.Get().SetPanelOffset(variant != null ? variant.panelOffset : (Vector2?)null);
                        ResolveRest(variant);
                    }
                };
            }
            case DoorType.Treasure:
            {
                var merchant = PickYellowRoomMerchant();
                if (merchant == null)
                    return new RoomPlan { title = L.T("Сокровищница"), background = treasureBackground, start = StartTreasureRoom };
                return new RoomPlan
                {
                    title = L.T(merchant.displayName),
                    background = merchant.background != null ? merchant.background : treasureBackground,
                    start = () => StartMerchantRoom(merchant)
                };
            }
            default:
            {
                var ev = PickNotLastEvent();
                return new RoomPlan
                {
                    title = ev != null ? L.T(ev.title) : L.T("Событие"),
                    background = ev != null ? ev.background : null,
                    start = () =>
                    {
                        if (ev == null) { hud.Notify(L.T("Случайное событие (пока пусто)")); OnRoomCleared(); return; }
                        player.enabled = false;
                        player.EnterCombatPose(ev.playerPosition, ev.playerScale);
                        if (ev.npcSprite != null)
                        {
                            var npc = SpawnRoomNpc("Event_" + ev.kind, ev.npcSprite, ev.npcPosition);
                            npc.flipX = ev.npcFlipX;
                            npc.transform.localScale = Vector3.one * ev.npcScale;
                            if (ev.npcMaskSize.x > 0 && ev.npcMaskSize.y > 0)
                            {
                                npc.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
                                var mask = new GameObject("Mask").AddComponent<SpriteMask>();
                                mask.sprite = PlaceholderSprites.Square(Color.white);
                                mask.transform.position = ev.npcMaskCenter;
                                mask.transform.localScale = new Vector3(ev.npcMaskSize.x, ev.npcMaskSize.y, 1f);
                                mask.transform.SetParent(npc.transform, true);
                            }
                        }
                        EventVisit.Start(ev, LeaveRoom);
                    }
                };
            }
        }
    }

    void ResolveRest(RestRoomVariant variant)
    {
        var gm = GameManager.Instance;
        if (variant == null)
        {
            gm.Heal(10);
            hud.Notify(L.T("Ты отдохнул: +10 HP"));
            AfterHeal();
            return;
        }

        switch (variant.kind)
        {
            case RestRoomKind.Campfire:
                ShowCampfire(variant);
                break;
            case RestRoomKind.Altar:
                ShowAltar(variant);
                break;
            case RestRoomKind.AbandonedCamp:
                ShowAbandonedCamp(variant);
                break;
            default:
                gm.Heal(variant.healAmount);
                hud.Notify($"{L.T(variant.description)}  +{variant.healAmount} HP", 4f);
                AfterHeal();
                break;
        }
    }

    void ShowCampfire(RestRoomVariant variant)
    {
        var gm = GameManager.Instance;
        var ui = RestRoomUI.Get();
        var options = new List<RestRoomUI.Option>
        {
            new RestRoomUI.Option
            {
                label = L.T("Отдохнуть"),
                description = L.F("Восстановить {0} HP", variant.healAmount),
                action = () =>
                {
                    ui.Hide();
                    gm.Heal(variant.healAmount);
                    hud.Notify(L.F("Ты отдохнул у костра: +{0} HP", variant.healAmount));
                    AfterHeal();
                }
            },
            new RestRoomUI.Option
            {
                label = L.T("Точить когти"),
                description = L.T("Улучшить одну карту из колоды"),
                action = () =>
                {
                    ui.Hide();
                    DeckPickerUI.Get().Show(
                        L.T("Выбери карту для улучшения"),
                        gm.playerDeck,
                        null,
                        card =>
                        {
                            var upgraded = gm.UpgradeCard(card);
                            hud.Notify($"«{L.T(upgraded.cardName)}»: {upgraded.EffectsSummary}", 4f);
                            OnRoomCleared();
                        },
                        () => ShowCampfire(variant),
                        upgradePreview: true);
                }
            }
        };
        ui.Show(L.T(variant.title), L.T(variant.description), options);
    }

    // Алтарь: лечение — или жертва ради избавления от карты
    void ShowAltar(RestRoomVariant variant)
    {
        var gm = GameManager.Instance;
        var ui = RestRoomUI.Get();
        const int sacrificeCost = 5;
        var removable = gm.playerDeck.FindAll(c => c != null && !c.permanent);
        var options = new List<RestRoomUI.Option>
        {
            new RestRoomUI.Option
            {
                label = L.T("Помолиться"),
                description = L.F("Восстановить {0} HP", variant.healAmount),
                action = () =>
                {
                    ui.Hide();
                    gm.Heal(variant.healAmount);
                    hud.Notify(L.F("Зелёный огонь теплеет. +{0} HP", variant.healAmount));
                    AfterHeal();
                }
            }
        };
        if (removable.Count > 1)
        {
            options.Add(new RestRoomUI.Option
            {
                label = L.T("Принести жертву"),
                description = L.F("−{0} HP: убрать одну карту из колоды навсегда", sacrificeCost),
                action = () =>
                {
                    ui.Hide();
                    DeckPickerUI.Get().Show(
                        L.T("Какую карту отдать алтарю?"),
                        removable,
                        null,
                        card =>
                        {
                            gm.LoseHPSafe(sacrificeCost);
                            gm.RemoveCard(card);
                            hud.Notify(L.F("Алтарь принимает «{0}». −{1} HP", L.T(card.cardName), sacrificeCost), 4f);
                            OnRoomCleared();
                        },
                        () => ShowAltar(variant));
                }
            });
        }
        ui.Show(L.T(variant.title), L.T(variant.description), options);
    }

    // Заброшенный лагерь: переночевать — или порыться в чужих вещах
    void ShowAbandonedCamp(RestRoomVariant variant)
    {
        var gm = GameManager.Instance;
        var ui = RestRoomUI.Get();
        var options = new List<RestRoomUI.Option>
        {
            new RestRoomUI.Option
            {
                label = L.T("Переночевать"),
                description = L.F("Восстановить {0} HP", variant.healAmount),
                action = () =>
                {
                    ui.Hide();
                    gm.Heal(variant.healAmount);
                    hud.Notify(L.F("Ночь проходит тихо. +{0} HP", variant.healAmount));
                    AfterHeal();
                }
            },
            new RestRoomUI.Option
            {
                label = L.T("Обыскать лагерь"),
                description = L.T("Без отдыха. Найти одну из двух оставленных карт"),
                action = () =>
                {
                    ui.Hide();
                    var pool = CardPools.Instance.RandomOfRarity(CardRarity.Common, 2);
                    CardChoiceUI.Get().Show(L.T("В вещах лагеря: выбери карту"), pool, card =>
                    {
                        if (card != null)
                        {
                            gm.playerDeck.Add(card);
                            hud.Notify(L.F("«{0}» добавлена в колоду", L.T(card.cardName)), 3f);
                        }
                        OnRoomCleared();
                    });
                }
            }
        };
        ui.Show(L.T(variant.title), L.T(variant.description), options);
    }

    EventData lastEvent;

    EventData PickNotLastEvent()
    {
        var mine = events.FindAll(e => e != null && e.AvailableNow);
        if (mine.Count == 0) return null;
        var pool = mine.Count > 1 && lastEvent != null ? mine.FindAll(e => e != lastEvent) : mine;
        lastEvent = pool[Random.Range(0, pool.Count)];
        return lastEvent;
    }

    public void StartAmbush(string message)
    {
        var enemy = PickNotLast(ForHero(enemies));
        lastEnemy = enemy;
        StartCoroutine(AmbushRoutine(enemy, message));
    }

    IEnumerator AmbushRoutine(EnemyData enemy, string message)
    {
        hud.Announce(message, true, 2.5f);
        yield return new WaitForSeconds(1.2f);
        yield return ScreenFader.Get().FadeTo(1f, fadeDuration);
        if (merchantVisual != null) Destroy(merchantVisual);
        SetBackground(enemy.arena != null ? enemy.arena : PickOrNull(combatBackgrounds));
        hud.SetRoom(L.F("Комната {0}: Засада", GameManager.Instance.roomsVisited));
        PlacePlayer(playerSpawn);
        CombatManager.Instance.StartCombat(enemy);
        yield return null;
        yield return ScreenFader.Get().FadeTo(0f, fadeDuration);
    }

    TreasureChest chest;
    GameObject merchantVisual;
    readonly Dictionary<string, int> forcedMerchants = new Dictionary<string, int>();
    bool lastYellowWasChest;

    public void ForceMerchant(MerchantKind kind, int withinYellowRooms = -1)
    {
        forcedMerchants[kind.ToString()] = withinYellowRooms > 0 ? withinYellowRooms : forcedMerchantWindow;
    }

    MerchantData FindMerchant(string id) => merchants.Find(m => m != null && m.Id == id);

    MerchantData PickYellowRoomMerchant()
    {
        if (merchants.Count == 0) return null;

        string due = null;
        foreach (var kv in forcedMerchants)
        {
            if (FindMerchant(kv.Key) == null) continue;
            if (kv.Value <= 1 || Random.Range(0, kv.Value) == 0) { due = kv.Key; break; }
        }
        if (due != null)
        {
            forcedMerchants.Remove(due);
            return FindMerchant(due);
        }

        var keys = new List<string>(forcedMerchants.Keys);
        foreach (var key in keys) forcedMerchants[key] = forcedMerchants[key] - 1;

        bool chest = Random.Range(0, 100) < chestChance && !lastYellowWasChest; // сундук — не два раза подряд
        lastYellowWasChest = chest;
        if (chest) return null;
        return merchants[Random.Range(0, merchants.Count)];
    }

    // Персонаж в комнате (купец, NPC события). Один за раз, убирается при возврате в хаб или засаде.
    SpriteRenderer SpawnRoomNpc(string name, Sprite sprite, Vector3 position)
    {
        if (merchantVisual != null) Destroy(merchantVisual);
        merchantVisual = new GameObject(name);
        var sr = merchantVisual.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 1;
        sr.sprite = sprite;
        merchantVisual.transform.position = position;
        return sr;
    }

    public void HideRoomNpc()
    {
        if (merchantVisual != null) Destroy(merchantVisual);
    }

    void StartMerchantRoom(MerchantData merchant)
    {
        var sr = SpawnRoomNpc("Merchant_" + merchant.Id, merchant.sprite, merchant.position);
        if (merchant.sprite == null)
        {
            sr.sprite = PlaceholderSprites.Square(merchant.placeholderColor);
            merchantVisual.transform.localScale = new Vector3(1.6f, merchant.spriteHeight, 1f);
        }

        player.EnterCombatPose(merchantPlayerPosition, merchantPlayerScale);
        player.enabled = false;
        MerchantVisit.Start(merchant, LeaveRoom);
    }

    void StartTreasureRoom()
    {
        if (chest != null) Destroy(chest.gameObject);
        player.enabled = true;
        chest = TreasureChest.Spawn(chestClosed, chestOpen, chestPosition, OnChestOpened);
    }

    void OnChestOpened()
    {
        // Сундук: общий список сокровищ, а если для героя там ничего нет — его собственные сильные карты
        var pool = treasureCards.FindAll(c => c != null && c.AvailableNow);
        var character = GameManager.Instance.selectedCharacter;
        if (pool.Count == 0 && character != null) pool = character.rewardCards.FindAll(c => c != null && c.AvailableNow);
        Shuffle(pool);
        if (pool.Count > treasureChoices) pool.RemoveRange(treasureChoices, pool.Count - treasureChoices);
        var bonus = GameManager.Instance.RollBonusCard();
        if (bonus != null) pool.Add(bonus); // особая карта героя — редкий гость в сундуке
        CardChoiceUI.Get().Show(L.T("Сундук! Выбери карту"), pool, card =>
        {
            if (card != null)
            {
                GameManager.Instance.playerDeck.Add(card);
                if (card == bonus) GameManager.Instance.OnBonusCardTaken();
                hud.Notify(L.F("«{0}» добавлена в колоду", L.T(card.cardName)), 3f);
            }
            OnRoomCleared();
        });
    }

    bool healedThisRoom;

    // После лечения: надпись остаётся в комнате, а особую карту (Ярость) предложим при выходе
    void AfterHeal()
    {
        healedThisRoom = true;
        OnRoomCleared();
    }

    public void OnRoomCleared()
    {
        player.enabled = true;
        hud.ShowExitButton(LeaveRoom);
    }

    // Перед уходом из комнаты: шанс на особую карту героя после лечения
    void OfferBonusThenLeave()
    {
        var bonus = healedThisRoom ? GameManager.Instance.RollBonusCard() : null;
        healedThisRoom = false;
        if (bonus == null) { StartCoroutine(ReturnToHub()); return; }
        CardChoiceUI.Get().Show(L.T("Силы возвращаются — и с ними ярость. Взять карту?"), new List<CardData> { bonus }, card =>
        {
            if (card != null) { GameManager.Instance.playerDeck.Add(card); GameManager.Instance.OnBonusCardTaken(); }
            StartCoroutine(ReturnToHub());
        });
    }

    // Сразу вернуться на перекрёсток без кнопки «Выйти» (после награды за бой)
    public void LeaveRoom()
    {
        hud.HideExitButton();
        if (!transitioning) OfferBonusThenLeave();
    }

    IEnumerator ReturnToHub()
    {
        transitioning = true;
        player.enabled = false;

        yield return ScreenFader.Get().FadeTo(1f, fadeDuration);
        hud.ClearNotify();
        SetBackground(startBackground);
        hud.SetRoom(HubName);
        player.ExitCombatPose(playerSpawn);
        if (chest != null) Destroy(chest.gameObject);
        if (merchantVisual != null) Destroy(merchantVisual);
        SpawnDoors();
        yield return ScreenFader.Get().FadeTo(0f, fadeDuration);

        transitioning = false;
        player.enabled = true;
        FlushAnnouncements();
    }

    // Объявления о долгах/залогах копим и показываем уже на перекрёстке
    readonly List<(string text, bool bad)> pendingAnnouncements = new List<(string, bool)>();
    void Announce(string text, bool bad) => pendingAnnouncements.Add((text, bad));
    void FlushAnnouncements()
    {
        if (pendingAnnouncements.Count == 0) return;
        var all = new List<string>();
        bool anyBad = false;
        foreach (var a in pendingAnnouncements) { all.Add(a.text); anyBad |= a.bad; }
        pendingAnnouncements.Clear();
        hud.Announce(string.Join("\n\n", all), anyBad, 5.5f);
    }

    void PlacePlayer(Vector3 position)
    {
        playerBody.position = position;
        player.transform.position = position;
    }

    // Враги, доступные текущему герою (у каждого героя могут быть свои уникальные)
    static List<EnemyData> ForHero(List<EnemyData> list) => list.FindAll(e => e != null && e.AvailableNow);

    List<EnemyData> CombatPool()
    {
        int room = CurrentRoomIndex;
        if (room <= earlyRooms && easyEnemies.Count > 0)
        {
            var mixed = ForHero(easyEnemies);
            mixed.AddRange(ForHero(enemies));
            return mixed;
        }
        return ForHero(enemies);
    }

    DoorType[] DoorPool()
    {
        int room = NextRoomIndex;
        if (room <= TutorialRooms) return tutorialDoors;
        if (room <= earlyRooms) return earlyDoors;
        return fullDoors;
    }

    void SpawnDoors()
    {
        ClearDoors();
        var pool = DoorPool();
        int count = doorsPerChoice;
        int restSlot = NextRoomIndex == guaranteedRestRoom ? Random.Range(0, count) : -1;
        bool treasureSpawned = false;
        bool restSpawned = restSlot >= 0;
        int combatSlot = -1;
        if (NeedsCombatDoor())
        {
            do combatSlot = Random.Range(0, count);
            while (combatSlot == restSlot && count > 1);
        }
        for (int i = 0; i < count; i++)
        {
            float x = (i - (count - 1) / 2f) * doorSpacing;
            var type = i == restSlot ? DoorType.Rest : i == combatSlot ? DoorType.Combat : pool[Random.Range(0, pool.Length)];
            if (restSlot >= 0 && i != restSlot && type == DoorType.Rest) type = DoorType.Combat;
            if (type == DoorType.Rest && (lastRoomType == DoorType.Rest || restSpawned)) type = DoorType.Combat; // две зелёные рядом — бессмысленно
            if (type == DoorType.Rest) restSpawned = true;
            if (type == DoorType.Treasure && (lastRoomType == DoorType.Treasure || treasureSpawned)) type = DoorType.Combat;
            if (type == DoorType.Treasure) treasureSpawned = true;
            doors.Add(Door.Create(type, new Vector3(x, doorsY, 0)).gameObject);
        }
    }

    bool NeedsCombatDoor()
    {
        foreach (var o in GameManager.Instance.obligations)
            if (o.type == ObligationType.PledgeFights && o.fightsWon < o.fightsRequired && o.roomsRemaining > 0) return true;
        return false;
    }

    void ClearDoors()
    {
        foreach (var door in doors) if (door != null) Destroy(door);
        doors.Clear();
    }

    void SetBackground(Sprite sprite)
    {
        if (background == null) return;
        background.sprite = sprite;
        background.enabled = sprite != null;
    }

    EnemyData RollDangerEncounter(out bool isElite)
    {
        var candidates = ForHero(elites);
        Shuffle(candidates);
        foreach (var elite in candidates)
        {
            int chance = Mathf.Min(100, eliteBaseChance + GameManager.Instance.EliteChance(elite));
            if (Random.Range(0, 100) < chance)
            {
                isElite = true;
                return elite;
            }
        }
        isElite = false;
        var danger = ForHero(dangerEnemies);
        return PickNotLast(danger.Count > 0 ? danger : ForHero(enemies));
    }

    static void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    static T Pick<T>(List<T> list) => list[Random.Range(0, list.Count)];

    EnemyData PickNotLast(List<EnemyData> list)
    {
        if (list.Count > 1 && lastEnemy != null)
        {
            var filtered = list.FindAll(e => e != lastEnemy);
            if (filtered.Count > 0) return Pick(filtered);
        }
        return Pick(list);
    }
    static T PickOrNull<T>(List<T> list) where T : class => list.Count > 0 ? list[Random.Range(0, list.Count)] : null;
}
