using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MerchantUI : MonoBehaviour
{
    public class Option
    {
        public string label;
        public string description;
        public Action action;
        public bool enabled = true;
    }

    static MerchantUI instance;

    RectTransform panel;
    Text nameText;
    Text speechText;
    RectTransform optionsArea;

    const float Width = 640f;

    public static MerchantUI Get()
    {
        if (instance != null) return instance;
        var canvas = UIFactory.CreateCanvas("MerchantCanvas", 20);
        instance = canvas.gameObject.AddComponent<MerchantUI>();
        instance.Build(canvas.transform);
        return instance;
    }

    void Build(Transform canvas)
    {
        panel = UIFactory.CreateRect(canvas, "Panel", new Vector2(0.5f, 0.5f), new Vector2(-150, 30), new Vector2(Width, 360));
        var bg = panel.gameObject.AddComponent<Image>();
        bool parchment = UISkin.Apply(bg, UISkin.Get(k => k.panelDialogue), new Color(0.05f, 0.05f, 0.08f, 0.9f));

        nameText = UIFactory.CreateText(panel, "Name", "", 28, TextAnchor.MiddleLeft, new Vector2(0, 1), new Vector2(48, -40), new Vector2(Width - 72, 36));
        nameText.color = parchment ? UISkin.Instance.parchmentTitle : new Color(1f, 0.85f, 0.55f);
        nameText.font = UIFactory.TitleFont;
        nameText.fontStyle = FontStyle.Normal;
        speechText = UIFactory.CreateText(panel, "Speech", "", 19, TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(48, -84), new Vector2(Width - 72, 110));
        speechText.color = parchment ? UISkin.Instance.parchmentText : new Color(1f, 1f, 1f, 0.9f);
        speechText.fontStyle = FontStyle.Italic;
        optionsArea = UIFactory.CreateRect(panel, "Options", new Vector2(0.5f, 0), new Vector2(0, 30), new Vector2(Width - 72, 160));
        panel.gameObject.SetActive(false);
    }

    public void Show(string merchantName, string speech, List<Option> options)
    {
        nameText.text = merchantName;
        speechText.text = speech;
        foreach (Transform child in optionsArea) Destroy(child.gameObject);

        const float buttonHeight = 58f;
        const float gap = 8f;
        float totalHeight = options.Count * buttonHeight + (options.Count - 1) * gap;
        for (int i = 0; i < options.Count; i++)
        {
            var option = options[i];
            float y = totalHeight / 2f - buttonHeight / 2f - i * (buttonHeight + gap);
            string text = string.IsNullOrEmpty(option.description)
                ? option.label
                : $"{option.label}\n<size=14><color=#d8d0c0>{option.description}</color></size>";
            var button = UIFactory.CreateButton(optionsArea, "Option" + i, text, 19, new Vector2(0.5f, 0.5f), new Vector2(0, y), new Vector2(Width - 72, buttonHeight), new Color(0.28f, 0.26f, 0.22f));
            button.interactable = option.enabled;
            var captured = option;
            button.onClick.AddListener(() => captured.action?.Invoke());
        }

        optionsArea.sizeDelta = new Vector2(Width - 72, totalHeight);
        panel.sizeDelta = new Vector2(Width, 214 + totalHeight + 44);
        panel.gameObject.SetActive(true);
    }

    public void Hide()
    {
        panel.gameObject.SetActive(false);
    }
}
