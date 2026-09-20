using UnityEngine;

public enum EventKind
{
    Wanderer,
    Crack,
    Mirror,
    Pit
}

[CreateAssetMenu(fileName = "NewEvent", menuName = "CardGame/Event")]
public class EventData : ScriptableObject
{
    public EventKind kind;
    public string title;
    public Sprite background;
    [Tooltip("Чьё событие: пусто — для всех, иначе только этому герою")]
    public CharacterData owner;
    [Tooltip("Сдвиг окна диалога (по умолчанию −150, 30)")]
    public Vector2 panelOffset = new Vector2(-150, 30);

    public bool AvailableNow => GameManager.Instance == null || owner == null || owner == GameManager.Instance.selectedCharacter;

    [Header("Персонаж в комнате (если есть)")]
    public Sprite npcSprite;
    public Vector3 npcPosition = new Vector3(3.5f, -4.9f, 0);
    public bool npcFlipX;
    public float npcScale = 1f;
    [Tooltip("Показывать персонажа только внутри прямоугольника (например, в зеркале). Размер 0 = без маски")]
    public Vector2 npcMaskSize;
    public Vector3 npcMaskCenter;
    [Tooltip("Позиция героя, когда в комнате есть персонаж")]
    public Vector3 playerPosition = new Vector3(-3.5f, -2.4f, 0);
    public float playerScale = 1.3f;
}
