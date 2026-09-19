using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public static class UIFactory
{
    static Font font;
    public static Font Font
    {
        get
        {
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return font;
        }
    }

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

    public static Button CreateButton(Transform parent, string name, string label, int fontSize, Vector2 anchor, Vector2 anchoredPos, Vector2 size, Color color)
    {
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
}
