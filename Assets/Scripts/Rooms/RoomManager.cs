using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomManager : MonoBehaviour
{
    public static RoomManager Instance { get; private set; }

    public DoorVisuals doorVisuals;

    [Tooltip("Обычные враги за красной дверью")]
    public List<EnemyData> enemies = new List<EnemyData>();
    [Tooltip("Опасные враги за фиолетовой дверью (без учёта элит)")]
    public List<EnemyData> dangerEnemies = new List<EnemyData>();
    [Tooltip("Элиты: могут выйти за фиолетовой дверью, метки на картах повышают их шанс")]
    public List<EnemyData> elites = new List<EnemyData>();
    [Range(0, 100)] public int eliteBaseChance = 25;
    public Vector3 playerSpawn = new Vector3(0, -3f, 0);
    public float doorsY = 3.5f;
    public float doorSpacing = 3f;

    static readonly DoorType[] doorWeights =
    {
        DoorType.Combat, DoorType.Combat, DoorType.Combat,
        DoorType.Danger, DoorType.Rest, DoorType.Treasure, DoorType.Event, DoorType.Random
    };

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
        hud.SetRoom("Стартовая комната");

        foreach (var door in FindObjectsByType<Door>(FindObjectsSortMode.None))
            doors.Add(door.gameObject);
    }

    public void EnterDoor(DoorType type)
    {
        if (transitioning) return;
        StartCoroutine(Transition(type));
    }

    IEnumerator Transition(DoorType type)
    {
        transitioning = true;
        player.enabled = false;
        ClearDoors();

        if (type == DoorType.Random) type = (DoorType)Random.Range(0, 5);
        GameManager.Instance.roomsVisited++;
        hud.SetRoom($"Комната {GameManager.Instance.roomsVisited}: {RoomName(type)}");

        playerBody.position = playerSpawn;
        player.transform.position = playerSpawn;

        yield return new WaitForSeconds(0.4f);
        transitioning = false;
        ResolveRoom(type);
    }

    void ResolveRoom(DoorType type)
    {
        switch (type)
        {
            case DoorType.Combat:
                CombatManager.Instance.StartCombat(Pick(enemies));
                break;
            case DoorType.Danger:
                CombatManager.Instance.StartCombat(RollDangerEncounter());
                break;
            case DoorType.Rest:
                GameManager.Instance.Heal(15);
                hud.Notify("Ты отдохнул: +15 HP");
                OnRoomCleared();
                break;
            case DoorType.Treasure:
                hud.Notify("Сокровищница (пока пусто)");
                OnRoomCleared();
                break;
            case DoorType.Event:
                hud.Notify("Случайное событие (пока пусто)");
                OnRoomCleared();
                break;
        }
    }

    public void OnRoomCleared()
    {
        player.enabled = true;
        SpawnDoors();
    }

    void SpawnDoors()
    {
        int count = Random.Range(2, 4);
        for (int i = 0; i < count; i++)
        {
            float x = (i - (count - 1) / 2f) * doorSpacing;
            var type = doorWeights[Random.Range(0, doorWeights.Length)];
            doors.Add(Door.Create(type, new Vector3(x, doorsY, 0)).gameObject);
        }
    }

    void ClearDoors()
    {
        foreach (var door in doors) if (door != null) Destroy(door);
        doors.Clear();
    }

    EnemyData RollDangerEncounter()
    {
        var candidates = new List<EnemyData>(elites);
        Shuffle(candidates);
        foreach (var elite in candidates)
        {
            int chance = Mathf.Min(100, eliteBaseChance + GameManager.Instance.EliteChance(elite));
            if (Random.Range(0, 100) < chance)
            {
                hud.Notify($"{elite.enemyName} почуял тебя!", 2.5f);
                return elite;
            }
        }
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

    static EnemyData Pick(List<EnemyData> list) => list[Random.Range(0, list.Count)];

    static string RoomName(DoorType type)
    {
        switch (type)
        {
            case DoorType.Combat: return "Бой";
            case DoorType.Danger: return "Опасная комната";
            case DoorType.Rest: return "Отдых";
            case DoorType.Treasure: return "Сокровище";
            case DoorType.Event: return "Событие";
            default: return type.ToString();
        }
    }
}
