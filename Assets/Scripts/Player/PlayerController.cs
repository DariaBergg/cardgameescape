using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 4f;

    Rigidbody2D rb;
    Collider2D ownCollider;
    Vector2? target;
    Door hoveredDoor;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        ownCollider = GetComponent<Collider2D>();
    }

    void Start()
    {
        var sr = GetComponent<SpriteRenderer>();
        if (sr == null) return;

        var character = GameManager.Instance != null ? GameManager.Instance.selectedCharacter : null;
        if (character != null && character.sprite != null)
        {
            sr.sprite = character.sprite;
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
        SetHovered(null);
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

        Vector2 world = Camera.main.ScreenToWorldPoint(mouse.position.ReadValue());
        bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        SetHovered(overUI ? null : DoorAt(world));

        if (mouse.leftButton.wasPressedThisFrame && !overUI)
            target = hoveredDoor != null ? hoveredDoor.EntryPoint - FeetOffset : world - FeetOffset;
    }

    Door DoorAt(Vector2 point)
    {
        foreach (var hit in Physics2D.OverlapPointAll(point))
        {
            if (hit == ownCollider) continue;
            var door = hit.GetComponent<Door>();
            if (door != null) return door;
        }
        return null;
    }

    void SetHovered(Door door)
    {
        hoveredDoor = door;
    }

    void FixedUpdate()
    {
        Vector2 dir = KeyboardDirection();
        if (dir != Vector2.zero)
        {
            target = null;
            rb.MovePosition(rb.position + dir * moveSpeed * Time.fixedDeltaTime);
            return;
        }

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

    static Vector2 KeyboardDirection()
    {
        var kb = Keyboard.current;
        if (kb == null) return Vector2.zero;
        Vector2 dir = Vector2.zero;
        if (kb.wKey.isPressed || kb.upArrowKey.isPressed) dir.y += 1;
        if (kb.sKey.isPressed || kb.downArrowKey.isPressed) dir.y -= 1;
        if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) dir.x -= 1;
        if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) dir.x += 1;
        return dir.normalized;
    }
}
