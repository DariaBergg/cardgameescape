using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class RoomManager : MonoBehaviour
{
    public static RoomManager Instance { get; private set; }

    public DoorVisuals doorVisuals;
    public SpriteRenderer background;
    public Sprite startBackground;
    public List<Sprite> combatBackgrounds = new List<Sprite>();
    public List<RestRoomVariant> restVariants = new List<RestRoomVariant>();
    [Tooltip("События за синей дверью (пока используют те же варианты, что и отдых)")]
    public List<RestRoomVariant> eventVariants = new List<RestRoomVariant>();

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
    public Vector3 merchantPlayerPosition = new Vector3(-3f, -3f, 0);

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

    const string HubName = "Перекрёсток";

    static readonly DoorType[] tutorialDoors = { DoorType.Combat };
    static readonly DoorType[] earlyDoors = { DoorType.Combat, DoorType.Combat, DoorType.Rest, DoorType.Treasure, DoorType.Event };
    static readonly DoorType[] fullDoors =
    {
        DoorType.Combat, DoorType.Combat, DoorType.Combat,
        DoorType.Danger, DoorType.Rest, DoorType.Treasure, DoorType.Event, DoorType.Random
    };

    int NextRoomIndex => GameManager.Instance.roomsVisited + 1;
    int CurrentRoomIndex => GameManager.Instance.roomsVisited;
    int TutorialRooms => tutorialSequence.Count;
    public bool MarkedRewardsUnlocked => CurrentRoomIndex > upgradeRewardRooms;

    class RoomPlan
    {
        public string title;
        public Sprite background;
        public Action start;
    }

    readonly List<GameObject> doors = new List<GameObject>();
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

        SpawnDoors();
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
                hud.Notify($"Пришло время расплаты: −{o.hpCost} HP ({o.source})", 4f);
                break;
            case ObligationType.PledgeFights:
                hud.Notify(success ? $"Залог выполнен: карта остаётся у тебя ({o.source})" : $"Залог провален: карта ослабла ({o.source})", 4f);
                break;
            case ObligationType.PledgeNoHeal:
                hud.Notify(success ? $"Залог выполнен: карта остаётся у тебя ({o.source})" : $"Залог нарушен лечением: карта ослабла ({o.source})", 4f);
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
        GameManager.Instance.roomsVisited++;
        GameManager.Instance.OnRoomEntered();
        var plan = PlanRoom(type);

        yield return ScreenFader.Get().FadeTo(1f, fadeDuration);
        SetBackground(plan.background);
        hud.SetRoom($"Комната {GameManager.Instance.roomsVisited}: {plan.title}");
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
                    ? tutorialSequence[CurrentRoomIndex - 1]
                    : Pick(CombatPool());
                return new RoomPlan { title = "Бой", background = enemy.arena != null ? enemy.arena : PickOrNull(combatBackgrounds), start = () => CombatManager.Instance.StartCombat(enemy) };
            }
            case DoorType.Danger:
            {
                var enemy = RollDangerEncounter(out bool isElite);
                return new RoomPlan
                {
                    title = "Опасная комната",
                    background = enemy.arena != null ? enemy.arena : PickOrNull(combatBackgrounds),
                    start = () =>
                    {
                        if (isElite) hud.Notify($"{enemy.enemyName} почуял тебя!", 2.5f);
                        CombatManager.Instance.StartCombat(enemy);
                    }
                };
            }
            case DoorType.Rest:
            {
                var variant = PickOrNull(restVariants);
                return new RoomPlan
                {
                    title = variant != null ? variant.title : "Отдых",
                    background = variant != null ? variant.background : null,
                    start = () => ResolveRest(variant)
                };
            }
            case DoorType.Treasure:
            {
                var merchant = PickYellowRoomMerchant();
                if (merchant == null)
                    return new RoomPlan { title = "Сокровищница", background = treasureBackground, start = StartTreasureRoom };
                return new RoomPlan
                {
                    title = merchant.displayName,
                    background = merchant.background != null ? merchant.background : treasureBackground,
                    start = () => StartMerchantRoom(merchant)
                };
            }
            default:
            {
                var variant = PickOrNull(eventVariants);
                return new RoomPlan
                {
                    title = variant != null ? $"Событие — {variant.title}" : "Событие",
                    background = variant != null ? variant.background : null,
                    start = () =>
                    {
                        if (variant == null) { hud.Notify("Случайное событие (пока пусто)"); OnRoomCleared(); }
                        else ResolveRest(variant);
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
            hud.Notify("Ты отдохнул: +10 HP");
            OnRoomCleared();
            return;
        }

        switch (variant.kind)
        {
            case RestRoomKind.Campfire:
                ShowCampfire(variant);
                break;
            default:
                gm.Heal(variant.healAmount);
                hud.Notify($"{variant.description}  +{variant.healAmount} HP", 4f);
                OnRoomCleared();
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
                label = "Отдохнуть",
                description = $"Восстановить {variant.healAmount} HP",
                action = () =>
                {
                    ui.Hide();
                    gm.Heal(variant.healAmount);
                    hud.Notify($"Ты отдохнул у костра: +{variant.healAmount} HP");
                    OnRoomCleared();
                }
            },
            new RestRoomUI.Option
            {
                label = "Точить когти",
                description = "Улучшить одну карту из колоды",
                action = () =>
                {
                    ui.Hide();
                    DeckPickerUI.Get().Show(
                        "Выбери карту для улучшения",
                        gm.playerDeck,
                        null,
                        card =>
                        {
                            var upgraded = gm.UpgradeCard(card);
                            hud.Notify($"«{upgraded.cardName}»: {upgraded.EffectsSummary}", 4f);
                            OnRoomCleared();
                        },
                        () => ShowCampfire(variant),
                        upgradePreview: true);
                }
            }
        };
        ui.Show(variant.title, variant.description, options);
    }

    TreasureChest chest;
    GameObject merchantVisual;
    readonly Dictionary<string, int> forcedMerchants = new Dictionary<string, int>();

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

        if (Random.Range(0, 100) < chestChance) return null;
        return merchants[Random.Range(0, merchants.Count)];
    }

    void StartMerchantRoom(MerchantData merchant)
    {
        if (merchantVisual != null) Destroy(merchantVisual);
        merchantVisual = new GameObject("Merchant_" + merchant.Id);
        var sr = merchantVisual.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 1;
        if (merchant.sprite != null)
        {
            sr.sprite = merchant.sprite;
        }
        else
        {
            sr.sprite = PlaceholderSprites.Square(merchant.placeholderColor);
            merchantVisual.transform.localScale = new Vector3(1.6f, merchant.spriteHeight, 1f);
        }
        merchantVisual.transform.position = merchant.position;

        PlacePlayer(merchantPlayerPosition);
        player.enabled = false;
        MerchantVisit.Start(merchant, OnRoomCleared);
    }

    void StartTreasureRoom()
    {
        if (chest != null) Destroy(chest.gameObject);
        player.enabled = true;
        chest = TreasureChest.Spawn(chestClosed, chestOpen, chestPosition, OnChestOpened);
    }

    void OnChestOpened()
    {
        var pool = new List<CardData>(treasureCards);
        Shuffle(pool);
        if (pool.Count > treasureChoices) pool.RemoveRange(treasureChoices, pool.Count - treasureChoices);
        CardChoiceUI.Get().Show("Сундук! Выбери карту", pool, card =>
        {
            if (card != null)
            {
                GameManager.Instance.playerDeck.Add(card);
                hud.Notify($"«{card.cardName}» добавлена в колоду", 3f);
            }
            OnRoomCleared();
        });
    }

    public void OnRoomCleared()
    {
        player.enabled = true;
        hud.ShowExitButton(() => StartCoroutine(ReturnToHub()));
    }

    IEnumerator ReturnToHub()
    {
        transitioning = true;
        player.enabled = false;

        yield return ScreenFader.Get().FadeTo(1f, fadeDuration);
        SetBackground(startBackground);
        hud.SetRoom(HubName);
        player.ExitCombatPose(playerSpawn);
        if (chest != null) Destroy(chest.gameObject);
        if (merchantVisual != null) Destroy(merchantVisual);
        SpawnDoors();
        yield return ScreenFader.Get().FadeTo(0f, fadeDuration);

        transitioning = false;
        player.enabled = true;
    }

    void PlacePlayer(Vector3 position)
    {
        playerBody.position = position;
        player.transform.position = position;
    }

    List<EnemyData> CombatPool()
    {
        int room = CurrentRoomIndex;
        if (room <= earlyRooms && easyEnemies.Count > 0)
        {
            var mixed = new List<EnemyData>(easyEnemies);
            mixed.AddRange(enemies);
            return mixed;
        }
        return enemies;
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
        var candidates = new List<EnemyData>(elites);
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
        return Pick(dangerEnemies.Count > 0 ? dangerEnemies : enemies);
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
    static T PickOrNull<T>(List<T> list) where T : class => list.Count > 0 ? list[Random.Range(0, list.Count)] : null;
}
