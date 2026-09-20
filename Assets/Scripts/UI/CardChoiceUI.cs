using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CardChoiceUI : MonoBehaviour
{
    static CardChoiceUI instance;

    RectTransform panel;
    Text titleText;
    RectTransform cardsArea;
    Button skipButton;
    Action<CardData> onChosen;

    static readonly Vector2 CardSize = new Vector2(271, 400);
    const float Spacing = 310f;

    public static CardChoiceUI Get()
    {
        if (instance != null) return instance;
        var canvas = UIFactory.CreateCanvas("CardChoiceCanvas", 25);
        instance = canvas.gameObject.AddComponent<CardChoiceUI>();
        instance.Build(canvas.transform);
        return instance;
    }

    void Build(Transform canvas)
    {
        panel = UIFactory.CreateFullscreenPanel(canvas, "Panel", new Color(0, 0, 0, 0.75f));
        var titlePlate = UIFactory.CreatePanel(panel, "TitlePlate", new Vector2(0.5f, 0.5f), new Vector2(0, 250), new Vector2(720, 64), UISkin.Get(k => k.labelTitle), Color.clear);
        titlePlate.GetComponent<Image>().raycastTarget = false;
        titleText = UIFactory.CreateText(titlePlate, "Title", "", 28, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0, 1), new Vector2(620, 50));
        titleText.color = new Color(0.93f, 0.88f, 0.78f);
        titleText.font = UIFactory.TitleFont;
        UIFactory.AddShadow(titleText);
        cardsArea = UIFactory.CreateRect(panel, "Cards", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000, 420));
        skipButton = UIFactory.CreateButton(panel, "Skip", "Пропустить", 20, new Vector2(0.5f, 0.5f), new Vector2(0, -265), new Vector2(180, 48), new Color(0.3f, 0.3f, 0.3f));
        skipButton.onClick.AddListener(() => Choose(null));
        panel.gameObject.SetActive(false);
    }

    public void Show(string title, IList<CardData> cards, Action<CardData> chosen, bool allowSkip = true)
    {
        onChosen = chosen;
        titleText.text = title;
        foreach (Transform child in cardsArea) Destroy(child.gameObject);

        int count = cards.Count;
        for (int i = 0; i < count; i++)
        {
            var card = cards[i];
            float x = (i - (count - 1) / 2f) * Spacing;
            var button = CardView.Create(cardsArea, card, new Vector2(0.5f, 0.5f), new Vector2(x, 0), CardSize);
            button.onClick.AddListener(() => Choose(card));
        }
        skipButton.gameObject.SetActive(allowSkip);
        panel.gameObject.SetActive(true);
    }

    void Choose(CardData card)
    {
        panel.gameObject.SetActive(false);
        if (Tooltip.Instance != null) Tooltip.Instance.Hide();
        var cb = onChosen;
        onChosen = null;
        cb?.Invoke(card);
    }
}
