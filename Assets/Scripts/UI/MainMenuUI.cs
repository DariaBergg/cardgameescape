using System;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    RectTransform panel;
    Sprite titleSprite;
    Action onStart;
    Action onLanguageChanged;

    public static MainMenuUI Show(Sprite title, Action onStart, Action onLanguageChanged = null)
    {
        var canvas = UIFactory.CreateCanvas("MainMenuCanvas", 80);
        var menu = canvas.gameObject.AddComponent<MainMenuUI>();
        menu.titleSprite = title;
        menu.onStart = onStart;
        menu.onLanguageChanged = onLanguageChanged;
        menu.Build();
        return menu;
    }

    void Build()
    {
        if (panel != null) Destroy(panel.gameObject);
        panel = UIFactory.CreateFullscreenPanel(transform, "Panel", Color.black);
        var art = UIFactory.CreateFullscreenPanel(panel, "Title", Color.white);
        var img = art.GetComponent<Image>();
        img.sprite = titleSprite;
        img.preserveAspect = true;
        img.raycastTarget = false;

        var button = UIFactory.CreateSpriteButton(panel, "Start", L.T("Начать путь"), 32, new Vector2(0.5f, 0), new Vector2(0, 60), new Vector2(380, 90), UISkin.Get(k => k.mainButton), new Color(0.35f, 0.12f, 0.1f, 0.95f));
        button.onClick.AddListener(() =>
        {
            Destroy(gameObject);
            onStart?.Invoke();
        });

        // Переключатель языка в правом верхнем углу: показывает, на какой язык переключит
        string other = L.IsRussian ? "English" : "Русский";
        var lang = UIFactory.CreateButton(panel, "Language", other, 18, new Vector2(1, 1), new Vector2(-16, -14), new Vector2(150, 42), new Color(0.25f, 0.25f, 0.35f, 0.9f));
        lang.onClick.AddListener(() =>
        {
            L.Language = L.IsRussian ? "en" : "ru";
            onLanguageChanged?.Invoke();
            Build();
        });
    }
}
