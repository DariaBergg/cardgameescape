using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 4f;

    Rigidbody2D rb;
    Vector2? target;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        ApplyCharacter(GameManager.Instance != null ? GameManager.Instance.selectedCharacter : null);
    }

    // Подменяет спрайт героя (после выбора персонажа)
    public void ApplyCharacter(CharacterData character)
    {
        var sr = GetComponent<SpriteRenderer>();
        if (sr == null) return;

        if (character != null && character.sprite != null)
        {
            sr.sprite = character.sprite;
            sr.flipX = character.flipSprite;
            transform.localScale = Vector3.one;
        }
        else if (sr.sprite == null)
        {
            sr.sprite = PlaceholderSprites.Square(new Color(0.2f, 0.8f, 0.9f));
        }
        ApplyFeetCollider(GetComponent<BoxCollider2D>(), sr.sprite);
    }

    const float FeetHeight = 0.5f;

    public static void ApplyFeetCollider(BoxCollider2D box, Sprite sprite)
    {
        if (box == null || sprite == null) return;
        var bounds = sprite.bounds;
        box.size = new Vector2(Mathf.Min(bounds.size.x * 0.5f, 1f), FeetHeight);
        box.offset = new Vector2(bounds.center.x, bounds.min.y + FeetHeight * 0.5f);
    }

    Vector2 FeetOffset
    {
        get
        {
            var box = GetComponent<BoxCollider2D>();
            return box != null ? box.offset : Vector2.zero;
        }
    }

    void OnDisable()
    {
        target = null;
    }

    public void EnterCombatPose(Vector3 position, float scale)
    {
        target = null;
        rb.position = position;
        transform.position = position;
        transform.localScale = Vector3.one * scale;
    }

    public void ExitCombatPose(Vector3 position)
    {
        target = null;
        rb.position = position;
        transform.position = position;
        transform.localScale = Vector3.one;
    }

    void Update()
    {
        var mouse = Mouse.current;
        if (mouse == null || Camera.main == null) return;
        if (!mouse.leftButton.wasPressedThisFrame) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        Vector2 world = Camera.main.ScreenToWorldPoint(mouse.position.ReadValue());
        var door = DoorAt(world);
        if (door != null) target = door.EntryPoint - FeetOffset;
    }

    static Door DoorAt(Vector2 point)
    {
        foreach (var door in FindObjectsByType<Door>(FindObjectsSortMode.None))
        {
            var sr = door.GetComponent<SpriteRenderer>();
            if (sr == null || sr.sprite == null) continue;
            var b = sr.bounds;
            if (point.x >= b.min.x && point.x <= b.max.x && point.y >= b.min.y && point.y <= b.max.y) return door;
        }
        return null;
    }

    void FixedUpdate()
    {
        if (target == null) return;
        Vector2 to = target.Value - rb.position;
        float step = moveSpeed * Time.fixedDeltaTime;
        if (to.magnitude <= step)
        {
            rb.MovePosition(target.Value);
            target = null;
        }
        else
        {
            rb.MovePosition(rb.position + to.normalized * step);
        }
    }
}
