using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DoorVisuals", menuName = "CardGame/Door Visuals")]
public class DoorVisuals : ScriptableObject
{
    [System.Serializable]
    public class Entry
    {
        public DoorType type;
        public Sprite sprite;
    }

    public List<Entry> entries = new List<Entry>();

    public Sprite SpriteFor(DoorType type)
    {
        foreach (var entry in entries)
            if (entry.type == type) return entry.sprite;
        return null;
    }
}
