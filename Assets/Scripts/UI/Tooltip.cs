using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class Tooltip : MonoBehaviour
{
    public static Tooltip Instance { get; private set; }

    RectTransform canvasRect;
    RectTransform panel;
    Text text;

    public static Tooltip Get()
    {
        if (Instance != null) return Instance;

        var canvas = UIFactory.CreateCanvas("TooltipCanvas", 100);
        canvas.GetComponent<GraphicRaycaster>().enabled = false;
        var tooltip = canvas.gameObject.AddComponent<Tooltip>();
        tooltip.Build(canvas);
        Instance = tooltip;
        return tooltip;
    }

    void Build(Canvas canvas)
    {
        canvasRect = canvas.GetComponent<RectTransform>();

        panel = UIFactory.CreateRect(canvas.transform, "Panel", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(340, 100));
        panel.pivot = new Vector2(0, 0);
        var bg = panel.gameObject.AddComponent<Image>();
        bool skinned = UISkin.Apply(bg, UISkin.Get(k => k.panelMenu), new Color(0.08f, 0.08f, 0.1f, 0.95f));
        bg.pixelsPerUnitMultiplier = 2.5f; // тонкая рамка для маленькой подсказки
        bg.raycastTarget = false;

        var layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = skinned ? new RectOffset(22, 22, 18, 20) : new RectOffset(14, 14, 10, 12);
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        var fitter = panel.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        text = UIFactory.CreateText(panel, "Text", "", 20, TextAnchor.UpperLeft, new Vector2(0, 1), Vector2.zero, new Vector2(320, 40));
        text.raycastTarget = false;
        text.supportRichText = true;
        var element = text.gameObject.AddComponent<LayoutElement>();
        element.preferredWidth = 320;

        panel.gameObject.SetActive(false);
    }

    public void Show(string content)
    {
        text.text = content;
        panel.gameObject.SetActive(true);
        LayoutRebuilder.ForceRebuildLayoutImmediate(panel);
        UpdatePosition();
    }

    public void Hide()
    {
        panel.gameObject.SetActive(false);
    }

    void Update()
    {
        if (panel.gameObject.activeSelf) UpdatePosition();
    }

    void UpdatePosition()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        Vector2 screen = mouse.position.ReadValue();
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, null, out Vector2 local);

        Vector2 pos = local + new Vector2(18, 18);
        Vector2 size = panel.rect.size;
        Rect bounds = canvasRect.rect;
        pos.x = Mathf.Min(pos.x, bounds.xMax - size.x - 8);
        pos.y = Mathf.Min(pos.y, bounds.yMax - size.y - 8);
        pos.x = Mathf.Max(pos.x, bounds.xMin + 8);
        pos.y = Mathf.Max(pos.y, bounds.yMin + 8);
        panel.anchoredPosition = pos;
    }
}

public class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public string content;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!string.IsNullOrEmpty(content)) Tooltip.Get().Show(content);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (Tooltip.Instance != null) Tooltip.Instance.Hide();
    }

    void OnDisable()
    {
        if (Tooltip.Instance != null) Tooltip.Instance.Hide();
    }
}
