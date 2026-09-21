using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DeckPickerUI : MonoBehaviour
{
    static DeckPickerUI instance;

    RectTransform panel;
    Text titleText;
    ScrollRect scroll;
    RectTransform content;
    Button cancelButton;
    Action<CardData> onPicked;
    Action onCancel;

    static readonly Vector2 CardSize = new Vector2(230, 340);
    const int PerRow = 3;
    const float SpacingX = 260f;
    const float SpacingY = 365f;
    const float ViewportHeight = 520f;

    public static DeckPickerUI Get()
    {
        if (instance != null) return instance;
        var canvas = UIFactory.CreateCanvas("DeckPickerCanvas", 30);
        instance = canvas.gameObject.AddComponent<DeckPickerUI>();
        instance.Build(canvas.transform);
        return instance;
    }

    void Build(Transform canvas)
    {
        panel = UIFactory.CreateFullscreenPanel(canvas, "Panel", new Color(0, 0, 0, 0.82f));
        // Каменная рамка вокруг области карт
        var frame = UIFactory.CreatePanel(panel, "Frame", new Vector2(0.5f, 0.5f), new Vector2(0, -18), new Vector2(PerRow * SpacingX + 100, ViewportHeight + 40), UISkin.Get(k => k.panelMenu), Color.clear);
        frame.GetComponent<Image>().raycastTarget = false;

        var titlePlate = UIFactory.CreatePanel(panel, "TitlePlate", new Vector2(0.5f, 1), new Vector2(0, -12), new Vector2(620, 60), UISkin.Get(k => k.labelTitle), Color.clear);
        titlePlate.GetComponent<Image>().raycastTarget = false;
        titleText = UIFactory.CreateText(titlePlate, "Title", "", 22, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0, 1), new Vector2(540, 44));
        titleText.color = new Color(0.93f, 0.88f, 0.78f);
        titleText.font = UIFactory.TitleFont;
        UIFactory.AddShadow(titleText);

        var viewport = UIFactory.CreateRect(panel, "Viewport", new Vector2(0.5f, 0.5f), new Vector2(0, -18), new Vector2(PerRow * SpacingX + 40, ViewportHeight));
        viewport.gameObject.AddComponent<RectMask2D>();
        var viewportImage = viewport.gameObject.AddComponent<Image>();
        viewportImage.color = new Color(0, 0, 0, 0.01f);

        content = UIFactory.CreateRect(viewport, "Content", new Vector2(0.5f, 1f), Vector2.zero, new Vector2(PerRow * SpacingX, ViewportHeight));
        content.pivot = new Vector2(0.5f, 1f);

        scroll = panel.gameObject.AddComponent<ScrollRect>();
        scroll.viewport = viewport;
        scroll.content = content;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 40f;

        cancelButton = UIFactory.CreateButton(panel, "Cancel", L.T("Назад"), 20, new Vector2(0.5f, 0), new Vector2(0, 26), new Vector2(180, 48), new Color(0.3f, 0.3f, 0.3f));
        cancelButton.onClick.AddListener(() => { Hide(); onCancel?.Invoke(); });
        panel.gameObject.SetActive(false);
    }

    public void Show(string title, IList<CardData> cards, Func<CardData, bool> selectable, Action<CardData> picked, Action cancelled, bool upgradePreview = false, string closeLabel = null)
    {
        onPicked = picked;
        onCancel = cancelled;
        titleText.text = title;
        cancelButton.transform.Find("Label").GetComponent<Text>().text = closeLabel ?? L.T("Назад");
        foreach (Transform child in content) Destroy(child.gameObject);

        int rows = Mathf.Max(1, Mathf.CeilToInt(cards.Count / (float)PerRow));
        float contentHeight = Mathf.Max(ViewportHeight, rows * SpacingY + 44f);
        content.sizeDelta = new Vector2(PerRow * SpacingX, contentHeight);

        for (int i = 0; i < cards.Count; i++)
        {
            var card = cards[i];
            int row = i / PerRow;
            int inRow = Mathf.Min(PerRow, cards.Count - row * PerRow);
            int col = i % PerRow;
            float x = (col - (inRow - 1) / 2f) * SpacingX;
            float y = -(row * SpacingY + 34f);
            var button = CardView.Create(content, card, new Vector2(0.5f, 1f), new Vector2(x, y), CardSize);
            button.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 1f);
            var tipTrigger = button.GetComponent<TooltipTrigger>();
            if (tipTrigger != null) tipTrigger.preferLeft = col >= PerRow / 2; // у правых карт подсказка слева, чтобы не закрывать карту
            bool ok = selectable == null || selectable(card);
            if (picked != null) CardView.SetInteractable(button, ok);
            else button.interactable = false;
            if (upgradePreview && ok) CardView.AddUpgradePreview(button, card);
            if (ok && picked != null) button.onClick.AddListener(() => { Hide(); onPicked?.Invoke(card); });
        }

        content.anchoredPosition = Vector2.zero;
        panel.gameObject.SetActive(true);
    }

    public void Hide()
    {
        panel.gameObject.SetActive(false);
        if (Tooltip.Instance != null) Tooltip.Instance.Hide();
    }
}
