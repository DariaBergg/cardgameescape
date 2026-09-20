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
        var titlePlate = UIFactory.CreatePanel(panel, "TitlePlate", new Vector2(0.5f, 0.5f), new Vector2(0, 252), new Vector2(920, 66), UISkin.Get(k => k.labelTitle), Color.clear);
        titlePlate.GetComponent<Image>().raycastTarget = false;
        titleText = UIFactory.CreateText(titlePlate, "Title", "", 24, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0, 1), new Vector2(820, 52));
        titleText.color = new Color(0.93f, 0.88f, 0.78f);
        titleText.font = UIFactory.TitleFont;
        UIFactory.AddShadow(titleText);
        cardsArea = UIFactory.CreateRect(panel, "Cards", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000, 420));
        skipButton = UIFactory.CreateButton(panel, "Skip", L.T("Пропустить"), 20, new Vector2(0.5f, 0.5f), new Vector2(0, -265), new Vector2(180, 48), new Color(0.3f, 0.3f, 0.3f));
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

    // Показать карты без выбора: кнопка «В колоду» — карты улетают вверх, потом колбэк
    public void Reveal(string title, IList<CardData> cards, Action onDone)
    {
        Show(title, cards, _ => { }, allowSkip: true);
        foreach (Transform child in cardsArea)
        {
            var b = child.GetComponent<Button>();
            if (b != null) b.onClick.RemoveAllListeners();
        }
        skipButton.onClick.RemoveAllListeners();
        skipButton.transform.Find("Label").GetComponent<Text>().text = L.T("В колоду");
        skipButton.onClick.AddListener(() => StartCoroutine(FlyAway(onDone)));
    }

    System.Collections.IEnumerator FlyAway(Action onDone)
    {
        skipButton.interactable = false;
        var views = new System.Collections.Generic.List<RectTransform>();
        foreach (Transform child in cardsArea) views.Add(child.GetComponent<RectTransform>());
        var starts = views.ConvertAll(v => v.anchoredPosition);
        var target = new Vector2(560, 380); // к кнопке «Колода» в правом верхнем углу
        for (float t = 0; t < 0.45f; t += Time.deltaTime)
        {
            float k = t / 0.45f; k = k * k;
            for (int i = 0; i < views.Count; i++)
            {
                if (views[i] == null) continue;
                views[i].anchoredPosition = Vector2.Lerp(starts[i], target, k);
                views[i].localScale = Vector3.one * Mathf.Lerp(1f, 0.2f, k);
            }
            yield return null;
        }
        // вернуть кнопке стандартный вид
        skipButton.interactable = true;
        skipButton.onClick.RemoveAllListeners();
        skipButton.onClick.AddListener(() => Choose(null));
        skipButton.transform.Find("Label").GetComponent<Text>().text = L.T("Пропустить");
        panel.gameObject.SetActive(false);
        if (Tooltip.Instance != null) Tooltip.Instance.Hide();
        onDone?.Invoke();
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
