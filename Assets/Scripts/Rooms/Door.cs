using UnityEngine;

public class Door : MonoBehaviour
{
    public DoorType doorType;

    void Start()
    {
        ApplyVisual();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            RoomManager.Instance.EnterDoor(doorType);
        }
    }

    public void ApplyVisual()
    {
        var sr = GetComponent<SpriteRenderer>();
        if (sr == null) return;

        var visuals = RoomManager.Instance != null ? RoomManager.Instance.doorVisuals : null;
        var sprite = visuals != null ? visuals.SpriteFor(doorType) : null;
        sr.sprite = sprite != null ? sprite : PlaceholderSprites.Square(ColorFor(doorType));

        var box = GetComponent<BoxCollider2D>();
        if (box != null)
        {
            var bounds = sr.sprite.bounds;
            box.size = new Vector2(bounds.size.x * 0.4f, ThresholdHeight);
            box.offset = new Vector2(bounds.center.x, bounds.min.y + ThresholdHeight * 0.5f);
        }
    }

    const float ThresholdHeight = 0.6f;

    public Vector2 EntryPoint
    {
        get
        {
            var sr = GetComponent<SpriteRenderer>();
            float bottom = sr != null && sr.sprite != null ? sr.sprite.bounds.min.y : -0.5f;
            return (Vector2)transform.position + new Vector2(0, bottom + ThresholdHeight * 0.5f);
        }
    }

    public static Door Create(DoorType type, Vector3 position)
    {
        var go = new GameObject("Door_" + type);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 1;
        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        var door = go.AddComponent<Door>();
        door.doorType = type;
        go.transform.position = position;
        door.ApplyVisual();
        return door;
    }

    public static Color ColorFor(DoorType type)
    {
        switch (type)
        {
            case DoorType.Combat: return new Color(0.9f, 0.15f, 0.15f);
            case DoorType.Danger: return new Color(0.6f, 0.2f, 0.85f);
            case DoorType.Rest: return new Color(0.2f, 0.8f, 0.3f);
            case DoorType.Treasure: return new Color(0.95f, 0.85f, 0.2f);
            case DoorType.Event: return new Color(0.2f, 0.45f, 0.95f);
            case DoorType.Random: return new Color(0.12f, 0.12f, 0.12f);
            default: return Color.gray;
        }
    }
}
