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
}
