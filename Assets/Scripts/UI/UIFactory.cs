using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public static class UIFactory
{
    static Font fallbackFont;
    static Font Fallback
    {
        get
        {
            if (fallbackFont == null) fallbackFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return fallbackFont;
        }
    }

    // Основной шрифт (из скина, иначе стандартный)
    public static Font Font => UISkin.Instance != null && UISkin.Instance.bodyFont != null ? UISkin.Instance.bodyFont : Fallback;
    // Шрифт заголовков, кнопок и названий карт
    public static Font TitleFont => UISkin.Instance != null && UISkin.Instance.titleFont != null ? UISkin.Instance.titleFont : Font;

    public static Canvas CreateCanvas(string name, int sortingOrder = 0)
    {
        var go = new GameObject(name);
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();

        if (Object.FindFirstObjectByType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>();
        }
        return canvas;
    }

    public static RectTransform CreateRect(Transform parent, string name, Vector2 anchor, Vector2 anchoredPos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = anchor;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;
        return rt;
    }

    public static RectTransform CreateFullscreenPanel(Transform parent, string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        var img = go.AddComponent<Image>();
        img.color = color;
        return rt;
    }

    public static Text CreateText(Transform parent, string name, string content, int fontSize, TextAnchor alignment, Vector2 anchor, Vector2 anchoredPos, Vector2 size)
    {
        var rt = CreateRect(parent, name, anchor, anchoredPos, size);
        var text = rt.gameObject.AddComponent<Text>();
        text.font = Font;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        text.text = content;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    public static Shadow AddShadow(Graphic graphic, float strength = 0.8f)
    {
        var shadow = graphic.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, strength);
        shadow.effectDistance = new Vector2(1, -2);
        return shadow;
    }

    // Панель на 9-slice спрайте из скина; без спрайта — цветной прямоугольник
    public static RectTransform CreatePanel(Transform parent, string name, Vector2 anchor, Vector2 anchoredPos, Vector2 size, Sprite sprite, Color fallback)
    {
        var rt = CreateRect(parent, name, anchor, anchoredPos, size);
        var img = rt.gameObject.AddComponent<Image>();
        UISkin.Apply(img, sprite, fallback);
        return rt;
    }

    // Обычная кнопка. Если в скине есть спрайт кнопки, а цвет не прозрачный (не карта) — рисуется на нём.
    public static Button CreateButton(Transform parent, string name, string label, int fontSize, Vector2 anchor, Vector2 anchoredPos, Vector2 size, Color color)
    {
        var skinSprite = color.a > 0.01f ? UISkin.Get(s => s.button) : null;
        if (skinSprite != null) return CreateSpriteButton(parent, name, label, fontSize, anchor, anchoredPos, size, skinSprite, color);

        var rt = CreateRect(parent, name, anchor, anchoredPos, size);
        var img = rt.gameObject.AddComponent<Image>();
        img.color = color;
        var button = rt.gameObject.AddComponent<Button>();
        button.targetGraphic = img;

        var colors = button.colors;
        colors.highlightedColor = color * 1.2f;
        colors.pressedColor = color * 0.8f;
        colors.disabledColor = new Color(color.r * 0.4f, color.g * 0.4f, color.b * 0.4f, 1f);
        button.colors = colors;

        var text = CreateText(rt, "Label", label, fontSize, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, size - new Vector2(16, 16));
        text.raycastTarget = false;
        return button;
    }

    // Кнопка на нарисованном 9-slice спрайте.
    public static Button CreateSpriteButton(Transform parent, string name, string label, int fontSize, Vector2 anchor, Vector2 anchoredPos, Vector2 size, Sprite sprite, Color fallback)
    {
        if (sprite == null)
        {
            var plainSprite = fallback.a > 0.01f ? UISkin.Get(s => s.button) : null;
            if (plainSprite == null) return CreateButton(parent, name, label, fontSize, anchor, anchoredPos, size, fallback);
            sprite = plainSprite;
        }

        var rt = CreateRect(parent, name, anchor, anchoredPos, size);
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.type = Image.Type.Sliced;
        var button = rt.gameObject.AddComponent<Button>();
        button.targetGraphic = img;

        var colors = button.colors;
        colors.highlightedColor = new Color(1.25f, 1.15f, 1.05f, 1f);
        colors.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 1f);
        button.colors = colors;

        var text = CreateText(rt, "Label", label, fontSize, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, size - new Vector2(Mathf.Min(40f, size.x * 0.2f), 12));
        text.font = label.Contains("\n") ? Font : TitleFont; // многострочные (с описанием) — основным шрифтом
        text.fontStyle = FontStyle.Normal;
        text.color = new Color(0.93f, 0.88f, 0.78f);
        text.raycastTarget = false;
        AddShadow(text);
        return button;
    }
}
