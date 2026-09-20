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
}
