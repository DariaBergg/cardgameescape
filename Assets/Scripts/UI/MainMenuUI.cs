using System;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    RectTransform panel;

    public static MainMenuUI Show(Sprite title, Action onStart)
    {
        var canvas = UIFactory.CreateCanvas("MainMenuCanvas", 80);
        var menu = canvas.gameObject.AddComponent<MainMenuUI>();
        menu.Build(canvas.transform, title, onStart);
        return menu;
    }

    void Build(Transform canvas, Sprite title, Action onStart)
    {
        panel = UIFactory.CreateFullscreenPanel(canvas, "Panel", Color.black);
        var art = UIFactory.CreateFullscreenPanel(panel, "Title", Color.white);
        var img = art.GetComponent<Image>();
        img.sprite = title;
        img.preserveAspect = true;
        img.raycastTarget = false;

        var button = UIFactory.CreateButton(panel, "Start", "Начать путь", 30, new Vector2(0.5f, 0), new Vector2(0, 70), new Vector2(320, 76), new Color(0.35f, 0.12f, 0.1f, 0.95f));
        button.onClick.AddListener(() =>
        {
            Destroy(gameObject);
            onStart?.Invoke();
        });
    }
}
