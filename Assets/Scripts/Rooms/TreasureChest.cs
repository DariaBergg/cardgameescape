using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class TreasureChest : MonoBehaviour
{
    SpriteRenderer renderer2d;
    Sprite openSprite;
    Action onOpened;
    bool opened;

    public static TreasureChest Spawn(Sprite closed, Sprite open, Vector3 position, Action onOpened)
    {
        var go = new GameObject("TreasureChest");
        go.transform.position = position;
        var chest = go.AddComponent<TreasureChest>();
        chest.renderer2d = go.AddComponent<SpriteRenderer>();
        chest.renderer2d.sprite = closed;
        chest.renderer2d.sortingOrder = 1;
        chest.openSprite = open;
        chest.onOpened = onOpened;
        return chest;
    }

    void Update()
    {
        if (opened) return;
        var mouse = Mouse.current;
        if (mouse == null || Camera.main == null || !mouse.leftButton.wasPressedThisFrame) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        Vector2 world = Camera.main.ScreenToWorldPoint(mouse.position.ReadValue());
        var b = renderer2d.bounds;
        if (world.x < b.min.x || world.x > b.max.x || world.y < b.min.y || world.y > b.max.y) return;

        opened = true;
        StartCoroutine(OpenRoutine());
    }

    IEnumerator OpenRoutine()
    {
        Vector3 baseScale = transform.localScale;
        for (float t = 0; t < 0.15f; t += Time.deltaTime)
        {
            transform.localScale = baseScale * (1f + 0.08f * Mathf.Sin(t / 0.15f * Mathf.PI));
            yield return null;
        }
        transform.localScale = baseScale;
        renderer2d.sprite = openSprite;
        yield return new WaitForSeconds(0.6f);
        onOpened?.Invoke();
    }
}
