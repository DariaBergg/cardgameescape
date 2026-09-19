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

    [Tooltip("Лёгкие враги для первых комнат (обучение)")]
    public List<EnemyData> easyEnemies = new List<EnemyData>();
    [Tooltip("Обычные враги за красной дверью")]
    public List<EnemyData> enemies = new List<EnemyData>();
    [Tooltip("Опасные враги за фиолетовой дверью (без учёта элит)")]
    public List<EnemyData> dangerEnemies = new List<EnemyData>();
    [Tooltip("Элиты: могут выйти за фиолетовой дверью, метки на картах повышают их шанс")]
    public List<EnemyData> elites = new List<EnemyData>();
    [Range(0, 100)] public int eliteBaseChance = 25;
    [Tooltip("Комнаты 1..N — только лёгкие бои, награда без меток")]
    public int tutorialRooms = 2;
    [Tooltip("Комнаты до N включительно — смешанные враги, без фиолетовой двери")]
    public int earlyRooms = 4;
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
    public bool MarkedRewardsUnlocked => CurrentRoomIndex > tutorialRooms;

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

        SpawnDoors();
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
                var enemy = CurrentRoomIndex <= tutorialRooms && easyEnemies.Count > 0
                    ? easyEnemies[(CurrentRoomIndex - 1) % easyEnemies.Count]
                    : Pick(CombatPool());
                return new RoomPlan { title = "Бой", background = PickOrNull(combatBackgrounds), start = () => CombatManager.Instance.StartCombat(enemy) };
            }
            case DoorType.Danger:
            {
                var enemy = RollDangerEncounter(out bool isElite);
                return new RoomPlan
                {
                    title = "Опасная комната",
                    background = PickOrNull(combatBackgrounds),
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
                return new RoomPlan { title = "Сокровище", background = null, start = () => { hud.Notify("Сокровищница (пока пусто)"); OnRoomCleared(); } };
            default:
                return new RoomPlan { title = "Событие", background = null, start = () => { hud.Notify("Случайное событие (пока пусто)"); OnRoomCleared(); } };
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
        if (room <= tutorialRooms && easyEnemies.Count > 0) return easyEnemies;
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
        if (room <= tutorialRooms) return tutorialDoors;
        if (room <= earlyRooms) return earlyDoors;
        return fullDoors;
    }

    void SpawnDoors()
    {
        ClearDoors();
        var pool = DoorPool();
        int count = doorsPerChoice;
        for (int i = 0; i < count; i++)
        {
            float x = (i - (count - 1) / 2f) * doorSpacing;
            var type = pool[Random.Range(0, pool.Length)];
            doors.Add(Door.Create(type, new Vector3(x, doorsY, 0)).gameObject);
        }
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
