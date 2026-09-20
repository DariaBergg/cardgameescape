using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RestRoomUI : MonoBehaviour
{
    public class Option
    {
        public string label;
        public string description;
        public Action action;
    }

    static RestRoomUI instance;

    RectTransform panel;
    Text titleText;
    Text descriptionText;
    RectTransform buttonsArea;

    public static RestRoomUI Get()
    {
        if (instance != null) return instance;
        var canvas = UIFactory.CreateCanvas("RestCanvas", 20);
        instance = canvas.gameObject.AddComponent<RestRoomUI>();
        instance.Build(canvas.transform);
        return instance;
    }

    void Build(Transform canvas)
    {
        panel = UIFactory.CreateRect(canvas, "Panel", new Vector2(0.5f, 0.5f), new Vector2(0, 40), new Vector2(560, 300));
        var bg = panel.gameObject.AddComponent<Image>();
        bool parchment = UISkin.Apply(bg, UISkin.Get(k => k.panelDialogue), new Color(0.05f, 0.05f, 0.08f, 0.88f));

        titleText = UIFactory.CreateText(panel, "Title", "", 30, TextAnchor.MiddleCenter, new Vector2(0.5f, 1), new Vector2(0, -40), new Vector2(500, 40));
        titleText.font = UIFactory.TitleFont;
        titleText.fontStyle = FontStyle.Normal;
        titleText.color = parchment ? UISkin.Instance.parchmentTitle : Color.white;
        descriptionText = UIFactory.CreateText(panel, "Description", "", 18, TextAnchor.UpperCenter, new Vector2(0.5f, 1), new Vector2(0, -86), new Vector2(480, 60));
        descriptionText.color = parchment ? UISkin.Instance.parchmentText : new Color(1f, 1f, 1f, 0.8f);
        buttonsArea = UIFactory.CreateRect(panel, "Buttons", new Vector2(0.5f, 0), new Vector2(0, 34), new Vector2(480, 150));
        panel.gameObject.SetActive(false);
    }

    static readonly Vector2 DefaultPanelOffset = new Vector2(0, 40);
    public void SetPanelOffset(Vector2? offset) => panel.anchoredPosition = offset ?? DefaultPanelOffset;

    public void Show(string title, string description, List<Option> options)
    {
        titleText.text = title;
        descriptionText.text = description;
        foreach (Transform child in buttonsArea) Destroy(child.gameObject);

        const float buttonHeight = 64f;
        const float gap = 12f;
        float totalHeight = options.Count * buttonHeight + (options.Count - 1) * gap;
        for (int i = 0; i < options.Count; i++)
        {
            var option = options[i];
            float y = totalHeight / 2f - buttonHeight / 2f - i * (buttonHeight + gap);
            var button = UIFactory.CreateButton(buttonsArea, "Option" + i, $"{option.label}\n<size=15><color=#d8d0c0>{option.description}</color></size>", 20, new Vector2(0.5f, 0.5f), new Vector2(0, y), new Vector2(480, buttonHeight), new Color(0.25f, 0.3f, 0.25f));
            button.onClick.AddListener(() => option.action?.Invoke());
        }

        panel.sizeDelta = new Vector2(560, 172 + totalHeight + 48);
        panel.gameObject.SetActive(true);
    }

    public void Hide()
    {
        panel.gameObject.SetActive(false);
    }
}
