using UnityEngine;

public enum RestRoomKind
{
    Campfire,
    Spring,
    Altar,
    AbandonedCamp
}

[CreateAssetMenu(fileName = "NewRestRoom", menuName = "CardGame/Rest Room Variant")]
public class RestRoomVariant : ScriptableObject
{
    public string title;
    [TextArea] public string description;
    public RestRoomKind kind;
    public Sprite background;
    public int healAmount = 15;

    [Header("Позиция героя в комнате")]
    public bool overridePlayerPosition;
    public Vector3 playerPosition = new Vector3(-6.2f, -3.6f, 0);
    public float playerScale = 1.15f;
    [Tooltip("Сдвиг окна выбора (по умолчанию 0, 40)")]
    public Vector2 panelOffset = new Vector2(0, 40);
}
