using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DeckPickerUI : MonoBehaviour
{
    static DeckPickerUI instance;

    RectTransform panel;
    Text titleText;
    RectTransform cardsArea;
    Button cancelButton;
    Action<CardData> onPicked;
    Action onCancel;

    static readonly Vector2 CardSize = new Vector2(153, 225);

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
        titleText = UIFactory.CreateText(panel, "Title", "", 32, TextAnchor.MiddleCenter, new Vector2(0.5f, 1), new Vector2(0, -40), new Vector2(1000, 44));
        cardsArea = UIFactory.CreateRect(panel, "Cards", new Vector2(0.5f, 0.5f), new Vector2(0, 10), new Vector2(1200, 500));
        cancelButton = UIFactory.CreateButton(panel, "Cancel", "Назад", 20, new Vector2(0.5f, 0), new Vector2(0, 30), new Vector2(180, 48), new Color(0.3f, 0.3f, 0.3f));
        cancelButton.onClick.AddListener(() => { Hide(); onCancel?.Invoke(); });
        panel.gameObject.SetActive(false);
    }

    public void Show(string title, IList<CardData> cards, Func<CardData, bool> selectable, Action<CardData> picked, Action cancelled, bool upgradePreview = false)
    {
        onPicked = picked;
        onCancel = cancelled;
        titleText.text = title;
        foreach (Transform child in cardsArea) Destroy(child.gameObject);

        const int perRow = 7;
        const float spacingX = 165f;
        const float spacingY = 245f;
        int rows = Mathf.CeilToInt(cards.Count / (float)perRow);
        for (int i = 0; i < cards.Count; i++)
        {
            var card = cards[i];
            int row = i / perRow;
            int inRow = Mathf.Min(perRow, cards.Count - row * perRow);
            int col = i % perRow;
            float x = (col - (inRow - 1) / 2f) * spacingX;
            float y = ((rows - 1) / 2f - row) * spacingY;
            var button = CardView.Create(cardsArea, card, new Vector2(0.5f, 0.5f), new Vector2(x, y), CardSize);
            bool ok = selectable == null || selectable(card);
            CardView.SetInteractable(button, ok);
            if (upgradePreview && ok) CardView.AddUpgradePreview(button, card);
            if (ok) button.onClick.AddListener(() => { Hide(); onPicked?.Invoke(card); });
        }
        panel.gameObject.SetActive(true);
    }

    public void Hide()
    {
        panel.gameObject.SetActive(false);
        if (Tooltip.Instance != null) Tooltip.Instance.Hide();
    }
}
